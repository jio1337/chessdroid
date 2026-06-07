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
        private static readonly Regex PgnOpeningHeaderRegex = new(@"\[Opening ""([^""]+)""\]", RegexOptions.Compiled);
        private static readonly Regex PgnMoveTokenPrefixRegex = new(@"^\d+\.", RegexOptions.Compiled);

        /// <summary>
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

        // Move list grid — one MovePair per ListBox row
        private sealed class MovePair
        {
            public MoveNode? White;
            public MoveNode? Black;
            public bool IsVariation;
            public int  IndentLevel;       // pixels = IndentLevel * 14
            public bool IsVariationStart;  // show "(" prefix on first row of a variation
            public string? WhiteSymbol;
            public string? BlackSymbol;
        }
        private List<MovePair> _movePairs = new();
        private MoveNode? _activeNode;  // which half is highlighted in the current row

        // Engine match
        private EngineMatchService? matchService;
        private System.Windows.Forms.Timer clockTimer = null!;
        private bool matchRunning = false;
        private double? _previousMatchEval; // Track previous eval for brilliant move detection
        private bool _awaitingMatchAnimation = false;
        private int _matchWhiteElo;
        private int _matchBlackElo;
        private string _matchWhiteFileName = "";
        private string _matchBlackFileName = "";
        private string[] _matchEngineFiles = [];
        // Series tracking
        private int    _seriesTotal    = 1;
        private int    _seriesPlayed   = 0;
        private double _seriesEng1Score = 0;
        private double _seriesEng2Score = 0;
        private string _seriesEng1File = "";
        private string _seriesEng2File = "";
        private string _seriesCurrentWhiteFile = "";
        private string _seriesStartFen = "";
        private OpeningEntry? _matchBookOpening;
        // Overlay labels on top/bottom material strips showing engine name + ELO during a match
        private Label _lblBlackEngineInfo = null!;
        private Label _lblWhiteEngineInfo = null!;

        // Bot mode
        private bool _botModeActive  = false;
        private bool _chess960Active = false;
        private bool _drillBotActive   = false; // true while a drill Practice vs Bot session is running
        private bool _drillWatchActive = false; // true while a drill Watch Engines session is running
        private Dictionary<string, int> _watchPositionCounts = new();
        private bool _matchPanelActive = false;
        private BotSettings? _botSettings;
        private ChessEngineService? _botEngine;
        private Dictionary<string, int> _botPositionCounts = new();
        private CancellationTokenSource? _botMoveCts;
        private AppConfig? _challengeSnapshot; // non-null while challenge mode is active
        private bool _bookArrowsActive = false; // true when book arrows are shown (suppress engine arrows)
        private System.Windows.Forms.Timer _autoPlayTimer = null!;
        private bool _autoPlaying = false;

        // Piece set hover preview — query the combobox's internal listbox for the hovered item
        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public int rcItemLeft, rcItemTop, rcItemRight, rcItemBottom;
            public int rcButtonLeft, rcButtonTop, rcButtonRight, rcButtonBottom;
            public int stateButton;
            public IntPtr hwndCombo, hwndItem, hwndList;
        }
        [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO pcbi);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern IntPtr GetFocus();
        private const int LB_GETCURSEL = 0x0188;
        private string _hoverPreviewSaved = "";
        private int    _pieceSavedIndex = -1;
        private System.Windows.Forms.Timer? _hoverPreviewTimer;

        // Board color hover preview
        private string _boardColorSavedLight = "";
        private string _boardColorSavedDark = "";
        private bool _boardColorSavedRainbow = false;
        private bool _boardColorSavedWave = false;
        private bool _boardColorSavedMonochrome = false;
        private int  _boardColorSavedIndex = -1;
        private bool _restoringBoardColor = false;
        private System.Windows.Forms.Timer? _boardColorHoverTimer;
        private System.Windows.Forms.Timer? _drillHoverTimer;
        private int _drillHoverLastIdx = -1;

        private readonly AudioService _audioService = new();

        // Console font (managed here to allow proper disposal on change)
        private Font _consoleFont = new Font("Consolas", 10f);

        // Game library
        private GameLibraryService? _libraryService;
        private Dictionary<string, string> _pgnHeaders = new();
        private string _matchWhiteName = "";
        private string _matchBlackName = "";
        private string _libraryGameId = "";

        private EvalGraphControl? _evalGraph;

        public AnalysisBoardForm(AppConfig config, ChessEngineService? sharedEngineService = null)
        {
            this.config = config;
            this.engineService = sharedEngineService ?? new ChessEngineService(config);
            this.sharpnessAnalyzer = new MoveSharpnessAnalyzer();

            InitializeComponent();

            // Engine info labels — overlay right side of material strips during engine matches
            var infoLabelFont = new Font("Consolas", 8F);
            _lblBlackEngineInfo = new Label
            {
                Visible = false, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Font = infoLabelFont, BackColor = Color.Transparent
            };
            _lblWhiteEngineInfo = new Label
            {
                Visible = false, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Font = infoLabelFont, BackColor = Color.Transparent
            };
            leftPanel.Controls.Add(_lblBlackEngineInfo);
            leftPanel.Controls.Add(_lblWhiteEngineInfo);
            _lblBlackEngineInfo.BringToFront();
            _lblWhiteEngineInfo.BringToFront();

            _evalGraph = new EvalGraphControl { Dock = DockStyle.Top, Height = 65, Name = "evalGraph" };
            _evalGraph.MoveNodeSelected += EvalGraph_MoveNodeSelected;
            rightPanel.Controls.Add(_evalGraph);
            // z-order: lblAnalysis(top) → evalGraph → grpEngineMatch → analysisOutput(fill)
            rightPanel.Controls.SetChildIndex(_evalGraph, rightPanel.Controls.IndexOf(grpEngineMatch));
            grpEngineMatch.Visible = false; // hidden until user toggles ⚔ Match button

            ApplyTheme();
            boardControl.SetSquareColors(
                ColorTranslator.FromHtml(config.LightSquareColor),
                ColorTranslator.FromHtml(config.DarkSquareColor));
            boardControl.MonochromeMode  = config.MonochromeBoard;
            boardControl.ShowSquareLabels = config.ShowSquareLabels;
            boardControl.HideCoordinates  = !config.ShowCoordinates;
            boardControl.ShowLastMoveHighlight = config.ShowLastMoveHighlight;
            boardControl.AnimationDurationMs = config.AnimationDurationMs;
            ApplyBoardFxFromConfig();
            InitializeSounds();
            InitializeServices();
            InitializeMatchControls();
            PopulatePiecesComboBox();
            PopulateBoardColorComboBox();

            // Initialize move tree with starting position
            moveTree = new MoveTree(boardControl.GetFEN());

            // Shown fires after the form is fully laid out — set splitter positions then
            this.Shown += async (s, e) =>
            {
                // Outer split: board | (moves + analysis)
                outerSplit.Panel1MinSize = 200;
                outerSplit.Panel2MinSize = 280;
                if (config.BoardSplitterDistance > 0)
                {
                    outerSplit.SplitterDistance = Math.Clamp(config.BoardSplitterDistance,
                        outerSplit.Panel1MinSize, outerSplit.Width - outerSplit.Panel2MinSize);
                }
                else
                {
                    bool showStrips = config?.ShowMaterialStrips != false;
                    int sh = showStrips ? 22 : 0;
                    int sg = showStrips ? 2 : 0;
                    int evalBarTotal = config?.ShowEvalBar != false ? 32 : 0;
                    int boardSize = Math.Max(300, outerSplit.Height - 2 * sh - 2 * sg);
                    int idealLeft = boardSize + evalBarTotal + 10;
                    outerSplit.SplitterDistance = Math.Clamp(idealLeft,
                        outerSplit.Panel1MinSize, outerSplit.Width - outerSplit.Panel2MinSize);
                }

                // Inner split: moves | analysis
                splitRightPanels.Panel1MinSize = 80;
                splitRightPanels.Panel2MinSize = 200;
                splitRightPanels.SplitterDistance = config!.SplitterDistance > 0 ? config.SplitterDistance : 130;

                pnlBoardControls.Height = 148;
                LeftPanel_Resize(leftPanel, EventArgs.Empty);
                PnlBoardControls_Resize(pnlBoardControls, EventArgs.Empty);
                this.MinimumSize = this.Size;
                InitTrainingPanel();

                toolTip.SetToolTip(btnNewGame, "New Game");
                toolTip.SetToolTip(btnChess960, "Chess 960 — Position Browser");

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
            var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");
            bool isDarkMode = ThemeService.IsDarkTheme(config?.Theme);

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

            // Pieces + board color comboboxes
            cmbPieces.BackColor = scheme.ButtonBackColor;
            cmbPieces.ForeColor = scheme.TextColor;
            cmbBoardColor.BackColor = scheme.ButtonBackColor;
            cmbBoardColor.ForeColor = scheme.TextColor;

            // Standard buttons
            foreach (var btn in new[] { btnSettings, btnNewGame, btnFlipBoard, btnTakeBack, btnPrevMove,
                                        btnNextMove, btnAutoPlay, btnPlayBot, btnEditPosition, btnTraining, btnMatch, btnTournament, btnChess960, btnLoadFen, btnCopyFen, btnClassifyMoves,
                                        btnExportPgn, btnImportPgn, btnSaveToLibrary, btnOpenLibrary, btnOpenings })
            {
                btn.BackColor = scheme.ButtonBackColor;
                btn.ForeColor = scheme.ButtonForeColor;
            }

            // Checkboxes
            chkFromPosition.ForeColor = scheme.TextColor;
            chkAdjudicate.ForeColor   = scheme.TextColor;
            chkAutoSavePgn.ForeColor  = scheme.TextColor;
            chkUseBook.ForeColor      = scheme.TextColor;

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
            btnEngineProfiles.BackColor = scheme.ButtonBackColor;
            btnEngineProfiles.ForeColor = scheme.ButtonForeColor;

            // Engine info overlay labels
            Color infoLabelColor = isDarkMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(70, 70, 70);
            _lblBlackEngineInfo.ForeColor = infoLabelColor;
            _lblWhiteEngineInfo.ForeColor = infoLabelColor;

            // Material strips text color
            Color stripTextColor = isDarkMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(70, 70, 70);
            _materialTop?.SetTextColor(stripTextColor);
            _materialBottom?.SetTextColor(stripTextColor);
            _materialTop?.SetDarkMode(isDarkMode);
            _materialBottom?.SetDarkMode(isDarkMode);

            // Update FEN display
            UpdateFenDisplay();

            if (_evalGraph != null) _evalGraph.Visible = config?.ShowEvalGraph ?? true;
            RefreshEvalGraph();
            ApplyConsoleFont();
            ApplyTrainingTheme();
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

        private void InitializeSounds()
        {
            _audioService.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio"));
        }

        private void PlayMoveSound(bool isCapture, string san)
        {
            _audioService.PlayMoveSound(isCapture, san, config.SoundEffectsEnabled);
        }

        private void PlayGameEndSound()
        {
            _audioService.PlayGameEndSound(config.SoundEffectsEnabled);
        }

        private void InitializeServices()
        {
            ExplanationFormatter.LoadFromConfig(config);

            // Initialize console formatter for analysis output
            consoleFormatter = new ConsoleOutputFormatter(
                analysisOutput,
                config,
                MovesExplanation.GenerateMoveExplanation);
            consoleFormatter.OnSeeLineClicked  += InsertPvIntoMoveTree;
            consoleFormatter.OnNavigateToNode  += node =>
            {
                int idx = _movePairs.FindIndex(p => p.White == node || p.Black == node);
                if (idx >= 0) { _activeNode = node; moveListBox.SelectedIndex = idx; }
            };
            consoleFormatter.OnShowGameReview  += () =>
            {
                if (_currentClassification != null)
                    consoleFormatter.DisplayClassificationSummary(_currentClassification);
            };

            // Initialize game library
            _libraryService = new GameLibraryService(AppDomain.CurrentDomain.BaseDirectory);

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
            // Wire hover-preview events once (guard against double-subscribe on re-populate)
            cmbPieces.DropDown     -= CmbPieces_DropDown;
            cmbPieces.DropDownClosed -= CmbPieces_DropDownClosed;
            cmbPieces.DropDown     += CmbPieces_DropDown;
            cmbPieces.DropDownClosed += CmbPieces_DropDownClosed;

            if (_hoverPreviewTimer == null)
            {
                _hoverPreviewTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _hoverPreviewTimer.Tick += HoverPreviewTimer_Tick;
            }

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

                if (config != null && config.SelectedSite != selectedTemplate)
                {
                    config.SelectedSite = selectedTemplate;
                    config.Save();
                }
            }
        }

        private void CmbPieces_DropDown(object? sender, EventArgs e)
        {
            _hoverPreviewSaved = cmbPieces.SelectedItem?.ToString() ?? "";
            _pieceSavedIndex   = cmbPieces.SelectedIndex;
            _hoverPreviewTimer?.Start();
        }

        private void HoverPreviewTimer_Tick(object? sender, EventArgs e)
        {
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (!GetComboBoxInfo(cmbPieces.Handle, ref info) || info.hwndList == IntPtr.Zero) return;
            int idx = SendMessage(info.hwndList, LB_GETCURSEL, 0, 0);
            if (idx < 0 || idx >= cmbPieces.Items.Count) return;
            string? hovered = cmbPieces.Items[idx]?.ToString();
            if (string.IsNullOrEmpty(hovered)) return;
            boardControl.SetTemplateSet(hovered);
            _materialTop?.SetTemplateSet(hovered);
            _materialBottom?.SetTemplateSet(hovered);
        }

        private void CmbPieces_DropDownClosed(object? sender, EventArgs e)
        {
            _hoverPreviewTimer?.Stop();

            // If index changed, user committed — SelectedIndexChanged will apply and save
            if (cmbPieces.SelectedIndex != _pieceSavedIndex) return;

            // User cancelled — revert board visuals (combo is already at saved index)
            if (!string.IsNullOrEmpty(_hoverPreviewSaved))
            {
                boardControl.SetTemplateSet(_hoverPreviewSaved);
                _materialTop?.SetTemplateSet(_hoverPreviewSaved);
                _materialBottom?.SetTemplateSet(_hoverPreviewSaved);
            }
        }

        private void ApplyBoardFxFromConfig()
        {
            boardControl.GradientBoard    = config.GradientBoard;
            boardControl.VignetteEnabled  = config.BoardVignette;
            boardControl.VignetteAlpha    = config.VignetteAlpha;
            boardControl.PieceGlowEnabled = config.PieceGlow;
            boardControl.BoardFrameEnabled = config.BoardFrame;
            boardControl.BoardFrameWidth  = config.BoardFrameWidth;
            try { boardControl.BoardFrameColor = ColorTranslator.FromHtml(config.BoardFrameColor); }
            catch { boardControl.BoardFrameColor = System.Drawing.Color.FromArgb(80, 50, 25); }
        }

        private void PopulateBoardColorComboBox()
        {
            cmbBoardColor.DropDown     -= CmbBoardColor_DropDown;
            cmbBoardColor.DropDownClosed -= CmbBoardColor_DropDownClosed;
            cmbBoardColor.DropDown     += CmbBoardColor_DropDown;
            cmbBoardColor.DropDownClosed += CmbBoardColor_DropDownClosed;

            if (_boardColorHoverTimer == null)
            {
                _boardColorHoverTimer = new System.Windows.Forms.Timer { Interval = 50 };
                _boardColorHoverTimer.Tick += BoardColorHoverTimer_Tick;
            }

            cmbBoardColor.Items.Clear();
            foreach (var (name, _, _) in SettingsForm.ColorPresets)
                cmbBoardColor.Items.Add(name);
            cmbBoardColor.Items.Add("🌈 Rainbow");
            cmbBoardColor.Items.Add("🌊 Wave");
            cmbBoardColor.Items.Add("◻ Monochromatic");

            if (boardControl.RainbowMode)
                cmbBoardColor.SelectedIndex = SettingsForm.ColorPresets.Length;
            else if (boardControl.WaveMode)
                cmbBoardColor.SelectedIndex = SettingsForm.ColorPresets.Length + 1;
            else if (boardControl.MonochromeMode)
                cmbBoardColor.SelectedIndex = SettingsForm.ColorPresets.Length + 2;
            else
            {
                int match = Array.FindIndex(SettingsForm.ColorPresets, p =>
                    string.Equals(p.Light, config.LightSquareColor, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Dark,  config.DarkSquareColor,  StringComparison.OrdinalIgnoreCase));
                cmbBoardColor.SelectedIndex = match;
            }
        }

        private void CmbBoardColor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_restoringBoardColor) return;

            int idx = cmbBoardColor.SelectedIndex;
            if (idx < 0) return;

            ApplyBoardColorPreview(idx);

            // Always save on real selection — hover never fires SelectedIndexChanged
            if (config != null)
            {
                config.MonochromeBoard = idx == SettingsForm.ColorPresets.Length + 2;
                if (idx < SettingsForm.ColorPresets.Length)
                {
                    var (_, light, dark) = SettingsForm.ColorPresets[idx];
                    config.LightSquareColor = light;
                    config.DarkSquareColor  = dark;
                }
                config.Save();
            }
        }

        private void ApplyBoardColorPreview(int idx)
        {
            if (idx == SettingsForm.ColorPresets.Length)
            {
                boardControl.RainbowMode    = true;
                boardControl.MonochromeMode = false;
                return;
            }
            if (idx == SettingsForm.ColorPresets.Length + 1)
            {
                boardControl.WaveMode       = true;
                boardControl.MonochromeMode = false;
                return;
            }
            if (idx == SettingsForm.ColorPresets.Length + 2)
            {
                boardControl.RainbowMode    = false;
                boardControl.WaveMode       = false;
                boardControl.MonochromeMode = true;
                return;
            }
            boardControl.RainbowMode    = false;
            boardControl.WaveMode       = false;
            boardControl.MonochromeMode = false;
            var (_, light, dark) = SettingsForm.ColorPresets[idx];
            boardControl.SetSquareColors(ColorTranslator.FromHtml(light), ColorTranslator.FromHtml(dark));
        }

        private void CmbBoardColor_DropDown(object? sender, EventArgs e)
        {
            _boardColorSavedLight      = config.LightSquareColor;
            _boardColorSavedDark       = config.DarkSquareColor;
            _boardColorSavedRainbow    = boardControl.RainbowMode;
            _boardColorSavedWave       = boardControl.WaveMode;
            _boardColorSavedMonochrome = boardControl.MonochromeMode;
            _boardColorSavedIndex = cmbBoardColor.SelectedIndex;
            _boardColorHoverTimer?.Start();
        }

        private void BoardColorHoverTimer_Tick(object? sender, EventArgs e)
        {
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (!GetComboBoxInfo(cmbBoardColor.Handle, ref info) || info.hwndList == IntPtr.Zero) return;
            int idx = SendMessage(info.hwndList, LB_GETCURSEL, 0, 0);
            if (idx < 0 || idx >= cmbBoardColor.Items.Count) return;
            ApplyBoardColorPreview(idx);
        }

        private void DrillHoverTimer_Tick(object? sender, EventArgs e)
        {
            if (_cmbDrillChapter == null) return;
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (!GetComboBoxInfo(_cmbDrillChapter.Handle, ref info) || info.hwndList == IntPtr.Zero) return;
            int idx = SendMessage(info.hwndList, LB_GETCURSEL, 0, 0);
            if (idx < 0 || idx >= _cmbDrillChapter.Items.Count || idx == _drillHoverLastIdx) return;
            _drillHoverLastIdx = idx;
            string study = _cmbDrillStudy?.SelectedItem?.ToString() ?? "";
            var chapters = GetChaptersForStudy(study);
            var ch = idx < chapters.Count ? chapters[idx] : null;
            if (ch == null) return;
            SetDrillDescription(ch.Description);
            boardControl.LoadFEN(ch.Fen);
        }

        private void CmbBoardColor_DropDownClosed(object? sender, EventArgs e)
        {
            _boardColorHoverTimer?.Stop();

            // If the selected index changed, user committed — SelectedIndexChanged will save it
            if (cmbBoardColor.SelectedIndex != _boardColorSavedIndex) return;

            // User closed without committing — revert board visuals only (combo is already at saved index)
            boardControl.RainbowMode    = _boardColorSavedRainbow;
            boardControl.WaveMode       = _boardColorSavedWave;
            boardControl.MonochromeMode = _boardColorSavedMonochrome;
            if (!_boardColorSavedRainbow && !_boardColorSavedWave && !_boardColorSavedMonochrome)
                boardControl.SetSquareColors(
                    ColorTranslator.FromHtml(_boardColorSavedLight),
                    ColorTranslator.FromHtml(_boardColorSavedDark));
        }

        private void InitializeMatchControls()
        {
            // Populate engine combo boxes
            var resolver = new EnginePathResolver(config);
            _matchEngineFiles = resolver.GetAvailableEngines();
            cmbWhiteEngine.Items.Clear();
            cmbBlackEngine.Items.Clear();
            foreach (var eng in _matchEngineFiles)
            {
                config.EngineProfiles.TryGetValue(eng, out var prof);
                string label = !string.IsNullOrEmpty(prof?.DisplayName)
                    ? prof.DisplayName : Path.GetFileNameWithoutExtension(eng);
                if (prof?.Elo > 0) label += $" ({prof.Elo})";
                cmbWhiteEngine.Items.Add(label);
                cmbBlackEngine.Items.Add(label);
            }
            if (_matchEngineFiles.Length > 0) cmbWhiteEngine.SelectedIndex = 0;
            if (_matchEngineFiles.Length > 1) cmbBlackEngine.SelectedIndex = 1;
            else if (_matchEngineFiles.Length > 0) cmbBlackEngine.SelectedIndex = 0;

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

        }

        #region Event Handlers

        private void OuterSplit_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            config.BoardSplitterDistance = outerSplit.SplitterDistance;
            config.Save();
        }

        private void SplitRightPanels_SplitterMoved(object? sender, SplitterEventArgs e)
        {
            config.SplitterDistance = splitRightPanels.SplitterDistance;
            config.Save();
        }

        private void LeftPanel_Resize(object? sender, EventArgs e)
        {
            if (boardControl == null || _materialTop == null || _materialBottom == null
                || _lblBlackEngineInfo == null || _lblWhiteEngineInfo == null)
                return;

            if (sender is Panel panel)
            {
                const int evalBarWidth = 24;
                const int evalBarGap   = 8;
                bool showEvalBar = config?.ShowEvalBar != false;
                evalBar.Visible  = showEvalBar;
                int evalBarTotal = showEvalBar ? evalBarWidth + evalBarGap : 0;

                bool showStrips = config?.ShowMaterialStrips != false;
                int STRIP_H  = showStrips ? 22 : 0;
                int STRIP_GAP = showStrips ? 2 : 0;
                _materialTop.Visible    = showStrips;
                _materialBottom.Visible = showStrips;

                const int V_PAD = 8;
                int availableWidth  = panel.Width  - 20 - evalBarTotal;
                int availableHeight = panel.Height - 2 * STRIP_H - 2 * STRIP_GAP - 2 * V_PAD;

                int boardSize = Math.Min(availableWidth, availableHeight);
                boardSize = Math.Max(boardSize, 300);

                int groupWidth = boardSize + evalBarTotal;
                int groupX = Math.Max(panel.Width - groupWidth - 5, 5);
                int topSpace = Math.Max(V_PAD, (panel.Height - boardSize - 2 * STRIP_H - 2 * STRIP_GAP) / 2);

                int boardX = groupX + evalBarTotal;
                boardControl.Size     = new Size(boardSize, boardSize);
                boardControl.Location = new Point(boardX, topSpace + STRIP_H + STRIP_GAP);

                evalBar.Location = new Point(groupX, boardControl.Top);
                evalBar.Size     = new Size(evalBarWidth, boardControl.Height);

                _materialTop.Location = new Point(boardX, topSpace);
                _materialTop.Size     = new Size(boardSize, STRIP_H);
                _materialBottom.Location = new Point(boardX, boardControl.Bottom + STRIP_GAP);
                _materialBottom.Size     = new Size(boardSize, STRIP_H);

                // Engine info labels — swap positions when board is flipped
                const int infoW = 170;
                int infoH = Math.Max(STRIP_H, 14);
                bool flipped = boardControl.IsFlipped;
                var topInfoLabel    = flipped ? _lblWhiteEngineInfo : _lblBlackEngineInfo;
                var bottomInfoLabel = flipped ? _lblBlackEngineInfo : _lblWhiteEngineInfo;
                topInfoLabel.Location    = new Point(boardX + boardSize - infoW, topSpace);
                topInfoLabel.Size        = new Size(infoW, infoH);
                bottomInfoLabel.Location = new Point(boardX + boardSize - infoW, boardControl.Bottom + STRIP_GAP);
                bottomInfoLabel.Size     = new Size(infoW, infoH);
            }
        }

        private void PnlBoardControls_Resize(object? sender, EventArgs e)
        {
            if (lblTurn == null || pnlBoardControls == null) return;

            int w = pnlBoardControls.Width;
            const int pad = 4;
            const int gap = 4;

            // Row 1 (Y=3): "White to move" label left, BoardColor + Pieces selectors right
            lblTurn.Location = new Point(pad, 3);
            cmbPieces.Width = 95;
            cmbPieces.Location = new Point(w - cmbPieces.Width - pad, 0);
            lblPieces.Location = new Point(cmbPieces.Left - lblPieces.Width - gap, 3);
            cmbBoardColor.Width = 110;
            cmbBoardColor.Location = new Point(lblPieces.Left - cmbBoardColor.Width - gap, 0);
            lblTurn.Visible = (pad + lblTurn.Width + gap) < cmbBoardColor.Left;

            // Row 2a (Y=28): board controls — New / Flip / TakeBack / Prev / Next / AutoPlay / Bot
            const int btnW = 40;
            const int btnH = 28;
            const int row2a = 28;

            foreach (var b in new[] { btnNewGame, btnFlipBoard, btnTakeBack, btnPrevMove,
                                      btnNextMove, btnAutoPlay, btnPlayBot })
                b.Size = new Size(btnW, btnH);

            btnNewGame.Location   = new Point(pad, row2a);
            btnFlipBoard.Location = new Point(btnNewGame.Right  + gap, row2a);
            btnTakeBack.Location  = new Point(btnFlipBoard.Right + gap, row2a);
            btnPrevMove.Location  = new Point(btnTakeBack.Right  + gap, row2a);
            btnNextMove.Location  = new Point(btnPrevMove.Right  + 2,   row2a);
            btnAutoPlay.Location  = new Point(btnNextMove.Right  + 2,   row2a);
            btnPlayBot.Location   = new Point(btnAutoPlay.Right  + gap, row2a);

            // Row 2b (Y=58): feature launchers — Edit / Training / Match / Tournament / 960
            const int row2b = 60;

            foreach (var b in new[] { btnEditPosition, btnTraining, btnMatch, btnTournament, btnChess960 })
                b.Size = new Size(btnW, btnH);

            btnEditPosition.Location = new Point(pad, row2b);
            btnTraining.Location     = new Point(btnEditPosition.Right + gap, row2b);
            btnMatch.Location        = new Point(btnTraining.Right     + gap, row2b);
            btnTournament.Location   = new Point(btnMatch.Right        + gap, row2b);
            btnChess960.Location     = new Point(btnTournament.Right   + gap, row2b);

            // Row 3 (Y=90): FEN row — label | input | Load | Copy | ⚙
            const int fenY     = 91;
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

            // Row 4 (Y=118): Status text
            lblStatus.Location = new Point(pad, 121);
            lblStatus.Width    = w - 2 * pad;
        }

        private void BoardControl_MoveMade(object? sender, MoveEventArgs e)
        {
            // Cancel any PV animation in progress
            _pvAnimationCts?.Cancel();

            // Clear engine arrows and threat arrows (new analysis will redraw them)
            boardControl.ClearEngineArrows();
            boardControl.ClearThreatArrows();



            // Opening Training recreate phase — intercept before tree processing
            if (_openingRecreatePhase)
            {
                string moveSan = ConvertUciToSan(e.UciMove, _openingFens[_openingRecreateIndex]);
                PlayMoveSound(e.IsCapture, moveSan);
                OpeningTrainingValidateMove(e.UciMove);
                return;
            }

            // Puzzle Training — intercept player moves
            if (_puzzleActive && !_puzzleLocked)
            {
                PlayMoveSound(e.IsCapture, ConvertUciToSan(e.UciMove, boardControl.GetFEN()));
                PuzzleValidateMove(e.UciMove);
                return;
            }
            if (_puzzleActive && _puzzleLocked) return;

            // Skip if we're navigating (not making a new move)
            if (isNavigating) return;

            // Convert UCI to SAN for display
            string san = ConvertUciToSan(e.UciMove, moveTree.CurrentNode.FEN);

            // Sound: checkmate (#) → game over, check (+) → check, capture → take, else → move
            PlayMoveSound(e.IsCapture, san);

            // Add move to tree; if a PV was sitting at Children[0], promote the real move to front.
            moveTree.AddMove(e.UciMove, san, e.FEN);
            PromoteToMainLine(moveTree.CurrentNode);

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
                string userFen = boardControl.GetFEN();
                string? draw = CheckBotDrawConditions(userFen);
                if (draw != null) { HandleBotGameEnd(userFen, draw); return; }
                _ = MakeBotMoveAsync();
            }
        }

        private async Task TriggerAutoAnalysis()
        {
            if (_autoPlaying) return;
            if (_trainingGameActive) return;

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

        private void ResetPositionState(string fen)
        {
            moveTree.Clear(fen);
            moveListBox.Items.Clear();
            _movePairs.Clear();
            analysisOutput.Clear();
            evalBar?.Reset();
            _analysisCache.Clear();
        }

        private void BtnNewGame_Click(object? sender, EventArgs e)
        {
            CancelClassification();
            StopAutoPlay();
            if (_botModeActive) StopBotMode();
            boardControl.ClearEngineArrows();
            boardControl.ClearMoveAnnotation();

            // Leaving Chess960 mode → reset engine flag and board mode
            if (_chess960Active)
            {
                _chess960Active = false;
                boardControl.Chess960Mode = false;
                _ = engineService?.SetChess960Async(false);
            }

            boardControl.ResetBoard();
            ResetPositionState(boardControl.GetFEN());
            _currentClassification = null;
            _classificationLookup = null;
            consoleFormatter?.SetActiveClassification(null);
            _pgnHeaders = new();
            _matchWhiteName = "";
            _matchBlackName = "";
            _matchWhiteFileName = "";
            _matchBlackFileName = "";
            _lblBlackEngineInfo.Visible = false;
            _lblWhiteEngineInfo.Visible = false;
            _libraryGameId = "";
            UpdateFenDisplay();
            UpdateTurnLabel();
            lblStatus.Text = "New game started";
            _ = TriggerAutoAnalysis();
        }

        private void BtnChess960_Click(object? sender, EventArgs e) => ShowChess960Dialog();

        private async void ShowChess960Dialog()
        {
            using var browser = new Chess960BrowserDialog(ThemeService.IsDarkTheme(config?.Theme));
            if (browser.ShowDialog(this) != DialogResult.OK) return;

            int position = browser.SelectedPosition;
            await StartChess960GameAsync(position);

            if (browser.StartBot)
            {
                if (string.IsNullOrEmpty(config?.SelectedEngine))
                {
                    lblStatus.Text = "No engine configured — click ⚙ to set up";
                    return;
                }
                string[] engines = Directory.Exists(config.GetEnginesPath())
                    ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                    : Array.Empty<string>();
                using var botDlg = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                    engines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
                if (botDlg.ShowDialog(this) != DialogResult.OK) return;
                await StartBotEngineAsync(botDlg.Settings, resetBoard: false);
            }
        }

        private async Task StartChess960GameAsync(int position)
        {
            CancelClassification();
            StopAutoPlay();
            if (_botModeActive) StopBotMode();

            string fen = Chess960Service.GetStartFen(position);

            boardControl.ClearEngineArrows();
            boardControl.ClearMoveAnnotation();
            boardControl.Chess960Mode = true;
            boardControl.LoadFEN(fen);

            _chess960Active = true;
            ResetPositionState(fen);
            _currentClassification = null;
            _classificationLookup = null;
            consoleFormatter?.SetActiveClassification(null);
            _pgnHeaders = new();
            _matchWhiteName = ""; _matchBlackName = "";
            _matchWhiteFileName = ""; _matchBlackFileName = "";
            _lblBlackEngineInfo.Visible = false;
            _lblWhiteEngineInfo.Visible = false;
            _libraryGameId = "";
            UpdateFenDisplay();
            UpdateTurnLabel();
            lblStatus.Text = $"Chess 960 — Position {position} (SP-{position})";

            // Enable Chess960 in the engine — must be done while engine is idle
            if (engineService != null)
                await engineService.SetChess960Async(true);

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
                PopulateBoardColorComboBox(); // re-sync preset selector after custom color edits
                boardControl.ShowSquareLabels = config.ShowSquareLabels;
                boardControl.HideCoordinates  = !config.ShowCoordinates;
                boardControl.ShowLastMoveHighlight = config.ShowLastMoveHighlight;
                if (!config.ShowThreatArrows) boardControl.ClearThreatArrows();
                if (_evalGraph != null) _evalGraph.Visible = config.ShowEvalGraph;
                boardControl.AnimationDurationMs = config.AnimationDurationMs;
                ApplyBoardFxFromConfig();

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
            LeftPanel_Resize(leftPanel, EventArgs.Empty);
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
            if (matchService?.IsRunning == true) return;
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
            if (matchService?.IsRunning == true) return;
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
                    string navSan = moveTree.CurrentNode.SanMove;
                    PlayMoveSound(navSan.Contains('x'), navSan);
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
            if (matchService?.IsRunning == true) return;
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
                    ResetPositionState(fen);
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
                ResetPositionState(fen);
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
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
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
            try { Clipboard.SetText(fen); }
            catch { Clipboard.SetDataObject(fen, true); }
            lblStatus.Text = "FEN copied to clipboard";
        }


        private void MoveListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _movePairs.Count) return;
            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) return;

            Font drawFont = moveListBox.Font;
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
            bool rowSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var pair = _movePairs[e.Index];
            int indentPx = pair.IndentLevel * 14;

            if (pair.Black == null)
            {
                // Full-width row (unpaired or variation starting with a black move).
                DrawMoveCell(e.Graphics, e.Bounds, pair.White!, pair.WhiteSymbol, rowSelected, drawFont, isDark,
                             isFullWidth: true, pair.IsVariation, indentPx, pair.IsVariationStart);
                e.DrawFocusRectangle();
                return;
            }

            // --- Paired row (main line or paired variation): split into halves ---
            int indentedLeft = e.Bounds.Left + indentPx;
            int usableWidth  = e.Bounds.Width - indentPx;
            int mid          = indentedLeft + usableWidth / 2;
            var leftBounds   = new Rectangle(indentedLeft, e.Bounds.Top, usableWidth / 2, e.Bounds.Height);
            var rightBounds  = new Rectangle(mid,          e.Bounds.Top, usableWidth / 2, e.Bounds.Height);

            bool leftActive  = _activeNode == pair.White;
            bool rightActive = _activeNode == pair.Black;

            // Clear the indent gutter.
            if (indentPx > 0)
            {
                using var gutter = new SolidBrush(moveListBox.BackColor);
                e.Graphics.FillRectangle(gutter, new Rectangle(e.Bounds.Left, e.Bounds.Top, indentPx, e.Bounds.Height));
            }

            // Alternating row tint on unselected main-line rows for readability.
            Color rowTint = !pair.IsVariation && e.Index % 2 == 1
                ? (isDark ? Color.FromArgb(18, 255, 255, 255) : Color.FromArgb(12, 0, 0, 0))
                : Color.Transparent;

            // Left half background.
            Color leftBg = (rowSelected && leftActive) || (rowSelected && !rightActive)
                ? SystemColors.Highlight
                : moveListBox.BackColor;
            using (var lb = new SolidBrush(leftBg))
                e.Graphics.FillRectangle(lb, leftBounds);
            if (leftBg != SystemColors.Highlight && rowTint != Color.Transparent)
                using (var tb = new SolidBrush(rowTint))
                    e.Graphics.FillRectangle(tb, leftBounds);

            // Right half background.
            Color rightBg = rowSelected && rightActive ? SystemColors.Highlight : moveListBox.BackColor;
            using (var rb = new SolidBrush(rightBg))
                e.Graphics.FillRectangle(rb, rightBounds);
            if (rightBg != SystemColors.Highlight && rowTint != Color.Transparent)
                using (var tb = new SolidBrush(rowTint))
                    e.Graphics.FillRectangle(tb, rightBounds);

            // White/left half.
            bool leftSel = (rowSelected && leftActive) || (rowSelected && !rightActive);
            DrawMoveCell(e.Graphics, leftBounds, pair.White!, pair.WhiteSymbol, leftSel, drawFont, isDark,
                         isFullWidth: false, pair.IsVariation, 0, pair.IsVariationStart);

            // Black/right half.
            bool rightSel = rowSelected && rightActive;
            DrawMoveCellBlack(e.Graphics, rightBounds, pair.Black, pair.BlackSymbol, rightSel, drawFont, isDark, pair.IsVariation);

            // Subtle column divider.
            using var sepPen = new Pen(isDark ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(50, 0, 0, 0));
            e.Graphics.DrawLine(sepPen, mid, e.Bounds.Top + 2, mid, e.Bounds.Bottom - 2);

            e.DrawFocusRectangle();
        }

        private void DrawMoveCell(Graphics g, Rectangle bounds, MoveNode node, string? symbol,
                                  bool isSelected, Font drawFont, bool isDark, bool isFullWidth,
                                  bool isVariation = false, int indentPx = 0, bool isVariationStart = false)
        {
            if (isFullWidth)
            {
                using var bg = new SolidBrush(isSelected ? SystemColors.Highlight : moveListBox.BackColor);
                g.FillRectangle(bg, bounds);
            }

            string san = node.SanMove;
            // Variation rows get "(" on the first line and " " (alignment spacer) on continuations.
            string prefix = isVariation ? (isVariationStart ? "(" : " ") : "";
            string moveText = node.IsWhiteMove
                ? $"{prefix}{node.MoveNumber}. {san}"
                : $"{prefix}{node.MoveNumber}...{san}";

            string sym = symbol ?? "";
            (string display, Color symColor) = ExtractSymbol(moveText, sym, isDark);
            Color textColor = isVariation && !isSelected
                ? (isDark ? Color.Silver : Color.DimGray)
                : ResolveTextColor(node, isSelected, sym, symColor, isDark);

            float textX = bounds.Left + 3 + indentPx;

            try
            {
                using var brush = new SolidBrush(textColor);
                g.DrawString(display, drawFont, brush, textX, bounds.Top + 1);

                if (!string.IsNullOrEmpty(sym))
                {
                    var sz = g.MeasureString(display + " ", drawFont);
                    using var symBrush = new SolidBrush(symColor);
                    using var boldFont = new Font(drawFont.FontFamily, drawFont.Size, FontStyle.Bold);
                    g.DrawString(sym, boldFont, symBrush, textX + sz.Width - 4, bounds.Top + 1);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException or ObjectDisposedException)
            {
                // GDI resource in a bad state — recreate font from config to guarantee text is visible.
                try
                {
                    using var safeFont = new Font(config?.ConsoleFontFamily ?? "Consolas", config?.ConsoleFontSize ?? 10f);
                    using var safeBrush = new SolidBrush(isSelected ? (isDark ? Color.White : SystemColors.HighlightText) : (isDark ? Color.White : Color.Black));
                    g.DrawString(display, safeFont, safeBrush, textX, bounds.Top + 1);
                }
                catch { }
            }
        }

        private void DrawMoveCellBlack(Graphics g, Rectangle bounds, MoveNode? node, string? symbol,
                                       bool isSelected, Font drawFont, bool isDark, bool isVariation = false)
        {
            if (node == null) return;

            string sym = symbol ?? "";
            string san = node.SanMove;
            (string display, Color symColor) = ExtractSymbol(san, sym, isDark);
            Color textColor = isVariation && !isSelected
                ? (isDark ? Color.Silver : Color.DimGray)
                : ResolveTextColor(node, isSelected, sym, symColor, isDark);

            try
            {
                using var brush = new SolidBrush(textColor);
                g.DrawString(display, drawFont, brush, bounds.Left + 6, bounds.Top + 1);

                if (!string.IsNullOrEmpty(sym))
                {
                    var sz = g.MeasureString(display + " ", drawFont);
                    using var symBrush = new SolidBrush(symColor);
                    using var boldFont = new Font(drawFont.FontFamily, drawFont.Size, FontStyle.Bold);
                    g.DrawString(sym, boldFont, symBrush, bounds.Left + 6 + sz.Width - 4, bounds.Top + 1);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException or ObjectDisposedException)
            {
                try
                {
                    using var safeFont = new Font(config?.ConsoleFontFamily ?? "Consolas", config?.ConsoleFontSize ?? 10f);
                    using var safeBrush = new SolidBrush(isSelected ? (isDark ? Color.White : SystemColors.HighlightText) : (isDark ? Color.White : Color.Black));
                    g.DrawString(display, safeFont, safeBrush, bounds.Left + 6, bounds.Top + 1);
                }
                catch { }
            }
        }

        private static (string text, Color symColor) ExtractSymbol(string moveText, string sym, bool isDark)
        {
            if (string.IsNullOrEmpty(sym)) return (moveText, Color.Empty);
            Color c = sym switch
            {
                "!!"  => ColorScheme.BrilliantColor,
                "??"  => ColorScheme.BlunderColor,
                "?!"  => isDark ? ColorScheme.InaccuracyColor : Color.DarkGoldenrod,
                "?"   => isDark ? ColorScheme.MistakeColor : Color.Chocolate,
                "!"   => ColorScheme.OnlyMoveColor,
                _     => Color.Empty
            };
            return (moveText, c);
        }

        private Color ResolveTextColor(MoveNode node, bool isSelected, string sym, Color symColor, bool isDark)
        {
            if (isSelected) return isDark ? Color.White : SystemColors.HighlightText;
            if (!string.IsNullOrEmpty(sym) && symColor != Color.Empty) return symColor;

            if (_classificationLookup != null && _classificationLookup.TryGetValue(node, out var cr))
            {
                return cr.Quality switch
                {
                    MoveQualityAnalyzer.MoveQuality.Precise   => isDark ? Color.FromArgb(89, 153, 191) : Color.SteelBlue,
                    MoveQualityAnalyzer.MoveQuality.Best      => isDark ? ColorScheme.BestMoveColor : Color.ForestGreen,
                    MoveQualityAnalyzer.MoveQuality.Excellent => isDark ? ColorScheme.ExcellentMoveColor : Color.SeaGreen,
                    MoveQualityAnalyzer.MoveQuality.Good      => isDark ? ColorScheme.GoodMoveColor : Color.OliveDrab,
                    _ => isDark ? Color.White : Color.Black
                };
            }
            return isDark ? Color.White : Color.Black;
        }

        private Color GetQualityColor(int index, bool isDark)
        {
            if (index < 0 || index >= _movePairs.Count) return isDark ? Color.White : Color.Black;
            var pair = _movePairs[index];
            var node = _activeNode == pair.Black ? pair.Black : pair.White;
            if (node == null) return isDark ? Color.White : Color.Black;
            return ResolveTextColor(node, false, "", Color.Empty, isDark);
        }

        private void MoveListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            int idx = moveListBox.IndexFromPoint(e.Location);
            if (idx < 0 || idx >= _movePairs.Count) return;

            var pair = _movePairs[idx];
            if (pair.Black == null)
            {
                // Full-width row — just let SelectedIndexChanged handle it.
                _activeNode = pair.White;
                return;
            }

            // Determine which half was clicked, accounting for variation indent.
            int indentPx = pair.IndentLevel * 14;
            int mid = indentPx + (moveListBox.Width - indentPx) / 2;
            bool clickedLeft = e.X < mid;
            _activeNode = clickedLeft ? pair.White : pair.Black;

            // If the row was already selected, SelectedIndexChanged won't re-fire — navigate directly.
            if (moveListBox.SelectedIndex == idx)
            {
                NavigateToNode(_activeNode!);
                moveListBox.Invalidate(moveListBox.GetItemRectangle(idx));
            }
        }

        private void MoveListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isNavigating) return;
            if (matchService?.IsRunning == true) return;

            int selected = moveListBox.SelectedIndex;
            if (selected < 0 || selected >= _movePairs.Count) return;

            var pair = _movePairs[selected];

            // Use _activeNode if it belongs to this row; otherwise default to white (left).
            if (_activeNode != pair.White && _activeNode != pair.Black)
                _activeNode = pair.White ?? pair.Black;

            if (_activeNode == null) return;
            NavigateToNode(_activeNode);
        }

        private void NavigateToNode(MoveNode node)
        {
            if (isNavigating || matchRunning) return;
            isNavigating = true;
            try
            {
                moveTree.GoToNode(node);
                boardControl.LoadFEN(node.FEN);
                SetLastMoveHighlight();
                UpdateMoveAnnotation(node);
                UpdateFenDisplay();
                UpdateTurnLabel();

                string statusText = $"Move {node.MoveNumber}";
                if (node.VariationDepth > 0) statusText += " (variation)";
                lblStatus.Text = statusText;

                if (!matchRunning) _ = TriggerAutoAnalysis();
            }
            finally { isNavigating = false; }
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
            var focused = Control.FromHandle(GetFocus());
            if (e.KeyCode == Keys.Back && focused is not (TextBox or NumericUpDown or RichTextBox or ComboBox))
            {
                BtnTakeBack_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion







    }
}
