using System.Collections.Concurrent;
using System.Diagnostics;
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

        public AnalysisBoardForm(AppConfig config, ChessEngineService? sharedEngineService = null)
        {
            this.config = config;
            this.engineService = sharedEngineService ?? new ChessEngineService(config);
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();

            InitializeComponent();
            ApplyTheme();
            InitializeServices();
            InitializeMatchControls();
            PopulatePiecesComboBox();

            // Initialize move tree with starting position
            moveTree = new MoveTree(boardControl.GetFEN());

            // Pre-initialize engine when form is shown (async, non-blocking)
            this.Shown += async (s, e) => await InitializeEngineAsync();
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
                                        btnNextMove, btnLoadFen, btnCopyFen, btnClassifyMoves,
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

            // Update FEN display
            UpdateFenDisplay();
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

            // Trigger initial layout for responsive checkbox positioning
            GrpEngineMatch_Resize(grpEngineMatch, EventArgs.Empty);
        }

        #region Event Handlers

        private void LeftPanel_Resize(object? sender, EventArgs e)
        {
            // Guard against resize during initialization
            if (boardControl == null || lblTurn == null || btnNewGame == null || lblPieces == null || cmbPieces == null)
                return;

            if (sender is Panel panel)
            {
                // Eval bar dimensions
                const int evalBarWidth = 24;
                const int evalBarGap = 4;
                int evalBarTotal = evalBarWidth + evalBarGap; // 28px reserved for eval bar

                // Calculate the largest square that fits in the available space
                // Leave room for eval bar on the left and controls below (about 110 pixels)
                int availableWidth = panel.Width - 20 - evalBarTotal; // 10px padding each side + eval bar
                int availableHeight = panel.Height - 110; // Room for controls below

                int boardSize = Math.Min(availableWidth, availableHeight);
                boardSize = Math.Max(boardSize, 300); // Minimum size

                // Center the board+evalbar group horizontally in the panel
                int groupWidth = boardSize + evalBarTotal;
                int groupX = (panel.Width - groupWidth) / 2;
                groupX = Math.Max(groupX, 10); // Minimum left margin

                // Position eval bar (left of board, same height)
                evalBar.Location = new Point(groupX, 5);
                evalBar.Size = new Size(evalBarWidth, boardSize);

                // Position board (right of eval bar)
                int boardX = groupX + evalBarTotal;
                boardControl.Size = new Size(boardSize, boardSize);
                boardControl.Location = new Point(boardX, 5);

                // Reposition controls below the board (aligned with board, not eval bar)
                int controlsY = boardControl.Bottom + 5;

                lblTurn.Location = new Point(boardX, controlsY);

                // Pieces selector (right of turn label)
                lblPieces.Location = new Point(boardX + 200, controlsY + 3);
                cmbPieces.Location = new Point(boardX + 255, controlsY);
                cmbPieces.Width = Math.Min(120, boardSize - 270);

                int buttonY = controlsY + 25;
                int buttonWidth = Math.Min(90, (boardSize - 30) / 5);
                int buttonSpacing = 5;

                btnNewGame.Location = new Point(boardX, buttonY);
                btnNewGame.Width = buttonWidth;

                btnFlipBoard.Location = new Point(boardX + buttonWidth + buttonSpacing, buttonY);
                btnFlipBoard.Width = buttonWidth;

                btnTakeBack.Location = new Point(boardX + 2 * (buttonWidth + buttonSpacing), buttonY);
                btnTakeBack.Width = buttonWidth;

                // Navigation buttons (smaller)
                int navButtonWidth = 35;
                int navX = boardX + 3 * (buttonWidth + buttonSpacing);
                btnPrevMove.Location = new Point(navX, buttonY);
                btnPrevMove.Width = navButtonWidth;
                btnNextMove.Location = new Point(navX + navButtonWidth + 2, buttonY);
                btnNextMove.Width = navButtonWidth;

                // FEN input row
                int fenY = buttonY + 32;
                int fenLabelWidth = 35;
                int fenButtonWidth = 55;
                int fenInputWidth = Math.Max(150, boardSize - fenLabelWidth - 2 * fenButtonWidth - 20);

                lblFen.Location = new Point(boardX, fenY + 3);
                txtFen.Location = new Point(boardX + fenLabelWidth, fenY);
                txtFen.Width = fenInputWidth;
                btnLoadFen.Location = new Point(txtFen.Right + 5, fenY);
                btnCopyFen.Location = new Point(btnLoadFen.Right + 5, fenY);
                btnSettings.Location = new Point(btnCopyFen.Right + 5, fenY);

                // Status label
                lblStatus.Location = new Point(boardX, fenY + 30);
                lblStatus.Width = boardSize;

                // Let middle and right panels fill the full form height
                // (they use Dock.Fill in the TableLayoutPanel)
            }
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

            // Clear engine arrows (new analysis will redraw them)
            boardControl.ClearEngineArrows();

            // Skip if we're navigating (not making a new move)
            if (isNavigating) return;

            // Convert UCI to SAN for display
            string san = ConvertUciToSan(e.UciMove, moveTree.CurrentNode.FEN);

            // Add move to tree (handles variations automatically)
            moveTree.AddMove(e.UciMove, san, e.FEN);

            // Update move list display
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Auto-analyze if enabled
            if (!matchRunning)
            {
                _ = TriggerAutoAnalysis();
            }
        }

        private async Task TriggerAutoAnalysis()
        {
            // Cancel previous analysis if still running
            autoAnalysisCts?.Cancel();
            autoAnalysisCts = new CancellationTokenSource();
            var token = autoAnalysisCts.Token;

            // Small delay to avoid analyzing during rapid moves
            await Task.Delay(150);

            if (!token.IsCancellationRequested)
            {
                await AnalyzeCurrentPosition();
            }
        }

        private void BoardControl_BoardChanged(object? sender, EventArgs e)
        {
            UpdateFenDisplay();
            UpdateTurnLabel();
        }

        private void BtnNewGame_Click(object? sender, EventArgs e)
        {
            boardControl.ClearEngineArrows();
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
            using var settingsForm = new SettingsForm(config);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Reload engine service with new settings
                engineService?.Dispose();
                engineService = new ChessEngineService(config);
                InitializeServices();
                ApplyTheme();
                _analysisCache.Clear(); // Clear cache when settings change

                // Initialize the engine immediately
                await InitializeEngineAsync();
            }
        }

        private void BtnFlipBoard_Click(object? sender, EventArgs e)
        {
            boardControl.FlipBoard();
        }

        private void BtnTakeBack_Click(object? sender, EventArgs e)
        {
            // Take back removes the current move from the tree
            if (moveTree.CurrentNode != moveTree.Root)
            {
                var parent = moveTree.CurrentNode.Parent;
                if (parent != null)
                {
                    // Remove this node from parent's children
                    parent.Children.Remove(moveTree.CurrentNode);
                    moveTree.CurrentNode = parent;

                    // Load previous position
                    boardControl.LoadFEN(parent.FEN);

                    UpdateMoveList();
                    UpdateFenDisplay();
                    UpdateTurnLabel();
                    lblStatus.Text = "Move taken back";
                }
            }
        }

        private void BtnPrevMove_Click(object? sender, EventArgs e)
        {
            _pvAnimationCts?.Cancel();
            if (moveTree.GoBack())
            {
                isNavigating = true;
                try
                {
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
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

        private void BtnLoadFen_Click(object? sender, EventArgs e)
        {
            string fen = txtFen.Text.Trim();
            if (!string.IsNullOrEmpty(fen))
            {
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
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Invalid FEN: {ex.Message}";
                }
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
            if (e.Index < 0) return;

            // Draw background
            e.DrawBackground();

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
                symbolColor = ColorScheme.InaccuracyColor;
            }
            else if (text.EndsWith("?") && !text.EndsWith("??") && !text.EndsWith("?!"))
            {
                symbol = "?";
                moveText = text[..^1].TrimEnd();
                symbolColor = ColorScheme.MistakeColor;
            }
            else if (text.EndsWith("!") && !text.EndsWith("!!"))
            {
                symbol = "!";
                moveText = text[..^1].TrimEnd();
                symbolColor = ColorScheme.OnlyMoveColor;
            }

            // Determine text color based on selection and theme
            Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? (isDark ? Color.White : SystemColors.HighlightText)
                : (isDark ? Color.White : Color.Black);

            // Color the whole move text to match the symbol for annotated moves
            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected && !string.IsNullOrEmpty(symbol))
            {
                textColor = symbolColor;
            }

            // Color move text based on classification quality (Best/Excellent/Good)
            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected &&
                string.IsNullOrEmpty(symbol) &&
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

            // Draw move text
            using (var brush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(moveText, e.Font!, brush, e.Bounds.Left + 2, e.Bounds.Top + 1);
            }

            // Draw symbol in color if present
            if (!string.IsNullOrEmpty(symbol))
            {
                // Measure move text width to position symbol
                var moveSize = e.Graphics.MeasureString(moveText + " ", e.Font!);

                using (var symbolBrush = new SolidBrush(symbolColor))
                using (var boldFont = new Font(e.Font!.FontFamily, e.Font.Size, FontStyle.Bold))
                {
                    e.Graphics.DrawString(symbol, boldFont, symbolBrush,
                        e.Bounds.Left + 2 + moveSize.Width - 4, e.Bounds.Top + 1);
                }
            }

            // Draw focus rectangle if focused
            e.DrawFocusRectangle();
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

        private void NavigateToStart()
        {
            isNavigating = true;
            try
            {
                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
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
            isNavigating = true;
            try
            {
                moveTree.GoToEnd();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
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
            moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear(); // Clear analysis cache for new match

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
            if (result.Termination != MatchTermination.UserStopped)
            {
                analysisOutput.AppendText($"White remaining: {FormatClock(result.WhiteTimeRemainingMs)}\n");
                analysisOutput.AppendText($"Black remaining: {FormatClock(result.BlackTimeRemainingMs)}\n");
            }
            analysisOutput.ScrollToCaret();

            lblStatus.Text = result.GetResultString();

            // Re-enable controls
            SetMatchControlsEnabled(false);

            // Clean up match service
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

        #region Analysis

        private async Task AnalyzeCurrentPosition()
        {
            if (engineService == null)
            {
                lblStatus.Text = "Engine not available";
                return;
            }

            string fen = boardControl.GetFEN();
            string cacheKey = GetPositionKey(fen);
            int depth = config?.EngineDepth ?? 15;

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
                // Use cached result - instant display
                DisplayAnalysisResult(fen, cached.BestMove, cached.Evaluation,
                    cached.PVs, cached.Evaluations, cached.WDL, cached.Depth, fromCache: true);
                return;
            }

            lblStatus.Text = "Analyzing...";


            try
            {
                // Get engine analysis
                int multiPV = 3; // Always get 3 lines for analysis board

                var result = await engineService.GetBestMoveAsync(fen, depth, multiPV);

                if (string.IsNullOrEmpty(result.bestMove))
                {
                    lblStatus.Text = "Analysis failed";

                    return;
                }

                var pvs = result.pvs ?? new List<string>();
                var evals = result.evaluations ?? new List<string>();

                // Cache the result
                _analysisCache[cacheKey] = new CachedAnalysis
                {
                    BestMove = result.bestMove,
                    Evaluation = result.evaluation,
                    PVs = new List<string>(pvs),
                    Evaluations = new List<string>(evals),
                    WDL = result.wdl,
                    Depth = depth
                };

                // Display results
                DisplayAnalysisResult(fen, result.bestMove, result.evaluation,
                    pvs, evals, result.wdl, depth, fromCache: false);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Analysis error: {ex.Message}";
                Debug.WriteLine($"Analysis error: {ex}");
            }
            finally
            {
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

            // Get book moves
            List<BookMove>? bookMoves = null;
            if (openingBookService?.IsLoaded == true)
            {
                var moves = openingBookService.GetBookMovesForPosition(fen);
                if (moves != null && moves.Count > 0)
                {
                    bookMoves = moves.Select(pm => new BookMove
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
            }

            // Display results (respect user's line visibility settings)
            consoleFormatter?.DisplayAnalysisResults(
                recommendedMove,
                evaluation,
                pvs,
                evals,
                fen,
                null, // No previous eval for blunder detection
                config?.ShowBestLine ?? true,
                config?.ShowSecondLine ?? true,
                config?.ShowThirdLine ?? true,
                wdl,
                bookMoves);

            // Update engine arrows on board (suppress during PV animation)
            if (!isNavigating)
            {
                if (config?.ShowEngineArrows == true)
                    UpdateEngineArrows(pvs);
                else
                    boardControl.ClearEngineArrows();
            }

            // Update eval bar with the evaluation
            UpdateEvalBar(evaluation);

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

        private void UpdateEngineArrows(List<string> pvs)
        {
            var arrows = new List<(int fromRow, int fromCol, int toRow, int toCol, Color color)>();

            var lineConfigs = new[]
            {
                (enabled: config?.ShowBestLine ?? true, index: 0, color: Color.FromArgb(180, 0, 200, 80)),      // Green
                (enabled: config?.ShowSecondLine ?? false, index: 1, color: Color.FromArgb(180, 200, 200, 0)),   // Yellow
                (enabled: config?.ShowThirdLine ?? false, index: 2, color: Color.FromArgb(180, 200, 60, 60))     // Red
            };

            foreach (var line in lineConfigs)
            {
                if (line.enabled && line.index < pvs.Count && !string.IsNullOrEmpty(pvs[line.index]))
                {
                    string firstMove = pvs[line.index].Split(' ')[0];
                    if (firstMove.Length >= 4)
                    {
                        var sq = UciToSquares(firstMove);
                        arrows.Add((sq.fromRow, sq.fromCol, sq.toRow, sq.toCol, line.color));
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
            moveListBox.Items.Clear();
            displayedNodes.Clear();

            // Build the display list with variations
            BuildMoveListRecursive(moveTree.Root, 0);

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
            // Find current node in displayed nodes
            var current = moveTree.CurrentNode;
            if (current == moveTree.Root)
            {
                moveListBox.ClearSelected();
                return;
            }

            int idx = displayedNodes.IndexOf(current);
            if (idx >= 0 && idx < moveListBox.Items.Count)
            {
                moveListBox.SelectedIndex = idx;
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
            try
            {
                foreach (var node in nodes)
                {
                    await Task.Delay(400, ct);
                    if (ct.IsCancellationRequested) return;

                    moveTree.GoToNode(node);
                    boardControl.LoadFEN(node.FEN);
                    UpdateMoveListSelection();
                    UpdateFenDisplay();
                    UpdateTurnLabel();

                    string statusText = $"Move {node.MoveNumber}";
                    if (node.VariationDepth > 0)
                        statusText += " (variation)";
                    lblStatus.Text = statusText;
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                isNavigating = false;
            }
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

            // Add FEN if not standard starting position
            string rootFen = moveTree.Root.FEN;
            if (rootFen != "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
            {
                sb.AppendLine($"[FEN \"{rootFen}\"]");
                sb.AppendLine("[SetUp \"1\"]");
            }

            sb.AppendLine();

            // Generate move text
            var mainLine = moveTree.GetMainLine();
            if (mainLine.Count > 0)
            {
                var moveText = new System.Text.StringBuilder();
                int lineLength = 0;

                foreach (var node in mainLine)
                {
                    string moveStr;
                    if (node.IsWhiteMove)
                    {
                        moveStr = $"{node.MoveNumber}. {node.SanMove}";
                    }
                    else
                    {
                        moveStr = node.SanMove;
                    }

                    // Word wrap at ~80 characters
                    if (lineLength + moveStr.Length + 1 > 80)
                    {
                        moveText.AppendLine();
                        lineLength = 0;
                    }
                    else if (moveText.Length > 0)
                    {
                        moveText.Append(' ');
                        lineLength++;
                    }

                    moveText.Append(moveStr);
                    lineLength += moveStr.Length;
                }

                sb.Append(moveText);
                sb.Append(" *"); // Result
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
                        // Parse header: [Key "Value"]
                        int keyEnd = line.IndexOf(' ');
                        if (keyEnd > 1)
                        {
                            string key = line.Substring(1, keyEnd - 1);
                            int valueStart = line.IndexOf('"') + 1;
                            int valueEnd = line.LastIndexOf('"');
                            if (valueStart > 0 && valueEnd > valueStart)
                            {
                                string value = line.Substring(valueStart, valueEnd - valueStart);
                                headers[key] = value;
                            }
                        }
                        moveTextStart = i + 1;
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        moveTextStart = i;
                        break;
                    }
                }

                // Get starting FEN (or use standard position)
                string startFen = headers.TryGetValue("FEN", out var fenValue)
                    ? fenValue
                    : "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

                // Reset board and tree
                boardControl.LoadFEN(startFen);
                moveTree.Clear(startFen);
                moveListBox.Items.Clear();
                displayedNodes.Clear();
                _analysisCache.Clear();

                // Extract move text (everything after headers)
                string moveText = string.Join(" ", lines.Skip(moveTextStart));

                // Clean up the move text using cached regex patterns
                moveText = PgnCommentRegex.Replace(moveText, " ");      // Remove comments
                moveText = PgnVariationRegex.Replace(moveText, " ");    // Remove variations (simple)
                moveText = PgnNagRegex.Replace(moveText, " ");          // Remove NAGs
                moveText = PgnContinuationRegex.Replace(moveText, " "); // Remove continuation dots
                moveText = moveText.Replace("\r", " ").Replace("\n", " ");

                // Parse moves
                var tokens = moveText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string currentFen = startFen;
                int skippedMoves = 0;
                var skippedMovesList = new List<string>();

                // Set navigating flag to prevent BoardControl_MoveMade from double-adding moves
                isNavigating = true;

                foreach (var token in tokens)
                {
                    // Skip results
                    if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                        continue;

                    // Skip standalone move numbers like "1." or "1"
                    if (PgnMoveNumberRegex.IsMatch(token))
                        continue;

                    string san = token.Trim();

                    // Handle move numbers attached to moves: "1.e4" -> "e4", "12.Nf3" -> "Nf3"
                    var attachedMoveMatch = PgnAttachedMoveRegex.Match(san);
                    if (attachedMoveMatch.Success)
                    {
                        san = attachedMoveMatch.Groups[1].Value;
                    }

                    if (string.IsNullOrEmpty(san))
                        continue;

                    // Convert SAN to UCI
                    string? uciMove = ConvertSanToUci(san, currentFen);
                    if (uciMove == null)
                    {
                        Debug.WriteLine($"Failed to parse move: {san} in position {currentFen}");
                        skippedMoves++;
                        if (skippedMovesList.Count < 5) skippedMovesList.Add(san);
                        continue;
                    }

                    // Apply the move using the board control
                    boardControl.LoadFEN(currentFen);
                    if (!boardControl.MakeMove(uciMove))
                    {
                        Debug.WriteLine($"Failed to apply move: {uciMove}");
                        skippedMoves++;
                        if (skippedMovesList.Count < 5) skippedMovesList.Add(san);
                        continue;
                    }
                    string newFen = boardControl.GetFEN();

                    // Add to tree
                    moveTree.AddMove(uciMove, san, newFen);
                    currentFen = newFen;
                }

                // Reset navigating flag
                isNavigating = false;

                // Update display
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                UpdateMoveList();
                UpdateFenDisplay();
                UpdateTurnLabel();

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
                    lblStatus.Text = $"Imported {moveCount} moves from PGN";
                }

                // Navigate to start
                moveTree.GoToStart();
                boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                UpdateMoveListSelection();
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

        private async Task ClassifyMoves(List<MoveNode> mainLine)
        {
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
                        var analysisResult = await engineService.GetBestMoveAsync(beforeFen, classification.EngineDepth, 3);
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
                        var afterResult = await engineService.GetBestMoveAsync(node.FEN, classification.EngineDepth, 3);
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
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error classifying move {i + 1}: {ex.Message}");
                }
            }

            // Store final stats
            classification.WhiteMoveCount = whiteMoves;
            classification.BlackMoveCount = blackMoves;

            _currentClassification = classification;

            // Update the move list with classification symbols
            UpdateMoveListWithClassification();

            btnClassifyMoves.Enabled = true;
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

        #endregion
    }
}

