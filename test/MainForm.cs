using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    // Use a smaller template/cell size for much faster matching
    public partial class MainForm : Form
    {
        private const int TEMPLATE_SIZE = 64;
        private System.Windows.Forms.Timer? manualTimer;

        private bool moveInProgress = false;

        private System.Windows.Forms.Timer? moveTimeoutTimer;


        // Cache for engine results to avoid recomputation
        private class EngineResultCache
        {
            public string PositionKey { get; set; } = "";
            public int Depth { get; set; }
            public string BestMove { get; set; } = "";
            public string Evaluation { get; set; } = "";
            public System.Collections.Generic.List<string> PVs { get; set; } = new();
            public System.Collections.Generic.List<string> Evaluations { get; set; } = new();
            public DateTime CachedAt { get; set; }
        }

        private EngineResultCache? lastEngineResult;

        // Track previous evaluation for blunder detection
        private double? previousEvaluation = null;

        // Board state tracking for blunder detection
        private string lastAnalyzedFEN = "";

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private ChessDroid.Services.ChessEngineService? engineService;
        private ScreenCaptureService screenCaptureService = new ScreenCaptureService();
        private BoardDetectionService boardDetectionService = new BoardDetectionService();
        private PieceRecognitionService pieceRecognitionService = new PieceRecognitionService();
        private PositionStateManager positionStateManager = new PositionStateManager();
        private EngineRestartManager engineRestartManager = new EngineRestartManager();
        private ConsoleOutputFormatter? consoleFormatter;

        private AppConfig? config;

        // Helper property to ensure config is never null
        private AppConfig Config => config ?? throw new InvalidOperationException("Configuration not loaded");

        private const int BOARD_SIZE = 8;
        // Removed fixed RECTIFIED_BOARD_SIZE - now using dynamic sizing based on detected board

        private async Task ExecuteMoveAsync(Mat boardMat, Rectangle boardRect, ChessBoard currentBoard, bool blackAtBottom)
        {
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            // 1) Update castling and en passant rights
            var swUpdatePos = System.Diagnostics.Stopwatch.StartNew();
            positionStateManager.UpdatePositionState(currentBoard);
            swUpdatePos.Stop();

            // 2) Generate complete FEN
            var swFen = System.Diagnostics.Stopwatch.StartNew();
            string completeFen = positionStateManager.GenerateCompleteFEN(currentBoard);
            swFen.Stop();

            // 3) Check cache first
            int depth = Config.EngineDepth;

            // MultiPV only depends on how many lines you want to show (1/2/3), not depth.
            int multiPVCount = config?.ShowThirdLine == true ? 3
                             : config?.ShowSecondLine == true ? 2
                             : 1;

            multiPVCount = Math.Min(multiPVCount, depth);

            bool useCache = lastEngineResult != null &&
                           lastEngineResult.PositionKey == completeFen &&
                           lastEngineResult.Depth == depth &&
                           (DateTime.Now - lastEngineResult.CachedAt).TotalSeconds < 2;

            var swEngine = System.Diagnostics.Stopwatch.StartNew();
            var result = useCache && lastEngineResult != null
                ? (lastEngineResult.BestMove, lastEngineResult.Evaluation, lastEngineResult.PVs, lastEngineResult.Evaluations)
                : await GetEngineResultWithDegradation(completeFen, depth, multiPVCount);
            swEngine.Stop();

            if (string.IsNullOrEmpty(result.Item1))
            {
                engineRestartManager.RecordFailure();
                Debug.WriteLine($"ERROR: Analysis failed for this position");
                Debug.WriteLine($"Failed FEN: {completeFen}");

                // Clear cache so next attempt tries again
                lastEngineResult = null;

                // Clear UI to show something happened
                this.Invoke((MethodInvoker)(() =>
                {
                    var (failures, restarts) = engineRestartManager.GetMetrics();
                    richTextBoxConsole.Clear();
                    richTextBoxConsole.AppendText($"Analysis failed - attempt {failures + 1}{Environment.NewLine}");
                    richTextBoxConsole.AppendText($"Position: {completeFen}{Environment.NewLine}");
                }));

                if (engineRestartManager.ShouldAttemptRestart())
                {
                    if (engineRestartManager.ShouldRestartApplication())
                    {
                        // Too many restarts - automatically restart application
                        var (failures, restarts) = engineRestartManager.GetMetrics();
                        this.Invoke((MethodInvoker)(() =>
                        {
                            labelStatus.Text = "Engine unstable - restarting application...";
                            richTextBoxConsole.AppendText($"Too many engine restarts ({restarts}). Automatically restarting application...{Environment.NewLine}");
                            buttonReset_Click(this, EventArgs.Empty);
                        }));
                        return;
                    }

                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Restarting engine..."));

                    bool restartSuccess = await engineRestartManager.RestartEngineAsync(engineService);
                    var (_, restartCount) = engineRestartManager.GetMetrics();

                    if (restartSuccess)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            labelStatus.Text = "Engine restarted - try again";
                            richTextBoxConsole.AppendText($"Engine restarted successfully (restart #{restartCount}). Please try again.{Environment.NewLine}");
                        }));
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Engine restart failed"));
                    }
                }
                else
                {
                    var (failures, _) = engineRestartManager.GetMetrics();
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Analysis failed ({failures}) - try again"));
                }

                // Save state and continue instead of stopping completely
                positionStateManager.SaveMoveState(currentBoard);
                return;
            }

            // Reset failure counter on success
            engineRestartManager.RecordSuccess();

            // Update status to Ready after successful analysis
            this.Invoke((MethodInvoker)(() => labelStatus.Text = "Ready"));

            // Track board changes for proper blunder detection
            UpdateBoardChangeTracking(completeFen, result.Item2);

            // 4) Update UI with results
            var swUI = System.Diagnostics.Stopwatch.StartNew();
            UpdateUIWithMoveResults(result.Item1, result.Item2, result.Item3, result.Item4, completeFen);
            swUI.Stop();

            // Overlay feature removed - analysis now shown in console only

            // 6) Save state for next move
            var swSave = System.Diagnostics.Stopwatch.StartNew();
            positionStateManager.SaveMoveState(currentBoard);
            swSave.Stop();

            swTotal.Stop();
            Debug.WriteLine($"[PERF] ExecuteMoveAsync timings: UpdatePos={swUpdatePos.ElapsedMilliseconds}ms, FEN={swFen.ElapsedMilliseconds}ms, Engine={swEngine.ElapsedMilliseconds}ms, UI={swUI.ElapsedMilliseconds}ms, Save={swSave.ElapsedMilliseconds}ms, TOTAL={swTotal.ElapsedMilliseconds}ms");
        }

        private async Task<(string, string, List<string>, List<string>)> GetEngineResultWithDegradation(string fen, int depth, int multiPVCount)
        {
            try
            {
                if (engineService == null)
                {
                    Debug.WriteLine("Engine service is null");
                    return ("", "", new List<string>(), new List<string>());
                }

                Debug.WriteLine($"GetEngineResultWithDegradation - FEN: {fen}, Depth: {depth}, MultiPV: {multiPVCount}");

                // Try with requested depth first
                var result = await engineService.GetBestMoveAsync(fen, depth, multiPVCount);

                Debug.WriteLine($"Engine result - BestMove: '{result.bestMove}', Eval: '{result.evaluation}', PVs: {result.pvs?.Count ?? 0}");

                // If failed, try degraded modes
                if (string.IsNullOrEmpty(result.bestMove) && depth > 5)
                {
                    Debug.WriteLine($"Analysis failed at depth {depth}, trying degraded mode...");
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Retrying with lower depth..."));

                    // Try with 75% depth
                    int degradedDepth = (int)(depth * 0.75);
                    result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(multiPVCount, degradedDepth));

                    // Still failed? Try with 50% depth
                    if (string.IsNullOrEmpty(result.bestMove) && degradedDepth > 3)
                    {
                        degradedDepth = Math.Max(3, depth / 2);
                        Debug.WriteLine($"Still failed, trying depth {degradedDepth}...");
                        result = await engineService.GetBestMoveAsync(fen, degradedDepth, Math.Min(multiPVCount, degradedDepth));
                    }

                    if (!string.IsNullOrEmpty(result.bestMove))
                    {
                        Debug.WriteLine($"Success with degraded depth {degradedDepth}");
                        this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Analysis at depth {degradedDepth}"));
                    }
                }

                // Cache the result if successful
                if (!string.IsNullOrEmpty(result.bestMove))
                {
                    lastEngineResult = new EngineResultCache
                    {
                        PositionKey = fen,
                        Depth = depth,
                        BestMove = result.bestMove,
                        Evaluation = result.evaluation,
                        PVs = result.pvs ?? new List<string>(),
                        Evaluations = result.evaluations ?? new List<string>(),
                        CachedAt = DateTime.Now
                    };
                }

                return (result.bestMove, result.evaluation, result.pvs ?? new List<string>(), result.evaluations ?? new List<string>());
            }
            catch (Exception ex)
            {
                // Check if it's a pipe closure or engine crash
                if (ex.Message.Contains("pipe") || ex.Message.Contains("closed") || ex is System.IO.IOException)
                {
                    Debug.WriteLine($"Engine pipe closed or crashed: {ex.Message}");
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = "Engine crashed, restarting..."));

                    // Restart engine immediately
                    try
                    {
                        if (engineService != null)
                            await engineService.RestartAsync();
                        // Don't reset the counter here - let the main flow handle it
                        this.Invoke((MethodInvoker)(() => labelStatus.Text = "Engine restarted"));

                        // Try one more time with the restarted engine
                        if (engineService != null)
                        {
                            var result = await engineService.GetBestMoveAsync(fen, Math.Max(3, depth / 2), Math.Max(3, multiPVCount / 2));
                            if (!string.IsNullOrEmpty(result.bestMove))
                            {
                                Debug.WriteLine("Analysis succeeded after engine restart");
                                this.Invoke((MethodInvoker)(() => labelStatus.Text = "Ready"));
                                return (result.bestMove, result.evaluation, result.pvs ?? new List<string>(), result.evaluations ?? new List<string>());
                            }
                        }
                    }
                    catch (Exception restartEx)
                    {
                        Debug.WriteLine($"Failed to restart engine: {restartEx.Message}");
                        this.Invoke((MethodInvoker)(() => labelStatus.Text = "Engine restart failed"));
                    }
                }
                else
                {
                    Debug.WriteLine($"Engine error: {ex.Message}");
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Engine error: {ex.Message}"));
                }

                return ("", "", new List<string>(), new List<string>());
            }
        }


        private void UpdateUIWithMoveResults(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, string completeFen)
        {
            if (consoleFormatter == null) return;

            consoleFormatter.Clear();

            // Check for blunders
            double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
            if (currentEval.HasValue && previousEvaluation.HasValue)
            {
                // Extract whose turn it is from FEN to determine who just moved
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                bool whiteJustMoved = !whiteToMove;

                var (isBlunder, blunderType, evalDrop, whiteBlundered) = MovesExplanation.DetectBlunder(
                    currentEval, previousEvaluation, whiteJustMoved);

                if (isBlunder)
                {
                    consoleFormatter.DisplayBlunderWarning(blunderType, evalDrop, whiteBlundered);
                }
            }

            // Best line
            string bestSanFull = ConvertPvToSan(pvs, 0, bestMove, completeFen);
            string formattedEval = consoleFormatter.FormatEvaluationWithWinPercentage(evaluation, completeFen);
            consoleFormatter.DisplayMoveLine(
                "Best line",
                bestSanFull,
                formattedEval,
                completeFen,
                pvs,
                bestMove,
                Color.MediumSeaGreen,
                Color.PaleGreen);

            // Second best
            if (config?.ShowSecondLine == true && pvs.Count >= 2)
            {
                var secondSan = ChessNotationService.ConvertFullPvToSan(pvs[1], completeFen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string secondMove = pvs[1].Split(' ')[0];
                string secondEval = evaluations.Count >= 2 ? evaluations[1] : "";
                string formattedSecondEval = consoleFormatter.FormatEvaluationWithWinPercentage(secondEval, completeFen);

                consoleFormatter.DisplayMoveLine(
                    "Second best",
                    secondSan,
                    formattedSecondEval,
                    completeFen,
                    pvs,
                    secondMove,
                    Color.Yellow,
                    Color.DarkGoldenrod);
            }

            // Third best
            if (config?.ShowThirdLine == true && pvs.Count >= 3)
            {
                var thirdSan = ChessNotationService.ConvertFullPvToSan(pvs[2], completeFen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string thirdMove = pvs[2].Split(' ')[0];
                string thirdEval = evaluations.Count >= 3 ? evaluations[2] : "";

                consoleFormatter.DisplayMoveLine(
                    "Third best",
                    thirdSan,
                    thirdEval,
                    completeFen,
                    pvs,
                    thirdMove,
                    Color.Red,
                    Color.DarkRed);
            }

            consoleFormatter.ResetFormatting();

            // Update previous evaluation for next move comparison
            previousEvaluation = currentEval;
        }

        private static string ConvertPvToSan(List<string> pvs, int index, string fallbackMove, string completeFen)
        {
            if (pvs != null && pvs.Count > index && !string.IsNullOrWhiteSpace(pvs[index]))
            {
                return ChessNotationService.ConvertFullPvToSan(pvs[index], completeFen, ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
            }
            return ChessNotationService.ConvertFullPvToSan(fallbackMove, completeFen, ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == 1)
                {
                    button1.PerformClick();
                }
                else if (hotkeyId == 2)
                {
                    buttonReset.PerformClick();
                }
            }
            base.WndProc(ref m);
        }

        public MainForm()
        {
            InitializeComponent();
            config = AppConfig.Load();
            engineService = new ChessDroid.Services.ChessEngineService(config);

            // Initialize game state via PositionStateManager
            positionStateManager.InitializeGameState(chkWhiteTurn.Checked);

            // Initialize console formatter
            consoleFormatter = new ConsoleOutputFormatter(
                richTextBoxConsole,
                config,
                MovesExplanation.GenerateMoveExplanation);

            // Apply theme from config
            ApplyTheme(config.Theme == "Dark");

            // Load explanation settings from config
            ExplanationFormatter.LoadFromConfig(config);

            LoadTemplatesAndMasks();
        }

        private void LoadTemplatesAndMasks()
        {
            // Use selected site from config
            string selectedSite = config?.SelectedSite ?? "Lichess";
            pieceRecognitionService.LoadTemplatesAndMasks(selectedSite, Config);
        }

        private ChessBoard ExtractBoardFromMat(Mat boardMat, bool blackAtBottom)
        {
            // Cleanup old cache entries periodically
            pieceRecognitionService.ClearOldCacheEntries();

            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            using (Mat grayBoard = new Mat())
            {
                CvInvoke.CvtColor(boardMat, grayBoard, ColorConversion.Bgr2Gray);

                int boardSize = grayBoard.Width;
                int cellSize = boardSize / BOARD_SIZE;
                char[,] board = new char[BOARD_SIZE, BOARD_SIZE];

                // Dynamic board sizing - accept any board size
                Debug.WriteLine($"Board detected at native size: {boardSize}x{boardSize} pixels (cell size: {cellSize}px)");

                System.Threading.Tasks.Parallel.For(0, BOARD_SIZE * BOARD_SIZE, idx =>
                {
                    int row = idx / BOARD_SIZE;
                    int col = idx % BOARD_SIZE;
                    var swCell = System.Diagnostics.Stopwatch.StartNew();
                    Rectangle roi = new Rectangle(col * cellSize, row * cellSize, cellSize, cellSize);
                    using (Mat cell = new Mat(grayBoard, roi))
                    {
                        if (blackAtBottom)
                        {
                            CvInvoke.Flip(cell, cell, FlipType.Vertical);
                            CvInvoke.Flip(cell, cell, FlipType.Horizontal);
                        }

                        (string detectedPiece, double confidence) = pieceRecognitionService.DetectPieceAndConfidence(cell, Config.MatchThreshold);

                        // Debug logging for each cell
                        string square = $"{(char)('a' + col)}{8 - row}";
                        if (!string.IsNullOrEmpty(detectedPiece))
                        {
                            Debug.WriteLine($"[{square}] Detected: {detectedPiece} (confidence: {confidence:F3})");
                        }
                        else if (confidence > 0.3) // Log close misses
                        {
                            Debug.WriteLine($"[{square}] Empty but had match with confidence: {confidence:F3}");
                        }

                        board[row, col] = string.IsNullOrEmpty(detectedPiece) || detectedPiece.Length == 0 ? '.' : detectedPiece[0];
                    }
                    swCell.Stop();
                    if (swCell.ElapsedMilliseconds > 10)
                        Debug.WriteLine($"[PERF] Cell ({row},{col}) extraction+match: {swCell.ElapsedMilliseconds}ms");
                });
                swTotal.Stop();
                Debug.WriteLine($"[PERF] ExtractBoardFromMat TOTAL: {swTotal.ElapsedMilliseconds}ms");
                return new ChessBoard(board);
            }
        }





        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.X)
            {
                button1.PerformClick();
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            TopMost = true;

            await InitializeEngineAsync();
            bool registered = RegisterHotKey(this.Handle, 1, MOD_ALT, (uint)Keys.X);
            if (!registered)
            {
                MessageBox.Show("Failed to register global hotkey.", "Hotkey Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            bool registered2 = RegisterHotKey(this.Handle, 2, MOD_ALT, (uint)Keys.K);
            if (!registered2)
            {
                MessageBox.Show("Failed to register ALT+K hotkey.", "Hotkey Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            manualTimer = new System.Windows.Forms.Timer();
            manualTimer.Interval = 1000;
            manualTimer.Tick += ManualTimer_Tick;

            // Timeout timer to prevent moveInProgress from getting stuck
            moveTimeoutTimer = new System.Windows.Forms.Timer();
            moveTimeoutTimer.Interval = Config.MoveTimeoutMs;
            moveTimeoutTimer.Tick += (s, ev) =>
            {
                if (moveInProgress)
                {
                    moveInProgress = false;
                    moveTimeoutTimer.Stop();
                }
            };
        }

        private async Task InitializeEngineAsync()
        {
            try
            {
                if (engineService == null)
                {
                    Debug.WriteLine("Cannot initialize engine - service is null");
                    return;
                }

                // Get selected engine from config or find first available engine
                string selectedEngine = "";

                if (!string.IsNullOrEmpty(config?.SelectedEngine))
                {
                    selectedEngine = config.SelectedEngine;
                }
                else
                {
                    // Find first available engine in Engines folder
                    string enginesFolder = Config.GetEnginesPath();
                    if (Directory.Exists(enginesFolder))
                    {
                        string[] engineFiles = Directory.GetFiles(enginesFolder, "*.exe");
                        if (engineFiles.Length > 0)
                        {
                            selectedEngine = Path.GetFileName(engineFiles[0]);
                            // Save the found engine to config
                            if (config != null)
                            {
                                config.SelectedEngine = selectedEngine;
                                config.Save();
                            }
                        }
                        else
                        {
                            selectedEngine = "stockfish.exe"; // Fallback
                        }
                    }
                    else
                    {
                        selectedEngine = "stockfish.exe"; // Fallback
                    }
                }

                string enginePath = Path.Combine(Config.GetEnginesPath(), selectedEngine);

                if (!File.Exists(enginePath))
                {
                    Debug.WriteLine($"Engine not found at: {enginePath}");
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = "Engine not found"));
                    return;
                }

                await engineService.InitializeAsync(enginePath);
                this.Invoke((MethodInvoker)(() => labelStatus.Text = "Ready"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting engine: {ex.Message}");
                this.Invoke((MethodInvoker)(() => labelStatus.Text = "Engine unavailable"));
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            try
            {
                engineService?.Dispose();
                // Reset all failure tracking flags
                engineRestartManager.Reset();
                engineService = new ChessDroid.Services.ChessEngineService(Config);
                positionStateManager.Reset();
                lastEngineResult = null;
                previousEvaluation = null; // Reset blunder detection
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error resetting application: {ex.Message}");
            }

            Application.Restart();
        }

        private void chkWhiteTurn_CheckedChanged(object? sender, EventArgs e)
        {
            // Update game state based on checkbox
            positionStateManager.SetWhiteToMove(chkWhiteTurn.Checked);

            // Update checkbox text based on state
            chkWhiteTurn.Text = chkWhiteTurn.Checked ? "White to move" : "Black to move";
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(Config, () =>
            {
                // Reload config and apply changes
                config = AppConfig.Load();

                // Apply explanation settings
                ExplanationFormatter.LoadFromConfig(config);

                // Apply theme changes
                this.Invoke((MethodInvoker)(() =>
                {
                    ApplyTheme(config.Theme == "Dark");
                }));

                // Restart engine with new timeout settings
                _ = Task.Run(async () =>
                {
                    if (engineService != null)
                        await engineService.RestartAsync();
                    this.Invoke((MethodInvoker)(() =>
                    {
                        labelStatus.Text = "Settings applied - engine restarted";
                    }));
                });
            });

            settingsForm.ShowDialog();
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            // If a move is already in progress, ignore this invocation
            if (moveInProgress)
            {
                MessageBox.Show("A move is already being processed. Please wait.");
                return;
            }

            // Show immediate feedback
            labelStatus.Text = "Analyzing...";

            // Mark that we are processing a move
            moveInProgress = true;
            moveTimeoutTimer?.Stop();
            moveTimeoutTimer?.Start();

            try
            {
                // 1) Capture and extract the board
                Mat? fullScreenMat = screenCaptureService.CaptureScreenAsMat();
                if (fullScreenMat == null)
                {
                    labelStatus.Text = "Screen capture failed";
                    return;
                }

                // Try to detect the board automatically
                var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullScreenMat);
                Mat? boardMat = detectedResult.boardMat;
                Rectangle boardRect = detectedResult.boardRect;

                if (boardMat == null)
                {
                    labelStatus.Text = "Board detection failed";
                    return;
                }

                bool blackAtBottom = !positionStateManager.IsWhiteToMove();
                if (blackAtBottom)
                {
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical);
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Horizontal);
                }

                ChessBoard currentBoard = ExtractBoardFromMat(boardMat, blackAtBottom);

                // Validate detected board is not empty
                bool boardIsEmpty = true;
                var arr = currentBoard.GetArray();
                for (int r = 0; r < 8 && boardIsEmpty; r++)
                    for (int c = 0; c < 8 && boardIsEmpty; c++)
                        if (arr[r, c] != '.')
                            boardIsEmpty = false;

                if (boardIsEmpty)
                {
                    labelStatus.Text = "No pieces detected. Please try again.";
                    moveInProgress = false;
                    return;
                }

                // Validate FEN
                string completeFen = positionStateManager.GenerateCompleteFEN(currentBoard);
                if (string.IsNullOrWhiteSpace(completeFen) || completeFen.Contains("..") || completeFen.StartsWith("/"))
                {
                    labelStatus.Text = "Invalid FEN detected. Please try again.";
                    moveInProgress = false;
                    return;
                }

                // Show visual board display for debugging (if enabled)
                if (Config.ShowDebugCells)
                {
                    string templatesPath = Config.GetTemplatesPath();
                    var boardVisualizer = new BoardVisualizer(currentBoard, templatesPath);
                    boardVisualizer.ShowDialog();
                }

                // 2) Execute move with engine
                await ExecuteMoveAsync(
                    boardMat,
                    boardRect,
                    currentBoard,
                    blackAtBottom
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in button1_Click_1: {ex.Message}");
                labelStatus.Text = $"Error: {ex.Message}";
                engineRestartManager.RecordFailure();
            }
            finally
            {
                // ALWAYS reset the flag so button can be clicked again
                moveTimeoutTimer?.Stop();
                moveInProgress = false;
            }
        }

        private void ManualTimer_Tick(object? sender, EventArgs e)
        {
            Mat? fullMat = screenCaptureService.CaptureScreenAsMat();
            if (fullMat == null) return;

            var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullMat);
            Mat? boardMat = detectedResult.boardMat;

            if (boardMat == null) return;

            bool blackAtBottom = !positionStateManager.IsWhiteToMove();
            if (blackAtBottom)
                CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical | FlipType.Horizontal);

            ChessBoard currentBoard = ExtractBoardFromMat(boardMat, blackAtBottom);

            if (positionStateManager.LastDetectedBoard != null && ChessRulesService.CountBoardDifferences(positionStateManager.LastDetectedBoard, currentBoard) >= 2)
            {
                manualTimer?.Stop();
                positionStateManager.SaveMoveState(currentBoard);
            }
        }

        // Track board changes and update evaluation history properly
        private void UpdateBoardChangeTracking(string currentFEN, string currentEvaluation)
        {
            try
            {
                // Extract just the position part of FEN (ignore move counters)
                string currentPosition = ChessNotationService.GetPositionFromFEN(currentFEN);
                string lastPosition = ChessNotationService.GetPositionFromFEN(lastAnalyzedFEN);

                // Check if board actually changed
                if (currentPosition != lastPosition)
                {
                    // Board changed! This is a new move
                    Debug.WriteLine($"Board changed detected: {lastPosition} -> {currentPosition}");

                    // Parse current evaluation for next comparison
                    double? currentEval = MovesExplanation.ParseEvaluation(currentEvaluation);

                    // Only update previousEvaluation if we had a previous analysis
                    // This ensures we're tracking consecutive moves properly
                    if (!string.IsNullOrEmpty(lastAnalyzedFEN) && currentEval.HasValue)
                    {
                        previousEvaluation = currentEval;
                    }
                    else if (currentEval.HasValue)
                    {
                        // First analysis - just set it without comparison
                        previousEvaluation = currentEval;
                    }

                    // Update last analyzed position
                    lastAnalyzedFEN = currentFEN;
                }
                else
                {
                    // Same position re-analyzed (deeper analysis or user clicked again)
                    // Don't update previousEvaluation - just update the FEN and eval
                    lastAnalyzedFEN = currentFEN;
                    Debug.WriteLine("Same position re-analyzed, keeping evaluation history intact");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateBoardChangeTracking: {ex.Message}");
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            // Suspend layout to prevent flickering and improve performance
            this.SuspendLayout();

            if (isDarkMode)
            {
                // Dark Mode Colors
                this.BackColor = Color.FromArgb(30, 30, 35);

                // Labels
                labelStatus.BackColor = Color.FromArgb(60, 60, 65);
                labelStatus.ForeColor = Color.White;

                // Buttons
                button1.BackColor = Color.FromArgb(45, 45, 48);
                button1.ForeColor = Color.Thistle;
                buttonReset.BackColor = Color.FromArgb(45, 45, 48);
                buttonReset.ForeColor = Color.LightCoral;
                buttonSettings.BackColor = Color.FromArgb(45, 45, 48);
                buttonSettings.ForeColor = Color.Orange;

                // Console
                richTextBoxConsole.BackColor = Color.FromArgb(30, 30, 35);
                richTextBoxConsole.ForeColor = Color.LightGray;

                // Checkbox
                chkWhiteTurn.BackColor = Color.FromArgb(30, 30, 35);
                chkWhiteTurn.ForeColor = Color.White;
            }
            else
            {
                // Light Mode Colors
                this.BackColor = Color.WhiteSmoke;

                // Labels
                labelStatus.BackColor = Color.Gainsboro;
                labelStatus.ForeColor = Color.Black;

                // Buttons
                button1.BackColor = Color.Lavender;
                button1.ForeColor = Color.DarkSlateBlue;
                buttonReset.BackColor = Color.MistyRose;
                buttonReset.ForeColor = Color.DarkRed;
                buttonSettings.BackColor = Color.LightYellow;
                buttonSettings.ForeColor = Color.DarkGoldenrod;

                // Console
                richTextBoxConsole.BackColor = Color.AliceBlue;
                richTextBoxConsole.ForeColor = Color.Black;

                // Checkbox
                chkWhiteTurn.BackColor = Color.WhiteSmoke;
                chkWhiteTurn.ForeColor = Color.Black;
            }

            // Resume layout to apply all changes at once
            this.ResumeLayout();
        }
    }
}