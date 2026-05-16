using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    /// <summary>
    /// Offline analysis board form with interactive chess board, move list, and engine analysis.
    /// Provides a complete analysis experience without needing internet connection.
    /// Supports variations/alternative lines through a move tree structure.
    /// </summary>
    public partial class AnalysisBoardForm : Form
    {
        // Cached regex patterns for PGN parsing (compiled for performance)
        private static readonly Regex PgnCommentRegex = new(@"\{[^}]*\}", RegexOptions.Compiled);
        private static readonly Regex PgnVariationRegex = new(@"\([^)]*\)", RegexOptions.Compiled);
        private static readonly Regex PgnNagRegex = new(@"\$\d+", RegexOptions.Compiled);
        private static readonly Regex PgnContinuationRegex = new(@"[0-9]+\.\.\.", RegexOptions.Compiled);
        private static readonly Regex PgnMoveNumberRegex = new(@"^\d+\.?$", RegexOptions.Compiled);
        private static readonly Regex PgnAttachedMoveRegex = new(@"^\d+\.(.+)$", RegexOptions.Compiled);
        private static readonly Regex PgnEvalCommentRegex = new(@"\[([+\-]?\d+[.,]?\d*)\]", RegexOptions.Compiled);

        /// <summary>
        /// Cached engine analysis result for a position.
        /// </summary>
        private class CachedAnalysis
        {
            public string BestMove { get; set; } = "";
            public string Evaluation { get; set; } = "";
            public List<string> PVs { get; set; } = new();
            public List<string> Evaluations { get; set; } = new();
            public WDLInfo? WDL { get; set; }
            public int Depth { get; set; }
        }

        // Analysis cache - keyed by FEN (position only, not full FEN with move counters)
        // Thread-safe for concurrent async access
        private ConcurrentDictionary<string, CachedAnalysis> _analysisCache = new();
        private int _cachedDepth = 0; // Track the depth used for cached analyses

        // Classification lookup for O(1) DrawItem color lookups
        private Dictionary<MoveNode, MoveReviewResult>? _classificationLookup;

        // Auto-analysis
        private CancellationTokenSource? autoAnalysisCts;
        private CancellationTokenSource? _pvAnimationCts;

        // Services
        private ChessEngineService? engineService;
        private MoveSharpnessAnalyzer sharpnessAnalyzer;
        private ConsoleOutputFormatter? consoleFormatter;
        private PolyglotBookService? openingBookService;
        private AppConfig config;

        // Game state - move tree for variations support
        private MoveTree moveTree = null!;
        private bool isNavigating = false;

        // Track nodes for listbox mapping
        private List<MoveNode> displayedNodes = new List<MoveNode>();

        // Engine match
        private EngineMatchService? matchService;
        private System.Windows.Forms.Timer clockTimer = null!;
        private bool matchRunning = false;
        private double? _previousMatchEval; // Track previous eval for brilliant move detection
        private bool _awaitingMatchAnimation = false;

        // Bot mode
        private bool _botModeActive = false;
        private BotSettings? _botSettings;
        private ChessEngineService? _botEngine;
        private CancellationTokenSource? _botMoveCts;
        private AppConfig? _challengeSnapshot; // non-null while challenge mode is active
        private bool _bookArrowsActive = false; // true when book arrows are shown (suppress engine arrows)
        private System.Windows.Forms.Timer _autoPlayTimer = null!;
        private bool _autoPlaying = false;

        // Console font (managed here to allow proper disposal on change)
        private Font _consoleFont = new Font("Consolas", 10f);

        private EvalGraphControl? _evalGraph;

        public AnalysisBoardForm(AppConfig config, ChessEngineService? sharedEngineService = null)
        {
            this.config = config;
            this.engineService = sharedEngineService ?? new ChessEngineService(config);
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();

            InitializeComponent();

            _evalGraph = new EvalGraphControl { Dock = DockStyle.Top, Height = 65, Name = "evalGraph" };
            _evalGraph.MoveNodeSelected += EvalGraph_MoveNodeSelected;
            rightPanel.Controls.Add(_evalGraph);
            // z-order: lblAnalysis(top) → evalGraph → grpEngineMatch → analysisOutput(fill)
            rightPanel.Controls.SetChildIndex(_evalGraph, rightPanel.Controls.IndexOf(grpEngineMatch));

            ApplyTheme();
            boardControl.SetSquareColors(
                ColorTranslator.FromHtml(config.LightSquareColor),
                ColorTranslator.FromHtml(config.DarkSquareColor));
            boardControl.ShowSquareLabels = config.ShowSquareLabels;
            boardControl.ShowLastMoveHighlight = config.ShowLastMoveHighlight;
            boardControl.AnimationDurationMs = config.AnimationDurationMs;
            InitializeServices();
            InitializeMatchControls();
            PopulatePiecesComboBox();

            // Initialize move tree with starting position
            moveTree = new MoveTree(boardControl.GetFEN());

            // Shown fires after TableLayoutPanel finalizes its layout — recalculate positions then
            this.Shown += async (s, e) =>
            {
                LeftPanel_Resize(leftPanel, EventArgs.Empty);
                PnlBoardControls_Resize(pnlBoardControls, EventArgs.Empty);
                this.MinimumSize = this.Size; // initial size becomes the enforced minimum
                await InitializeEngineAsync();
            };
        }

        private async Task InitializeEngineAsync()
        {
            if (string.IsNullOrEmpty(config?.SelectedEngine))
            {
                lblStatus.Text = "No engine configured - click ⚙ to set up";
                return;
            }

            try
            {
                lblStatus.Text = "Starting engine...";
                // Build full path: Engines folder + selected engine filename
                string enginePath = Path.Combine(config.GetEnginesPath(), config.SelectedEngine);
                await engineService!.InitializeAsync(enginePath);
                lblStatus.Text = "Engine ready";
                _ = TriggerAutoAnalysis();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Engine failed: {ex.Message}";
                Debug.WriteLine($"Engine init failed: {ex}");
            }
        }

        private void ApplyTheme()
        {
            bool isDarkMode = config?.Theme == "Dark";
            var scheme = ThemeService.GetColorScheme(isDarkMode);

            // Form colors
            this.BackColor = scheme.FormBackColor;
            pnlBoardControls.BackColor = scheme.FormBackColor;

            // Labels
            lblTurn.ForeColor = scheme.TextColor;
            lblFen.ForeColor = scheme.TextColor;
            lblStatus.ForeColor = scheme.StatusColor;
            lblMoves.ForeColor = scheme.TextColor;
            lblAnalysis.ForeColor = scheme.TextColor;
            lblPieces.ForeColor = scheme.TextColor;

            // Pieces combobox
            cmbPieces.BackColor = scheme.ButtonBackColor;
            cmbPieces.ForeColor = scheme.TextColor;

            // Standard buttons
            foreach (var btn in new[] { btnSettings, btnNewGame, btnFlipBoard, btnTakeBack, btnPrevMove,
                                        btnNextMove, btnAutoPlay, btnPlayBot, btnEditPosition, btnLoadFen, btnCopyFen, btnClassifyMoves,
                                        btnExportPgn, btnImportPgn })
            {
                btn.BackColor = scheme.ButtonBackColor;
                btn.ForeColor = scheme.ButtonForeColor;
            }

            // Checkboxes
            chkFromPosition.ForeColor = scheme.TextColor;

            // TextBox and ListBox
            txtFen.BackColor = scheme.PanelColor;
            txtFen.ForeColor = scheme.TextColor;
            moveListBox.BackColor = scheme.PanelColor;
            moveListBox.ForeColor = scheme.TextColor;

            // RichTextBox
            analysisOutput.BackColor = scheme.PanelColor;
            analysisOutput.ForeColor = scheme.TextColor;

            // Engine Match controls
            grpEngineMatch.ForeColor = scheme.TextColor;
            grpEngineMatch.BackColor = scheme.GroupBoxBackColor;

            foreach (var lbl in new[] { lblWhiteEngine, lblBlackEngine, lblTimeControl,
                                        lblDepth, lblMoveTime, lblTotalTime, lblIncrement })
            {
                lbl.ForeColor = scheme.TextColor;
            }

            foreach (var cmb in new[] { cmbWhiteEngine, cmbBlackEngine, cmbTimeControlType })
            {
                cmb.BackColor = scheme.PanelColor;
                cmb.ForeColor = scheme.TextColor;
            }

            pnlTimeParams.BackColor = scheme.GroupBoxBackColor;

            foreach (var num in new[] { numDepth, numMoveTime, numTotalTime, numIncrement })
            {
                num.BackColor = scheme.PanelColor;
                num.ForeColor = scheme.TextColor;
            }

            // Clock labels
            lblWhiteClock.ForeColor = scheme.WhiteClockForeColor;
            lblWhiteClock.BackColor = scheme.ClockBackColor;
            lblBlackClock.ForeColor = scheme.BlackClockForeColor;
            lblBlackClock.BackColor = scheme.ClockBackColor;

            // Match control buttons
            btnStartMatch.BackColor = scheme.StartMatchButtonBackColor;
            btnStartMatch.ForeColor = Color.White;
            btnStopMatch.BackColor = scheme.StopMatchButtonBackColor;
            btnStopMatch.ForeColor = Color.White;

            // Material strips text color
            Color stripTextColor = isDarkMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(70, 70, 70);
            _materialTop?.SetTextColor(stripTextColor);
            _materialBottom?.SetTextColor(stripTextColor);

            // Update FEN display
            UpdateFenDisplay();

            if (_evalGraph != null) _evalGraph.Visible = config?.ShowEvalGraph ?? true;
            RefreshEvalGraph();
            ApplyConsoleFont();
        }

        private void ApplyConsoleFont()
        {
            string family = config?.ConsoleFontFamily ?? "Consolas";
            float size = config?.ConsoleFontSize ?? 10.0f;

            var oldFont = _consoleFont;
            _consoleFont = new Font(family, size);

            analysisOutput.Font = _consoleFont;
            moveListBox.Font = _consoleFont;
            moveListBox.ItemHeight = Math.Max(14, (int)Math.Ceiling(_consoleFont.GetHeight()));

            // Defer disposal so any in-flight WM_DRAWITEM messages finish before the font is freed.
            // If the handle doesn't exist yet (startup), dispose immediately — no draw messages possible.
            if (IsHandleCreated)
                BeginInvoke(() => oldFont?.Dispose());
            else
                oldFont?.Dispose();
        }

        private void InitializeServices()
        {
            // Initialize console formatter for analysis output
            consoleFormatter = new ConsoleOutputFormatter(
                analysisOutput,
                config,
                MovesExplanation.GenerateMoveExplanation);
            consoleFormatter.OnSeeLineClicked += InsertPvIntoMoveTree;

            // Initialize opening book service
            openingBookService = new PolyglotBookService();
            if (config?.UseOpeningBook == true && !string.IsNullOrEmpty(config.OpeningBooksFolder))
            {
                string booksPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.OpeningBooksFolder);
                if (Directory.Exists(booksPath))
                {
                    openingBookService.LoadBooksFromFolder(booksPath);
                }
            }
        }

        private void PopulatePiecesComboBox()
        {
            try
            {
                string templatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                cmbPieces.Items.Clear();

                if (Directory.Exists(templatesFolder))
                {
                    string[] siteFolders = Directory.GetDirectories(templatesFolder);
                    foreach (string folder in siteFolders)
                    {
                        cmbPieces.Items.Add(Path.GetFileName(folder));
                    }
                }

                // Select configured site or default to first available
                string? templateToUse = null;
                if (!string.IsNullOrEmpty(config?.SelectedSite) && cmbPieces.Items.Contains(config.SelectedSite))
                {
                    cmbPieces.SelectedItem = config.SelectedSite;
                    templateToUse = config.SelectedSite;
                }
                else if (cmbPieces.Items.Count > 0)
                {
                    cmbPieces.SelectedIndex = 0;
                    templateToUse = cmbPieces.Items[0]?.ToString();
                }

                // Explicitly update the board (event might not fire during init)
                if (!string.IsNullOrEmpty(templateToUse))
                {
                    boardControl.SetTemplateSet(templateToUse);
                    _materialTop?.SetTemplateSet(templateToUse);
                    _materialBottom?.SetTemplateSet(templateToUse);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading piece templates: {ex.Message}");
            }
        }

        private void CmbPieces_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedTemplate = cmbPieces.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedTemplate))
            {
                boardControl.SetTemplateSet(selectedTemplate);
                _materialTop?.SetTemplateSet(selectedTemplate);
                _materialBottom?.SetTemplateSet(selectedTemplate);

                // Remember the selection for next time
                if (config != null && config.SelectedSite != selectedTemplate)
                {
                    config.SelectedSite = selectedTemplate;
                    config.Save();
                }
            }
        }

        private void InitializeMatchControls()
        {
            // Populate engine combo boxes
            var resolver = new EnginePathResolver(config);
            string[] engines = resolver.GetAvailableEngines();
            cmbWhiteEngine.Items.Clear();
            cmbBlackEngine.Items.Clear();
            foreach (var eng in engines)
            {
                cmbWhiteEngine.Items.Add(eng);
                cmbBlackEngine.Items.Add(eng);
            }
            if (engines.Length > 0) cmbWhiteEngine.SelectedIndex = 0;
            if (engines.Length > 1) cmbBlackEngine.SelectedIndex = 1;
            else if (engines.Length > 0) cmbBlackEngine.SelectedIndex = 0;

            // Default time control selection
            cmbTimeControlType.SelectedIndex = 0; // Fixed Depth
            UpdateTimeControlParams();

            // Initialize clock timer
            clockTimer = new System.Windows.Forms.Timer();
            clockTimer.Interval = 100;
            clockTimer.Tick += ClockTimer_Tick;

            _autoPlayTimer = new System.Windows.Forms.Timer();
            _autoPlayTimer.Interval = 400;
            _autoPlayTimer.Tick += AutoPlayTimer_Tick;

            // Trigger initial layout for responsive checkbox positioning
            GrpEngineMatch_Resize(grpEngineMatch, EventArgs.Empty);
        }

        #region Event Handlers

        private void LeftPanel_Resize(object? sender, EventArgs e)
        {
            // Guard against resize during initialization
            if (boardControl == null || _materialTop == null || _materialBottom == null)
                return;

            if (sender is Panel panel)
            {
                // Eval bar dimensions
                const int evalBarWidth = 24;
                const int evalBarGap = 4;
                bool showEvalBar = config?.ShowEvalBar != false;
                evalBar.Visible = showEvalBar;
                int evalBarTotal = showEvalBar ? evalBarWidth + evalBarGap : 0;

                // Material strips above/below board
                bool showStrips = config?.ShowMaterialStrips != false;
                int STRIP_H = showStrips ? 22 : 0;
                _materialTop.Visible = showStrips;
                _materialBottom.Visible = showStrips;

                // Board is centered in leftPanel — use all available space
                int availableWidth = panel.Width - 20 - evalBarTotal;
                int availableHeight = panel.Height - 2 * STRIP_H;

                int boardSize = Math.Min(availableWidth, availableHeight);
                boardSize = Math.Max(boardSize, 300);

                // Center board+evalbar horizontally and vertically
                int groupWidth = boardSize + evalBarTotal;
                int groupX = Math.Max((panel.Width - groupWidth) / 2, 5);

                int topSpace = Math.Max(0, (panel.Height - boardSize - 2 * STRIP_H) / 2);

                int boardX = groupX + evalBarTotal;
                boardControl.Size = new Size(boardSize, boardSize);
                boardControl.Location = new Point(boardX, topSpace + STRIP_H);

                evalBar.Location = new Point(groupX, boardControl.Top);
                evalBar.Size = new Size(evalBarWidth, boardControl.Height);

                // Material strips hug the board top/bottom
                _materialTop.Location = new Point(boardX, topSpace);
                _materialTop.Size = new Size(boardSize, STRIP_H);
                _materialBottom.Location = new Point(boardX, boardControl.Bottom);
                _materialBottom.Size = new Size(boardSize, STRIP_H);
            }
        }

        private void PnlBoardControls_Resize(object? sender, EventArgs e)
        {
            if (lblTurn == null || pnlBoardControls == null) return;

            int w = pnlBoardControls.Width;
            const int pad = 4;
            const int gap = 4;

            // Row 1 (Y=3): "White to move" label left, Pieces selector right
            lblTurn.Location = new Point(pad, 3);
            cmbPieces.Width = 95;
            cmbPieces.Location = new Point(w - cmbPieces.Width - pad, 0);
            lblPieces.Location = new Point(cmbPieces.Left - lblPieces.Width - gap, 3);

            // Row 2 (Y=28): icon buttons — all fixed widths, no dynamic scaling
            const int buttonY = 28;
            const int iconW  = 30;  // ⊕ ⇅ ↩ ♞
            const int navW   = 35;  // ◀ ▶
            const int autoW  = 38;  // >>
            const int editW  = 28;  // ✏

            btnNewGame.Width    = iconW;
            btnFlipBoard.Width  = iconW;
            btnTakeBack.Width   = iconW;
            btnPrevMove.Width   = navW;
            btnNextMove.Width   = navW;
            btnAutoPlay.Width   = autoW;
            btnPlayBot.Width    = iconW;
            btnEditPosition.Width = editW;

            btnNewGame.Location    = new Point(pad, buttonY);
            btnFlipBoard.Location  = new Point(btnNewGame.Right   + gap, buttonY);
            btnTakeBack.Location   = new Point(btnFlipBoard.Right + gap, buttonY);
            btnPrevMove.Location   = new Point(btnTakeBack.Right  + gap, buttonY);
            btnNextMove.Location   = new Point(btnPrevMove.Right  + 2,   buttonY);
            btnAutoPlay.Location   = new Point(btnNextMove.Right  + 2,   buttonY);
            btnPlayBot.Location    = new Point(btnAutoPlay.Right  + gap, buttonY);
            btnEditPosition.Location = new Point(btnPlayBot.Right + gap, buttonY);

            // Row 3 (Y=60): FEN row — label | input | Load | Copy | ⚙
            const int fenY     = 60;
            const int fenLblW  = 35;
            const int fenBtnW  = 50;
            const int settingsW = 28;
            int inputW = Math.Max(60, w - pad - fenLblW - 2 * fenBtnW - settingsW - 4 * gap);

            lblFen.Location   = new Point(pad, fenY + 3);
            txtFen.Location   = new Point(pad + fenLblW, fenY);
            txtFen.Width      = inputW;
            btnLoadFen.Location  = new Point(txtFen.Right + gap, fenY);
            btnLoadFen.Width     = fenBtnW;
            btnCopyFen.Location  = new Point(btnLoadFen.Right + gap, fenY);
            btnCopyFen.Width     = fenBtnW;
            btnSettings.Location = new Point(btnCopyFen.Right + gap, fenY);

            // Row 4 (Y=88): Status text
            lblStatus.Location = new Point(pad, 88);
            lblStatus.Width    = w - 2 * pad;
        }

        private void GrpEngineMatch_Resize(object? sender, EventArgs e)
        {
            // Checkbox is now at fixed position below buttons - no dynamic repositioning needed
            // This handler is kept for potential future responsive adjustments
        }

        private void BoardControl_MoveMade(object? sender, MoveEventArgs e)
        {
            // Cancel any PV animation in progress
            _pvAnimationCts?.Cancel();

            // Clear engine arrows and threat arrows (new analysis will redraw them)
            boardControl.ClearEngineArrows();
            boardControl.ClearThreatArrows();

            // Skip if we're navigating (not making a new move)
            if (isNavigating) return;

            // Convert UCI to SAN for display
            string san = ConvertUciToSan(e.UciMove, moveTree.CurrentNode.FEN);

            // Add move to tree (handles variations automatically)
            moveTree.AddMove(e.UciMove, san, e.FEN);

            UpdateMoveAnnotation(moveTree.CurrentNode);

            // Update move list display — guard with isNavigating so the SelectedIndexChanged
            // event fired by UpdateMoveListSelection() doesn't trigger a spurious analysis.
            isNavigating = true;
            try { UpdateMoveList(); }
            finally { isNavigating = false; }
            UpdateFenDisplay();
            UpdateTurnLabel();
            UpdateMaterialStrips();

            // Auto-analyze if enabled (skip in bot mode — analysis runs after bot responds)
            if (!matchRunning && !_botModeActive)
            {
                _ = TriggerAutoAnalysis();
            }

            // Bot mode: trigger bot's response after user's move
            if (_botModeActive && !matchRunning)
            {
                _ = MakeBotMoveAsync();
            }
        }

        private async Task TriggerAutoAnalysis()
        {
            if (_autoPlaying) return;

            // Cancel previous analysis if still running
            autoAnalysisCts?.Cancel();
            autoAnalysisCts = new CancellationTokenSource();
            var token = autoAnalysisCts.Token;

            // Debounce — pass token so rapid navigation cancels the sleep immediately
            try { await Task.Delay(150, token); }
            catch (OperationCanceledException) { return; }

            if (!token.IsCancellationRequested)
            {
                await AnalyzeCurrentPosition(token);
            }
        }

        private void BoardControl_BoardChanged(object? sender, EventArgs e)
        {
            UpdateFenDisplay();
            UpdateTurnLabel();
            UpdateMaterialStrips();
        }

        private void BtnNewGame_Click(object? sender, EventArgs e)
        {
            CancelClassification();
            StopAutoPlay();
            if (_botModeActive) StopBotMode();
            boardControl.ClearEngineArrows();
            boardControl.ClearMoveAnnotation();
            boardControl.ResetBoard();
            moveTree.Clear(boardControl.GetFEN());
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            analysisOutput.Clear();
            evalBar?.Reset();
            _analysisCache.Clear(); // Clear analysis cache for new game
            _currentClassification = null;
            _classificationLookup = null;
            UpdateFenDisplay();
            UpdateTurnLabel();
            lblStatus.Text = "New game started";
            _ = TriggerAutoAnalysis();
        }


        private async void BtnSettings_Click(object? sender, EventArgs e)
        {
            // Snapshot analysis-relevant settings before the dialog modifies config
            string prevEngine = config.SelectedEngine;
            int prevDepth = config.EngineDepth;
            int prevMaxDepth = config.ContinuousAnalysisMaxDepth;
            bool prevPlayStyle = config.PlayStyleEnabled;
            int prevAggressiveness = config.Aggressiveness;
            string prevTheme = config.Theme;

            using var settingsForm = new SettingsForm(config);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                bool engineChanged = config.SelectedEngine != prevEngine;
                bool analysisSettingsChanged = engineChanged
                    || config.EngineDepth != prevDepth
                    || config.ContinuousAnalysisMaxDepth != prevMaxDepth
                    || config.PlayStyleEnabled != prevPlayStyle
                    || config.Aggressiveness != prevAggressiveness;

                if (engineChanged)
                {
                    engineService?.Dispose();
                    engineService = new ChessEngineService(config);
                }
                InitializeServices();
                ApplyTheme();
                if (config.Theme != prevTheme)
                {
                    consoleFormatter?.Clear();
                    _ = TriggerAutoAnalysis();
                }
                LeftPanel_Resize(leftPanel, EventArgs.Empty);
                boardControl.SetSquareColors(
                    ColorTranslator.FromHtml(config.LightSquareColor),
                    ColorTranslator.FromHtml(config.DarkSquareColor));
                boardControl.ShowSquareLabels = config.ShowSquareLabels;
                boardControl.ShowLastMoveHighlight = config.ShowLastMoveHighlight;
                if (!config.ShowThreatArrows) boardControl.ClearThreatArrows();
                if (_evalGraph != null) _evalGraph.Visible = config.ShowEvalGraph;
                boardControl.AnimationDurationMs = config.AnimationDurationMs;

                // Only clear cache when settings that affect analysis results change
                if (analysisSettingsChanged)
                    _analysisCache.Clear();

                if (engineChanged)
                    await InitializeEngineAsync();
            }
        }

        private void BtnFlipBoard_Click(object? sender, EventArgs e)
        {
            boardControl.FlipBoard();
            UpdateMaterialStrips();
        }

        private void BtnTakeBack_Click(object? sender, EventArgs e)
        {
            StopAutoPlay();
            // Cancel any pending bot move
            _botMoveCts?.Cancel();

            // In bot mode, take back 2 moves (bot's move + user's move)
            int movesToTakeBack = _botModeActive ? 2 : 1;

            for (int i = 0; i < movesToTakeBack; i++)
            {
                if (moveTree.CurrentNode != moveTree.Root)
                {
                    var parent = moveTree.CurrentNode.Parent;
                    if (parent != null)
                    {
                        parent.Children.Remove(moveTree.CurrentNode);
                        moveTree.CurrentNode = parent;
                    }
                }
            }

            // Load the position we landed on
            boardControl.LoadFEN(moveTree.CurrentNode.FEN);
            SetLastMoveHighlight();
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();
            boardControl.InteractionEnabled = true;
            lblStatus.Text = _botModeActive ? "Your turn — move taken back" : "Move taken back";
        }

        private void BtnPrevMove_Click(object? sender, EventArgs e)
        {
            StopAutoPlay();
            _pvAnimationCts?.Cancel();
            if (moveTree.GoBack())
            {
                isNavigating = true;
                try
                {
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                    SetLastMoveHighlight();
                    UpdateMoveAnnotation(moveTree.CurrentNode);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    string statusText = moveTree.CurrentNode == moveTree.Root
                        ? "Start position"
                        : $"Move {moveTree.CurrentNode.MoveNumber}";
                    if (moveTree.CurrentNode.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

                    // Auto-analyze if enabled
                    if (!matchRunning)
                    {
                        _ = TriggerAutoAnalysis();
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private void BtnNextMove_Click(object? sender, EventArgs e)
        {
            _pvAnimationCts?.Cancel();
            if (moveTree.GoForward())
            {
                isNavigating = true;
                try
                {
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                    SetLastMoveHighlight();
                    if (config?.ShowAnimations == true && !string.IsNullOrEmpty(moveTree.CurrentNode.UciMove))
                        boardControl.StartAnimation(moveTree.CurrentNode.UciMove);
                    UpdateMoveAnnotation(moveTree.CurrentNode);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    string statusText = $"Move {moveTree.CurrentNode.MoveNumber}";
                    if (moveTree.CurrentNode.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

                    // Auto-analyze if enabled
                    if (!matchRunning)
                    {
                        _ = TriggerAutoAnalysis();
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private void BtnAutoPlay_Click(object? sender, EventArgs e)
        {
            if (_autoPlaying)
                StopAutoPlay();
            else
                StartAutoPlay();
        }

        private void StartAutoPlay()
        {
            if (moveTree.CurrentNode.Next() == null) return; // already at end
            _autoPlaying = true;
            _autoPlayTimer.Interval = config.AutoPlayInterval;
            btnAutoPlay.Text = "||";
            autoAnalysisCts?.Cancel(); // cancel any in-flight analysis
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            _autoPlayTimer.Start();
        }

        private void StopAutoPlay()
        {
            _autoPlaying = false;
            _autoPlayTimer.Stop();
            btnAutoPlay.Text = ">>";
            if (!matchRunning)
                _ = TriggerAutoAnalysis(); // analyze the position we landed on
        }

        private void AutoPlayTimer_Tick(object? sender, EventArgs e)
        {
            if (moveTree.CurrentNode.Next() == null)
            {
                StopAutoPlay();
                return;
            }
            BtnNextMove_Click(this, EventArgs.Empty);
        }

        private void BtnLoadFen_Click(object? sender, EventArgs e)
        {
            string fen = txtFen.Text.Trim();
            if (!string.IsNullOrEmpty(fen))
            {
                CancelClassification();
                try
                {
                    boardControl.LoadFEN(fen);
                    moveTree.Clear(fen);
                    moveListBox.Items.Clear();
                    displayedNodes.Clear();
                    analysisOutput.Clear();
                    evalBar?.Reset();
                    _analysisCache.Clear(); // Clear analysis cache for new position
                    UpdateTurnLabel();
                    lblStatus.Text = "Position loaded from FEN";
                    _ = TriggerAutoAnalysis();
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Invalid FEN: {ex.Message}";
                }
            }
        }

        private void LoadFenIntoBoard(string fen)
        {
            CancelClassification();
            try
            {
                boardControl.LoadFEN(fen);
                moveTree.Clear(fen);
                moveListBox.Items.Clear();
                displayedNodes.Clear();
                analysisOutput.Clear();
                evalBar?.Reset();
                _analysisCache.Clear();
                UpdateTurnLabel();
                UpdateFenDisplay();
                lblStatus.Text = "Position loaded";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Invalid position: {ex.Message}";
            }
        }

        private async void BtnEditPosition_Click(object? sender, EventArgs e)
        {
            if (matchRunning || _botModeActive)
            {
                lblStatus.Text = "Stop current mode before editing position";
                return;
            }

            string currentFen = boardControl.GetFEN();
            bool isDark = config?.Theme == "Dark";
            string templateSet = config?.SelectedSite ?? "Lichess";
            string templatesPath = config?.GetTemplatesPath() ?? "Templates";

            using var editor = new PositionEditorForm(currentFen, templateSet, isDark, templatesPath);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                // Cancel any in-progress analysis
                autoAnalysisCts?.Cancel();

                LoadFenIntoBoard(editor.ResultFen);

                // Restart engine clean — guarantees fresh state for any custom position
                if (engineService != null)
                {
                    lblStatus.Text = "Restarting engine...";
                    await engineService.RestartAsync();
                }

                _ = TriggerAutoAnalysis();
            }
        }

        private void BtnCopyFen_Click(object? sender, EventArgs e)
        {
            string fen = boardControl.GetFEN();
            Clipboard.SetText(fen);
            lblStatus.Text = "FEN copied to clipboard";
        }

        private void MoveListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= moveListBox.Items.Count) return;
            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) return;

            // Draw background manually so the color is always our theme color,
            // not whatever the system might have cached.
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var bgBrush = new SolidBrush(isSelected ? SystemColors.Highlight : moveListBox.BackColor))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Always use the control's live Font property — e.Font can be a stale
            // reference to a previously-disposed font object.
            Font drawFont = moveListBox.Font;
            string text = moveListBox.Items[e.Index]?.ToString() ?? "";
            bool isDark = config?.Theme == "Dark";

            // Check if text contains a classification symbol
            string moveText = text;
            string symbol = "";
            Color symbolColor = e.ForeColor;

            // Extract symbol from the end of the text (using centralized theme colors)
            if (text.EndsWith("!!"))
            {
                symbol = "!!";
                moveText = text[..^2].TrimEnd();
                symbolColor = ColorScheme.BrilliantColor;
            }
            else if (text.EndsWith("??"))
            {
                symbol = "??";
                moveText = text[..^2].TrimEnd();
                symbolColor = ColorScheme.BlunderColor;
            }
            else if (text.EndsWith("?!"))
            {
                symbol = "?!";
                moveText = text[..^2].TrimEnd();
                symbolColor = isDark ? ColorScheme.InaccuracyColor : Color.DarkGoldenrod;
            }
            else if (text.EndsWith("?") && !text.EndsWith("??") && !text.EndsWith("?!"))
            {
                symbol = "?";
                moveText = text[..^1].TrimEnd();
                symbolColor = isDark ? ColorScheme.MistakeColor : Color.Chocolate;
            }
            else if (text.EndsWith("!") && !text.EndsWith("!!"))
            {
                symbol = "!";
                moveText = text[..^1].TrimEnd();
                symbolColor = ColorScheme.OnlyMoveColor;
            }

            // Determine text color based on selection and theme
            Color textColor = isSelected
                ? (isDark ? Color.White : SystemColors.HighlightText)
                : (isDark ? Color.White : Color.Black);

            // Color the whole move text to match the symbol for annotated moves
            if (!isSelected && !string.IsNullOrEmpty(symbol))
            {
                textColor = symbolColor;
            }

            // Color move text based on classification quality (Best/Excellent/Good)
            if (!isSelected && string.IsNullOrEmpty(symbol) &&
                _classificationLookup != null && e.Index < displayedNodes.Count &&
                _classificationLookup.TryGetValue(displayedNodes[e.Index], out var classResult))
            {
                switch (classResult.Quality)
                {
                    case MoveQualityAnalyzer.MoveQuality.Best:
                        textColor = isDark ? ColorScheme.BestMoveColor : Color.ForestGreen;
                        break;
                    case MoveQualityAnalyzer.MoveQuality.Excellent:
                        textColor = isDark ? ColorScheme.ExcellentMoveColor : Color.SeaGreen;
                        break;
                    case MoveQualityAnalyzer.MoveQuality.Good:
                        textColor = isDark ? ColorScheme.GoodMoveColor : Color.OliveDrab;
                        break;
                }
            }

            try
            {
                // Draw move text
                using (var brush = new SolidBrush(textColor))
                {
                    e.Graphics.DrawString(moveText, drawFont, brush, e.Bounds.Left + 2, e.Bounds.Top + 1);
                }

                // Draw symbol in color if present
                if (!string.IsNullOrEmpty(symbol))
                {
                    var moveSize = e.Graphics.MeasureString(moveText + " ", drawFont);

                    using (var symbolBrush = new SolidBrush(symbolColor))
                    using (var boldFont = new Font(drawFont.FontFamily, drawFont.Size, FontStyle.Bold))
                    {
                        e.Graphics.DrawString(symbol, boldFont, symbolBrush,
                            e.Bounds.Left + 2 + moveSize.Width - 4, e.Bounds.Top + 1);
                    }
                }

                // Draw focus rectangle if focused
                e.DrawFocusRectangle();
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException or ObjectDisposedException)
            {
                // GDI resource in a bad state — recreate font from config strings (cannot be
                // externally disposed), preserve symbol colors, and draw symbol separately.
                try
                {
                    string safeFamily = config?.ConsoleFontFamily ?? "Consolas";
                    float safeSize = config?.ConsoleFontSize ?? 10f;
                    using var safeFont = new Font(safeFamily, safeSize);
                    Color safeTextColor = isSelected
                        ? (isDark ? Color.White : SystemColors.HighlightText)
                        : (!string.IsNullOrEmpty(symbol) ? symbolColor : GetQualityColor(e.Index, isDark));

                    using (var safeBrush = new SolidBrush(safeTextColor))
                        e.Graphics.DrawString(moveText, safeFont, safeBrush,
                            e.Bounds.Left + 2, e.Bounds.Top + 1);
                    if (!string.IsNullOrEmpty(symbol))
                    {
                        var sz = e.Graphics.MeasureString(moveText + " ", safeFont);
                        using var symBrush = new SolidBrush(symbolColor);
                        using var boldFont = new Font(safeFont.FontFamily, safeSize, FontStyle.Bold);
                        e.Graphics.DrawString(symbol, boldFont, symBrush,
                            e.Bounds.Left + 2 + sz.Width - 4, e.Bounds.Top + 1);
                    }
                }
                catch { }
            }
        }

        private Color GetQualityColor(int index, bool isDark)
        {
            if (_classificationLookup != null && index < displayedNodes.Count &&
                _classificationLookup.TryGetValue(displayedNodes[index], out var classResult))
            {
                return classResult.Quality switch
                {
                    MoveQualityAnalyzer.MoveQuality.Best => isDark ? ColorScheme.BestMoveColor : Color.ForestGreen,
                    MoveQualityAnalyzer.MoveQuality.Excellent => isDark ? ColorScheme.ExcellentMoveColor : Color.SeaGreen,
                    MoveQualityAnalyzer.MoveQuality.Good => isDark ? ColorScheme.GoodMoveColor : Color.OliveDrab,
                    _ => isDark ? Color.White : Color.Black
                };
            }
            return isDark ? Color.White : Color.Black;
        }

        private void MoveListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Skip if we're already navigating (triggered by UpdateMoveListSelection)
            if (isNavigating) return;

            int selected = moveListBox.SelectedIndex;
            if (selected >= 0 && selected < displayedNodes.Count)
            {
                isNavigating = true;
                try
                {
                    var node = displayedNodes[selected];
                    moveTree.GoToNode(node);
                    boardControl.LoadFEN(node.FEN);
                    SetLastMoveHighlight();
                    UpdateMoveAnnotation(node);
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    string statusText = $"Move {node.MoveNumber}";
                    if (node.VariationDepth > 0)
                        statusText += $" (variation)";
                    lblStatus.Text = statusText;

                    // Auto-analyze if enabled
                    if (!matchRunning)
                    {
                        _ = TriggerAutoAnalysis();
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Disable navigation during engine match
            if (matchRunning) return base.ProcessCmdKey(ref msg, keyData);

            // Intercept arrow keys before they're used for control navigation
            switch (keyData)
            {
                case Keys.Left:
                    BtnPrevMove_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Right:
                    if (_autoPlaying) StopAutoPlay();
                    BtnNextMove_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Up:
                    // Navigate to previous variation at same level
                    NavigateVariation(-1);
                    return true;
                case Keys.Down:
                    // Navigate to next variation at same level
                    NavigateVariation(1);
                    return true;
                case Keys.Home:
                    NavigateToStart();
                    return true;
                case Keys.End:
                    NavigateToEnd();
                    return true;
                case Keys.N | Keys.Control:
                    BtnNewGame_Click(this, EventArgs.Empty);
                    return true;
                case Keys.F | Keys.Control:
                    BtnFlipBoard_Click(this, EventArgs.Empty);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SetLastMoveHighlight()
        {
            if (!config.ShowLastMoveHighlight) { boardControl.LastMove = null; return; }
            var node = moveTree.CurrentNode;
            if (string.IsNullOrEmpty(node.UciMove) || node.UciMove.Length < 4) { boardControl.LastMove = null; return; }
            int fromCol = node.UciMove[0] - 'a';
            int fromRow = 7 - (node.UciMove[1] - '1');
            int toCol   = node.UciMove[2] - 'a';
            int toRow   = 7 - (node.UciMove[3] - '1');
            boardControl.LastMove = (fromRow, fromCol, toRow, toCol);
            UpdateMaterialStrips();
        }

        private void UpdateMaterialStrips()
        {
            if (_materialTop == null || _materialBottom == null) return;

            string fen = boardControl.GetFEN();
            string placement = fen.Contains(' ') ? fen[..fen.IndexOf(' ')] : fen;

            // Count pieces currently on the board
            var counts = new Dictionary<char, int>();
            foreach (char c in placement)
                if (char.IsLetter(c)) counts[c] = counts.GetValueOrDefault(c) + 1;

            static int PieceVal(char p) => char.ToLower(p) switch { 'q' => 9, 'r' => 5, 'b' => 3, 'n' => 3, 'p' => 1, _ => 0 };

            // Black pieces captured by white (lowercase chars)
            var whiteCaptured = new List<char>();
            foreach (var (p, start) in new (char, int)[] { ('p', 8), ('n', 2), ('b', 2), ('r', 2), ('q', 1) })
            {
                int gone = Math.Max(0, start - counts.GetValueOrDefault(p, 0));
                for (int i = 0; i < gone; i++) whiteCaptured.Add(p);
            }

            // White pieces captured by black (uppercase chars)
            var blackCaptured = new List<char>();
            foreach (var (p, start) in new (char, int)[] { ('P', 8), ('N', 2), ('B', 2), ('R', 2), ('Q', 1) })
            {
                int gone = Math.Max(0, start - counts.GetValueOrDefault(p, 0));
                for (int i = 0; i < gone; i++) blackCaptured.Add(p);
            }

            // Sort ascending (pawns first, queen last — chess.com convention)
            whiteCaptured.Sort((a, b) => PieceVal(a).CompareTo(PieceVal(b)));
            blackCaptured.Sort((a, b) => PieceVal(a).CompareTo(PieceVal(b)));

            int whiteGained = whiteCaptured.Sum(PieceVal);
            int blackGained = blackCaptured.Sum(PieceVal);
            int diff = whiteGained - blackGained; // >0 = white is winning materially

            // Top strip is near the side at the top of the board; bottom strip near the bottom side.
            // When unflipped: white at bottom → bottom strip = white's captures, top = black's captures.
            // When flipped: black at bottom → swap.
            if (!boardControl.IsFlipped)
            {
                _materialTop.UpdateMaterial(blackCaptured.ToArray(), diff < 0 ? -diff : 0);
                _materialBottom.UpdateMaterial(whiteCaptured.ToArray(), diff > 0 ? diff : 0);
            }
            else
            {
                _materialTop.UpdateMaterial(whiteCaptured.ToArray(), diff > 0 ? diff : 0);
                _materialBottom.UpdateMaterial(blackCaptured.ToArray(), diff < 0 ? -diff : 0);
            }
        }

        private void NavigateToStart()
        {
            if (_autoPlaying) StopAutoPlay();
            isNavigating = true;
            try
            {
                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                SetLastMoveHighlight();
                boardControl.ClearMoveAnnotation();
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMoveListSelection();
                lblStatus.Text = "Start position";

                // Auto-analyze if enabled
                if (!matchRunning)
                {
                    _ = TriggerAutoAnalysis();
                }
            }
            finally
            {
                isNavigating = false;
            }
        }

        private void NavigateToEnd()
        {
            if (_autoPlaying) StopAutoPlay();
            isNavigating = true;
            try
            {
                moveTree.GoToEnd();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                SetLastMoveHighlight();
                UpdateMoveAnnotation(moveTree.CurrentNode);
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMoveListSelection();

                string statusText = moveTree.CurrentNode == moveTree.Root
                    ? "Start position"
                    : $"Move {moveTree.CurrentNode.MoveNumber}";
                if (moveTree.CurrentNode.VariationDepth > 0)
                    statusText += $" (variation)";
                lblStatus.Text = statusText;

                // Auto-analyze if enabled
                if (!matchRunning)
                {
                    _ = TriggerAutoAnalysis();
                }
            }
            finally
            {
                isNavigating = false;
            }
        }

        private void NavigateVariation(int direction)
        {
            var current = moveTree.CurrentNode;
            MoveNode? target = direction > 0 ? current.NextVariation() : current.PreviousVariation();

            if (target != null)
            {
                isNavigating = true;
                try
                {
                    moveTree.GoToNode(target);
                    boardControl.LoadFEN(target.FEN);
                    SetLastMoveHighlight();
                    UpdateMoveAnnotation(target);
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    UpdateMoveListSelection();

                    int varIdx = target.GetVariationIndex();
                    lblStatus.Text = $"Move {target.MoveNumber} - Variation {varIdx + 1}";

                    // Auto-analyze if enabled
                    if (!matchRunning)
                    {
                        _ = TriggerAutoAnalysis();
                    }
                }
                finally
                {
                    isNavigating = false;
                }
            }
        }

        private void UpdateMoveAnnotation(MoveNode? node)
        {
            if (node == null || node == moveTree.Root ||
                _classificationLookup == null ||
                !_classificationLookup.TryGetValue(node, out var result) ||
                string.IsNullOrEmpty(result.Symbol))
            {
                boardControl.ClearMoveAnnotation();
                return;
            }

            // Decode destination square from UCI move (e.g. "e2e4" -> col=4, row=4)
            if (node.UciMove.Length >= 4)
            {
                int toCol = node.UciMove[2] - 'a';
                int toRow = 7 - (node.UciMove[3] - '1');
                boardControl.SetMoveAnnotation(result.Symbol, toRow, toCol);
            }
            else
            {
                boardControl.ClearMoveAnnotation();
            }
        }

        private void AnalysisBoardForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Arrow keys and Home/End are handled in ProcessCmdKey
            if (e.KeyCode == Keys.Back)
            {
                BtnTakeBack_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion

        #region Engine Match

        private void CmbTimeControlType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateTimeControlParams();
        }

        private void UpdateTimeControlParams()
        {
            int idx = cmbTimeControlType.SelectedIndex;

            // Fixed Depth
            lblDepth.Visible = idx == 0;
            numDepth.Visible = idx == 0;

            // Time per Move
            lblMoveTime.Visible = idx == 1;
            numMoveTime.Visible = idx == 1;

            // Total + Increment
            lblTotalTime.Visible = idx == 2;
            numTotalTime.Visible = idx == 2;
            lblIncrement.Visible = idx == 2;
            numIncrement.Visible = idx == 2;
        }

        private async void BtnStartMatch_Click(object? sender, EventArgs e)
        {
            if (_botModeActive)
            {
                lblStatus.Text = "Stop bot mode first";
                return;
            }

            if (cmbWhiteEngine.SelectedItem == null || cmbBlackEngine.SelectedItem == null)
            {
                lblStatus.Text = "Select both engines first";
                return;
            }

            string whiteEngineName = cmbWhiteEngine.SelectedItem.ToString()!;
            string blackEngineName = cmbBlackEngine.SelectedItem.ToString()!;

            // Resolve engine paths
            string enginesPath = config.GetEnginesPath();
            string whiteEnginePath = Path.Combine(enginesPath, whiteEngineName);
            string blackEnginePath = Path.Combine(enginesPath, blackEngineName);

            if (!File.Exists(whiteEnginePath) || !File.Exists(blackEnginePath))
            {
                lblStatus.Text = "Engine file not found";
                return;
            }

            // Build time control from UI
            var tc = new EngineMatchTimeControl();
            switch (cmbTimeControlType.SelectedIndex)
            {
                case 0: // Fixed Depth
                    tc.Type = TimeControlType.FixedDepth;
                    tc.Depth = (int)numDepth.Value;
                    break;
                case 1: // Time per Move
                    tc.Type = TimeControlType.FixedTimePerMove;
                    tc.MoveTimeMs = (int)numMoveTime.Value;
                    break;
                case 2: // Total + Increment
                    tc.Type = TimeControlType.TotalPlusIncrement;
                    tc.TotalTimeMs = (int)numTotalTime.Value * 1000;
                    tc.IncrementMs = (int)numIncrement.Value * 1000;
                    break;
            }

            // Get starting FEN - either current position or standard starting position
            string startFen;
            if (chkFromPosition.Checked)
            {
                // Use current board position
                startFen = boardControl.GetFEN();
            }
            else
            {
                // Reset to standard starting position
                boardControl.ResetBoard();
                startFen = boardControl.GetFEN();
            }
            CancelClassification();
            moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear(); // Clear analysis cache for new match
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;

            // Set up match log
            analysisOutput.Clear();
            analysisOutput.SelectionColor = analysisOutput.ForeColor;
            analysisOutput.AppendText($"Engine Match: {whiteEngineName} vs {blackEngineName}\n");
            analysisOutput.AppendText($"Time Control: {tc}\n");
            if (chkFromPosition.Checked)
            {
                analysisOutput.AppendText("Starting from custom position\n");
            }
            analysisOutput.AppendText("\n");

            // Disable conflicting controls
            SetMatchControlsEnabled(true);

            // Create and start match service
            matchService?.Dispose();
            matchService = new EngineMatchService(config);
            _previousMatchEval = null; // Reset for brilliant move detection
            matchService.OnMovePlayed += MatchService_OnMovePlayed;
            matchService.OnClockUpdated += MatchService_OnClockUpdated;
            matchService.OnMatchEnded += MatchService_OnMatchEnded;
            matchService.OnStatusChanged += MatchService_OnStatusChanged;
            matchService.WaitForAnimation = config.ShowAnimations;
            boardControl.AnimationCompleted += MatchBoard_AnimationCompleted;

            // Initialize clocks display
            if (tc.Type == TimeControlType.TotalPlusIncrement)
            {
                UpdateClockDisplay(tc.TotalTimeMs, tc.TotalTimeMs, true);
            }
            else
            {
                lblWhiteClock.Text = "W: --:--";
                lblBlackClock.Text = "B: --:--";
            }

            clockTimer.Start();

            // Run match (fire and forget - events handle the rest)
            await matchService.StartMatchAsync(
                whiteEnginePath, blackEnginePath,
                whiteEngineName, blackEngineName,
                tc,
                startFen);
        }

        private void BtnStopMatch_Click(object? sender, EventArgs e)
        {
            matchService?.StopMatch();
        }

        private void MatchBoard_AnimationCompleted(object? sender, EventArgs e)
        {
            if (!_awaitingMatchAnimation) return;
            _awaitingMatchAnimation = false;
            matchService?.NotifyAnimationCompleted();
        }

        private void MatchService_OnMovePlayed(string uciMove, string fen, long moveTimeMs, string? eval)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnMovePlayed(uciMove, fen, moveTimeMs, eval));
                return;
            }

            isNavigating = true;
            try
            {
                // Get FEN before the move for brilliant detection
                string fenBeforeMove = moveTree.CurrentNode.FEN;

                // Make the move on the visual board
                boardControl.MakeMove(uciMove);
                if (config?.ShowAnimations == true)
                {
                    _awaitingMatchAnimation = true;
                    boardControl.StartAnimation(uciMove);
                }

                // Convert to SAN and add to move tree
                string san = ConvertUciToSan(uciMove, fenBeforeMove);
                string newFen = boardControl.GetFEN();

                // Check for brilliant move
                string brilliantSymbol = "";
                string? brilliantExplanation = null;
                double? currentEval = null;

                if (!string.IsNullOrEmpty(eval))
                {
                    currentEval = MovesExplanation.ParseEvaluation(eval);
                    if (currentEval.HasValue)
                    {
                        var (isBrilliant, explanation) = ConsoleOutputFormatter.IsBrilliantMove(
                            fenBeforeMove, uciMove, currentEval.Value, _previousMatchEval);

                        if (isBrilliant)
                        {
                            brilliantSymbol = "!!";
                            brilliantExplanation = explanation;
                        }
                    }
                }

                // Add move to tree (with symbol if brilliant)
                string sanWithSymbol = san + brilliantSymbol;
                moveTree.AddMove(uciMove, sanWithSymbol, newFen);

                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();

                // Update eval bar with engine's evaluation
                if (!string.IsNullOrEmpty(eval))
                {
                    UpdateEvalBar(eval);
                }

                // Update previous eval for next move
                _previousMatchEval = currentEval;

                // Log to analysis output
                var currentNode = moveTree.CurrentNode;
                double timeSec = moveTimeMs / 1000.0;
                string evalStr = !string.IsNullOrEmpty(eval) ? $" [{eval}]" : "";
                string timeStr = $"({timeSec:F1}s)";
                string brilliantStr = !string.IsNullOrEmpty(brilliantExplanation) ? $" {brilliantExplanation}" : "";

                if (currentNode.IsWhiteMove)
                {
                    analysisOutput.AppendText($"{currentNode.MoveNumber}. {sanWithSymbol}{evalStr}{brilliantStr} {timeStr}  ");
                }
                else
                {
                    analysisOutput.AppendText($"{sanWithSymbol}{evalStr}{brilliantStr} {timeStr}\n");
                }
                analysisOutput.ScrollToCaret();
            }
            finally
            {
                isNavigating = false;
            }
        }

        private void MatchService_OnClockUpdated(long whiteMs, long blackMs, bool whiteToMove)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnClockUpdated(whiteMs, blackMs, whiteToMove));
                return;
            }

            UpdateClockDisplay(whiteMs, blackMs, whiteToMove);
        }

        private void MatchService_OnMatchEnded(EngineMatchResult result)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnMatchEnded(result));
                return;
            }

            clockTimer.Stop();
            matchRunning = false;

            // Show result
            analysisOutput.AppendText($"\n\n{result.GetResultString()}\n");
            if (result.TimeControl == TimeControlType.TotalPlusIncrement &&
                result.Termination != MatchTermination.UserStopped)
            {
                analysisOutput.AppendText($"White remaining: {FormatClock(result.WhiteTimeRemainingMs)}\n");
                analysisOutput.AppendText($"Black remaining: {FormatClock(result.BlackTimeRemainingMs)}\n");
            }
            analysisOutput.ScrollToCaret();

            lblStatus.Text = result.GetResultString();

            // Re-enable controls
            SetMatchControlsEnabled(false);

            // Clean up match service
            boardControl.AnimationCompleted -= MatchBoard_AnimationCompleted;
            _awaitingMatchAnimation = false;
            if (matchService != null)
            {
                matchService.OnMovePlayed -= MatchService_OnMovePlayed;
                matchService.OnClockUpdated -= MatchService_OnClockUpdated;
                matchService.OnMatchEnded -= MatchService_OnMatchEnded;
                matchService.OnStatusChanged -= MatchService_OnStatusChanged;
                matchService.Dispose();
                matchService = null;
            }
        }

        private void MatchService_OnStatusChanged(string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnStatusChanged(status));
                return;
            }

            lblStatus.Text = status;
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            if (matchService?.IsRunning != true) return;

            UpdateClockDisplay(
                matchService.WhiteRemainingMs,
                matchService.BlackRemainingMs,
                matchService.WhiteToMove);
        }

        private void UpdateClockDisplay(long whiteMs, long blackMs, bool whiteToMove)
        {
            lblWhiteClock.Text = $"W: {FormatClock(whiteMs)}";
            lblBlackClock.Text = $"B: {FormatClock(blackMs)}";

            // Highlight active side using theme colors
            if (matchRunning)
            {
                var scheme = ThemeService.GetColorScheme(config?.Theme == "Dark");
                lblWhiteClock.BackColor = whiteToMove ? scheme.ClockActiveBackColor : scheme.ClockBackColor;
                lblBlackClock.BackColor = !whiteToMove ? scheme.ClockActiveBackColor : scheme.ClockBackColor;
            }
        }

        private static string FormatClock(long ms)
        {
            if (ms <= 0) return "0:00.0";
            int totalSeconds = (int)(ms / 1000);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            int tenths = (int)((ms % 1000) / 100);
            return $"{minutes}:{seconds:D2}.{tenths}";
        }

        private void SetMatchControlsEnabled(bool running)
        {
            matchRunning = running;

            // Disable/enable game controls
            btnNewGame.Enabled = !running;
            btnTakeBack.Enabled = !running;
            btnLoadFen.Enabled = !running;
            btnPlayBot.Enabled = !running;
            boardControl.InteractionEnabled = !running;

            // Match controls
            cmbWhiteEngine.Enabled = !running;
            cmbBlackEngine.Enabled = !running;
            cmbTimeControlType.Enabled = !running;
            numDepth.Enabled = !running;
            numMoveTime.Enabled = !running;
            numTotalTime.Enabled = !running;
            numIncrement.Enabled = !running;

            // Toggle start/stop buttons
            btnStartMatch.Visible = !running;
            btnStopMatch.Visible = running;

            // Cancel auto-analysis during match
            if (running)
            {
                autoAnalysisCts?.Cancel();
            }
        }

        #endregion

        #region Bot Mode

        private async void BtnPlayBot_Click(object? sender, EventArgs e)
        {
            if (_botModeActive)
            {
                StopBotMode();
                return;
            }

            if (matchRunning)
            {
                lblStatus.Text = "Stop the engine match first";
                return;
            }

            if (string.IsNullOrEmpty(config?.SelectedEngine))
            {
                lblStatus.Text = "No engine configured — click ⚙ to set up";
                return;
            }

            using var dialog = new BotSettingsDialog(config?.Theme == "Dark");
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            _botSettings = dialog.Settings;

            // Reset the board for a new game
            CancelClassification();
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            boardControl.ResetBoard();
            moveTree.Clear(boardControl.GetFEN());
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear();
            _currentClassification = null;
            _classificationLookup = null;
            analysisOutput.Clear();
            evalBar?.Reset();

            // Flip board if user plays Black
            bool userPlaysBlack = _botSettings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped)
                boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped)
                boardControl.FlipBoard();

            // Initialize bot engine
            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string enginePath = Path.Combine(enginesPath, config.SelectedEngine);

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(enginePath);

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose();
                    _botEngine = null;
                    return;
                }

                // Set skill level
                await _botEngine.SetSkillLevelAsync(_botSettings.GetSkillLevel());
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Bot engine error: {ex.Message}";
                _botEngine?.Dispose();
                _botEngine = null;
                return;
            }

            _botModeActive = true;
            _botMoveCts = new CancellationTokenSource();
            btnPlayBot.Text = "⏹";
            toolTip.SetToolTip(btnPlayBot, "Stop Bot");
            boardControl.InteractionEnabled = true;

            if (_botSettings.ChallengeMode)
                ApplyChallengeMode();

            string diffLabel = _botSettings.GetDifficultyLabel();
            string colorLabel = userPlaysBlack ? "Black" : "White";
            string typeLabel = _botSettings.ChallengeMode ? "Challenge" : "Friendly";
            analysisOutput.AppendText($"Bot Mode: You play {colorLabel}\n");
            analysisOutput.AppendText($"Difficulty: {diffLabel}  |  {typeLabel}\n\n");
            lblStatus.Text = $"Bot mode — {diffLabel}";

            // Disable engine match controls during bot mode
            btnStartMatch.Enabled = false;

            // If bot plays White, make the first move
            if (_botSettings.BotPlaysWhite)
            {
                _ = MakeBotMoveAsync();
            }
            else
            {
                // Trigger analysis for the starting position
                _ = TriggerAutoAnalysis();
            }
        }

        private async Task MakeBotMoveAsync()
        {
            if (!_botModeActive || _botEngine == null || _botSettings == null)
                return;

            // Brief delay so user can see their move before bot responds
            try
            {
                await Task.Delay(300, _botMoveCts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { return; }

            boardControl.InteractionEnabled = false;
            lblStatus.Text = "Bot thinking...";

            try
            {
                string fen = boardControl.GetFEN();
                string goCommand = $"go movetime {_botSettings.GetMoveTimeMs()}";
                int timeoutMs = _botSettings.GetMoveTimeMs() + 5000;
                var token = _botMoveCts?.Token ?? CancellationToken.None;

                var (bestMove, eval) = await _botEngine.GetMoveForMatchAsync(fen, goCommand, timeoutMs, token);

                if (string.IsNullOrEmpty(bestMove))
                {
                    // No legal moves — game over
                    HandleBotGameEnd(fen);
                    return;
                }

                // Apply bot's move to the board
                isNavigating = true;
                try
                {
                    string fenBeforeMove = moveTree.CurrentNode.FEN;
                    boardControl.MakeMove(bestMove);
                    if (config?.ShowAnimations == true)
                        boardControl.StartAnimation(bestMove);

                    string san = ConvertUciToSan(bestMove, fenBeforeMove);
                    string newFen = boardControl.GetFEN();
                    moveTree.AddMove(bestMove, san, newFen);

                    UpdateMoveList();
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    if (!string.IsNullOrEmpty(eval))
                        UpdateEvalBar(eval);
                }
                finally
                {
                    isNavigating = false;
                }

                // Check if user has any legal moves after bot's move
                string currentFen = boardControl.GetFEN();
                if (!HasAnyLegalMoveFromFen(currentFen))
                {
                    HandleBotGameEnd(currentFen);
                    return;
                }

                boardControl.InteractionEnabled = true;
                string diffLabel = _botSettings.GetDifficultyLabel();
                lblStatus.Text = $"Your turn — {diffLabel}";

                // Trigger analysis for the new position (human's turn)
                _ = TriggerAutoAnalysis();
            }
            catch (OperationCanceledException)
            {
                // Bot move was cancelled (take back, stop, etc.)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BotMove error: {ex.Message}");
                lblStatus.Text = "Bot error — try again";
                boardControl.InteractionEnabled = true;
            }
        }

        private void HandleBotGameEnd(string fen)
        {
            _botModeActive = false;

            // Determine result by checking if king is in check
            var fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length >= 2 && fenParts[1] == "w";

            // Use board control to check if king is in check
            // If in check with no legal moves = checkmate, otherwise stalemate
            bool inCheck = false;
            try
            {
                // Try to detect check from the FEN by attempting a null analysis
                // Simple heuristic: if the engine returned no move, it's either checkmate or stalemate
                // We can check by seeing if the position evaluation is mate
                inCheck = IsSideInCheck(whiteToMove);
            }
            catch { }

            string result;
            if (inCheck)
            {
                // Checkmate
                bool botWins = (_botSettings?.BotPlaysWhite == true && !whiteToMove) ||
                               (_botSettings?.BotPlaysWhite == false && whiteToMove);
                if (botWins)
                    result = "Checkmate — Bot wins!";
                else
                    result = "Checkmate — You win!";
            }
            else
            {
                result = "Stalemate — Draw!";
            }

            analysisOutput.AppendText($"\n{result}\n");
            boardControl.InteractionEnabled = false;
            btnPlayBot.Text = "♞";
            toolTip.SetToolTip(btnPlayBot, "Play vs Bot");
            btnStartMatch.Enabled = true;

            _botEngine?.Dispose();
            _botEngine = null;

            RestoreChallengeSnapshot();
            lblStatus.Text = result;
        }

        private bool IsSideInCheck(bool whiteKing)
        {
            // Check if the king of the given color is in check on the current board
            var board = boardControl.GetBoardState();
            if (board == null) return false;

            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow != -1) break;
            }

            if (kingRow == -1) return false;

            // Check if any enemy piece attacks the king
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isEnemy = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (isEnemy && ChessRulesService.CanReachSquare(board, r, c, piece, kingRow, kingCol))
                    {
                        // For sliding pieces, also check path is clear
                        char pl = char.ToLower(piece);
                        if (pl == 'n' || pl == 'k' || pl == 'p')
                        {
                            if (pl == 'p' && c == kingCol) continue; // Pawns don't attack forward
                            return true;
                        }
                        // Sliding piece — check path
                        int dr = Math.Sign(kingRow - r);
                        int dc = Math.Sign(kingCol - c);
                        int cr = r + dr, cc = c + dc;
                        bool pathClear = true;
                        while (cr != kingRow || cc != kingCol)
                        {
                            if (board.GetPiece(cr, cc) != '.') { pathClear = false; break; }
                            cr += dr; cc += dc;
                        }
                        if (pathClear) return true;
                    }
                }
            }
            return false;
        }

        private bool HasAnyLegalMoveFromFen(string fen)
        {
            // Quick check: ask the board control if the current side has any legal moves
            // by checking all pieces of the current side
            var fenParts = fen.Split(' ');
            bool whiteToMove = fenParts.Length >= 2 && fenParts[1] == "w";
            var board = boardControl.GetBoardState();
            if (board == null) return true;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isOwn = whiteToMove ? char.IsUpper(piece) : char.IsLower(piece);
                    if (!isOwn) continue;

                    // Check all target squares for this piece
                    for (int tr = 0; tr < 8; tr++)
                    {
                        for (int tc = 0; tc < 8; tc++)
                        {
                            if (r == tr && c == tc) continue;
                            char target = board.GetPiece(tr, tc);
                            // Can't capture own piece
                            if (target != '.' && ((whiteToMove && char.IsUpper(target)) ||
                                                   (!whiteToMove && char.IsLower(target))))
                                continue;

                            if (ChessRulesService.CanReachSquare(board, r, c, piece, tr, tc))
                            {
                                // Verify path is clear for sliding pieces
                                char pl = char.ToLower(piece);
                                if (pl != 'n' && pl != 'k' && pl != 'p')
                                {
                                    int dr = Math.Sign(tr - r);
                                    int dc = Math.Sign(tc - c);
                                    int cr = r + dr, cc = c + dc;
                                    bool blocked = false;
                                    while (cr != tr || cc != tc)
                                    {
                                        if (board.GetPiece(cr, cc) != '.') { blocked = true; break; }
                                        cr += dr; cc += dc;
                                    }
                                    if (blocked) continue;
                                }

                                // Simulate the move and check if king is safe
                                using var pooled = BoardPool.Rent(board);
                                var testBoard = pooled.Board;
                                testBoard.SetPiece(tr, tc, piece);
                                testBoard.SetPiece(r, c, '.');

                                // Check en passant capture
                                if (pl == 'p' && c != tc && target == '.')
                                    testBoard.SetPiece(r, tc, '.');

                                if (!IsKingInCheckOnBoard(testBoard, whiteToMove))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool IsKingInCheckOnBoard(ChessBoard testBoard, bool whiteKing)
        {
            char king = whiteKing ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (testBoard.GetPiece(r, c) == king)
                    {
                        kingRow = r; kingCol = c; break;
                    }
                }
                if (kingRow != -1) break;
            }
            if (kingRow == -1) return false;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = testBoard.GetPiece(r, c);
                    if (piece == '.') continue;
                    bool isEnemy = whiteKing ? char.IsLower(piece) : char.IsUpper(piece);
                    if (!isEnemy) continue;

                    if (!ChessRulesService.CanReachSquare(testBoard, r, c, piece, kingRow, kingCol))
                        continue;

                    char pl = char.ToLower(piece);
                    if (pl == 'n' || pl == 'k')
                        return true;
                    if (pl == 'p')
                        return c != kingCol; // Pawns only attack diagonally

                    // Sliding piece path check
                    int dr = Math.Sign(kingRow - r);
                    int dc = Math.Sign(kingCol - c);
                    int cr = r + dr, cc = c + dc;
                    bool pathClear = true;
                    while (cr != kingRow || cc != kingCol)
                    {
                        if (testBoard.GetPiece(cr, cc) != '.') { pathClear = false; break; }
                        cr += dr; cc += dc;
                    }
                    if (pathClear) return true;
                }
            }
            return false;
        }

        private void StopBotMode()
        {
            _botMoveCts?.Cancel();
            _botModeActive = false;

            _botEngine?.Dispose();
            _botEngine = null;
            _botSettings = null;

            btnPlayBot.Text = "♞";
            toolTip.SetToolTip(btnPlayBot, "Play vs Bot");
            btnStartMatch.Enabled = true;
            boardControl.InteractionEnabled = true;

            RestoreChallengeSnapshot();
        }

        private void RestoreChallengeSnapshot()
        {
            if (_challengeSnapshot == null) return;
            config?.CopyFrom(_challengeSnapshot);
            _challengeSnapshot = null;
            ApplyTheme();
            ApplyConsoleFont();
            LeftPanel_Resize(leftPanel, EventArgs.Empty);
            evalBar?.Reset();
            lblStatus.Text = "Bot mode stopped — analysis restored";
            _ = TriggerAutoAnalysis();
        }

        private void ApplyChallengeMode()
        {
            if (config == null) return;
            _challengeSnapshot = new AppConfig();
            _challengeSnapshot.CopyFrom(config);

            config.ShowBestLine = false;
            config.ShowSecondLine = false;
            config.ShowThirdLine = false;
            config.ShowEngineArrows = false;
            config.ShowEvalBar = false;
            LeftPanel_Resize(leftPanel, EventArgs.Empty);
            config.ShowTacticalAnalysis = false;
            config.ShowPositionalAnalysis = false;
            config.ShowEndgameAnalysis = false;
            config.ShowOpeningPrinciples = false;
            config.ShowThreats = false;
            config.ShowWDL = false;
            config.PlayStyleEnabled = false;
            config.ShowOpeningName = false;
            config.ShowMoveQuality = false;
            config.ContinuousAnalysis = false;
            config.ShowBookMoves = false;

            boardControl.ClearEngineArrows();
            evalBar?.Reset();
            analysisOutput.Clear();
        }

        #endregion

        #region Analysis

        private async Task AnalyzeCurrentPosition(CancellationToken ct = default)
        {
            if (_challengeSnapshot != null) return; // challenge mode: no hints

            if (engineService == null)
            {
                Debug.WriteLine("[Analysis] FAILED — engineService is null");
                lblStatus.Text = "Engine not available";
                return;
            }

            string fen = boardControl.GetFEN();
            Debug.WriteLine($"[Analysis] Starting — FEN: {fen}  State: {engineService.State}");
            string cacheKey = GetPositionKey(fen);

            // Book arrows are instant — show them before the engine even starts
            UpdateBookArrowsForPosition(fen);
            int depth = config?.EngineDepth ?? 15;
            int multiPV = 3;

            // If a classification is active and the cache already has this position's result,
            // show it directly — skip continuous analysis entirely.
            if (_classificationLookup != null &&
                _analysisCache.TryGetValue(cacheKey, out var classifiedCache) &&
                classifiedCache.Depth >= depth)
            {
                DisplayAnalysisResult(fen, classifiedCache.BestMove, classifiedCache.Evaluation,
                    classifiedCache.PVs, classifiedCache.Evaluations, classifiedCache.WDL,
                    classifiedCache.Depth, fromCache: true);
                return;
            }

            if (config?.ContinuousAnalysis == true)
            {
                lblStatus.Text = "Analyzing...";
                int maxDepth = config?.ContinuousAnalysisMaxDepth ?? 50;

                ShowBookInfoImmediate(fen);

                string lastBestMove = "", lastEval = "";
                List<string> lastPvs = new(), lastEvals = new();
                WDLInfo? lastWdl = null;
                int lastDepth = 0;

                try
                {
                    await engineService.RunContinuousAnalysisAsync(fen, multiPV, maxDepth,
                        (bestMove, eval, pvs, evals, wdl, currentDepth) =>
                        {
                            if (ct.IsCancellationRequested) return;
                            lastBestMove = bestMove; lastEval = eval;
                            lastPvs = pvs; lastEvals = evals;
                            lastWdl = wdl; lastDepth = currentDepth;

                            void Update()
                            {
                                if (ct.IsCancellationRequested) return;
                                lblStatus.Text = $"Analyzing... depth {currentDepth}";
                                consoleFormatter?.DisplayLiveLines(fen, eval, pvs, evals, wdl, currentDepth);
                                if (!isNavigating && !_bookArrowsActive)
                                {
                                    int arrowCount = config?.EngineArrowCount ?? 1;
                                    if (arrowCount > 0) UpdateEngineArrows(pvs, arrowCount);
                                }
                                UpdateEvalBar(eval);
                            }
                            if (InvokeRequired) BeginInvoke(Update); else Update();
                        }, ct);

                    // Engine finished at max depth — switch to full analysis display
                    if (!ct.IsCancellationRequested && !string.IsNullOrEmpty(lastBestMove))
                    {
                        _analysisCache[cacheKey] = new CachedAnalysis
                        {
                            BestMove = lastBestMove,
                            Evaluation = lastEval,
                            PVs = new List<string>(lastPvs),
                            Evaluations = new List<string>(lastEvals),
                            WDL = lastWdl,
                            Depth = lastDepth
                        };

                        void ShowFull()
                        {
                            if (ct.IsCancellationRequested) return;
                            DisplayAnalysisResult(fen, lastBestMove, lastEval, lastPvs, lastEvals, lastWdl, lastDepth, fromCache: false);
                        }
                        if (InvokeRequired) Invoke(ShowFull); else ShowFull();
                    }
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        lblStatus.Text = $"Analysis error: {ex.Message}";
                        Debug.WriteLine($"Analysis error: {ex}");
                    }
                }
                return;
            }

            // Fixed-depth mode

            // Check if depth setting changed - invalidate cache if so
            if (_cachedDepth != depth)
            {
                _analysisCache.Clear();
                _cachedDepth = depth;
            }

            // Check cache first (depth check only - PV count varies by position)
            if (_analysisCache.TryGetValue(cacheKey, out var cached) &&
                cached.Depth >= depth)
            {
                DisplayAnalysisResult(fen, cached.BestMove, cached.Evaluation,
                    cached.PVs, cached.Evaluations, cached.WDL, cached.Depth, fromCache: true);
                return;
            }

            lblStatus.Text = "Analyzing...";
            ShowBookInfoImmediate(fen);

            try
            {
                var result = await engineService.GetBestMoveAsync(fen, depth, multiPV, ct: ct);

                if (string.IsNullOrEmpty(result.bestMove))
                {
                    Debug.WriteLine($"[Analysis] FAILED — bestMove empty. Engine state: {engineService.State}. FEN: {fen}");
                    lblStatus.Text = "Analysis failed";
                    return;
                }

                var pvs = result.pvs ?? new List<string>();
                var evals = result.evaluations ?? new List<string>();

                _analysisCache[cacheKey] = new CachedAnalysis
                {
                    BestMove = result.bestMove,
                    Evaluation = result.evaluation,
                    PVs = new List<string>(pvs),
                    Evaluations = new List<string>(evals),
                    WDL = result.wdl,
                    Depth = depth
                };

                // Stale check: discard if user navigated away while engine was thinking.
                if (ct.IsCancellationRequested || GetPositionKey(boardControl.GetFEN()) != cacheKey)
                    return;

                DisplayAnalysisResult(fen, result.bestMove, result.evaluation,
                    pvs, evals, result.wdl, depth, fromCache: false);
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    lblStatus.Text = $"Analysis error: {ex.Message}";
                    Debug.WriteLine($"Analysis error: {ex}");
                }
            }
        }

        /// <summary>
        /// Extracts the position-only part of FEN for cache key (excludes move counters).
        /// </summary>
        private static string GetPositionKey(string fen)
        {
            // FEN format: pieces side castling enpassant halfmove fullmove
            // We only need the first 4 parts for position identity
            var parts = fen.Split(' ');
            if (parts.Length >= 4)
            {
                return $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
            }
            return fen;
        }

        /// <summary>
        /// Displays analysis results (shared between cached and fresh analysis).
        /// </summary>
        private void DisplayAnalysisResult(string fen, string bestMove, string evaluation,
            List<string> pvs, List<string> evals, WDLInfo? wdl,
            int depth, bool fromCache)
        {
            // Apply aggressiveness filter
            var candidates = new List<(string move, string evaluation, string pvLine, int sharpness)>();

            for (int i = 0; i < Math.Min(pvs.Count, evals.Count); i++)
            {
                string pvLine = pvs[i];
                string eval = evals[i];
                string firstMove = pvLine.Split(' ')[0];
                int sharpness = sharpnessAnalyzer.CalculateSharpness(firstMove, fen, eval, pvLine);
                candidates.Add((firstMove, eval, pvLine, sharpness));
            }

            // Select based on style
            int aggressiveness = config?.Aggressiveness ?? 50;
            string recommendedMove = bestMove;

            if (candidates.Count >= 2 && aggressiveness != 50 && config?.PlayStyleEnabled == true)
            {
                int selectedIndex = sharpnessAnalyzer.SelectMoveByAggressiveness(candidates, aggressiveness, 0.30);
                if (selectedIndex >= 0 && selectedIndex < candidates.Count)
                {
                    recommendedMove = candidates[selectedIndex].move;
                }
            }

            var bookMoves = FetchBookMoves(fen);

            // Display results (respect user's line visibility settings)
            consoleFormatter?.DisplayAnalysisResults(
                recommendedMove,
                evaluation,
                pvs,
                evals,
                fen,
                config?.ShowBestLine ?? true,
                config?.ShowSecondLine ?? true,
                config?.ShowThirdLine ?? true,
                wdl,
                bookMoves);

            // Update engine arrows (book arrows already set upfront in UpdateBookArrowsForPosition)
            if (!isNavigating && !_bookArrowsActive)
            {
                int arrowCount = config?.EngineArrowCount ?? 1;
                if (arrowCount > 0)
                    UpdateEngineArrows(pvs, arrowCount);
                else
                    boardControl.ClearEngineArrows();
            }

            // Store evaluation on the move node so the eval graph can read it
            moveTree.CurrentNode.Evaluation = MovesExplanation.ParseEvaluation(evaluation);
            RefreshEvalGraph();

            // Update eval bar with the evaluation
            UpdateEvalBar(evaluation);

            // Update threat arrows — derived from the same detection as the text output
            if (config?.ShowThreatArrows == true)
            {
                string[] fenParts = fen.Split(' ');
                bool weAreWhite = fenParts.Length > 1 && fenParts[1] == "w";
                string ep = fenParts.Length > 4 ? fenParts[3] : "-";
                var threats = ThreatDetection.GetThreatArrows(boardControl.GetBoardState(), weAreWhite, ep);
                boardControl.SetThreatArrows(threats);
            }
            else
            {
                boardControl.ClearThreatArrows();
            }

            string cacheIndicator = fromCache ? " (cached)" : "";
            lblStatus.Text = $"Analysis complete (depth {depth}){cacheIndicator}";
        }

        private (int fromRow, int fromCol, int toRow, int toCol) UciToSquares(string uci)
        {
            int fromCol = uci[0] - 'a';
            int fromRow = 7 - (uci[1] - '1');
            int toCol = uci[2] - 'a';
            int toRow = 7 - (uci[3] - '1');
            return (fromRow, fromCol, toRow, toCol);
        }

        private List<BookMove>? FetchBookMoves(string fen)
        {
            if (openingBookService?.IsLoaded != true) return null;
            var moves = openingBookService.GetBookMovesForPosition(fen);
            if (moves == null || moves.Count == 0) return null;
            return moves.Select(pm => new BookMove
            {
                UciMove = pm.UciMove,
                Games = pm.Weight,
                Priority = pm.Weight,
                WinRate = 50,
                Wins = 0,
                Losses = 0,
                Draws = 0,
                Source = "Book"
            }).ToList();
        }

        private void ShowBookInfoImmediate(string fen)
        {
            var bookMoves = FetchBookMoves(fen);
            consoleFormatter?.SetBookContext(fen, bookMoves);
            if (config?.ShowOpeningName == true || config?.ShowBookMoves == true)
                consoleFormatter?.ShowBookContextNow(fen, bookMoves);
        }

        private void UpdateBookArrowsForPosition(string fen)
        {
            if (isNavigating) return;
            bool inBook = config?.ShowBookMoves == true && config?.ShowBookArrows == true && openingBookService?.IsLoaded == true;
            if (inBook)
            {
                var moves = openingBookService!.GetBookMovesForPosition(fen);
                inBook = moves.Count > 0;
                if (inBook)
                {
                    int totalWeight = moves.Sum(m => m.Weight);
                    double topPct = totalWeight > 0 ? moves[0].Weight / (double)totalWeight * 100.0 : 1.0;
                    var arrows = new List<(int, int, int, int, Color)>();
                    foreach (var bm in moves.Take(5))
                    {
                        if (bm.UciMove.Length < 4) continue;
                        var (fromRow, fromCol, toRow, toCol) = UciToSquares(bm.UciMove);
                        double pct = totalWeight > 0 ? bm.Weight / (double)totalWeight * 100.0 : 0;
                        int alpha = Math.Max(80, (int)(pct / topPct * 200));
                        arrows.Add((fromRow, fromCol, toRow, toCol, Color.FromArgb(alpha, 15, 155, 200)));
                    }
                    boardControl.ClearEngineArrows();
                    boardControl.SetBookArrows(arrows);
                }
            }
            if (!inBook)
                boardControl.ClearBookArrows();
            _bookArrowsActive = inBook;
        }

        private void UpdateEngineArrows(List<string> pvs, int arrowCount)
        {
            var arrows = new List<(int fromRow, int fromCol, int toRow, int toCol, Color color)>();

            var colors = new[]
            {
                Color.FromArgb(180, 0, 200, 80),    // Green  — best
                Color.FromArgb(180, 200, 200, 0),   // Yellow — 2nd
                Color.FromArgb(180, 200, 60, 60)    // Red    — 3rd
            };

            for (int i = 0; i < arrowCount && i < pvs.Count; i++)
            {
                if (!string.IsNullOrEmpty(pvs[i]))
                {
                    string firstMove = pvs[i].Split(' ')[0];
                    if (firstMove.Length >= 4)
                    {
                        var sq = UciToSquares(firstMove);
                        arrows.Add((sq.fromRow, sq.fromCol, sq.toRow, sq.toCol, colors[i]));
                    }
                }
            }

            boardControl.SetEngineArrows(arrows);
        }

        /// <summary>
        /// Parses the evaluation string and updates the eval bar control.
        /// </summary>
        private void UpdateEvalBar(string evaluation)
        {
            if (evalBar == null || string.IsNullOrEmpty(evaluation))
                return;

            if (evaluation.StartsWith("Mate in "))
            {
                string mateStr = evaluation.Replace("Mate in ", "").Trim();
                if (int.TryParse(mateStr, out int mateIn))
                {
                    evalBar.SetMate(mateIn);
                }
            }
            else
            {
                string cleaned = evaluation.Replace("+", "");
                if (double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double pawns))
                {
                    evalBar.SetEvaluation(pawns * 100.0); // Convert pawns to centipawns
                }
            }
        }

        #endregion

        #region Helpers

        private void UpdateMoveList()
        {
            moveListBox.BeginUpdate();
            try
            {
                moveListBox.Items.Clear();
                displayedNodes.Clear();

                // Build the display list with variations
                BuildMoveListRecursive(moveTree.Root, 0);
            }
            finally
            {
                moveListBox.EndUpdate();
                // Re-assert font and item height — WinForms OwnerDrawFixed can reset these
                // after Items.Clear() + EndUpdate in some scenarios.
                moveListBox.Font = _consoleFont;
                moveListBox.ItemHeight = Math.Max(14, (int)Math.Ceiling(_consoleFont.GetHeight()));
            }

            // Scroll to current position
            UpdateMoveListSelection();
        }

        private void BuildMoveListRecursive(MoveNode node, int indentLevel)
        {
            // Process main line first
            foreach (var child in node.Children)
            {
                // Add the move to display
                string indent = new string(' ', indentLevel * 2);
                string moveText;

                if (child.IsWhiteMove)
                {
                    moveText = $"{indent}{child.MoveNumber}. {child.SanMove}";
                }
                else
                {
                    moveText = $"{indent}{child.MoveNumber}...{child.SanMove}";
                }

                // Mark variations
                if (child.VariationDepth > 0 || child.GetVariationIndex() > 0)
                {
                    moveText = $"{indent}({child.SanMove})";
                }

                moveListBox.Items.Add(moveText);
                displayedNodes.Add(child);

                // Process this node's children (continue main line at same indent)
                if (child.Children.Count > 0)
                {
                    // First child continues the main line
                    BuildMoveListRecursive(child, indentLevel);
                }

                // Process variations (children after the first one were already handled)
                // Variations are added when their parent is processed
                break; // Only process first child as main line continuation
            }

            // Now process variations (non-first children)
            if (node.Children.Count > 1)
            {
                for (int i = 1; i < node.Children.Count; i++)
                {
                    var variation = node.Children[i];
                    string indent = new string(' ', (indentLevel + 1) * 2);

                    string varText;
                    if (variation.IsWhiteMove)
                    {
                        varText = $"{indent}({variation.MoveNumber}. {variation.SanMove}";
                    }
                    else
                    {
                        varText = $"{indent}({variation.MoveNumber}...{variation.SanMove}";
                    }

                    moveListBox.Items.Add(varText);
                    displayedNodes.Add(variation);

                    // Continue the variation line
                    BuildVariationLine(variation, indentLevel + 1);
                }
            }
        }

        private void BuildVariationLine(MoveNode node, int indentLevel)
        {
            var current = node;
            while (current.Children.Count > 0)
            {
                var next = current.Children[0];
                string indent = new string(' ', indentLevel * 2);

                string moveText = $"{indent}{next.SanMove}";
                if (next.IsWhiteMove)
                {
                    moveText = $"{indent}{next.MoveNumber}. {next.SanMove}";
                }

                moveListBox.Items.Add(moveText);
                displayedNodes.Add(next);

                // Handle nested variations in this line
                if (current.Children.Count > 1)
                {
                    for (int i = 1; i < current.Children.Count; i++)
                    {
                        var nestedVar = current.Children[i];
                        string nestedIndent = new string(' ', (indentLevel + 1) * 2);
                        moveListBox.Items.Add($"{nestedIndent}({nestedVar.SanMove}");
                        displayedNodes.Add(nestedVar);
                        BuildVariationLine(nestedVar, indentLevel + 2);
                    }
                }

                current = next;
            }
        }

        private void UpdateFenDisplay()
        {
            txtFen.Text = boardControl.GetFEN();
        }

        private void UpdateTurnLabel()
        {
            lblTurn.Text = boardControl.WhiteToMove ? "White to move" : "Black to move";
            lblTurn.ForeColor = boardControl.WhiteToMove
                ? (config?.Theme == "Dark" ? Color.White : Color.Black)
                : (config?.Theme == "Dark" ? Color.LightGray : Color.DimGray);
        }

        private void UpdateMoveListSelection()
        {
            var current = moveTree.CurrentNode;
            if (current == moveTree.Root)
            {
                moveListBox.ClearSelected();
                RefreshEvalGraph();
                return;
            }

            int idx = displayedNodes.IndexOf(current);
            if (idx >= 0 && idx < moveListBox.Items.Count)
                moveListBox.SelectedIndex = idx;

            RefreshEvalGraph();
        }

        private void RefreshEvalGraph()
        {
            if (_evalGraph == null || moveTree == null) return;
            var current = moveTree.CurrentNode == moveTree.Root ? null : moveTree.CurrentNode;
            _evalGraph.SetData(moveTree, current, config?.Theme == "Dark");
        }

        private void EvalGraph_MoveNodeSelected(MoveNode node)
        {
            if (isNavigating || matchRunning) return;
            isNavigating = true;
            try
            {
                moveTree.GoToNode(node);
                boardControl.LoadFEN(node.FEN);
                UpdateMoveAnnotation(node);
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMoveListSelection();
                lblStatus.Text = $"Move {node.MoveNumber}";
                if (!matchRunning) _ = TriggerAutoAnalysis();
            }
            finally
            {
                isNavigating = false;
            }
        }

        private string ConvertUciToSan(string uciMove, string fen)
        {
            try
            {
                return ChessNotationService.ConvertFullPvToSan(
                    uciMove, fen,
                    ChessRulesService.ApplyUciMove,
                    ChessRulesService.CanReachSquare,
                    ChessRulesService.FindAllPiecesOfSameType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvertUciToSan failed for '{uciMove}': {ex.Message}");
                return uciMove; // Fallback to UCI notation
            }
        }

        /// <summary>
        /// Inserts an engine PV line into the move tree as a variation branch,
        /// then animates through the line on the board.
        /// Called when the user clicks [See line] in the analysis output.
        /// </summary>
        private void InsertPvIntoMoveTree(string pvUci, string startFen)
        {
            if (string.IsNullOrEmpty(pvUci)) return;

            // Verify we're still at the position the PV was computed for
            if (moveTree.CurrentNode.FEN != startFen)
                return;

            // Cancel any running PV animation
            _pvAnimationCts?.Cancel();
            _pvAnimationCts = new CancellationTokenSource();

            var savedNode = moveTree.CurrentNode;
            string currentFen = startFen;
            string[] uciMoves = pvUci.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var insertedNodes = new List<MoveNode>();

            try
            {
                foreach (string uciMove in uciMoves)
                {
                    if (uciMove.Length < 4) break;

                    // Convert UCI to SAN for display
                    string san;
                    try
                    {
                        san = ChessNotationService.ConvertUCIToSAN(
                            uciMove, currentFen,
                            ChessRulesService.CanReachSquare,
                            ChessRulesService.FindAllPiecesOfSameType);
                    }
                    catch
                    {
                        san = uciMove;
                    }

                    // Compute new FEN by applying the move to a temp board
                    var fenParts = currentFen.Split(' ');
                    string castling = fenParts.Length > 2 ? fenParts[2] : "-";
                    string enPassant = fenParts.Length > 3 ? fenParts[3] : "-";
                    bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                    var tempBoard = ChessBoard.FromFEN(currentFen);
                    ChessRulesService.ApplyUciMove(tempBoard, uciMove, ref castling, ref enPassant);

                    string nextSide = whiteToMove ? "b" : "w";
                    string newFen = $"{tempBoard.ToFEN()} {nextSide} {castling} {enPassant} 0 1";

                    // First move always starts a variation; rest chain naturally
                    MoveNode node;
                    if (insertedNodes.Count == 0)
                    {
                        // Check if this exact move already exists as a child
                        var existing = moveTree.CurrentNode.FindChild(uciMove);
                        if (existing != null)
                        {
                            node = existing;
                            moveTree.GoToNode(existing);
                        }
                        else
                        {
                            node = moveTree.CurrentNode.AddChild(uciMove, san, newFen, forceVariation: true);
                            moveTree.GoToNode(node);
                        }
                    }
                    else
                    {
                        node = moveTree.AddMove(uciMove, san, newFen);
                    }
                    insertedNodes.Add(node);
                    currentFen = newFen;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertPvIntoMoveTree error: {ex.Message}");
            }

            // Navigate back to start, update move list to show the new variation
            moveTree.GoToNode(savedNode);
            UpdateMoveList();

            // Animate through the variation on the board
            if (insertedNodes.Count > 0)
            {
                boardControl.ClearEngineArrows();
                _ = AnimatePvLineAsync(insertedNodes, _pvAnimationCts.Token);
            }
        }

        /// <summary>
        /// Animates through a list of PV nodes on the board, stepping through each move with a delay.
        /// </summary>
        private async Task AnimatePvLineAsync(List<MoveNode> nodes, CancellationToken ct)
        {
            isNavigating = true;
            bool completed = false;
            try
            {
                foreach (var node in nodes)
                {
                    await Task.Delay(400, ct);
                    if (ct.IsCancellationRequested) return;

                    moveTree.GoToNode(node);
                    boardControl.LoadFEN(node.FEN);

                    // Highlight the move that was just played so it's visually clear
                    if (node.UciMove.Length >= 4)
                    {
                        int fromCol = node.UciMove[0] - 'a';
                        int fromRow = 7 - (node.UciMove[1] - '1');
                        int toCol   = node.UciMove[2] - 'a';
                        int toRow   = 7 - (node.UciMove[3] - '1');
                        boardControl.LastMove = (fromRow, fromCol, toRow, toCol);
                    }

                    // Force immediate repaint so each move is visible before the next delay
                    boardControl.Refresh();

                    UpdateMoveListSelection();
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    string statusText = $"Move {node.MoveNumber}";
                    if (node.VariationDepth > 0)
                        statusText += " (variation)";
                    lblStatus.Text = statusText;
                }
                completed = true;
            }
            catch (TaskCanceledException) { }
            finally
            {
                isNavigating = false;
            }

            // Analyze the final position once the animation finishes naturally
            if (completed && !matchRunning)
                _ = TriggerAutoAnalysis();
        }

        #endregion

        #region PGN Import/Export

        private void BtnExportPgn_Click(object? sender, EventArgs e)
        {
            try
            {
                string pgn = GeneratePgn();

                using var dialog = new SaveFileDialog
                {
                    Filter = "PGN Files (*.pgn)|*.pgn|All Files (*.*)|*.*",
                    DefaultExt = "pgn",
                    FileName = $"game_{DateTime.Now:yyyyMMdd_HHmmss}.pgn"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, pgn);
                    lblStatus.Text = $"PGN saved to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Export error: {ex.Message}";
            }
        }

        private void BtnImportPgn_Click(object? sender, EventArgs e)
        {
            // Show a dialog to paste PGN or open from file
            using var inputForm = new Form
            {
                Text = "Import PGN",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            bool isDark = config?.Theme == "Dark";
            inputForm.BackColor = isDark ? Color.FromArgb(40, 40, 48) : Color.White;

            var lblInstructions = new Label
            {
                Text = "Paste PGN content below or open a file:",
                Location = new Point(10, 10),
                Size = new Size(360, 20),
                ForeColor = isDark ? Color.White : Color.Black
            };

            var btnOpenFile = new Button
            {
                Text = "Open File...",
                Location = new Point(380, 6),
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(230, 230, 230),
                ForeColor = isDark ? Color.White : Color.Black
            };

            var txtPgn = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 35),
                Size = new Size(460, 270),
                Font = new Font("Consolas", 9F),
                BackColor = isDark ? Color.FromArgb(30, 30, 35) : Color.White,
                ForeColor = isDark ? Color.White : Color.Black
            };

            btnOpenFile.Click += (s, args) =>
            {
                using var openDialog = new OpenFileDialog
                {
                    Filter = "PGN Files (*.pgn)|*.pgn|All Files (*.*)|*.*",
                    Title = "Open PGN File"
                };
                if (openDialog.ShowDialog(inputForm) == DialogResult.OK)
                {
                    try
                    {
                        txtPgn.Text = File.ReadAllText(openDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var btnOk = new Button
            {
                Text = "Import",
                Location = new Point(310, 315),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(60, 90, 140) : Color.FromArgb(70, 130, 180),
                ForeColor = Color.White
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(395, 315),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(230, 230, 230),
                ForeColor = isDark ? Color.White : Color.Black
            };

            inputForm.Controls.AddRange(new Control[] { lblInstructions, btnOpenFile, txtPgn, btnOk, btnCancel });
            inputForm.AcceptButton = btnOk;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string pgnText = txtPgn.Text.Trim();
                if (!string.IsNullOrEmpty(pgnText))
                {
                    ImportPgn(pgnText);
                }
            }
        }

        /// <summary>
        /// Generates PGN string from the current move tree.
        /// </summary>
        private string GeneratePgn()
        {
            var sb = new System.Text.StringBuilder();

            // Standard PGN headers
            sb.AppendLine("[Event \"Chess Analysis\"]");
            sb.AppendLine("[Site \"chessdroid\"]");
            sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            sb.AppendLine("[Round \"?\"]");
            sb.AppendLine("[White \"?\"]");
            sb.AppendLine("[Black \"?\"]");
            sb.AppendLine("[Result \"*\"]");
            if (_classificationLookup != null)
            {
                string annotator = _currentClassification?.EngineName is { Length: > 0 } n ? n : "chessdroid";
                sb.AppendLine($"[Annotator \"{annotator}\"]");
            }

            // Add FEN if not standard starting position
            string rootFen = moveTree.Root.FEN;
            if (rootFen != "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
            {
                sb.AppendLine($"[FEN \"{rootFen}\"]");
                sb.AppendLine("[SetUp \"1\"]");
            }

            sb.AppendLine();

            // Generate move text with annotations
            var mainLine = moveTree.GetMainLine();
            if (mainLine.Count > 0)
            {
                var moveText = new System.Text.StringBuilder();
                int lineLength = 0;

                foreach (var node in mainLine)
                {
                    // Strip inline symbols from SanMove — annotations are encoded as NAG + comment
                    string cleanSan = StripAnnotationSymbols(node.SanMove);
                    string moveStr = node.IsWhiteMove
                        ? $"{node.MoveNumber}. {cleanSan}"
                        : cleanSan;

                    // NAG + comment: prefer classification result, fall back to inline symbol
                    string nag = "";
                    string comment = "";
                    if (_classificationLookup != null &&
                        _classificationLookup.TryGetValue(node, out var result))
                    {
                        nag = GetNagForSymbol(result.Symbol);
                        comment = BuildPgnComment(result);
                    }
                    else
                    {
                        nag = GetNagForSymbol(GetInlineSymbol(node.SanMove));
                    }

                    string fullToken = moveStr;
                    if (!string.IsNullOrEmpty(nag)) fullToken += " " + nag;
                    if (!string.IsNullOrEmpty(comment)) fullToken += " " + comment;

                    // Embed engine cache data so analysis is restored on re-import
                    string posKey = GetPositionKey(node.FEN);
                    if (_analysisCache.TryGetValue(posKey, out var cachedEntry) && cachedEntry.Depth > 0)
                        fullToken += " " + SerializeCachedAnalysis(cachedEntry);

                    // Word wrap at ~80 characters
                    if (lineLength + fullToken.Length + 1 > 80)
                    {
                        moveText.AppendLine();
                        lineLength = 0;
                    }
                    else if (moveText.Length > 0)
                    {
                        moveText.Append(' ');
                        lineLength++;
                    }

                    moveText.Append(fullToken);
                    lineLength += fullToken.Length;
                }

                sb.Append(moveText);
                sb.Append(" *");
            }
            else
            {
                sb.Append("*");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Imports a PGN string and populates the move tree.
        /// Restores classification annotations (NAGs + eval comments) if present.
        /// </summary>
        private void ImportPgn(string pgn)
        {
            try
            {
                // Parse headers
                var headers = new Dictionary<string, string>();
                var lines = pgn.Split('\n');
                int moveTextStart = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        int keyEnd = line.IndexOf(' ');
                        if (keyEnd > 1)
                        {
                            string key = line.Substring(1, keyEnd - 1);
                            int valueStart = line.IndexOf('"') + 1;
                            int valueEnd = line.LastIndexOf('"');
                            if (valueStart > 0 && valueEnd > valueStart)
                                headers[key] = line.Substring(valueStart, valueEnd - valueStart);
                        }
                        moveTextStart = i + 1;
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        moveTextStart = i;
                        break;
                    }
                }

                string startFen = headers.TryGetValue("FEN", out var fenValue)
                    ? fenValue
                    : "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

                CancelClassification();
                boardControl.LoadFEN(startFen);
                moveTree.Clear(startFen);
                moveListBox.Items.Clear();
                displayedNodes.Clear();
                _analysisCache.Clear();
                _currentClassification = null;
                _classificationLookup = null;

                string moveText = string.Join(" ", lines.Skip(moveTextStart))
                    .Replace("\r", " ").Replace("\n", " ");

                var tokens = TokenizePgnMoveText(moveText);

                string currentFen = startFen;
                int skippedMoves = 0;
                var skippedMovesList = new List<string>();

                // Track annotations per node in move order
                var annotationList = new List<(MoveNode node, string nag, string comment)>();
                MoveNode? lastAppliedNode = null;

                isNavigating = true;

                foreach (var (type, value) in tokens)
                {
                    if (type == 'M')
                    {
                        string? uciMove = ConvertSanToUci(value, currentFen);
                        if (uciMove == null)
                        {
                            Debug.WriteLine($"Failed to parse move: {value} in position {currentFen}");
                            skippedMoves++;
                            if (skippedMovesList.Count < 5) skippedMovesList.Add(value);
                            continue;
                        }
                        boardControl.LoadFEN(currentFen);
                        if (!boardControl.MakeMove(uciMove))
                        {
                            Debug.WriteLine($"Failed to apply move: {uciMove}");
                            skippedMoves++;
                            if (skippedMovesList.Count < 5) skippedMovesList.Add(value);
                            continue;
                        }
                        string newFen = boardControl.GetFEN();
                        moveTree.AddMove(uciMove, value, newFen);
                        currentFen = newFen;
                        lastAppliedNode = moveTree.CurrentNode;
                        annotationList.Add((lastAppliedNode, "", ""));
                    }
                    else if (type == 'N' && lastAppliedNode != null && annotationList.Count > 0)
                    {
                        var last = annotationList[^1];
                        annotationList[^1] = (last.node, value, last.comment);
                    }
                    else if (type == 'C' && lastAppliedNode != null && annotationList.Count > 0)
                    {
                        if (value.StartsWith("[%cda "))
                        {
                            var ca = DeserializeCachedAnalysis(value);
                            if (ca != null)
                                _analysisCache[GetPositionKey(lastAppliedNode.FEN)] = ca;
                        }
                        else
                        {
                            var last = annotationList[^1];
                            annotationList[^1] = (last.node, last.nag, value);
                        }
                    }
                }

                isNavigating = false;

                bool hasAnnotations = annotationList.Any(a =>
                    !string.IsNullOrEmpty(a.nag) || !string.IsNullOrEmpty(a.comment));

                if (hasAnnotations)
                {
                    var moveResults = new List<MoveReviewResult>();
                    int whiteMoveCount = 0, blackMoveCount = 0;

                    foreach (var (node, nag, comment) in annotationList)
                    {
                        string symbol = !string.IsNullOrEmpty(nag) ? GetSymbolForNag(nag) : "";
                        double? evalAfter = !string.IsNullOrEmpty(comment) ? ParseEvalFromComment(comment) : null;
                        if (evalAfter.HasValue) node.Evaluation = evalAfter.Value;
                        var quality = !string.IsNullOrEmpty(symbol)
                            ? GetQualityForSymbol(symbol)
                            : !string.IsNullOrEmpty(comment)
                                ? ParseQualityFromComment(comment)
                                : MoveQualityAnalyzer.MoveQuality.Best;

                        moveResults.Add(new MoveReviewResult
                        {
                            Node = node,
                            PlayedMove = node.SanMove,
                            Quality = quality,
                            Symbol = symbol,
                            EvalAfter = evalAfter ?? 0,
                            IsWhiteMove = node.IsWhiteMove
                        });

                        if (node.IsWhiteMove) whiteMoveCount++;
                        else blackMoveCount++;
                    }

                    string annotator = headers.TryGetValue("Annotator", out var ann) ? ann : "chessdroid";
                    _currentClassification = new MoveClassificationResult
                    {
                        EngineName = annotator,
                        MoveResults = moveResults,
                        WhiteMoveCount = whiteMoveCount,
                        BlackMoveCount = blackMoveCount
                    };

                    foreach (var r in moveResults)
                    {
                        var counts = r.IsWhiteMove
                            ? _currentClassification.WhiteCounts
                            : _currentClassification.BlackCounts;
                        counts.TryGetValue(r.Quality, out int cnt);
                        counts[r.Quality] = cnt + 1;
                    }
                }

                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();

                if (hasAnnotations)
                    UpdateMoveListWithClassification();

                int moveCount = moveTree.GetMainLine().Count;
                if (skippedMoves > 0)
                {
                    string skippedInfo = skippedMovesList.Count < skippedMoves
                        ? $"{string.Join(", ", skippedMovesList)}..."
                        : string.Join(", ", skippedMovesList);
                    lblStatus.Text = $"Imported {moveCount} moves ({skippedMoves} skipped: {skippedInfo})";
                }
                else
                {
                    string suffix = hasAnnotations ? " (with annotations)" : "";
                    lblStatus.Text = $"Imported {moveCount} moves from PGN{suffix}";
                }

                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                SetLastMoveHighlight();
                UpdateMoveListSelection();
                RefreshEvalGraph();
            }
            catch (Exception ex)
            {
                isNavigating = false;
                lblStatus.Text = $"Import error: {ex.Message}";
                Debug.WriteLine($"PGN import error: {ex}");
            }
        }

        /// <summary>
        /// Converts SAN notation to UCI notation.
        /// </summary>
        private string? ConvertSanToUci(string san, string fen)
        {
            try
            {
                // Clean up the SAN
                san = san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "");

                string[] fenParts = fen.Split(' ');
                bool isWhiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                string enPassantSquare = fenParts.Length > 3 ? fenParts[3] : "-";

                // Handle castling
                if (san == "O-O" || san == "0-0")
                {
                    return isWhiteToMove ? "e1g1" : "e8g8";
                }
                if (san == "O-O-O" || san == "0-0-0")
                {
                    return isWhiteToMove ? "e1c1" : "e8c8";
                }

                // Parse piece type
                char pieceType = 'P'; // Default to pawn
                int idx = 0;
                if (san.Length > 0 && char.IsUpper(san[0]) && san[0] != 'O')
                {
                    pieceType = san[0];
                    idx = 1;
                }

                // Check for promotion
                char? promotion = null;
                if (san.Contains('='))
                {
                    int eqIdx = san.IndexOf('=');
                    if (eqIdx + 1 < san.Length)
                    {
                        promotion = char.ToLower(san[eqIdx + 1]);
                    }
                    san = san.Substring(0, eqIdx);
                }
                else if (san.Length >= 2 && char.IsUpper(san[san.Length - 1]) && san[san.Length - 1] != 'O')
                {
                    // Promotion without = (e.g., e8Q)
                    promotion = char.ToLower(san[san.Length - 1]);
                    san = san.Substring(0, san.Length - 1);
                }

                // Find destination square (last two characters that form a valid square)
                int destIdx = san.Length - 2;
                while (destIdx >= idx)
                {
                    if (destIdx + 1 < san.Length &&
                        san[destIdx] >= 'a' && san[destIdx] <= 'h' &&
                        san[destIdx + 1] >= '1' && san[destIdx + 1] <= '8')
                    {
                        break;
                    }
                    destIdx--;
                }

                if (destIdx < idx || destIdx + 1 >= san.Length)
                    return null;

                string destSquare = san.Substring(destIdx, 2);
                int destCol = destSquare[0] - 'a';
                int destRow = 7 - (destSquare[1] - '1'); // Convert to internal coordinates

                // Parse disambiguation (file and/or rank)
                char? disambigFile = null;
                char? disambigRank = null;
                if (destIdx > idx)
                {
                    string middle = san.Substring(idx, destIdx - idx).Replace("x", "");
                    foreach (char c in middle)
                    {
                        if (c >= 'a' && c <= 'h')
                            disambigFile = c;
                        else if (c >= '1' && c <= '8')
                            disambigRank = c;
                    }
                }

                // Build ChessBoard from FEN
                var board = ChessBoard.FromFEN(fen);

                // Determine piece character
                char pieceChar = isWhiteToMove ? pieceType : char.ToLower(pieceType);
                if (pieceType == 'P')
                    pieceChar = isWhiteToMove ? 'P' : 'p';

                // Find all pieces of this type that can move to destination
                var candidates = ChessRulesService.FindAllPiecesOfSameTypeWithEnPassant(board, char.ToLower(pieceChar), isWhiteToMove, destRow, destCol, enPassantSquare);

                foreach (var (row, col) in candidates)
                {
                    // Check disambiguation
                    char fileChar = (char)('a' + col);
                    char rankChar = (char)('1' + (7 - row));

                    if (disambigFile.HasValue && fileChar != disambigFile.Value)
                        continue;
                    if (disambigRank.HasValue && rankChar != disambigRank.Value)
                        continue;

                    // Check if this piece can reach the destination
                    if (ChessRulesService.CanReachSquareWithEnPassant(board, row, col, pieceChar, destRow, destCol, enPassantSquare))
                    {
                        string srcSquare = $"{fileChar}{rankChar}";
                        string uci = srcSquare + destSquare;
                        if (promotion.HasValue)
                            uci += promotion.Value;
                        return uci;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvertSanToUci error: {ex.Message} for {san}");
                return null;
            }
        }

        #endregion

        #region Move Classification

        // Store the current classification result
        private MoveClassificationResult? _currentClassification;
        private CancellationTokenSource? _classifyCts;

        private async void BtnClassifyMoves_Click(object? sender, EventArgs e)
        {
            var mainLine = moveTree.GetMainLine();
            if (mainLine.Count == 0)
            {
                lblStatus.Text = "No moves to classify";
                return;
            }

            if (engineService == null)
            {
                lblStatus.Text = "Engine not available";
                return;
            }

            // Confirm with user
            var result = MessageBox.Show(
                $"This will analyze all {mainLine.Count} moves and add quality symbols.\n" +
                $"This may take a while depending on engine depth ({config?.EngineDepth ?? 15}).\n\n" +
                "Continue?",
                "Classify Moves",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            await ClassifyMoves(mainLine);
        }

        private void CancelClassification()
        {
            _classifyCts?.Cancel();
        }

        private async Task ClassifyMoves(List<MoveNode> mainLine)
        {
            _classifyCts?.Dispose();
            _classifyCts = new CancellationTokenSource();
            var ct = _classifyCts.Token;

            btnClassifyMoves.Enabled = false;


            // Clear analysis cache to ensure fresh evaluations with correct perspective
            _analysisCache.Clear();

            // Set cached depth so navigation after classification uses the cache
            _cachedDepth = config?.EngineDepth ?? 15;

            var classification = new MoveClassificationResult
            {
                EngineName = engineService!.EngineName,
                EngineDepth = config?.EngineDepth ?? 15
            };

            // Initialize classification counts
            foreach (MoveQualityAnalyzer.MoveQuality q in Enum.GetValues(typeof(MoveQualityAnalyzer.MoveQuality)))
            {
                classification.WhiteCounts[q] = 0;
                classification.BlackCounts[q] = 0;
            }

            int whiteMoves = 0;
            int blackMoves = 0;

            for (int i = 0; i < mainLine.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var node = mainLine[i];
                lblStatus.Text = $"Classifying move {i + 1}/{mainLine.Count}: {node.SanMove}...";
                Application.DoEvents();

                try
                {
                    // Use ParentFEN for position before the move (more reliable than tracking)
                    string beforeFen = !string.IsNullOrEmpty(node.ParentFEN)
                        ? node.ParentFEN
                        : (i > 0 ? mainLine[i - 1].FEN : moveTree.Root.FEN);

                    // Analyze the position BEFORE the move to get the best move and eval
                    string cacheKey = GetPositionKey(beforeFen);
                    double? evalBeforeNullable = null;
                    string bestMove;
                    double evalBestMove;
                    string rawBeforeEval = "";

                    if (_analysisCache.TryGetValue(cacheKey, out var cached))
                    {
                        bestMove = cached.BestMove;
                        rawBeforeEval = cached.Evaluation;
                        evalBeforeNullable = ParseEvalNullable(cached.Evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;
                    }
                    else
                    {
                        // Run engine analysis with 3 PVs so cache is valid for position navigation
                        var analysisResult = await engineService.GetBestMoveAsync(beforeFen, classification.EngineDepth, 3, ct: ct);
                        bestMove = analysisResult.bestMove ?? "";
                        rawBeforeEval = analysisResult.evaluation;
                        evalBeforeNullable = ParseEvalNullable(analysisResult.evaluation);
                        evalBestMove = evalBeforeNullable ?? 0;

                        Debug.WriteLine($"  [Before] Raw eval: '{analysisResult.evaluation}' -> Parsed: {evalBeforeNullable?.ToString("F2") ?? "NULL"}");

                        // Cache it
                        _analysisCache[cacheKey] = new CachedAnalysis
                        {
                            BestMove = bestMove,
                            Evaluation = analysisResult.evaluation,
                            PVs = analysisResult.pvs ?? new List<string>(),
                            Evaluations = analysisResult.evaluations ?? new List<string>(),
                            WDL = analysisResult.wdl,
                            Depth = classification.EngineDepth
                        };
                    }

                    // Skip this move if we couldn't get a valid before evaluation
                    if (!evalBeforeNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty before evaluation from engine");
                        continue;
                    }

                    double evalBefore = evalBeforeNullable.Value;

                    // The played move's result is the evaluation AFTER the move
                    string afterCacheKey = GetPositionKey(node.FEN);
                    double? evalAfterNullable = null;
                    string rawAfterEval = "";

                    if (_analysisCache.TryGetValue(afterCacheKey, out var afterCached))
                    {
                        rawAfterEval = afterCached.Evaluation;
                        evalAfterNullable = ParseEvalNullable(afterCached.Evaluation);
                    }
                    else
                    {
                        // Use 3 PVs so cache is valid for position navigation
                        var afterResult = await engineService.GetBestMoveAsync(node.FEN, classification.EngineDepth, 3, ct: ct);
                        rawAfterEval = afterResult.evaluation;
                        evalAfterNullable = ParseEvalNullable(afterResult.evaluation);

                        Debug.WriteLine($"  [After] Raw eval: '{afterResult.evaluation}' -> Parsed: {evalAfterNullable?.ToString("F2") ?? "NULL"}");

                        _analysisCache[afterCacheKey] = new CachedAnalysis
                        {
                            BestMove = afterResult.bestMove ?? "",
                            Evaluation = afterResult.evaluation,
                            PVs = afterResult.pvs ?? new List<string>(),
                            Evaluations = afterResult.evaluations ?? new List<string>(),
                            WDL = afterResult.wdl,
                            Depth = classification.EngineDepth
                        };
                    }

                    // Skip this move if we couldn't get a valid evaluation
                    if (!evalAfterNullable.HasValue)
                    {
                        Debug.WriteLine($"  SKIPPING move {i + 1} - empty evaluation from engine");
                        continue;
                    }

                    double evalAfter = evalAfterNullable.Value;

                    // Calculate centipawn loss (from the moving side's perspective)
                    // All evaluations are in White's perspective (positive = good for White)
                    // For White's move: cpLoss = evalBefore - evalAfter (losing advantage is bad)
                    // For Black's move: cpLoss = evalAfter - evalBefore (opponent gaining advantage is bad)
                    double cpLoss = node.IsWhiteMove
                        ? (evalBefore - evalAfter)
                        : (evalAfter - evalBefore);

                    // Special handling for draw positions:
                    // If evalAfter is ~0.00 (draw), the player is accepting a draw.
                    // Cap the cpLoss at 1.5 pawns to avoid massive "blunders" for accepting draws.
                    if (IsDraw(evalAfter) && cpLoss > 1.5)
                    {
                        Debug.WriteLine($"  Draw position detected - capping cpLoss from {cpLoss:F2} to 1.50");
                        cpLoss = 1.5;
                    }

                    // Debug output for troubleshooting
                    Debug.WriteLine($"Move {i + 1}: {node.SanMove} | evalBefore={evalBefore:F2} evalAfter={evalAfter:F2} cpLoss={cpLoss:F2} (raw) | White={node.IsWhiteMove}");

                    // Clamp extreme values (cpLoss is in pawns, cap at 6 pawns = 600 centipawns)
                    if (cpLoss < 0) cpLoss = 0; // Can't have negative cp loss
                    if (cpLoss > 6) cpLoss = 6; // Cap extreme blunders at 6 pawns

                    // Check if it was the best move
                    bool isBestMove = node.UciMove == bestMove;

                    // Check for brilliant move using our dedicated detection
                    // This handles both capture sacrifices and implicit sacrifices (leaving pieces en prise)
                    bool isBrilliant = false;

                    // If move was already detected as brilliant in real-time (has !! in SanMove), preserve that
                    if (node.SanMove.EndsWith("!!"))
                    {
                        isBrilliant = true;
                    }
                    else if (isBestMove || cpLoss <= 0.10) // Only check moves that are best or very close
                    {
                        // Get the previous move's eval for context
                        double? prevEval = i > 0 ? classification.MoveResults.LastOrDefault()?.EvalAfter : null;

                        var (brilliant, _) = ConsoleOutputFormatter.IsBrilliantMove(
                            beforeFen, node.UciMove, evalAfter, prevEval);
                        isBrilliant = brilliant;
                    }

                    // Classify the move
                    // MoveQualityAnalyzer expects evals from the moving player's perspective
                    // For White: pass as-is (White's perspective)
                    // For Black: negate both to convert to Black's perspective
                    double qualityEvalBefore = node.IsWhiteMove ? evalBefore * 100 : -evalBefore * 100;
                    double qualityEvalAfter = node.IsWhiteMove ? evalAfter * 100 : -evalAfter * 100;

                    var quality = MoveQualityAnalyzer.AnalyzeMoveQuality(
                        evalBefore: qualityEvalBefore,
                        evalAfter: qualityEvalAfter,
                        isBestMove: isBestMove,
                        isSacrifice: isBrilliant
                    );

                    // If real-time detection marked this as brilliant, preserve that regardless of what
                    // the analyzer says. Real-time uses board analysis (actual sacrifice detection),
                    // which can catch brilliancies the eval-based analyzer misses.
                    string finalSymbol = quality.Symbol;
                    var finalQuality = quality.Quality;
                    if (isBrilliant && quality.Symbol != "!!")
                    {
                        finalSymbol = "!!";
                        finalQuality = MoveQualityAnalyzer.MoveQuality.Brilliant;
                    }

                    // Detect "only winning move" — best move where alternatives lose the advantage
                    if (isBestMove && finalSymbol == "" && finalQuality == MoveQualityAnalyzer.MoveQuality.Best)
                    {
                        if (_analysisCache.TryGetValue(cacheKey, out var beforeCached) &&
                            beforeCached.Evaluations.Count >= 2)
                        {
                            double? bestPvEval = ParseEvalNullable(beforeCached.Evaluations[0]);
                            double? secondPvEval = ParseEvalNullable(beforeCached.Evaluations[1]);

                            if (bestPvEval.HasValue && secondPvEval.HasValue)
                            {
                                double evalSwing = Math.Abs(bestPvEval.Value - secondPvEval.Value);
                                string[] fenParts = beforeFen.Split(' ');
                                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                                bool isOnlyWinningMove;
                                if (whiteToMove)
                                {
                                    bool basicTrigger = bestPvEval.Value >= 0.70 && secondPvEval.Value <= 0.27;
                                    bool swingTrigger = evalSwing >= 2.0 && bestPvEval.Value >= 0.27 && secondPvEval.Value <= 0.0;
                                    bool disasterTrigger = bestPvEval.Value >= 0.0 && secondPvEval.Value <= -1.50;
                                    isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger;
                                }
                                else
                                {
                                    bool basicTrigger = bestPvEval.Value <= -0.70 && secondPvEval.Value >= -0.27;
                                    bool swingTrigger = evalSwing >= 2.0 && bestPvEval.Value <= -0.27 && secondPvEval.Value >= 0.0;
                                    bool disasterTrigger = bestPvEval.Value <= 0.0 && secondPvEval.Value >= 1.50;
                                    isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger;
                                }

                                if (isOnlyWinningMove)
                                {
                                    finalSymbol = "!";
                                }
                            }
                        }
                    }

                    // Store eval on node so the graph has data for every move
                    node.Evaluation = evalAfter;
                    RefreshEvalGraph();

                    // Store the result
                    var moveResult = new MoveReviewResult
                    {
                        Node = node,
                        PlayedMove = node.SanMove,
                        BestMove = ConvertUciToSan(bestMove, beforeFen),
                        EvalBefore = evalBefore,
                        EvalAfter = evalAfter,
                        EvalBestMove = evalBestMove,
                        CentipawnLoss = cpLoss * 100, // Store in centipawns
                        Quality = finalQuality,
                        Symbol = finalSymbol,
                        IsWhiteMove = node.IsWhiteMove
                    };
                    classification.MoveResults.Add(moveResult);

                    // Update stats
                    if (node.IsWhiteMove)
                    {
                        whiteMoves++;
                        classification.WhiteCounts[quality.Quality]++;
                    }
                    else
                    {
                        blackMoves++;
                        classification.BlackCounts[quality.Quality]++;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error classifying move {i + 1}: {ex.Message}");
                }
            }

            btnClassifyMoves.Enabled = true;
            _classifyCts?.Dispose();
            _classifyCts = null;

            if (ct.IsCancellationRequested)
            {
                lblStatus.Text = "Classification cancelled";
                return;
            }

            // Store final stats
            classification.WhiteMoveCount = whiteMoves;
            classification.BlackMoveCount = blackMoves;

            _currentClassification = classification;

            // Update the move list with classification symbols
            UpdateMoveListWithClassification();
            RefreshEvalGraph();

            lblStatus.Text = $"Classification complete - {whiteMoves + blackMoves} moves analyzed";
        }

        /// <summary>
        /// Parse evaluation string. Returns null if parsing fails or string is empty.
        /// </summary>
        private double? ParseEvalNullable(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr))
                return null; // Empty = unknown, not 0!

            // Handle mate scores
            if (evalStr.Contains("Mate") || evalStr.StartsWith("M") || evalStr.StartsWith("+M") || evalStr.StartsWith("-M"))
            {
                string numPart = evalStr
                    .Replace("Mate in", "")
                    .Replace("M", "")
                    .Replace("+", "")
                    .Replace("-", "")
                    .Trim();

                if (int.TryParse(numPart, out int mateIn))
                {
                    double mateScore = Math.Max(10, 15 - mateIn * 0.5);
                    bool isNegative = evalStr.Contains("-");
                    return isNegative ? -mateScore : mateScore;
                }
                return evalStr.Contains("-") ? -12 : 12;
            }

            // Regular eval like "+1.25" or "-0.50" or "+-0.00" (draw)
            evalStr = evalStr.Replace("+", "").Trim();

            if (double.TryParse(evalStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double eval))
            {
                return eval;
            }

            if (double.TryParse(evalStr, out eval))
            {
                return eval;
            }

            return null;
        }

        private double ParseEval(string evalStr)
        {
            return ParseEvalNullable(evalStr) ?? 0;
        }

        /// <summary>
        /// Check if an evaluation indicates a draw (0.00 or very close)
        /// </summary>
        private bool IsDraw(double eval)
        {
            return Math.Abs(eval) < 0.05;
        }

        private void UpdateMoveListWithClassification()
        {
            if (_currentClassification == null) return;

            // Build dictionary for O(1) lookup instead of O(n) FirstOrDefault per item
            var resultLookup = _currentClassification.MoveResults.ToDictionary(r => r.Node, r => r);

            // Cache for DrawItem color lookups
            _classificationLookup = resultLookup;

            // Rebuild the move list items with classification symbols
            moveListBox.BeginUpdate();
            try
            {
                for (int i = 0; i < displayedNodes.Count && i < moveListBox.Items.Count; i++)
                {
                    var node = displayedNodes[i];
                    if (resultLookup.TryGetValue(node, out var result) && !string.IsNullOrEmpty(result.Symbol))
                    {
                        // Strip any existing annotation symbols from SanMove (e.g., from real-time detection)
                        // to avoid duplicates like "Nxd4!!!!" or "Bc5!!?!"
                        string cleanSan = StripAnnotationSymbols(node.SanMove);

                        // Update the item text to include the symbol
                        string moveText = node.IsWhiteMove
                            ? $"{node.MoveNumber}. {cleanSan}"
                            : $"{node.MoveNumber}...{cleanSan}";

                        moveListBox.Items[i] = $"{moveText} {result.Symbol}";
                    }
                }
            }
            finally
            {
                moveListBox.EndUpdate();
            }
        }

        /// <summary>
        /// Strips chess annotation symbols from a SAN move.
        /// Removes: !!, !, ?!, ?, ??, !? from the end of the move.
        /// </summary>
        private static string StripAnnotationSymbols(string san)
        {
            if (string.IsNullOrEmpty(san)) return san;

            // Remove annotation symbols from the end (order matters - check longer patterns first)
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var symbol in symbols)
            {
                if (san.EndsWith(symbol))
                {
                    san = san.Substring(0, san.Length - symbol.Length);
                    // Check again in case there are multiple (shouldn't happen but be safe)
                    break;
                }
            }
            return san;
        }

        private static string SerializeCachedAnalysis(CachedAnalysis ca)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"[%cda d={ca.Depth}");
            if (!string.IsNullOrEmpty(ca.BestMove)) sb.Append($";b={ca.BestMove}");
            if (!string.IsNullOrEmpty(ca.Evaluation)) sb.Append($";e={ca.Evaluation}");
            if (ca.PVs.Count > 0) sb.Append($";v={string.Join("~", ca.PVs)}");
            if (ca.Evaluations.Count > 0) sb.Append($";f={string.Join("~", ca.Evaluations)}");
            if (ca.WDL != null) sb.Append($";w={ca.WDL.Win}/{ca.WDL.Draw}/{ca.WDL.Loss}");
            sb.Append("]");
            return $"{{ {sb} }}";
        }

        private static CachedAnalysis? DeserializeCachedAnalysis(string comment)
        {
            if (!comment.StartsWith("[%cda ") || !comment.EndsWith("]")) return null;
            string inner = comment.Substring(6, comment.Length - 7);
            var ca = new CachedAnalysis();
            foreach (var field in inner.Split(';'))
            {
                int eq = field.IndexOf('=');
                if (eq < 0) continue;
                string key = field.Substring(0, eq);
                string val = field.Substring(eq + 1);
                switch (key)
                {
                    case "d":
                        if (int.TryParse(val, out int d)) ca.Depth = d;
                        break;
                    case "b":
                        ca.BestMove = val;
                        break;
                    case "e":
                        ca.Evaluation = val;
                        break;
                    case "v":
                        ca.PVs = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList();
                        break;
                    case "f":
                        ca.Evaluations = val.Split('~').Where(x => !string.IsNullOrEmpty(x)).ToList();
                        break;
                    case "w":
                        var wp = val.Split('/');
                        if (wp.Length == 3 &&
                            int.TryParse(wp[0], out int win) &&
                            int.TryParse(wp[1], out int draw) &&
                            int.TryParse(wp[2], out int loss))
                            ca.WDL = new WDLInfo(win, draw, loss);
                        break;
                }
            }
            return ca.Depth > 0 ? ca : null;
        }

        private static string GetNagForSymbol(string symbol) => symbol switch
        {
            "!!" => "$3",
            "!" => "$1",
            "!?" => "$5",
            "?!" => "$6",
            "?" => "$2",
            "??" => "$4",
            _ => ""
        };

        private static string GetSymbolForNag(string nag) => nag switch
        {
            "$3" => "!!",
            "$1" => "!",
            "$5" => "!?",
            "$6" => "?!",
            "$2" => "?",
            "$4" => "??",
            _ => ""
        };

        private static MoveQualityAnalyzer.MoveQuality GetQualityForSymbol(string symbol) => symbol switch
        {
            "!!" => MoveQualityAnalyzer.MoveQuality.Brilliant,
            "?!" => MoveQualityAnalyzer.MoveQuality.Inaccuracy,
            "?" => MoveQualityAnalyzer.MoveQuality.Mistake,
            "??" => MoveQualityAnalyzer.MoveQuality.Blunder,
            _ => MoveQualityAnalyzer.MoveQuality.Best
        };

        private static string GetInlineSymbol(string san)
        {
            if (string.IsNullOrEmpty(san)) return "";
            string[] symbols = { "!!", "??", "!?", "?!", "!", "?" };
            foreach (var s in symbols)
                if (san.EndsWith(s)) return s;
            return "";
        }

        private static string BuildPgnComment(MoveReviewResult result)
        {
            string eval = $"[{result.EvalAfter.ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture)}]";
            string label = result.Quality switch
            {
                MoveQualityAnalyzer.MoveQuality.Brilliant => "Brilliant",
                MoveQualityAnalyzer.MoveQuality.Best => "Best",
                MoveQualityAnalyzer.MoveQuality.Excellent => "Excellent",
                MoveQualityAnalyzer.MoveQuality.Good => "Good",
                MoveQualityAnalyzer.MoveQuality.Book => "Book",
                MoveQualityAnalyzer.MoveQuality.Inaccuracy => "Inaccuracy",
                MoveQualityAnalyzer.MoveQuality.Mistake => "Mistake",
                MoveQualityAnalyzer.MoveQuality.Blunder => "Blunder",
                MoveQualityAnalyzer.MoveQuality.Forced => "Forced",
                _ => "Best"
            };
            return $"{{ {eval} {label} }}";
        }

        private static double? ParseEvalFromComment(string comment)
        {
            var m = PgnEvalCommentRegex.Match(comment);
            if (!m.Success) return null;
            return double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : (double?)null;
        }

        private static MoveQualityAnalyzer.MoveQuality ParseQualityFromComment(string comment)
        {
            if (comment.Contains("Brilliant")) return MoveQualityAnalyzer.MoveQuality.Brilliant;
            if (comment.Contains("Blunder")) return MoveQualityAnalyzer.MoveQuality.Blunder;
            if (comment.Contains("Mistake")) return MoveQualityAnalyzer.MoveQuality.Mistake;
            if (comment.Contains("Inaccuracy")) return MoveQualityAnalyzer.MoveQuality.Inaccuracy;
            if (comment.Contains("Book")) return MoveQualityAnalyzer.MoveQuality.Book;
            if (comment.Contains("Excellent")) return MoveQualityAnalyzer.MoveQuality.Excellent;
            if (comment.Contains("Forced")) return MoveQualityAnalyzer.MoveQuality.Forced;
            if (comment.Contains("Good")) return MoveQualityAnalyzer.MoveQuality.Good;
            return MoveQualityAnalyzer.MoveQuality.Best;
        }

        // Returns tokens: 'M'=move, 'N'=NAG ($3 etc), 'C'=comment text
        private static List<(char type, string value)> TokenizePgnMoveText(string moveText)
        {
            var tokens = new List<(char, string)>();
            int i = 0, len = moveText.Length;
            while (i < len)
            {
                char c = moveText[i];
                if (c == '{')
                {
                    int end = moveText.IndexOf('}', i + 1);
                    if (end < 0) end = len - 1;
                    tokens.Add(('C', moveText.Substring(i + 1, end - i - 1).Trim()));
                    i = end + 1;
                }
                else if (c == '(')
                {
                    int depth = 1; i++;
                    while (i < len && depth > 0)
                    {
                        if (moveText[i] == '(') depth++;
                        else if (moveText[i] == ')') depth--;
                        i++;
                    }
                }
                else if (c == ';')
                {
                    while (i < len && moveText[i] != '\n') i++;
                }
                else if (c == '$')
                {
                    int start = i++;
                    while (i < len && char.IsDigit(moveText[i])) i++;
                    tokens.Add(('N', moveText.Substring(start, i - start)));
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    int start = i;
                    while (i < len && !char.IsWhiteSpace(moveText[i]) &&
                           moveText[i] != '{' && moveText[i] != '(' &&
                           moveText[i] != '$' && moveText[i] != ';')
                        i++;
                    string token = moveText.Substring(start, i - start);
                    if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                        continue;
                    if (PgnMoveNumberRegex.IsMatch(token)) continue;
                    var am = PgnAttachedMoveRegex.Match(token);
                    if (am.Success) token = am.Groups[1].Value.TrimStart('.');
                    if (!string.IsNullOrEmpty(token))
                        tokens.Add(('M', token));
                }
            }
            return tokens;
        }

        #endregion
    }
}

