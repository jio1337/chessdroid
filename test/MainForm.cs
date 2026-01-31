using ChessDroid.Models;
using ChessDroid.Services;
using Emgu.CV;
using System.Linq;
using Emgu.CV.CvEnum;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChessDroid
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer? manualTimer;

        private volatile bool moveInProgress = false;
        private readonly object _moveInProgressLock = new object();

        private System.Windows.Forms.Timer? moveTimeoutTimer;

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;

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
        private EngineAnalysisStrategy? analysisStrategy;
        private MoveAnalysisOrchestrator? moveOrchestrator;
        private BlunderTracker blunderTracker = new BlunderTracker();
        private EnginePathResolver? enginePathResolver;
        private BoardMonitorService? boardMonitorService;
        private PolyglotBookService? openingBookService;

        private AppConfig? config;

        // Helper property to ensure config is never null
        private AppConfig Config => config ?? throw new InvalidOperationException("Configuration not loaded");

        private async Task ExecuteMoveAsync(Mat boardMat, Rectangle boardRect, ChessBoard currentBoard, bool blackAtBottom)
        {
            if (moveOrchestrator == null) return;

            // Analyze position using orchestrator
            var result = await moveOrchestrator.AnalyzePosition(
                currentBoard,
                Config.EngineDepth,
                config?.ShowSecondLine == true,
                config?.ShowThirdLine == true);

            // Handle failure (null result)
            if (result == null)
            {
                // Check if app restart was requested
                if (engineRestartManager.ShouldRestartApplication())
                {
                    this.Invoke((MethodInvoker)(() => buttonReset_Click(this, EventArgs.Empty)));
                }
                return;
            }

            // Success - extract results
            var (bestMove, evaluation, pvs, evaluations, completeFen, wdl) = result.Value;

            // Update blunder tracking - ONLY when auto-monitoring is active
            // Manual analysis doesn't track consecutive positions, so blunder detection
            // would give false positives (comparing unrelated positions)
            double? previousEval = null;
            if (blunderTracker.IsTrackingEnabled())
            {
                previousEval = blunderTracker.GetPreviousEvaluation();
                blunderTracker.UpdateBoardChangeTracking(completeFen, evaluation);

                // Update previous evaluation for next comparison
                double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
                blunderTracker.SetPreviousEvaluation(currentEval);
            }

            // Get opening book moves if enabled (Polyglot .bin format)
            List<BookMove>? bookMoves = null;

            // Try to load opening books if not loaded yet (lazy loading)
            if (config?.UseOpeningBook == true && openingBookService != null && !openingBookService.IsLoaded)
            {
                if (!string.IsNullOrEmpty(config.OpeningBooksFolder))
                {
                    string booksPath = GetBooksPath(config.OpeningBooksFolder);
                    if (Directory.Exists(booksPath))
                        openingBookService.LoadBooksFromFolder(booksPath);
                }
            }

            if (config?.UseOpeningBook == true && openingBookService?.IsLoaded == true)
            {
                var moves = openingBookService.GetBookMovesForPosition(completeFen);

                if (moves != null && moves.Count > 0)
                {
                    bookMoves = new List<BookMove>();
                    foreach (var pm in moves)
                    {
                        bookMoves.Add(new BookMove
                        {
                            UciMove = pm.UciMove,
                            Games = pm.Weight,
                            Priority = pm.Weight,
                            WinRate = 50,
                            Wins = 0,
                            Losses = 0,
                            Draws = 0,
                            Source = "Book"
                        });
                    }
                }
            }

            // Display analysis results (only show blunder warning if tracking is active)
            consoleFormatter?.DisplayAnalysisResults(
                bestMove, evaluation, pvs, evaluations, completeFen,
                previousEval,
                config?.ShowBestLine == true,
                config?.ShowSecondLine == true,
                config?.ShowThirdLine == true,
                wdl,
                bookMoves);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == 1)
                {
                    // Alt+X - Manual analysis
                    buttonAnalyze.PerformClick();
                }
                else if (hotkeyId == 2)
                {
                    // Alt+K - Toggle auto-monitoring on/off
                    ToggleAutoMonitoring();
                }
                else if (hotkeyId == 3)
                {
                    // Ctrl+B - Open Analysis Board
                    OpenAnalysisBoard();
                }
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// Toggles auto-monitoring on/off via Alt+K hotkey
        /// Provides quick enable/disable without opening settings
        /// </summary>
        private void ToggleAutoMonitoring()
        {
            if (boardMonitorService == null || config == null)
                return;

            if (boardMonitorService.IsMonitoring())
            {
                // Currently ON -> Turn OFF
                boardMonitorService.StopMonitoring();
                blunderTracker.StopTracking();
                config.AutoMonitorBoard = false;
                config.Save();
                labelStatus.Text = "Auto-Monitor: OFF";
                Debug.WriteLine("Alt+K: Auto-monitoring DISABLED");
            }
            else
            {
                // Currently OFF -> Turn ON
                bool userIsWhite = chkWhiteTurn.Checked;
                boardMonitorService.StartMonitoring(userIsWhite);
                blunderTracker.StartTracking();
                config.AutoMonitorBoard = true;
                config.Save();
                labelStatus.Text = "Auto-Monitor: ON";
                Debug.WriteLine("Alt+K: Auto-monitoring ENABLED");
            }
        }

        public MainForm()
        {
            InitializeComponent();
            config = AppConfig.Load();
            engineService = new ChessDroid.Services.ChessEngineService(config);
            enginePathResolver = new EnginePathResolver(config);

            // Initialize game state via PositionStateManager
            positionStateManager.InitializeGameState(chkWhiteTurn.Checked);

            // Initialize console formatter
            consoleFormatter = new ConsoleOutputFormatter(
                richTextBoxConsole,
                config,
                MovesExplanation.GenerateMoveExplanation);

            // Initialize analysis strategy
            analysisStrategy = new EngineAnalysisStrategy(
                engineService,
                status => this.Invoke((MethodInvoker)(() => labelStatus.Text = status)),
                config);

            // Initialize move orchestrator
            moveOrchestrator = new MoveAnalysisOrchestrator(
                positionStateManager,
                analysisStrategy,
                engineRestartManager,
                config,
                status => this.Invoke((MethodInvoker)(() => labelStatus.Text = status)),
                () => this.Invoke((MethodInvoker)(() => richTextBoxConsole.Clear())),
                text => this.Invoke((MethodInvoker)(() => richTextBoxConsole.AppendText(text))));

            // Apply theme from config
            ApplyTheme(config.Theme == "Dark");

            // Load explanation settings from config
            ExplanationFormatter.LoadFromConfig(config);

            // Initialize BoardMonitorService
            boardMonitorService = new BoardMonitorService(
                screenCaptureService,
                boardDetectionService,
                pieceRecognitionService,
                positionStateManager,
                config);

            // Subscribe to turn change event
            boardMonitorService.UserTurnDetected += OnUserTurnDetected;

            // Initialize opening book service (loads all .bin files from folder)
            openingBookService = new PolyglotBookService();
            if (config.UseOpeningBook && !string.IsNullOrEmpty(config.OpeningBooksFolder))
            {
                string booksPath = GetBooksPath(config.OpeningBooksFolder);
                if (Directory.Exists(booksPath))
                    openingBookService.LoadBooksFromFolder(booksPath);
            }

            // Force auto-monitor OFF on every startup (user must enable with Alt+K)
            config.AutoMonitorBoard = false;
            config.Save();
            labelStatus.Text = "Ready (Alt+K to enable auto-monitor)";
            Debug.WriteLine("MainForm: Auto-monitor disabled on startup (use Alt+K to enable)");

            LoadTemplatesAndMasks();
        }

        private void LoadTemplatesAndMasks()
        {
            // Use selected site from config
            string selectedSite = config?.SelectedSite ?? "Lichess";
            pieceRecognitionService.LoadTemplatesAndMasks(selectedSite, Config);
        }

        /// <summary>
        /// Resolves the opening books folder path (handles both relative and absolute paths).
        /// </summary>
        private string GetBooksPath(string booksFolder)
        {
            // If it's an absolute path that exists, use it directly
            if (Path.IsPathRooted(booksFolder) && Directory.Exists(booksFolder))
                return booksFolder;

            // Try relative paths from application directory
            string[] possiblePaths = new[]
            {
                Path.Combine(Application.StartupPath, booksFolder),
                Path.Combine(AppContext.BaseDirectory, booksFolder),
                Path.Combine(Directory.GetCurrentDirectory(), booksFolder)
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            // Return the first option (may not exist yet)
            return possiblePaths[0];
        }

        /// <summary>
        /// Event handler triggered when BoardMonitorService detects it's the user's turn
        /// Auto-triggers analysis without user interaction
        /// </summary>
        private async void OnUserTurnDetected(object? sender, TurnChangedEventArgs e)
        {
            // Guard: Don't interrupt manual analysis
            if (moveInProgress)
            {
                Debug.WriteLine("OnUserTurnDetected: Skipped (manual analysis in progress)");
                return;
            }

            // Validate board before analysis (prevent engine crashes on bad boards)
            if (!IsValidBoardForAnalysis(e.CurrentBoard))
            {
                Debug.WriteLine("OnUserTurnDetected: Skipped (invalid board - would crash engine)");
                return;
            }

            Debug.WriteLine("OnUserTurnDetected: Auto-triggering analysis");

            // Auto-trigger analysis using pre-captured board state
            await ExecuteMoveAsync(e.BoardMat, e.BoardRect, e.CurrentBoard, e.BlackAtBottom);
        }

        /// <summary>
        /// Validates board has minimum pieces needed for valid engine analysis
        /// Prevents sending garbage boards to engine (causes pipe crashes)
        /// </summary>
        private bool IsValidBoardForAnalysis(ChessBoard board)
        {
            int pieceCount = 0;
            int kingCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board[r, c];
                    if (piece != '.')
                    {
                        pieceCount++;
                        if (char.ToUpper(piece) == 'K')
                            kingCount++;
                    }
                }
            }

            // Must have exactly 2 kings and at least 4 pieces
            return kingCount == 2 && pieceCount >= 4;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.X)
            {
                buttonAnalyze.PerformClick();
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

            // Register Ctrl+B for Analysis Board
            bool registered3 = RegisterHotKey(this.Handle, 3, MOD_CONTROL, (uint)Keys.B);
            if (!registered3)
            {
                Debug.WriteLine("Failed to register Ctrl+B hotkey for Analysis Board");
            }

            manualTimer = new System.Windows.Forms.Timer();
            manualTimer.Interval = 1000;
            manualTimer.Tick += ManualTimer_Tick;

            // Timeout timer to prevent moveInProgress from getting stuck
            moveTimeoutTimer = new System.Windows.Forms.Timer();
            moveTimeoutTimer.Interval = Config.MoveTimeoutMs;
            moveTimeoutTimer.Tick += (s, ev) =>
            {
                lock (_moveInProgressLock)
                {
                    if (moveInProgress)
                    {
                        moveInProgress = false;
                        moveTimeoutTimer.Stop();
                    }
                }
            };
        }

        private async Task InitializeEngineAsync()
        {
            try
            {
                if (engineService == null || enginePathResolver == null)
                {
                    Debug.WriteLine("Cannot initialize engine - service is null");
                    return;
                }

                // Resolve engine path using the service
                var (enginePath, wasAutoDiscovered) = enginePathResolver.ResolveEnginePath();

                // Validate that the engine exists
                if (!enginePathResolver.ValidateEnginePath(enginePath))
                {
                    Debug.WriteLine($"Engine not found at: {enginePath}");
                    Invoke((MethodInvoker)(() => labelStatus.Text = "Engine not found"));
                    return;
                }

                // Initialize the engine
                await engineService.InitializeAsync(enginePath);
                Invoke((MethodInvoker)(() => labelStatus.Text = "Ready"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting engine: {ex.Message}");
                Invoke((MethodInvoker)(() => labelStatus.Text = "Engine unavailable"));
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
            UnregisterHotKey(this.Handle, 3);
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
                moveOrchestrator?.ClearCache();
                blunderTracker.Reset();

                // Reset monitoring services
                if (boardMonitorService?.IsMonitoring() == true)
                {
                    boardMonitorService.StopMonitoring();
                    if (config?.AutoMonitorBoard == true)
                    {
                        bool userIsWhite = chkWhiteTurn.Checked;
                        boardMonitorService.StartMonitoring(userIsWhite);
                    }
                }
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
                // Reload config in-place so all services get updated values
                // (services hold references to config, replacing it would leave them with stale data)
                Config.Reload();

                // Apply explanation settings
                ExplanationFormatter.LoadFromConfig(Config);

                // Apply theme changes
                this.Invoke((MethodInvoker)(() =>
                {
                    ApplyTheme(Config.Theme == "Dark");
                }));

                // Handle auto-monitor toggle
                if (Config.AutoMonitorBoard && boardMonitorService?.IsMonitoring() != true)
                {
                    bool userIsWhite = chkWhiteTurn.Checked;
                    boardMonitorService?.StartMonitoring(userIsWhite);
                    blunderTracker.StartTracking();
                }
                else if (!Config.AutoMonitorBoard && boardMonitorService?.IsMonitoring() == true)
                {
                    boardMonitorService?.StopMonitoring();
                    blunderTracker.StopTracking();
                }

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

        private void buttonAnalysisBoard_Click(object sender, EventArgs e)
        {
            OpenAnalysisBoard();
        }

        private void OpenAnalysisBoard()
        {
            // Open analysis board form, sharing the engine service
            var analysisBoardForm = new AnalysisBoardForm(Config, engineService);
            analysisBoardForm.Show();
        }

        private async void buttonAnalyze_Click(object sender, EventArgs e)
        {
            // If a move is already in progress, ignore this invocation
            if (moveInProgress)
            {
                MessageBox.Show("A move is already being processed. Please wait.");
                return;
            }

            // Pause auto-monitor during manual analysis
            boardMonitorService?.PauseScanning();

            // Show immediate feedback
            labelStatus.Text = "Analyzing...";

            // Mark that we are processing a move
            moveInProgress = true;
            moveTimeoutTimer?.Stop();
            moveTimeoutTimer?.Start();

            Mat? fullScreenMat = null;
            Mat? boardMat = null;

            try
            {
                // 1) Capture and extract the board
                fullScreenMat = screenCaptureService.CaptureScreenAsMat();
                if (fullScreenMat == null)
                {
                    labelStatus.Text = "Screen capture failed";
                    return;
                }

                // Try to detect the board automatically
                var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullScreenMat);
                boardMat = detectedResult.boardMat;
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

                ChessBoard currentBoard = pieceRecognitionService.ExtractBoardFromMat(boardMat, blackAtBottom, Config.MatchThreshold);

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
                Debug.WriteLine($"Error in buttonAnalyze_Click: {ex.Message}");
                labelStatus.Text = $"Error: {ex.Message}";
                engineRestartManager.RecordFailure();
            }
            finally
            {
                // Dispose Mat objects to prevent memory leaks
                fullScreenMat?.Dispose();
                boardMat?.Dispose();

                // ALWAYS reset the flag so button can be clicked again
                moveTimeoutTimer?.Stop();
                moveInProgress = false;

                // Resume auto-monitor after manual analysis
                boardMonitorService?.ResumeScanning();
            }
        }

        private void ManualTimer_Tick(object? sender, EventArgs e)
        {
            Mat? fullMat = null;
            Mat? boardMat = null;

            try
            {
                fullMat = screenCaptureService.CaptureScreenAsMat();
                if (fullMat == null) return;

                var detectedResult = boardDetectionService.DetectBoardWithRectangle(fullMat);
                boardMat = detectedResult.boardMat;

                if (boardMat == null) return;

                bool blackAtBottom = !positionStateManager.IsWhiteToMove();
                if (blackAtBottom)
                    CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical | FlipType.Horizontal);

                ChessBoard currentBoard = pieceRecognitionService.ExtractBoardFromMat(boardMat, blackAtBottom, Config.MatchThreshold);

                if (positionStateManager.LastDetectedBoard != null && ChessRulesService.CountBoardDifferences(positionStateManager.LastDetectedBoard, currentBoard) >= 2)
                {
                    manualTimer?.Stop();
                    positionStateManager.SaveMoveState(currentBoard);
                }
            }
            finally
            {
                // Dispose Mat objects to prevent memory leaks
                fullMat?.Dispose();
                boardMat?.Dispose();
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            ThemeService.ApplyTheme(this, labelStatus, buttonAnalyze, buttonReset, buttonSettings, richTextBoxConsole, chkWhiteTurn, isDarkMode);
        }
    }
}