using ChessDroid.Models;
using ChessDroid.Services;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChessDroid
{
    // Use a smaller template/cell size for much faster matching
    public partial class MainForm : Form
    {
        private const int TEMPLATE_SIZE = 64;
        private System.Windows.Forms.Timer? manualTimer;

        private bool moveInProgress = false;

        private System.Windows.Forms.Timer? moveTimeoutTimer;

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
        private EngineAnalysisStrategy? analysisStrategy;
        private MoveAnalysisOrchestrator? moveOrchestrator;
        private BlunderTracker blunderTracker = new BlunderTracker();
        private EnginePathResolver? enginePathResolver;
        private BoardMonitorService? boardMonitorService;

        private AppConfig? config;

        // Helper property to ensure config is never null
        private AppConfig Config => config ?? throw new InvalidOperationException("Configuration not loaded");

        private const int BOARD_SIZE = 8;
        // Removed fixed RECTIFIED_BOARD_SIZE - now using dynamic sizing based on detected board

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
            var (bestMove, evaluation, pvs, evaluations, completeFen) = result.Value;

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

            // Display analysis results (only show blunder warning if tracking is active)
            consoleFormatter?.DisplayAnalysisResults(
                bestMove, evaluation, pvs, evaluations, completeFen,
                previousEval,
                config?.ShowSecondLine == true,
                config?.ShowThirdLine == true);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == 1)
                {
                    // Alt+X - Manual analysis
                    button1.PerformClick();
                }
                else if (hotkeyId == 2)
                {
                    // Alt+K - Toggle auto-monitoring on/off
                    ToggleAutoMonitoring();
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
                status => this.Invoke((MethodInvoker)(() => labelStatus.Text = status)));

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
                // Reload config and apply changes
                config = AppConfig.Load();

                // Apply explanation settings
                ExplanationFormatter.LoadFromConfig(config);

                // Apply theme changes
                this.Invoke((MethodInvoker)(() =>
                {
                    ApplyTheme(config.Theme == "Dark");
                }));

                // Handle auto-monitor toggle
                if (config.AutoMonitorBoard && boardMonitorService?.IsMonitoring() != true)
                {
                    bool userIsWhite = chkWhiteTurn.Checked;
                    boardMonitorService?.StartMonitoring(userIsWhite);
                    blunderTracker.StartTracking();
                }
                else if (!config.AutoMonitorBoard && boardMonitorService?.IsMonitoring() == true)
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

        private async void button1_Click_1(object sender, EventArgs e)
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
                Debug.WriteLine($"Error in button1_Click_1: {ex.Message}");
                labelStatus.Text = $"Error: {ex.Message}";
                engineRestartManager.RecordFailure();
            }
            finally
            {
                // ALWAYS reset the flag so button can be clicked again
                moveTimeoutTimer?.Stop();
                moveInProgress = false;

                // Resume auto-monitor after manual analysis
                boardMonitorService?.ResumeScanning();
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

            ChessBoard currentBoard = pieceRecognitionService.ExtractBoardFromMat(boardMat, blackAtBottom, Config.MatchThreshold);

            if (positionStateManager.LastDetectedBoard != null && ChessRulesService.CountBoardDifferences(positionStateManager.LastDetectedBoard, currentBoard) >= 2)
            {
                manualTimer?.Stop();
                positionStateManager.SaveMoveState(currentBoard);
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            ThemeService.ApplyTheme(this, labelStatus, button1, buttonReset, buttonSettings, richTextBoxConsole, chkWhiteTurn, isDarkMode);
        }
    }
}