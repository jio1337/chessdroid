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

            // Track board changes and display results
            blunderTracker.UpdateBoardChangeTracking(completeFen, evaluation);

            // Display analysis results
            consoleFormatter?.DisplayAnalysisResults(
                bestMove, evaluation, pvs, evaluations, completeFen,
                blunderTracker.GetPreviousEvaluation(),
                config?.ShowSecondLine == true,
                config?.ShowThirdLine == true);

            // Update blunder tracker with current evaluation
            double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
            blunderTracker.SetPreviousEvaluation(currentEval);
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

            LoadTemplatesAndMasks();
        }

        private void LoadTemplatesAndMasks()
        {
            // Use selected site from config
            string selectedSite = config?.SelectedSite ?? "Lichess";
            pieceRecognitionService.LoadTemplatesAndMasks(selectedSite, Config);
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
                moveOrchestrator?.ClearCache();
                blunderTracker.Reset();
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