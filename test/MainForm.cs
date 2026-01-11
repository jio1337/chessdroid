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
        private Rectangle? lastBoardRectCached = null;
        private DateTime lastBoardRectCachedAt = DateTime.MinValue;
        private System.Windows.Forms.Timer? manualTimer;

        private bool moveInProgress = false;

        private System.Windows.Forms.Timer? moveTimeoutTimer;

        private ChessDroid.Models.GameState? currentGameState;
        private ChessDroid.Models.ChessBoard? lastDetectedBoard;
        private DateTime lastMoveTime = DateTime.Now;

        // Track consecutive failures for better error handling
        private int consecutiveAnalysisFailures = 0;

        private const int MAX_CONSECUTIVE_FAILURES = 2;

        // Track engine restarts to prevent infinite loops
        private int engineRestartCount = 0;

        private DateTime lastRestartTime = DateTime.MinValue;
        private const int MAX_RESTARTS_PER_MINUTE = 3;

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

        // Cache for template matching results
        private class CellMatchCache
        {
            public string CellHash { get; set; } = "";
            public string DetectedPiece { get; set; } = "";
            public double Confidence { get; set; }
            public DateTime CachedAt { get; set; }
        }

        private ConcurrentDictionary<string, CellMatchCache> cellMatchCache = new ConcurrentDictionary<string, CellMatchCache>();
        private int cacheHits = 0;
        private int cacheMisses = 0;

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private ChessDroid.Services.ChessEngineService? engineService;

        private Dictionary<string, Mat> templates = new Dictionary<string, Mat>();

        private Dictionary<string, Mat> templateMasks = new Dictionary<string, Mat>();

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
            UpdatePositionState(currentBoard);
            swUpdatePos.Stop();

            // 2) Generate complete FEN
            var swFen = System.Diagnostics.Stopwatch.StartNew();
            string completeFen = GenerateCompleteFEN(currentBoard);
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
                consecutiveAnalysisFailures++;
                Debug.WriteLine($"ERROR: Analysis failed for this position (failures: {consecutiveAnalysisFailures}/{MAX_CONSECUTIVE_FAILURES})");
                Debug.WriteLine($"Failed FEN: {completeFen}");

                // Clear cache so next attempt tries again
                lastEngineResult = null;

                // Clear UI to show something happened
                this.Invoke((MethodInvoker)(() =>
                {
                    richTextBoxConsole.Clear();
                    richTextBoxConsole.AppendText($"Analysis failed - attempt {consecutiveAnalysisFailures} of {MAX_CONSECUTIVE_FAILURES}{Environment.NewLine}");
                    richTextBoxConsole.AppendText($"Position: {completeFen}{Environment.NewLine}");
                }));

                if (consecutiveAnalysisFailures >= MAX_CONSECUTIVE_FAILURES)
                {
                    // Check if we're in an infinite restart loop
                    var timeSinceLastRestart = DateTime.Now - lastRestartTime;
                    if (timeSinceLastRestart.TotalMinutes < 1)
                    {
                        engineRestartCount++;
                    }
                    else
                    {
                        // Reset counter if more than 1 minute has passed
                        engineRestartCount = 1;
                    }

                    lastRestartTime = DateTime.Now;

                    if (engineRestartCount > MAX_RESTARTS_PER_MINUTE)
                    {
                        // Too many restarts - automatically restart application
                        Debug.WriteLine($"Too many engine restarts ({engineRestartCount}), triggering application restart...");
                        this.Invoke((MethodInvoker)(() =>
                        {
                            labelStatus.Text = "Engine unstable - restarting application...";
                            richTextBoxConsole.AppendText($"Too many engine restarts ({engineRestartCount}). Automatically restarting application...{Environment.NewLine}");
                            buttonReset_Click(this, EventArgs.Empty);
                        }));
                        return;
                    }

                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Restarting engine..."));
                    Debug.WriteLine($"Too many consecutive failures ({consecutiveAnalysisFailures}), restarting engine (restart #{engineRestartCount})...");

                    try
                    {
                        if (engineService == null)
                        {
                            Debug.WriteLine("Engine service is null, cannot restart");
                            throw new InvalidOperationException("Engine service not initialized");
                        }

                        await engineService.RestartAsync();

                        // Wait for engine to fully initialize
                        await Task.Delay(1000);

                        consecutiveAnalysisFailures = 0;
                        this.Invoke((MethodInvoker)(() =>
                        {
                            labelStatus.Text = "Engine restarted - try again";
                            richTextBoxConsole.AppendText($"Engine restarted successfully (restart #{engineRestartCount}). Please try again.{Environment.NewLine}");
                        }));
                    }
                    catch (Exception restartEx)
                    {
                        Debug.WriteLine($"Engine restart failed: {restartEx.Message}");
                        this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Engine restart failed"));
                    }
                }
                else
                {
                    this.Invoke((MethodInvoker)(() => labelStatus.Text = $"Analysis failed ({consecutiveAnalysisFailures}/{MAX_CONSECUTIVE_FAILURES}) - try again"));
                }

                // Save state and continue instead of stopping completely
                SaveMoveState(currentBoard);
                return;
            }

            // Reset failure counter on success
            if (consecutiveAnalysisFailures > 0)
            {
                Debug.WriteLine($"Analysis recovered after {consecutiveAnalysisFailures} failures");
                consecutiveAnalysisFailures = 0;
            }

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
            SaveMoveState(currentBoard);
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

        private void UpdatePositionState(ChessBoard currentBoard)
        {
            if (currentGameState == null)
            {
                Debug.WriteLine("currentGameState is null in UpdatePositionState");
                return;
            }

            if (lastDetectedBoard != null)
            {
                (string uciMovePrev, string updatedCastlingPrev, string newEpPrev) =
                    ChessRulesService.DetectMoveAndUpdateCastling(lastDetectedBoard, currentBoard, currentGameState.CastlingRights);
                currentGameState.CastlingRights = updatedCastlingPrev;
                currentGameState.EnPassantTarget = newEpPrev;
            }
            else
            {
                // First position - infer castling rights from current board state
                currentGameState.CastlingRights = ChessRulesService.InferCastlingRights(currentBoard);
                currentGameState.EnPassantTarget = "-";
            }
        }

        private string GenerateCompleteFEN(ChessBoard currentBoard)
        {
            if (currentGameState == null)
            {
                Debug.WriteLine("currentGameState is null in GenerateCompleteFEN");
                return ChessNotationService.GenerateFENFromBoard(currentBoard) + " w KQkq - 0 1";
            }

            string fenPosition = ChessNotationService.GenerateFENFromBoard(currentBoard);
            string turn = currentGameState.WhiteToMove ? "w" : "b";
            string castling = string.IsNullOrEmpty(currentGameState.CastlingRights) ? "-" : currentGameState.CastlingRights;
            return $"{fenPosition} {turn} {castling} {currentGameState.EnPassantTarget} 0 1";
        }

        private void UpdateUIWithMoveResults(string bestMove, string evaluation, List<string> pvs, List<string> evaluations, string completeFen)
        {
            richTextBoxConsole.Clear();

            // Check for blunders
            double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
            if (currentEval.HasValue && previousEvaluation.HasValue)
            {
                // Extract whose turn it is from FEN to determine who just moved
                // FEN format: "position w/b ..." - second part tells whose turn it is NOW
                // If it's White's turn now, Black just moved
                // If it's Black's turn now, White just moved
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                bool whiteJustMoved = !whiteToMove; // If it's Black's turn, White just moved

                var (isBlunder, blunderType, evalDrop, whiteBlundered) = MovesExplanation.DetectBlunder(
                    currentEval, previousEvaluation, whiteJustMoved);

                if (isBlunder)
                {
                    richTextBoxConsole.SelectionBackColor = Color.Orange;
                    richTextBoxConsole.SelectionColor = Color.Black;
                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Bold);
                    richTextBoxConsole.AppendText($"⚠ {blunderType}! Eval swing: {evalDrop:F2} pawns{Environment.NewLine}");

                    // Determine who blundered
                    if (whiteBlundered)
                    {
                        richTextBoxConsole.AppendText($"White just blundered and gave Black a big opportunity!{Environment.NewLine}");
                    }
                    else
                    {
                        richTextBoxConsole.AppendText($"Black just blundered and gave White a big opportunity!{Environment.NewLine}");
                    }

                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Regular);
                    richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;
                    richTextBoxConsole.AppendText(Environment.NewLine);
                }
            }

            // Best line with explanation
            richTextBoxConsole.SelectionBackColor = Color.MediumSeaGreen;
            richTextBoxConsole.SelectionColor = Color.Black;
            string bestSanFull = ConvertPvToSan(pvs, 0, bestMove, completeFen);

            // Format evaluation with win percentage if enabled
            string formattedEval = evaluation;
            if (config?.ShowWinPercentage == true && ExplanationFormatter.CurrentLevel >= ExplanationFormatter.ComplexityLevel.Intermediate)
            {
                var tempBoard = ChessBoard.FromFEN(completeFen);
                int materialCount = EndgameAnalysis.CountTotalPieces(tempBoard);
                formattedEval = ExplanationFormatter.FormatEvaluationWithWinRate(evaluation, materialCount, completeFen);
            }

            richTextBoxConsole.AppendText($"Best line: {bestSanFull} {formattedEval}{Environment.NewLine}");

            // Add explanation for best move
            string explanation = MovesExplanation.GenerateMoveExplanation(bestMove, completeFen, pvs, evaluation);
            if (!string.IsNullOrEmpty(explanation))
            {
                richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;

                // Use move quality color if enabled, otherwise use default line color
                Color explanationColor = Color.PaleGreen; // Default for best line
                string qualitySymbol = "";

                if (config?.ShowMoveQualityColor == true)
                {
                    var quality = ExplanationFormatter.DetermineQualityFromEvaluation(explanation, evaluation);
                    explanationColor = ExplanationFormatter.GetQualityColor(quality);
                    qualitySymbol = ExplanationFormatter.GetQualitySymbol(quality);

                    // Add space after symbol if present
                    if (!string.IsNullOrEmpty(qualitySymbol))
                        qualitySymbol = qualitySymbol + " ";
                }

                richTextBoxConsole.SelectionColor = explanationColor;
                richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Italic);
                richTextBoxConsole.AppendText($"  → {qualitySymbol}{explanation}{Environment.NewLine}");
                richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Regular);
            }

            richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;
            richTextBoxConsole.SelectionColor = richTextBoxConsole.ForeColor;

            // Second best
            if (config?.ShowSecondLine == true && pvs.Count >= 2)
            {
                richTextBoxConsole.SelectionBackColor = Color.Yellow;
                richTextBoxConsole.SelectionColor = Color.Black;
                var secondSan = ChessNotationService.ConvertFullPvToSan(pvs[1], completeFen, ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);

                // Extract first move from second line for explanation
                string secondMove = pvs[1].Split(' ')[0];
                string secondEval = evaluations.Count >= 2 ? evaluations[1] : "";

                // Format evaluation with win percentage if enabled
                string formattedSecondEval = secondEval;
                if (config?.ShowWinPercentage == true && ExplanationFormatter.CurrentLevel >= ExplanationFormatter.ComplexityLevel.Intermediate)
                {
                    var tempBoard = ChessBoard.FromFEN(completeFen);
                    int materialCount = EndgameAnalysis.CountTotalPieces(tempBoard);
                    formattedSecondEval = ExplanationFormatter.FormatEvaluationWithWinRate(secondEval, materialCount, completeFen);
                }

                richTextBoxConsole.AppendText($"Second best: {secondSan} {formattedSecondEval}{Environment.NewLine}");

                // Add explanation for second move
                string secondExplanation = MovesExplanation.GenerateMoveExplanation(secondMove, completeFen, pvs, evaluation);
                if (!string.IsNullOrEmpty(secondExplanation))
                {
                    richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;

                    // Use move quality color if enabled, otherwise use default line color
                    Color explanationColor = Color.DarkGoldenrod; // Default for second line
                    string qualitySymbol = "";

                    if (config?.ShowMoveQualityColor == true)
                    {
                        var quality = ExplanationFormatter.DetermineQualityFromEvaluation(secondExplanation, secondEval);
                        explanationColor = ExplanationFormatter.GetQualityColor(quality);
                        qualitySymbol = ExplanationFormatter.GetQualitySymbol(quality);

                        // Add space after symbol if present
                        if (!string.IsNullOrEmpty(qualitySymbol))
                            qualitySymbol = qualitySymbol + " ";
                    }

                    richTextBoxConsole.SelectionColor = explanationColor;
                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Italic);
                    richTextBoxConsole.AppendText($"  → {qualitySymbol}{secondExplanation}{Environment.NewLine}");
                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Regular);
                }

                richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;
            }

            // Third best
            if (config?.ShowThirdLine == true && pvs.Count >= 3)
            {
                richTextBoxConsole.SelectionBackColor = Color.Red;
                richTextBoxConsole.SelectionColor = Color.Black;
                var thirdSan = ChessNotationService.ConvertFullPvToSan(pvs[2], completeFen, ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);

                // Extract first move from third line for explanation
                string thirdMove = pvs[2].Split(' ')[0];
                string thirdEval = evaluations.Count >= 3 ? evaluations[2] : "";
                richTextBoxConsole.AppendText($"Third best: {thirdSan} {thirdEval}{Environment.NewLine}");

                // Add explanation for third move
                string thirdExplanation = MovesExplanation.GenerateMoveExplanation(thirdMove, completeFen, pvs, evaluation);
                if (!string.IsNullOrEmpty(thirdExplanation))
                {
                    richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;

                    // Use move quality color if enabled, otherwise use default line color
                    Color explanationColor = Color.DarkRed; // Default for third line
                    string qualitySymbol = "";

                    if (config?.ShowMoveQualityColor == true)
                    {
                        var quality = ExplanationFormatter.DetermineQualityFromEvaluation(thirdExplanation, thirdEval);
                        explanationColor = ExplanationFormatter.GetQualityColor(quality);
                        qualitySymbol = ExplanationFormatter.GetQualitySymbol(quality);

                        // Add space after symbol if present
                        if (!string.IsNullOrEmpty(qualitySymbol))
                            qualitySymbol = qualitySymbol + " ";
                    }

                    richTextBoxConsole.SelectionColor = explanationColor;
                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Italic);
                    richTextBoxConsole.AppendText($"  → {qualitySymbol}{thirdExplanation}{Environment.NewLine}");
                    richTextBoxConsole.SelectionFont = new Font(richTextBoxConsole.Font, FontStyle.Regular);
                }

                richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;
            }

            richTextBoxConsole.SelectionBackColor = richTextBoxConsole.BackColor;
            richTextBoxConsole.SelectionColor = richTextBoxConsole.ForeColor;

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

        private void SaveMoveState(ChessBoard currentBoard)
        {
            lastMoveTime = DateTime.Now;
            if (currentGameState != null)
            {
                currentGameState.LastMoveTime = lastMoveTime;
                lastDetectedBoard = new ChessBoard(currentBoard.GetArray());
                currentGameState.Board = lastDetectedBoard;
            }
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
            currentGameState = new ChessDroid.Models.GameState();

            // Initialize game state from checkbox
            currentGameState.WhiteToMove = chkWhiteTurn.Checked;

            // Apply theme from config
            ApplyTheme(config.Theme == "Dark");

            // Load explanation settings from config
            ExplanationFormatter.LoadFromConfig(config);

            LoadTemplatesAndMasks();
        }

        private void LoadTemplatesAndMasks()
        {
            try
            {
                templates.Clear();
                templateMasks.Clear();

                // Use selected site from config
                string selectedSite = config?.SelectedSite ?? "Lichess";

                string templatesPath = Path.Combine(Config.GetTemplatesPath(), selectedSite);

                // Verify templates directory exists
                if (!Directory.Exists(templatesPath))
                {
                    throw new Exception($"Templates folder not found at: {templatesPath}\nPlease ensure the Templates/{selectedSite} folder is in the application directory.");
                }

                string[] pieces = { "wK", "wQ", "wR", "wB", "wN", "wP",
                                    "bK", "bQ", "bR", "bB", "bN", "bP" };

                foreach (string piece in pieces)
                {
                    string filePath = Path.Combine(templatesPath, piece + ".png");

                    if (!File.Exists(filePath))
                    {
                        throw new Exception($"Template file not found: {filePath}");
                    }

                    using (Mat templateColor = CvInvoke.Imread(filePath, ImreadModes.Unchanged))
                    {
                        if (templateColor.IsEmpty)
                            throw new Exception($"Failed to load template image: {filePath}");

                        Mat templateGray = new Mat();
                        // If template has alpha channel, use it for mask
                        if (templateColor.NumberOfChannels == 4)
                        {
                            // Extract alpha channel as mask
                            Mat[] channels = templateColor.Split();
                            Mat alphaMask = channels[3]; // Alpha channel
                            // Convert BGR to grayscale (ignoring alpha)
                            CvInvoke.CvtColor(templateColor, templateGray, ColorConversion.Bgra2Gray);
                            // Resize both template and mask to TEMPLATE_SIZE
                            Mat templateGrayResized = new Mat();
                            CvInvoke.Resize(templateGray, templateGrayResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));
                            Mat alphaMaskResized = new Mat();
                            CvInvoke.Resize(alphaMask, alphaMaskResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));
                            templates[piece] = templateGrayResized;
                            templateMasks[piece] = alphaMaskResized;
                            // Dispose other channels
                            channels[0].Dispose();
                            channels[1].Dispose();
                            channels[2].Dispose();
                            templateGray.Dispose();
                            alphaMask.Dispose();
                        }
                        else
                        {
                            // No alpha channel, convert to grayscale and create simple mask
                            CvInvoke.CvtColor(templateColor, templateGray, ColorConversion.Bgr2Gray);
                            Mat templateGrayResized = new Mat();
                            CvInvoke.Resize(templateGray, templateGrayResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));
                            Mat mask = new Mat();
                            CvInvoke.Threshold(templateGrayResized, mask, 10, 255, ThresholdType.Binary);
                            templates[piece] = templateGrayResized;
                            templateMasks[piece] = mask;
                            templateGray.Dispose();
                        }
                    }
                }

                Debug.WriteLine($"Loaded {templates.Count} templates from {templatesPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading templates and masks:\n\n{ex.Message}\n\nApplication Path: {Application.StartupPath}",
                    "Template Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ChessBoard ExtractBoardFromMat(Mat boardMat, bool blackAtBottom)
        {
            // Cleanup old cache entries periodically
            ClearOldCacheEntries();

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

                        (string detectedPiece, double confidence) = DetectPieceAndConfidence_Optimized(cell);

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

        // Optimized: resize cell once, not per template
        private (string, double) DetectPieceAndConfidence_Optimized(Mat celda)
        {
            string cellHash = ComputeCellHash(celda);
            string cacheKey = cellHash;
            if (cellMatchCache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.CachedAt).TotalSeconds < 5)
                {
                    cacheHits++;
                    return (cached.DetectedPiece, cached.Confidence);
                }
            }
            cacheMisses++;
            string mejorCoincidencia = "";
            double mejorValor = 0.0;

            // Assume all templates are the same size
            Mat celdaResized = new Mat();
            CvInvoke.Resize(celda, celdaResized, new Size(TEMPLATE_SIZE, TEMPLATE_SIZE));

            foreach (var kvp in templates)
            {
                string key = kvp.Key;
                Mat templ = kvp.Value;
                Mat mask = templateMasks[key];

                // Try matching without mask first for better black piece detection
                double valorSinMask = 0.0;
                using (Mat resultado = new Mat())
                {
                    CvInvoke.MatchTemplate(celdaResized, templ, resultado, TemplateMatchingType.CcoeffNormed);
                    double[] minVals, maxVals;
                    Point[] minLoc, maxLoc;
                    resultado.MinMax(out minVals, out maxVals, out minLoc, out maxLoc);
                    valorSinMask = maxVals[0];
                }

                // Also try with mask if available
                double valorConMask = 0.0;
                if (mask != null && !mask.IsEmpty)
                {
                    using (Mat resultado = new Mat())
                    {
                        CvInvoke.MatchTemplate(celdaResized, templ, resultado, TemplateMatchingType.CcoeffNormed, mask);
                        double[] minVals, maxVals;
                        Point[] minLoc, maxLoc;
                        resultado.MinMax(out minVals, out maxVals, out minLoc, out maxLoc);
                        valorConMask = maxVals[0];
                    }
                }

                double valor = Math.Max(valorSinMask, valorConMask);

                if (valor > mejorValor && valor >= Config.MatchThreshold)
                {
                    mejorValor = valor;
                    if (!string.IsNullOrEmpty(key) && key.Length > 1)
                    {
                        char pieceChar = key[1];
                        if (key[0] == 'b')
                            pieceChar = char.ToLower(pieceChar);
                        mejorCoincidencia = pieceChar.ToString();
                    }
                    else
                    {
                        mejorCoincidencia = key;
                    }
                }
            }
            celdaResized.Dispose();

            cellMatchCache[cacheKey] = new CellMatchCache
            {
                CellHash = cellHash,
                DetectedPiece = mejorCoincidencia,
                Confidence = mejorValor,
                CachedAt = DateTime.Now
            };
            return (mejorCoincidencia, mejorValor);
        }

        private Bitmap? CaptureFullScreen()
        {
            try
            {
                Rectangle screenBounds = Screen.PrimaryScreen!.Bounds;
                Bitmap bmp = new Bitmap(screenBounds.Width, screenBounds.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error capturing screen: " + ex.Message, "Screen Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private Mat BitmapToMat(Bitmap bmp)
        {
            // Fast path: if already 24bpp, avoid copy
            if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Mat matView = new Mat(bmp.Height, bmp.Width, DepthType.Cv8U, 3, data.Scan0, data.Stride);
                    return matView.Clone(); // clone to release lock
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
            else
            {
                // Only convert if necessary
                using (Bitmap work = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(work))
                        g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                    Rectangle rect = new Rectangle(0, 0, work.Width, work.Height);
                    BitmapData data = work.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        Mat matView = new Mat(work.Height, work.Width, DepthType.Cv8U, 3, data.Scan0, data.Stride);
                        return matView.Clone();
                    }
                    finally
                    {
                        work.UnlockBits(data);
                    }
                }
            }
        }

        private static Mat? DetectBoard(Mat fullMat)
        {
            // Try detection with multiple parameter sets (tight -> loose -> very loose)
            for (int pass = 0; pass < 3; pass++)
            {
                Mat gray = new Mat();
                CvInvoke.CvtColor(fullMat, gray, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 0);
                Mat canny = new Mat();
                CvInvoke.Canny(gray, canny, 50, 150);
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(canny, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    double maxArea = 0;
                    Point[]? boardContour = null;

                    // Parameter tuning per pass: tight -> loose -> very loose
                    double minAreaThreshold;
                    double approxEpsFactor;
                    double aspectMin;
                    double aspectMax;

                    if (pass == 0)
                    {
                        minAreaThreshold = 5000;
                        approxEpsFactor = 0.02;
                        aspectMin = 0.8;
                        aspectMax = 1.2;
                    }
                    else if (pass == 1)
                    {
                        minAreaThreshold = 1500;
                        approxEpsFactor = 0.04;
                        aspectMin = 0.7;
                        aspectMax = 1.3;
                    }
                    else
                    {
                        // Very loose: allow relatively large contours and sloppier approximation
                        // Use image-relative min area so very large boards are included
                        minAreaThreshold = Math.Max(1000, (fullMat.Width * fullMat.Height) * 0.001); // ~0.1% of image area
                        approxEpsFactor = 0.06;
                        aspectMin = 0.6;
                        aspectMax = 1.5;
                    }

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double area = CvInvoke.ContourArea(contour);
                            if (area < minAreaThreshold)
                                continue;
                            VectorOfPoint approx = new VectorOfPoint();
                            CvInvoke.ApproxPolyDP(contour, approx, approxEpsFactor * CvInvoke.ArcLength(contour, true), true);
                            // Accept contours that approximate to 4+ points; for very loose pass allow larger variance
                            if (approx.Size >= 4)
                            {
                                Rectangle rect = CvInvoke.BoundingRectangle(approx);
                                double aspectRatio = (double)rect.Width / rect.Height;
                                if (aspectRatio > aspectMin && aspectRatio < aspectMax && area > maxArea)
                                {
                                    maxArea = area;
                                    // Use rectangle corners as a robust fallback for warped boards
                                    boardContour = new Point[] {
                                        new Point(rect.X, rect.Y),
                                        new Point(rect.X + rect.Width, rect.Y),
                                        new Point(rect.X + rect.Width, rect.Y + rect.Height),
                                        new Point(rect.X, rect.Y + rect.Height)
                                    };
                                }
                            }
                        }
                    }

                    if (boardContour != null)
                    {
                        PointF[] srcPoints = ReorderPoints(boardContour);

                        // Calculate detected board size dynamically
                        Rectangle boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(boardContour));
                        int detectedSize = Math.Max(boundingRect.Width, boundingRect.Height);

                        // Keep board at detected size for better quality
                        PointF[] dstPoints = new PointF[]
                        {
                            new PointF(0,0),
                            new PointF(detectedSize,0),
                            new PointF(detectedSize,detectedSize),
                            new PointF(0,detectedSize)
                        };
                        Mat transform = CvInvoke.GetPerspectiveTransform(srcPoints, dstPoints);
                        Mat boardRectificado = new Mat();
                        CvInvoke.WarpPerspective(fullMat, boardRectificado, transform, new Size(detectedSize, detectedSize));
                        return boardRectificado;
                    }
                }
            }

            return null;
        }

        private (Mat? boardMat, Rectangle boardRect) DetectBoardWithRectangle(Mat fullMat)
        {
            // 1) Fast path: if we had a recent boardRect, crop directly (without contours)
            if (lastBoardRectCached.HasValue && (DateTime.Now - lastBoardRectCachedAt).TotalSeconds < 3)
            {
                var quick = CaptureFixedRectangle(fullMat, lastBoardRectCached.Value);
                if (quick != null)
                {
                    // Refresh timestamp to keep it alive
                    lastBoardRectCachedAt = DateTime.Now;
                    return (quick, lastBoardRectCached.Value);
                }
                // If crop failed for some reason, continue to normal detection
            }

            // 2) Try to detect board automatically
            Mat? detectedBoard = DetectBoard(fullMat);
            if (detectedBoard != null)
            {
                using Mat gray = new Mat();
                using Mat canny = new Mat();

                CvInvoke.CvtColor(fullMat, gray, ColorConversion.Bgr2Gray);
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 0);
                CvInvoke.Canny(gray, canny, 50, 150);

                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(canny, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                    double maxArea = 0;
                    Rectangle boardRect = Rectangle.Empty;
                    Point[]? bestContour = null;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        {
                            double area = CvInvoke.ContourArea(contour);
                            if (area < 5000) continue;

                            using (VectorOfPoint approx = new VectorOfPoint())
                            {
                                CvInvoke.ApproxPolyDP(contour, approx, 0.02 * CvInvoke.ArcLength(contour, true), true);
                                if (approx.Size == 4)
                                {
                                    Rectangle rect = CvInvoke.BoundingRectangle(approx);
                                    double aspectRatio = (double)rect.Width / rect.Height;

                                    if (aspectRatio > 0.8 && aspectRatio < 1.2 && area > maxArea)
                                    {
                                        maxArea = area;
                                        boardRect = rect;
                                        bestContour = approx.ToArray();
                                    }
                                }
                            }
                        }
                    }

                    // 2.1) If we found valid rectangle: padding + cache + return
                    if (maxArea > 0 && boardRect != Rectangle.Empty)
                    {
                        int padding = Math.Min(100, Math.Min(fullMat.Width, fullMat.Height) / 10);
                        int nx = Math.Max(0, boardRect.X - padding);
                        int ny = Math.Max(0, boardRect.Y - padding);
                        int nW = Math.Min(fullMat.Width - nx, boardRect.Width + padding * 2);
                        int nH = Math.Min(fullMat.Height - ny, boardRect.Height + padding * 2);
                        boardRect = new Rectangle(nx, ny, nW, nH);

                        // Save cache
                        lastBoardRectCached = boardRect;
                        lastBoardRectCachedAt = DateTime.Now;

                        return (detectedBoard, boardRect);
                    }

                    // 2.2) If DetectBoard gave something but no rectangle found: fall back to default
                }
            }

            // 3) Fallback: centered proportional square + cache
            int side = Math.Min(fullMat.Width, fullMat.Height) * 8 / 10;
            side = Math.Clamp(side, 600, Math.Min(fullMat.Width, fullMat.Height));

            int x = (fullMat.Width - side) / 2;
            int y = (fullMat.Height - side) / 2;

            Rectangle fallbackRect = new Rectangle(x, y, side, side);
            Mat? fallbackBoard = CaptureFixedRectangle(fullMat, fallbackRect);

            // Cache the fallback so next click is instant
            if (fallbackBoard != null)
            {
                lastBoardRectCached = fallbackRect;
                lastBoardRectCachedAt = DateTime.Now;
            }

            return (fallbackBoard, fallbackRect);
        }

        private Mat? CaptureFixedRectangle(Mat fullMat, Rectangle rect)
        {
            if (rect.X < 0 || rect.Y < 0 ||
                rect.X + rect.Width > fullMat.Width ||
                rect.Y + rect.Height > fullMat.Height)
                return null;

            // Return board at native size - no forced resizing
            using (Mat boardMat = new Mat(fullMat, rect))
            {
                return boardMat.Clone();
            }
        }

        private static PointF[] ReorderPoints(Point[] pts)
        {
            PointF[] ordered = new PointF[4];
            var sum = pts.Select(p => p.X + p.Y).ToArray();
            var diff = pts.Select(p => p.Y - p.X).ToArray();
            ordered[0] = pts[Array.IndexOf(sum, sum.Min())];
            ordered[2] = pts[Array.IndexOf(sum, sum.Max())];
            ordered[1] = pts[Array.IndexOf(diff, diff.Min())];
            ordered[3] = pts[Array.IndexOf(diff, diff.Max())];
            return ordered;
        }

        private static string ComputeCellHash(Mat celda)
        {
            // Compute simple hash based on mean and stddev for quick comparison
            MCvScalar mean = new MCvScalar();
            MCvScalar stdDev = new MCvScalar();
            CvInvoke.MeanStdDev(celda, ref mean, ref stdDev);
            return $"{mean.V0:F2}_{stdDev.V0:F2}";
        }

        private void ClearOldCacheEntries()
        {
            var now = DateTime.Now;
            var oldEntries = cellMatchCache
                .Where(kvp => kvp.Value != null && (now - kvp.Value.CachedAt).TotalSeconds > 10)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldEntries)
            {
                cellMatchCache.TryRemove(key, out _);
            }

            // Periodically log cache statistics
            if ((cacheHits + cacheMisses) % 100 == 0 && (cacheHits + cacheMisses) > 0)
            {
                double hitRate = (double)cacheHits / (cacheHits + cacheMisses) * 100;
                Debug.WriteLine($"Template matching cache: {hitRate:F1}% hit rate ({cacheHits} hits, {cacheMisses} misses)");
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
                consecutiveAnalysisFailures = 0;
                engineRestartCount = 0;
                engineService = new ChessDroid.Services.ChessEngineService(Config);
                currentGameState = new ChessDroid.Models.GameState();
                lastDetectedBoard = null;
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
            if (currentGameState != null)
            {
                currentGameState.WhiteToMove = chkWhiteTurn.Checked;
            }

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
                using Bitmap? fullScreenBitmap = CaptureFullScreen();
                if (fullScreenBitmap == null)
                {
                    labelStatus.Text = "Screen capture failed";
                    return;
                }

                Mat fullScreenMat = BitmapToMat(fullScreenBitmap);

                // Try to detect the board automatically
                var detectedResult = DetectBoardWithRectangle(fullScreenMat);
                Mat? boardMat = detectedResult.boardMat;
                Rectangle boardRect = detectedResult.boardRect;

                if (boardMat == null)
                {
                    labelStatus.Text = "Board detection failed";
                    return;
                }

                bool blackAtBottom = currentGameState?.WhiteToMove == false;
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
                string completeFen = GenerateCompleteFEN(currentBoard);
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
                consecutiveAnalysisFailures++;
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
            Bitmap? bmp = CaptureFullScreen();
            if (bmp == null) return;

            Mat fullMat = BitmapToMat(bmp);
            var detectedResult = DetectBoardWithRectangle(fullMat);
            Mat? boardMat = detectedResult.boardMat;

            if (boardMat == null) return;

            bool blackAtBottom = currentGameState?.WhiteToMove == false;
            if (blackAtBottom)
                CvInvoke.Flip(boardMat, boardMat, FlipType.Vertical | FlipType.Horizontal);

            ChessBoard currentBoard = ExtractBoardFromMat(boardMat, blackAtBottom);

            if (lastDetectedBoard != null && ChessRulesService.CountBoardDifferences(lastDetectedBoard, currentBoard) >= 2)
            {
                manualTimer?.Stop();
                lastDetectedBoard = currentBoard;
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