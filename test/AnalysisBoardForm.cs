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
        private OpeningEntry? _matchBookOpening;
        // Overlay labels on top/bottom material strips showing engine name + ELO during a match
        private Label _lblBlackEngineInfo = null!;
        private Label _lblWhiteEngineInfo = null!;

        // Bot mode
        private bool _botModeActive = false;
        private bool _matchPanelActive = false;
        private BotSettings? _botSettings;
        private ChessEngineService? _botEngine;
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

        // Sound effects
        private System.Media.SoundPlayer? _sndMove;
        private System.Media.SoundPlayer? _sndCapture;
        private System.Media.SoundPlayer? _sndCheck;
        private System.Media.SoundPlayer? _sndGameOver;
        private bool _gameOverSoundPlayed = false;

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
                    int evalBarTotal = config?.ShowEvalBar != false ? 28 : 0;
                    int boardSize = Math.Max(300, outerSplit.Height - 2 * sh - 2 * sg);
                    int idealLeft = boardSize + evalBarTotal + 10;
                    outerSplit.SplitterDistance = Math.Clamp(idealLeft,
                        outerSplit.Panel1MinSize, outerSplit.Width - outerSplit.Panel2MinSize);
                }

                // Inner split: moves | analysis
                splitRightPanels.Panel1MinSize = 80;
                splitRightPanels.Panel2MinSize = 200;
                splitRightPanels.SplitterDistance = config!.SplitterDistance > 0 ? config.SplitterDistance : 130;

                LeftPanel_Resize(leftPanel, EventArgs.Empty);
                PnlBoardControls_Resize(pnlBoardControls, EventArgs.Empty);
                this.MinimumSize = this.Size;
                InitTrainingPanel();
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
                                        btnNextMove, btnAutoPlay, btnPlayBot, btnEditPosition, btnTraining, btnMatch, btnLoadFen, btnCopyFen, btnClassifyMoves,
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
            string audioDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio");
            TryLoadSound(ref _sndMove,     Path.Combine(audioDir, "piece_move.wav"),  gainFactor: 1.5f);
            TryLoadSound(ref _sndCapture,  Path.Combine(audioDir, "piece_take.wav"),  gainFactor: 1.0f);
            TryLoadSound(ref _sndCheck,    Path.Combine(audioDir, "piece_check.wav"), gainFactor: 1.0f);
            TryLoadSound(ref _sndGameOver, Path.Combine(audioDir, "game_over.wav"),   gainFactor: 1.0f);
        }

        private static void TryLoadSound(ref System.Media.SoundPlayer? player, string path, float gainFactor = 1.0f)
        {
            if (!File.Exists(path)) return;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                if (gainFactor != 1.0f) AmplifyWav(data, gainFactor);
                player = new System.Media.SoundPlayer(new MemoryStream(data));
                player.Load();
            }
            catch { player = null; }
        }

        // Amplifies 16-bit PCM WAV samples in-place. Silently no-ops for non-16-bit files.
        private static void AmplifyWav(byte[] wav, float gain)
        {
            if (wav.Length < 44) return;
            short bitsPerSample = 16;
            int dataStart = -1, dataSize = 0;
            int i = 12;
            while (i < wav.Length - 8)
            {
                string id = System.Text.Encoding.ASCII.GetString(wav, i, 4);
                int size = BitConverter.ToInt32(wav, i + 4);
                if (id == "fmt " && size >= 16)
                    bitsPerSample = BitConverter.ToInt16(wav, i + 22);
                else if (id == "data") { dataStart = i + 8; dataSize = size; break; }
                i += 8 + size + (size % 2);
            }
            if (dataStart < 0 || bitsPerSample != 16) return;
            int dataEnd = Math.Min(dataStart + dataSize, wav.Length);
            for (int j = dataStart; j < dataEnd - 1; j += 2)
            {
                short sample = BitConverter.ToInt16(wav, j);
                short boosted = (short)Math.Clamp(sample * gain, short.MinValue, short.MaxValue);
                wav[j]     = (byte)(boosted & 0xFF);
                wav[j + 1] = (byte)((boosted >> 8) & 0xFF);
            }
        }

        private void PlayMoveSound(bool isCapture, string san)
        {
            if (!config.SoundEffectsEnabled) return;
            try
            {
                if (san.EndsWith('#'))
                {
                    _gameOverSoundPlayed = true;
                    _sndGameOver?.Play();
                }
                else if (san.EndsWith('+'))    _sndCheck?.Play();
                else if (san.StartsWith("O-O"))
                {
                    _sndMove?.Play();
                    Task.Delay(70).ContinueWith(_ => _sndMove?.Play());
                }
                else if (isCapture)            _sndCapture?.Play();
                else                           _sndMove?.Play();
            }
            catch { }
        }

        private void PlayGameEndSound()
        {
            if (!config.SoundEffectsEnabled) return;
            if (_gameOverSoundPlayed) { _gameOverSoundPlayed = false; return; }
            try { _sndGameOver?.Play(); }
            catch { }
        }

        private void InitializeServices()
        {
            // Initialize console formatter for analysis output
            consoleFormatter = new ConsoleOutputFormatter(
                analysisOutput,
                config,
                MovesExplanation.GenerateMoveExplanation);
            consoleFormatter.OnSeeLineClicked  += InsertPvIntoMoveTree;
            consoleFormatter.OnNavigateToNode  += node =>
            {
                int idx = displayedNodes.IndexOf(node);
                if (idx >= 0) moveListBox.SelectedIndex = idx;
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
            string study   = _cmbDrillStudy?.SelectedItem?.ToString() ?? "";
            string chapter = _cmbDrillChapter.Items[idx]?.ToString() ?? "";
            var ch = _drillChapters.FirstOrDefault(c => c.StudyName == study && c.ChapterName == chapter);
            if (ch == null) return;
            if (_lblDrillDesc != null) _lblDrillDesc.Text = ch.Description;
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

            // Trigger initial layout for responsive checkbox positioning
            GrpEngineMatch_Resize(grpEngineMatch, EventArgs.Empty);
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
                const int evalBarGap   = 4;
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
            btnTraining.Width   = iconW;
            btnMatch.Width      = iconW;

            btnNewGame.Location    = new Point(pad, buttonY);
            btnFlipBoard.Location  = new Point(btnNewGame.Right   + gap, buttonY);
            btnTakeBack.Location   = new Point(btnFlipBoard.Right + gap, buttonY);
            btnPrevMove.Location   = new Point(btnTakeBack.Right  + gap, buttonY);
            btnNextMove.Location   = new Point(btnPrevMove.Right  + 2,   buttonY);
            btnAutoPlay.Location   = new Point(btnNextMove.Right  + 2,   buttonY);
            btnPlayBot.Location    = new Point(btnAutoPlay.Right  + gap, buttonY);
            btnEditPosition.Location = new Point(btnPlayBot.Right + gap, buttonY);
            btnTraining.Location   = new Point(btnEditPosition.Right + gap, buttonY);
            btnMatch.Location      = new Point(btnTraining.Right   + gap, buttonY);

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
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);

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
            if (isNavigating) return;
            if (matchService?.IsRunning == true) return;

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

            if (cmbWhiteEngine.SelectedIndex < 0 || cmbBlackEngine.SelectedIndex < 0 ||
                cmbWhiteEngine.SelectedIndex >= _matchEngineFiles.Length ||
                cmbBlackEngine.SelectedIndex >= _matchEngineFiles.Length)
            {
                lblStatus.Text = "Select both engines first";
                return;
            }

            string whiteEngineName = _matchEngineFiles[cmbWhiteEngine.SelectedIndex];
            string blackEngineName = _matchEngineFiles[cmbBlackEngine.SelectedIndex];
            _matchWhiteName = Path.GetFileNameWithoutExtension(whiteEngineName);
            _matchBlackName = Path.GetFileNameWithoutExtension(blackEngineName);

            // Load engine profiles for ELO display
            config.EngineProfiles.TryGetValue(whiteEngineName, out var whiteProfile);
            config.EngineProfiles.TryGetValue(blackEngineName, out var blackProfile);
            _matchWhiteElo = whiteProfile?.Elo ?? 0;
            _matchBlackElo = blackProfile?.Elo ?? 0;
            _matchWhiteFileName = whiteEngineName;
            _matchBlackFileName = blackEngineName;
            SetEngineInfoLabels(whiteProfile, blackProfile, whiteEngineName, blackEngineName);

            // Series init
            _seriesTotal         = (int)numGames.Value;
            _seriesPlayed        = 0;
            _seriesEng1Score     = 0;
            _seriesEng2Score     = 0;
            _seriesEng1File      = whiteEngineName;
            _seriesEng2File      = blackEngineName;
            _seriesCurrentWhiteFile = whiteEngineName;
            lblSeriesScore.Text  = "";

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
            // Opening book injection (only from standard start, not custom position)
            bool bookReady = chkUseBook.Checked && !chkFromPosition.Checked &&
                             (rbBookChoose.Checked ? _matchBookOpening != null : openingBookService?.IsLoaded == true);
            bool chooseMode = bookReady && rbBookChoose.Checked && _matchBookOpening != null;
            if (bookReady) startFen = GetBookStartFen(startFen);

            CancelClassification();
            if (!chooseMode)
                moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear(); // Clear analysis cache for new match
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            if (chooseMode)
            {
                isNavigating = true;
                try { UpdateMoveList(); }
                finally { isNavigating = false; }
            }

            // Set up match log
            analysisOutput.Clear();
            analysisOutput.SelectionColor = analysisOutput.ForeColor;
            analysisOutput.AppendText($"Engine Match: {GetEngineLabel(whiteEngineName, true)} vs {GetEngineLabel(blackEngineName, true)}\n");
            if (_seriesTotal > 1)
                analysisOutput.AppendText($"Series: {_seriesTotal} games\n");
            analysisOutput.AppendText($"Time Control: {tc}\n");
            string arbiterFile = Path.GetFileName(config.SelectedEngine ?? "");
            analysisOutput.AppendText($"Arbiter: {GetEngineLabel(arbiterFile, false)} (depth {config.ContinuousAnalysisMaxDepth})\n");
            if (chkFromPosition.Checked)
                analysisOutput.AppendText("Starting from custom position\n");
            if (bookReady)
            {
                if (rbBookChoose.Checked && _matchBookOpening != null)
                    analysisOutput.AppendText($"Opening: {_matchBookOpening.Eco}  {_matchBookOpening.Name}\n");
                else
                    analysisOutput.AppendText($"Book: {startFen}\n");
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
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            matchService.WaitForAnimation    = config.ShowAnimations;
            matchService.AnnotatorEngine      = engineService;
            matchService.AnnotatorDepth       = config.EngineDepth;
            matchService.AdjudicationEnabled  = chkAdjudicate.Checked;
            boardControl.AnimationCompleted  += MatchBoard_AnimationCompleted;

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

        private void ChkUseBook_CheckedChanged(object? sender, EventArgs e)
        {
            bool on = chkUseBook.Checked;
            rbBookRandom.Enabled = on;
            rbBookChoose.Enabled = on;
            if (!on) lblMatchOpening.Visible = false;
        }

        private void RbBookChoose_CheckedChanged(object? sender, EventArgs e)
        {
            if (!rbBookChoose.Checked) { lblMatchOpening.Visible = false; return; }
            SelectMatchOpening();
        }

        private void SelectMatchOpening()
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            if (entries.Count == 0)
            {
                lblStatus.Text = "No openings found in Books folder";
                rbBookRandom.Checked = true;
                return;
            }

            using var dlg = new OpeningExplorerDialog(entries, pgn =>
            {
                var m = System.Text.RegularExpressions.Regex.Match(pgn, @"\[Opening ""([^""]+)""\]");
                if (!m.Success) return;
                string tag = m.Groups[1].Value;
                int dash = tag.IndexOf(" — ", StringComparison.Ordinal);
                if (dash < 0) return;
                string eco = tag[..dash];
                string name = tag[(dash + 3)..];
                _matchBookOpening = entries.FirstOrDefault(e => e.Eco == eco && e.Name == name)
                                 ?? entries.FirstOrDefault(e => e.Eco == eco);
                if (_matchBookOpening != null)
                {
                    lblMatchOpening.Text = $"{_matchBookOpening.Eco}  {_matchBookOpening.Name}";
                    lblMatchOpening.Visible = true;
                }
            }, ThemeService.IsDarkTheme(config?.Theme));
            dlg.ShowDialog(this);

            if (_matchBookOpening == null)
                rbBookRandom.Checked = true;
        }

        private void BtnEngineProfiles_Click(object? sender, EventArgs e)
        {
            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
            using var dlg = new EngineProfilesDialog(config!, isDark);
            dlg.ShowDialog(this);
        }

        private void SetEngineInfoLabels(EngineProfile? whiteProfile, EngineProfile? blackProfile,
            string whiteFileName, string blackFileName)
        {
            static string BuildLabel(EngineProfile? profile, string fileName)
            {
                string name = !string.IsNullOrEmpty(profile?.DisplayName)
                    ? profile.DisplayName
                    : Path.GetFileNameWithoutExtension(fileName);
                return profile?.Elo > 0 ? $"{name} [{profile.Elo}]" : name;
            }

            bool showStrips = config?.ShowMaterialStrips != false;
            _lblBlackEngineInfo.Text = BuildLabel(blackProfile, blackFileName);
            _lblWhiteEngineInfo.Text = BuildLabel(whiteProfile, whiteFileName);
            _lblBlackEngineInfo.Visible = showStrips;
            _lblWhiteEngineInfo.Visible = showStrips;
        }

        private string GetEngineLabel(string fileName, bool includeElo)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;
            config.EngineProfiles.TryGetValue(fileName, out var profile);
            string name = !string.IsNullOrEmpty(profile?.DisplayName)
                ? profile.DisplayName
                : Path.GetFileNameWithoutExtension(fileName);
            if (includeElo && profile?.Elo > 0)
                return $"{name} ({profile.Elo})";
            return name;
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

                PlayMoveSound(san.Contains('x'), san);

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

            // Elo change (FIDE K=10, informational) + Chessdroid Rating update (K=32, persistent)
            if (_matchWhiteElo > 0 && _matchBlackElo > 0 && result.Outcome != MatchOutcome.Interrupted)
            {
                double whiteScore = result.Outcome == MatchOutcome.WhiteWins ? 1.0
                    : result.Outcome == MatchOutcome.Draw ? 0.5 : 0.0;
                int whiteDelta = Services.EloCalculator.EloChange(_matchWhiteElo, _matchBlackElo, whiteScore);
                int blackDelta = Services.EloCalculator.EloChange(_matchBlackElo, _matchWhiteElo, 1.0 - whiteScore);
                string wLabel = GetEngineLabel(_matchWhiteFileName, true);
                string bLabel = GetEngineLabel(_matchBlackFileName, true);
                analysisOutput.AppendText($"\nElo change: {wLabel} {Services.EloCalculator.FormatDelta(whiteDelta)}  {bLabel} {Services.EloCalculator.FormatDelta(blackDelta)}\n");

                // Chessdroid Rating — seeds from CCRL on first game, then drifts with K=32
                config.EngineProfiles.TryGetValue(_matchWhiteFileName, out var wProf);
                config.EngineProfiles.TryGetValue(_matchBlackFileName, out var bProf);
                if (wProf != null && bProf != null)
                {
                    int wCurr = wProf.ChessdroidElo > 0 ? wProf.ChessdroidElo : wProf.Elo;
                    int bCurr = bProf.ChessdroidElo > 0 ? bProf.ChessdroidElo : bProf.Elo;
                    int wChessDelta = Services.EloCalculator.EloChangeChessdroid(wCurr, bCurr, whiteScore);
                    int bChessDelta = Services.EloCalculator.EloChangeChessdroid(bCurr, wCurr, 1.0 - whiteScore);
                    int wNew = wCurr + wChessDelta;
                    int bNew = bCurr + bChessDelta;

                    config.EngineProfiles[_matchWhiteFileName] = new EngineProfile
                    {
                        DisplayName = wProf.DisplayName, Elo = wProf.Elo,
                        ChessdroidElo = wNew, GamesPlayed = wProf.GamesPlayed + 1
                    };
                    config.EngineProfiles[_matchBlackFileName] = new EngineProfile
                    {
                        DisplayName = bProf.DisplayName, Elo = bProf.Elo,
                        ChessdroidElo = bNew, GamesPlayed = bProf.GamesPlayed + 1
                    };
                    config.Save();

                    string wName = !string.IsNullOrEmpty(wProf.DisplayName) ? wProf.DisplayName : Path.GetFileNameWithoutExtension(_matchWhiteFileName);
                    string bName = !string.IsNullOrEmpty(bProf.DisplayName) ? bProf.DisplayName : Path.GetFileNameWithoutExtension(_matchBlackFileName);
                    analysisOutput.AppendText($"Chessdroid:  {wName} {wCurr} → {wNew} ({Services.EloCalculator.FormatDelta(wChessDelta)})  |  {bName} {bCurr} → {bNew} ({Services.EloCalculator.FormatDelta(bChessDelta)})\n");
                }
            }

            analysisOutput.ScrollToCaret();

            lblStatus.Text = result.GetResultString();

            // Set eval bar to reflect the actual result.
            // The arbiter reports "Mate in 0" for the final checkmate position, which is ambiguous
            // (SetMate(0) always resolves to full-black). Use SetTerminalMate instead.
            if (result.Outcome == MatchOutcome.WhiteWins)
            {
                evalBar?.SetTerminalMate(true);
                boardControl.TriggerParticles();
                PlayGameEndSound();
            }
            else if (result.Outcome == MatchOutcome.BlackWins)
            {
                evalBar?.SetTerminalMate(false);
                boardControl.TriggerParticles();
                PlayGameEndSound();
            }
            else
            {
                PlayGameEndSound();
            }

            // Auto-save PGN
            if (chkAutoSavePgn.Checked && result.Outcome != MatchOutcome.Interrupted)
                AutoSaveMatchPgn();

            // Series: update scores and continue if games remain
            if (result.Outcome != MatchOutcome.Interrupted)
            {
                bool eng1WasWhite = _seriesCurrentWhiteFile == _seriesEng1File;
                if (result.Outcome == MatchOutcome.WhiteWins)
                    (eng1WasWhite ? ref _seriesEng1Score : ref _seriesEng2Score) += 1.0;
                else if (result.Outcome == MatchOutcome.BlackWins)
                    (eng1WasWhite ? ref _seriesEng2Score : ref _seriesEng1Score) += 1.0;
                else if (result.Outcome == MatchOutcome.Draw)
                { _seriesEng1Score += 0.5; _seriesEng2Score += 0.5; }

                _seriesPlayed++;
                UpdateSeriesScoreLabel();
            }

            // Clean up match service
            boardControl.AnimationCompleted -= MatchBoard_AnimationCompleted;
            _awaitingMatchAnimation = false;
            if (matchService != null)
            {
                matchService.OnMovePlayed -= MatchService_OnMovePlayed;
                matchService.OnClockUpdated -= MatchService_OnClockUpdated;
                matchService.OnMatchEnded -= MatchService_OnMatchEnded;
                matchService.OnStatusChanged -= MatchService_OnStatusChanged;
                matchService.OnAnnotatorEvalUpdated -= MatchService_OnAnnotatorEvalUpdated;
                matchService.Dispose();
                matchService = null;
            }

            // Continue series or re-enable controls
            if (result.Outcome != MatchOutcome.Interrupted && _seriesPlayed < _seriesTotal)
                _ = StartNextSeriesGameAsync();
            else
                SetMatchControlsEnabled(false);
        }

        private void UpdateSeriesScoreLabel()
        {
            if (_seriesTotal <= 1) return;
            string s1 = _seriesEng1Score % 1 == 0 ? $"{_seriesEng1Score:0}" : $"{_seriesEng1Score:0.0}";
            string s2 = _seriesEng2Score % 1 == 0 ? $"{_seriesEng2Score:0}" : $"{_seriesEng2Score:0.0}";
            lblSeriesScore.Text = $"({_seriesPlayed}/{_seriesTotal})  {s1} – {s2}";
        }

        private void AutoSaveMatchPgn()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MatchGames");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string file = Path.Combine(dir, $"match_{stamp}.pgn");
                File.WriteAllText(file, GeneratePgn());
                analysisOutput.AppendText($"Saved: {file}\n");
            }
            catch (Exception ex)
            {
                analysisOutput.AppendText($"Auto-save failed: {ex.Message}\n");
            }
        }

        private async Task StartNextSeriesGameAsync()
        {
            // Swap colors for odd-numbered games
            bool eng1IsWhite = _seriesPlayed % 2 == 0;
            string whiteFile = eng1IsWhite ? _seriesEng1File : _seriesEng2File;
            string blackFile = eng1IsWhite ? _seriesEng2File : _seriesEng1File;
            _seriesCurrentWhiteFile = whiteFile;

            _matchWhiteFileName = whiteFile;
            _matchBlackFileName = blackFile;
            _matchWhiteName = Path.GetFileNameWithoutExtension(whiteFile);
            _matchBlackName = Path.GetFileNameWithoutExtension(blackFile);

            config.EngineProfiles.TryGetValue(whiteFile, out var whiteProfile);
            config.EngineProfiles.TryGetValue(blackFile, out var blackProfile);
            _matchWhiteElo = whiteProfile?.Elo ?? 0;
            _matchBlackElo = blackProfile?.Elo ?? 0;
            SetEngineInfoLabels(whiteProfile, blackProfile, whiteFile, blackFile);

            string enginesPath = config.GetEnginesPath();
            string whitePath = Path.Combine(enginesPath, whiteFile);
            string blackPath = Path.Combine(enginesPath, blackFile);
            if (!File.Exists(whitePath) || !File.Exists(blackPath)) { SetMatchControlsEnabled(false); return; }

            // Build time control from UI
            var tc = new EngineMatchTimeControl();
            switch (cmbTimeControlType.SelectedIndex)
            {
                case 0: tc.Type = TimeControlType.FixedDepth;          tc.Depth       = (int)numDepth.Value;       break;
                case 1: tc.Type = TimeControlType.FixedTimePerMove;    tc.MoveTimeMs  = (int)numMoveTime.Value;    break;
                case 2: tc.Type = TimeControlType.TotalPlusIncrement;
                        tc.TotalTimeMs = (int)numTotalTime.Value * 1000;
                        tc.IncrementMs = (int)numIncrement.Value * 1000;                                            break;
            }

            boardControl.ResetBoard();
            string startFen = boardControl.GetFEN();
            bool bookReady = chkUseBook.Checked &&
                             (rbBookChoose.Checked ? _matchBookOpening != null : openingBookService?.IsLoaded == true);
            bool chooseMode = bookReady && rbBookChoose.Checked && _matchBookOpening != null;
            if (bookReady) startFen = GetBookStartFen(startFen);

            CancelClassification();
            if (!chooseMode)
                moveTree.Clear(startFen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            if (chooseMode)
            {
                isNavigating = true;
                try { UpdateMoveList(); }
                finally { isNavigating = false; }
            }

            string whiteName = GetEngineLabel(whiteFile, true);
            string blackName = GetEngineLabel(blackFile, true);
            analysisOutput.AppendText($"\n— Game {_seriesPlayed + 1}/{_seriesTotal}: {whiteName} (W) vs {blackName} (B) —\n\n");

            matchService?.Dispose();
            matchService = new EngineMatchService(config);
            _previousMatchEval = null;
            matchService.OnMovePlayed          += MatchService_OnMovePlayed;
            matchService.OnClockUpdated        += MatchService_OnClockUpdated;
            matchService.OnMatchEnded          += MatchService_OnMatchEnded;
            matchService.OnStatusChanged       += MatchService_OnStatusChanged;
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            matchService.WaitForAnimation      = config.ShowAnimations;
            matchService.AnnotatorEngine       = engineService;
            matchService.AnnotatorDepth        = config.EngineDepth;
            matchService.AdjudicationEnabled   = chkAdjudicate.Checked;
            boardControl.AnimationCompleted    += MatchBoard_AnimationCompleted;

            if (tc.Type == TimeControlType.TotalPlusIncrement)
                UpdateClockDisplay(tc.TotalTimeMs, tc.TotalTimeMs, true);
            else
            { lblWhiteClock.Text = "W: --:--"; lblBlackClock.Text = "B: --:--"; }

            clockTimer.Start();
            await matchService.StartMatchAsync(whitePath, blackPath, _matchWhiteName, _matchBlackName, tc, startFen);
        }

        private string GetBookStartFen(string startFen)
        {
            // Choose mode: replay ECO opening moves, populate move tree from move 1
            if (rbBookChoose.Checked && _matchBookOpening != null)
                return PopulateOpeningMovesToTree(_matchBookOpening.Moves);

            // Random mode: 2 Polyglot plies
            const int BookPlies = 2;
            var board = ChessBoard.FromFEN(startFen);
            string castling = "KQkq";
            string ep = "-";
            bool whiteToMove = true;
            string fenRnd = startFen;
            for (int i = 0; i < BookPlies; i++)
            {
                var move = openingBookService!.GetBestBookMove(fenRnd);
                if (move == null) break;
                ChessRulesService.ApplyUciMove(board, move.UciMove, ref castling, ref ep);
                whiteToMove = !whiteToMove;
                fenRnd = $"{board.ToFEN()} {(whiteToMove ? "w" : "b")} {castling} {ep} 0 1";
            }
            boardControl.LoadFEN(fenRnd);
            return fenRnd;
        }

        private string PopulateOpeningMovesToTree(string sanMoves)
        {
            const string standardStart = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            moveTree.Clear(standardStart);
            string currentFen = standardStart;
            bool savedNav = isNavigating;
            isNavigating = true;
            try
            {
                var tokens = sanMoves.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => !System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d+\.") &&
                                 t != "1-0" && t != "0-1" && t != "1/2-1/2" && t != "*")
                    .ToList();
                foreach (var san in tokens)
                {
                    string? uci = ConvertSanToUci(san, currentFen);
                    if (uci == null) break;
                    boardControl.LoadFEN(currentFen);
                    if (!boardControl.MakeMove(uci)) break;
                    currentFen = boardControl.GetFEN();
                    moveTree.AddMove(uci, san, currentFen);
                }
            }
            finally
            {
                isNavigating = savedNav;
            }
            return currentFen;
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

        private void MatchService_OnAnnotatorEvalUpdated(string eval)
        {
            if (InvokeRequired)
            {
                Invoke(() => MatchService_OnAnnotatorEvalUpdated(eval));
                return;
            }

            UpdateEvalBar(eval);
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
                var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");
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
            cmbWhiteEngine.Enabled   = !running;
            cmbBlackEngine.Enabled   = !running;
            cmbTimeControlType.Enabled = !running;
            numDepth.Enabled         = !running;
            numMoveTime.Enabled      = !running;
            numTotalTime.Enabled     = !running;
            numIncrement.Enabled     = !running;
            numGames.Enabled         = !running;
            chkAdjudicate.Enabled    = !running;
            chkAutoSavePgn.Enabled   = !running;
            chkUseBook.Enabled       = !running;
            rbBookRandom.Enabled     = !running && chkUseBook.Checked;
            rbBookChoose.Enabled     = !running && chkUseBook.Checked;

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

            string[] availableEngines = Directory.Exists(config.GetEnginesPath())
                ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();
            using var dialog = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
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
            consoleFormatter?.SetActiveClassification(null);
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
                string engineFile = !string.IsNullOrEmpty(_botSettings.EngineFileName)
                    ? _botSettings.EngineFileName : config.SelectedEngine;
                string enginePath = Path.Combine(enginesPath, engineFile);

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
                await _botEngine.SetEloTargetAsync(_botSettings.EloTarget, _botSettings.GetSkillLevel());
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
                    string san = ConvertUciToSan(bestMove, fenBeforeMove);
                    if (config?.ShowAnimations == true)
                        boardControl.StartAnimation(bestMove);
                    PlayMoveSound(san.Contains('x'), san);
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
            if (inCheck) boardControl.TriggerParticles();
            PlayGameEndSound();
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
                    // If bestMove is still empty the position is terminal (checkmate/stalemate)
                    if (!ct.IsCancellationRequested && (lastBestMove == "(none)" || lastBestMove == "0000" || string.IsNullOrEmpty(lastBestMove)))
                    {
                        var board = ChessBoard.FromFEN(fen);
                        bool stmIsWhite = fen.Split(' ').ElementAtOrDefault(1) == "w";
                        if (ChessUtilities.IsKingInCheck(board, stmIsWhite))
                        {
                            string winner = stmIsWhite ? "Black" : "White";
                            consoleFormatter?.ShowGameOver($"Checkmate — {winner} wins!");
                            evalBar?.SetMate(stmIsWhite ? -1 : 1);
                            lblStatus.Text = $"Checkmate — {winner} wins";
                            boardControl.TriggerParticles();
                        }
                        else
                        {
                            consoleFormatter?.ShowGameOver("Stalemate — Draw");
                            evalBar?.Reset();
                            lblStatus.Text = "Stalemate — Draw";
                        }
                        PlayGameEndSound();
                        return;
                    }
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

                if (string.IsNullOrEmpty(result.bestMove) || result.bestMove == "(none)" || result.bestMove == "0000")
                {
                    // No legal moves — checkmate or stalemate
                    var board = ChessBoard.FromFEN(fen);
                    bool sideToMoveIsWhite = fen.Split(' ').ElementAtOrDefault(1) == "w";
                    bool inCheck = ChessUtilities.IsKingInCheck(board, sideToMoveIsWhite);
                    if (inCheck)
                    {
                        string winner = sideToMoveIsWhite ? "Black" : "White";
                        consoleFormatter?.ShowGameOver($"Checkmate — {winner} wins!");
                        evalBar?.SetMate(sideToMoveIsWhite ? -1 : 1);
                        lblStatus.Text = $"Checkmate — {winner} wins";
                        boardControl.TriggerParticles();
                    }
                    else
                    {
                        consoleFormatter?.ShowGameOver("Stalemate — Draw");
                        evalBar?.Reset();
                        lblStatus.Text = "Stalemate — Draw";
                    }
                    PlayGameEndSound();
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
                ? (ThemeService.IsDarkTheme(config?.Theme) ? Color.White : Color.Black)
                : (ThemeService.IsDarkTheme(config?.Theme) ? Color.LightGray : Color.DimGray);
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
            _evalGraph.SetData(moveTree, current, ThemeService.IsDarkTheme(config?.Theme));
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

            bool isDark = ThemeService.IsDarkTheme(config?.Theme);
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

        private void BtnSaveToLibrary_Click(object? sender, EventArgs e)
        {
            if (_libraryService == null) return;
            if (moveTree.GetMainLine().Count == 0)
            {
                lblStatus.Text = "No game to save";
                return;
            }

            // Find the deepest position in the main line that has a known ECO opening
            string opening = OpeningBook.GetOpeningDisplay(moveTree.Root.FEN);
            foreach (var node in moveTree.GetMainLine())
            {
                string o = OpeningBook.GetOpeningDisplay(node.FEN);
                if (!string.IsNullOrEmpty(o)) opening = o;
            }

            string white = !string.IsNullOrEmpty(_matchWhiteName) ? _matchWhiteName : _pgnHeaders.GetValueOrDefault("White", "?");
            string black = !string.IsNullOrEmpty(_matchBlackName) ? _matchBlackName : _pgnHeaders.GetValueOrDefault("Black", "?");

            // If re-saving a library game and names are still unknown, use whatever was last saved
            if (!string.IsNullOrEmpty(_libraryGameId) && _libraryService != null)
            {
                var existing = _libraryService.Load(_libraryGameId);
                if (existing != null)
                {
                    if (white == "?") white = existing.White;
                    if (black == "?") black = existing.Black;
                }
            }

            var game = new Models.SavedGame
            {
                White = white,
                Black = black,
                Event = _pgnHeaders.GetValueOrDefault("Event", "Chess Analysis"),
                Result = _pgnHeaders.GetValueOrDefault("Result", "*"),
                EngineName = engineService?.EngineName ?? "",
                EngineDepth = config?.EngineDepth ?? 0,
                WhiteAccuracy = _currentClassification?.WhiteAccuracy,
                BlackAccuracy = _currentClassification?.BlackAccuracy,
                HasClassification = _currentClassification != null,
                Opening = opening,
                Pgn = GeneratePgn(white, black)
            };

            // Overwrite existing library record instead of creating a duplicate
            if (!string.IsNullOrEmpty(_libraryGameId))
                game.Id = _libraryGameId;

            _libraryService!.Save(game);
            _libraryGameId = game.Id;

            string label = game.White == "?" && game.Black == "?"
                ? "game"
                : $"{game.White} vs {game.Black}";
            lblStatus.Text = $"Saved \"{label}\" to library";
        }

        private void BtnOpenLibrary_Click(object? sender, EventArgs e)
        {
            if (_libraryService == null) return;

            using var dialog = new GameLibraryDialog(_libraryService, ThemeService.IsDarkTheme(config?.Theme));
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedGame != null)
            {
                var saved = dialog.SelectedGame;
                ImportPgn(saved.Pgn);
                // Restore library identity so re-saving overwrites the same record
                _libraryGameId = saved.Id;
                if (saved.White != "?") _pgnHeaders["White"] = saved.White;
                if (saved.Black != "?") _pgnHeaders["Black"] = saved.Black;
                // Restore accuracy scores and activate the review link
                if (_currentClassification != null && saved.HasClassification)
                {
                    if (saved.WhiteAccuracy.HasValue) _currentClassification.WhiteAccuracy = saved.WhiteAccuracy.Value;
                    if (saved.BlackAccuracy.HasValue) _currentClassification.BlackAccuracy = saved.BlackAccuracy.Value;
                    consoleFormatter?.SetActiveClassification(_currentClassification);
                    consoleFormatter?.DisplayClassificationSummary(_currentClassification);
                }
            }
        }

        private void BtnOpenings_Click(object? sender, EventArgs e)
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            using var dialog = new OpeningExplorerDialog(entries, pgn =>
            {
                ImportPgn(pgn);
                NavigateToEnd();
            }, ThemeService.IsDarkTheme(config?.Theme));
            dialog.ShowDialog(this);
        }

        /// <summary>
        /// Generates PGN string from the current move tree.
        /// </summary>
        private string GeneratePgn(string? white = null, string? black = null)
        {
            white ??= !string.IsNullOrEmpty(_matchWhiteName) ? _matchWhiteName : _pgnHeaders.GetValueOrDefault("White", "?");
            black ??= !string.IsNullOrEmpty(_matchBlackName) ? _matchBlackName : _pgnHeaders.GetValueOrDefault("Black", "?");

            var sb = new System.Text.StringBuilder();

            // Standard PGN headers
            sb.AppendLine("[Event \"Chess Analysis\"]");
            sb.AppendLine("[Site \"chessdroid\"]");
            sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            sb.AppendLine("[Round \"?\"]");
            sb.AppendLine($"[White \"{white}\"]");
            sb.AppendLine($"[Black \"{black}\"]");
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

                _pgnHeaders = headers;
                _matchWhiteName = "";
                _matchBlackName = "";
                _matchWhiteFileName = "";
                _matchBlackFileName = "";
                _lblBlackEngineInfo.Visible = false;
                _lblWhiteEngineInfo.Visible = false;
                _libraryGameId = "";

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
            var lastGraphRefresh = DateTime.UtcNow;

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
                                int _sp = beforeFen.IndexOf(' ');
                                bool whiteToMove = _sp >= 0 && _sp + 1 < beforeFen.Length && beforeFen[_sp + 1] == 'w';

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

                    // Update stats — use finalQuality so brilliant overrides are counted correctly
                    if (node.IsWhiteMove)
                    {
                        whiteMoves++;
                        classification.WhiteCounts[finalQuality]++;
                    }
                    else
                    {
                        blackMoves++;
                        classification.BlackCounts[finalQuality]++;
                    }

                    // Throttled graph refresh — update the visual at most every 500ms so the
                    // user sees the graph fill in progressively without hammering GDI on every move.
                    if ((DateTime.UtcNow - lastGraphRefresh).TotalMilliseconds >= 500)
                    {
                        RefreshEvalGraph();
                        lastGraphRefresh = DateTime.UtcNow;
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
            classification.WhiteAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: true);
            classification.BlackAccuracy = ComputeAccuracy(classification.MoveResults, forWhite: false);

            _currentClassification = classification;

            // isNavigating prevents MoveListBox_SelectedIndexChanged from firing TriggerAutoAnalysis
            // when we update item text with classification symbols — otherwise analysis overwrites the summary
            isNavigating = true;
            UpdateMoveListWithClassification();
            isNavigating = false;

            RefreshEvalGraph();
            consoleFormatter?.SetActiveClassification(classification);
            consoleFormatter?.DisplayClassificationSummary(classification);

            // Append Elo performance if player ratings are known (PGN headers or engine profiles)
            var eloText = TryGetEloPerformanceText();
            if (eloText != null) analysisOutput.AppendText(eloText);

            lblStatus.Text = $"Game review — White {classification.WhiteAccuracy:F1}%  Black {classification.BlackAccuracy:F1}%";
        }

        private string? TryGetEloPerformanceText()
        {
            // PGN headers take priority (imported games); fall back to engine match fields
            int whiteElo = 0, blackElo = 0;

            if (_pgnHeaders.TryGetValue("WhiteElo", out string? wStr) && int.TryParse(wStr, out int we) && we > 0)
                whiteElo = we;
            else if (_matchWhiteElo > 0)
                whiteElo = _matchWhiteElo;

            if (_pgnHeaders.TryGetValue("BlackElo", out string? bStr) && int.TryParse(bStr, out int be) && be > 0)
                blackElo = be;
            else if (_matchBlackElo > 0)
                blackElo = _matchBlackElo;

            if (whiteElo <= 0 || blackElo <= 0) return null;

            string resultStr = _pgnHeaders.GetValueOrDefault("Result", "*");
            double whiteScore = resultStr switch
            {
                "1-0"     => 1.0,
                "0-1"     => 0.0,
                "1/2-1/2" => 0.5,
                _         => -1.0
            };
            if (whiteScore < 0) return null;

            string white = !string.IsNullOrEmpty(_matchWhiteFileName) ? GetEngineLabel(_matchWhiteFileName, false)
                : _pgnHeaders.GetValueOrDefault("White", "White");
            string black = !string.IsNullOrEmpty(_matchBlackFileName) ? GetEngineLabel(_matchBlackFileName, false)
                : _pgnHeaders.GetValueOrDefault("Black", "Black");

            int whiteDelta = Services.EloCalculator.EloChange(whiteElo, blackElo, whiteScore);
            int blackDelta = Services.EloCalculator.EloChange(blackElo, whiteElo, 1.0 - whiteScore);

            return $"\nElo change: {white} {Services.EloCalculator.FormatDelta(whiteDelta)}  {black} {Services.EloCalculator.FormatDelta(blackDelta)}\n";
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

        private static double ComputeAccuracy(List<MoveReviewResult> results, bool forWhite)
        {
            var moves = results.Where(r => r.IsWhiteMove == forWhite).ToList();
            if (moves.Count == 0) return 100.0;

            double totalWinLoss = 0;
            foreach (var m in moves)
            {
                // EvalBefore/EvalAfter are in White's perspective (pawns). Flip for Black.
                double evalBefore = forWhite ? m.EvalBefore : -m.EvalBefore;
                double evalAfter  = forWhite ? m.EvalAfter  : -m.EvalAfter;

                // Logistic win probability from player's perspective (same scale as GetMoveClassification)
                double wpBefore = 1.0 / (1.0 + Math.Pow(10, -evalBefore / 4.0));
                double wpAfter  = 1.0 / (1.0 + Math.Pow(10, -evalAfter  / 4.0));
                totalWinLoss += Math.Max(0, wpBefore - wpAfter);
            }

            // Lichess accuracy formula: avgWinLoss in percentage points (0–100)
            double avgWinLoss = totalWinLoss / moves.Count * 100.0;
            return Math.Max(0, Math.Min(100, 103.1668 * Math.Exp(-0.04354 * avgWinLoss) - 3.1669));
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
        // "Sicilian: Najdorf, 6.Be3 e5 7.Nb3" → "Sicilian: Najdorf" (hides moves during recreate)
        private static string StripMovesFromOpeningName(string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(name, @",?\s*\d+\.");
            return m.Success ? name[..m.Index].TrimEnd(',', ' ') : name;
        }

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

        #region Square Training

        private const int TRAINING_WRONG_MS   = 280;
        private const int TRAINING_CORRECT_MS = 380;
        private const string TRAINING_EMPTY_FEN = "8/8/8/8/8/8/8/8 w - - 0 1";
        private static readonly Random _trainingRng = new();

        private static readonly Color TrainingHintColor  = Color.FromArgb(120, 255, 235, 80);
        private static readonly Color TrainingErrorColor = Color.FromArgb(175, 215, 50, 50);
        private static readonly Color TrainingOkColor    = Color.FromArgb(175, 50, 205, 50);

        private bool _trainingUiVisible = false;
        private bool _trainingGameActive = false;
        private string? _trainingPreFen;
        private bool _trainingPreFlipped;
        private int  _trainingTargetRow, _trainingTargetCol;
        private int  _trainingCorrect, _trainingWrong;
        private int  _trainingQuestions;   // set from numericupdown at round start
        private int  _trainingTimeLimitSec; // 0 = no limit
        private bool _trainingAwaitingNext;
        private DateTime _trainingRoundStart;
        private bool _trainingInWrongFlash, _trainingInCorrectFlash;
        private int  _trainingFlashMs;
        private int  _trainingCorrectRow, _trainingCorrectCol;

        private System.Windows.Forms.Timer? _trainingClockTimer;
        private System.Windows.Forms.Timer? _trainingFlashTimer;

        private Panel?          _pnlTraining;
        private Panel?          _pnlTrainingStart;
        private Panel?          _pnlTrainingGame;
        private Panel?          _pnlTrainingResult;
        private RadioButton?    _rbTrainingEasy;
        private RadioButton?    _rbTrainingBlack;
        private RadioButton?    _rbTrainingRandom;
        private NumericUpDown?  _numQuestions;
        private NumericUpDown?  _numTimeLimit;
        private Label?       _lblTrainingRound;
        private Label?       _lblTrainingTarget;
        private Label?       _lblTrainingScore;
        private Label?       _lblTrainingTimer;
        private Label?       _lblTrainingFinalScore;
        private Label?       _lblTrainingPB;

        // Opening Training state
        private bool _openingModeSelected;
        private bool _openingRecreatePhase;
        private bool _openingRecreateLocked; // true during flash + restore delay

        private OpeningEntry? _selectedTrainingOpening;
        private int _selectedWatches;
        private readonly List<string> _openingUciMoves = new();
        private readonly List<string> _openingFens = new();
        private int _openingPlayIndex;
        private int _openingWatchesLeft;
        private int _openingRecreateIndex;
        private readonly Dictionary<int, int> _openingPosMistakes = new(); // posIndex → attempt count
        private System.Windows.Forms.Timer? _openingAutoplayTimer;

        // Opening Training UI refs
        private Label?  _lblTrainingTitle;
        private Label?  _lblSquareDesc;
        private Label?  _lblSquareSettingsPB;
        private Label?  _lblPuzzleSettingsPB;
        private Label?  _lblVisionSettingsPB;
        private Button? _btnSqMode;
        private Button? _btnOpMode;
        private Button? _btnDrillMode;
        private Panel?  _pnlDrillSettings;
        private ComboBox? _cmbDrillStudy;
        private ComboBox? _cmbDrillChapter;
        private Label?    _lblDrillDesc;
        private Button?   _btnDrillVsBot;
        private List<EndgameChapter> _drillChapters = new();
        private string?  _drillsFolder;
        private Panel? _pnlSquareSettings;
        private Panel? _pnlOpeningSettings;
        private RadioButton? _rbOpRandom;
        private RadioButton? _rbOpSelected;
        private Label? _lblSelectedOpening;
        private RadioButton? _rbWatchesNone;
        private RadioButton? _rbWatches1;
        private RadioButton? _rbWatches2;
        private RadioButton? _rbWatches3;
        private Label?  _lblOpGameStatus;
        private Label?  _lblOpMissedMoves;
        private Button? _btnOpHint;
        private int     _openingHintsUsed;
        private int     _openingSessionRuns;
        private int     _openingSessionPerfect;

        // Puzzle Training state
        private bool   _puzzleModeSelected;
        private bool   _puzzleActive;
        private bool   _puzzleLocked;
        private LichessPuzzle? _currentPuzzle;
        private int    _puzzleMoveIndex;   // index into Moves[] the player must play next
        private readonly List<LichessPuzzle> _puzzleQueue = new();
        private int    _puzzlesClean;      // solved with zero wrong moves and zero hints
        private int    _puzzlesStruggled;  // solved but used hints or wrong moves
        private int    _puzzlesAttempted;
        private bool   _wrongThisPuzzle;   // any wrong move or hint used this puzzle
        private DateTime _puzzleStartTime; // when player first got control (after trigger)
        private string?  _puzzlesFolder;
        private string?  _puzzleThemeFilter;

        // Puzzle sub-mode: "training" | "rush" | "gauntlet"
        private string _puzzleSubMode = "training";
        private Button? _btnPuzzleSubTraining;
        private Button? _btnPuzzleSubRush;
        private Button? _btnPuzzleSubGauntlet;
        private Button? _btnPuzzleSubDaily;
        private Panel?  _pnlRushTimeRow;
        private Panel?  _pnlThemeFilterRow;
        private Panel?  _pnlOpeningFilterRow;
        private ComboBox? _cmbPuzzleOpening;
        private string? _puzzleOpeningFilter;
        private Panel?  _pnlRatingRow;
        private Button? _ratingBtnAny, _ratingBtnBeg, _ratingBtnInt, _ratingBtnAdv, _ratingBtnMaster;
        private Label?  _lblRatingRange;
        private int     _puzzleRatingMin = 0;
        private int     _puzzleRatingMax = int.MaxValue;
        private Label?  _lblGauntletDesc;
        private int     _rushDurationSeconds = 180;
        private Button? _rushTimeBtn1, _rushTimeBtn2, _rushTimeBtn3, _rushTimeBtn4, _rushTimeBtn5;
        private System.Windows.Forms.Timer? _puzzleRushTimer;
        private int     _rushSecondsRemaining;
        private Label?  _lblRushTimer;
        private int     _gauntletStreak;
        private int     _gauntletBestStreak;
        private int     _puzzleStreak;
        private int     _puzzleStreakBest;

        // Puzzle Training UI refs
        private Button?   _btnPuzzleMode;
        private Panel?    _pnlPuzzleGame;
        private Panel?    _pnlPuzzleSettings;
        private Panel?    _pnlPuzzleAutoNextRow;
        private CheckBox? _chkPuzzleAutoNext;
        private Button?   _btnPuzzleNext;
        private Button?   _btnPuzzleAnalyze;
        private ComboBox? _cmbPuzzleTheme;
        private Label?    _lblPuzzleRating;
        private Label?  _lblPuzzleThemes;
        private Label?  _lblPuzzleFeedback;
        private Label?  _lblPuzzleStats;
        private Button? _btnPuzzleHint;
        private Button? _btnPuzzleSkip;
        private int     _puzzleHintsUsed;

        // Board Vision state
        private bool    _visionModeSelected;
        // Endgame Drills state
        private bool    _drillModeSelected;
        private int     _visionCorrect;
        private int     _visionWrong;
        private int     _visionStreak;
        private int     _visionBestStreak;
        private string  _visionCurrentSquare = "a1";

        // Board Vision UI refs
        private Button? _btnVisionMode;
        private Panel?   _pnlVisionAutoNextRow;
        private CheckBox? _chkVisionAutoNext;
        private Button?   _btnVisionNext;
        private Panel?  _pnlVisionGame;
        private Panel?  _pnlVisionSettings;
        private Label?  _lblVisionQuestion;
        private Label?  _lblVisionScore;
        private Button? _btnVisionLight;
        private Button? _btnVisionDark;
        private Label?  _lblVisionTimer;
        private Label?  _lblVisionLives;

        // Board Vision sub-mode
        private string  _visionSubMode = "training";
        private Button? _btnVisionSubTraining, _btnVisionSubTimed, _btnVisionSubSurvival;
        private Panel?  _pnlVisionTimeRow;
        private Panel?  _pnlVisionGlobalTimeRow;
        private Label?  _lblVisionDesc;
        private int     _visionTimedSeconds = 5;
        private Button? _visionTimeBtn3, _visionTimeBtn5, _visionTimeBtn10;
        private System.Windows.Forms.Timer? _visionQuestionTimer;
        private int     _visionSecondsRemaining;
        private int     _visionLives;
        private int     _visionGlobalDurationSeconds = 180;
        private int     _visionGlobalSecondsRemaining;
        private Button? _visionGlobalBtn1, _visionGlobalBtn3, _visionGlobalBtn5;
        private System.Windows.Forms.Timer? _visionGlobalTimer;
        private Label?  _lblVisionGlobalTimer;

        private void InitTrainingPanel()
        {
            _trainingClockTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _trainingClockTimer.Tick += (_, _) =>
            {
                TrainingUpdateStats();
                if (_trainingGameActive && !_trainingAwaitingNext && TrainingCheckTimeExpired())
                {
                    _trainingAwaitingNext = true;
                    _trainingFlashTimer?.Stop();
                    boardControl.ClearTrainingHighlight();
                    TrainingShowResult();
                }
            };
            _trainingFlashTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _trainingFlashTimer.Tick += TrainingFlashTimer_Tick;

            _openingAutoplayTimer = new System.Windows.Forms.Timer { Interval = 900 };
            _openingAutoplayTimer.Tick += OpeningAutoplayTimer_Tick;

            boardControl.SquareClicked += TrainingSquareClicked;

            // Local font factory — every call returns a new instance (safe for WinForms disposal)
            Font F(float size, bool bold = false) =>
                new Font("Courier New", size, bold ? FontStyle.Bold : FontStyle.Regular);

            _pnlTraining = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(16, 12, 14, 10) };

            // ── Start panel ───────────────────────────────────────────────────
            _pnlTrainingStart = new Panel { Dock = DockStyle.Fill };
            _lblTrainingTitle = new Label
            {
                Text = "Square Training",
                Font = F(17f, true),
                Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.MiddleLeft
            };
            var lblTitle = _lblTrainingTitle;
            _lblSquareDesc = new Label
            {
                Text = "An empty board appears.\nClick the named square as fast as you can.\n10 questions per round.",
                Font = F(10f),
                Dock = DockStyle.Top, Height = 58, TextAlign = ContentAlignment.TopLeft
            };
            var lblDesc = _lblSquareDesc;
            var pnlMode = new Panel { Dock = DockStyle.Top, Height = 84 };
            var lblMode = new Label
            {
                Text = "Difficulty:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 2)
            };
            _rbTrainingEasy = new RadioButton
            {
                Text = "Easy  (hover shows the square name)",
                Font = F(10f),
                AutoSize = true, Location = new Point(0, 24), Checked = true
            };
            var rbTrainingChallenge = new RadioButton
            {
                Text = "Challenge  (no hints)",
                Font = F(10f),
                AutoSize = true, Location = new Point(0, 50)
            };
            _rbTrainingEasy.CheckedChanged    += (_, _) => UpdateSquarePBLabel();
            rbTrainingChallenge.CheckedChanged += (_, _) => UpdateSquarePBLabel();
            pnlMode.Controls.AddRange(new Control[] { lblMode, _rbTrainingEasy, rbTrainingChallenge });

            var pnlPerspective = new Panel { Dock = DockStyle.Top, Height = 50 };
            var lblPerspective = new Label
            {
                Text = "Perspective:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 2)
            };
            var rbTrainingWhite = new RadioButton
            {
                Text = "White",
                Font = F(10f),
                AutoSize = true, Location = new Point(0, 24), Checked = true
            };
            _rbTrainingBlack = new RadioButton
            {
                Text = "Black",
                Font = F(10f),
                AutoSize = true, Location = new Point(80, 24)
            };
            _rbTrainingRandom = new RadioButton
            {
                Text = "Random",
                Font = F(10f),
                AutoSize = true, Location = new Point(160, 24)
            };
            rbTrainingWhite.CheckedChanged   += (_, _) => UpdateSquarePBLabel();
            _rbTrainingBlack.CheckedChanged  += (_, _) => UpdateSquarePBLabel();
            _rbTrainingRandom.CheckedChanged += (_, _) => UpdateSquarePBLabel();
            pnlPerspective.Controls.AddRange(new Control[]
                { lblPerspective, rbTrainingWhite, _rbTrainingBlack, _rbTrainingRandom });

            var pnlCount = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblQuestions = new Label
            {
                Text = "Questions:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 4)
            };
            _numQuestions = new NumericUpDown
            {
                Minimum = 5, Maximum = 100, Value = 10, Width = 54,
                Font = F(10f), Location = new Point(112, 2)
            };
            _numQuestions.ValueChanged += (_, _) =>
            {
                if (_lblSquareDesc != null)
                    _lblSquareDesc.Text = $"An empty board appears.\nClick the named square as fast as you can.\n{(int)_numQuestions.Value} questions per round.";
            };
            pnlCount.Controls.AddRange(new Control[] { lblQuestions, _numQuestions });

            var pnlTime = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblTimeLimit = new Label
            {
                Text = "Time limit:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 4)
            };
            _numTimeLimit = new NumericUpDown
            {
                Minimum = 0, Maximum = 600, Value = 0, Width = 54,
                Font = F(10f), Location = new Point(112, 2)
            };
            var lblTimeSuffix = new Label
            {
                Text = "s  (0 = no limit)", Font = F(9f),
                AutoSize = true, Location = new Point(170, 5)
            };
            pnlTime.Controls.AddRange(new Control[] { lblTimeLimit, _numTimeLimit, lblTimeSuffix });

            var pnlStartGap = new Panel { Dock = DockStyle.Top, Height = 10 };
            var btnStart = new Button
            {
                Text = "Start", Font = F(11f, true),
                Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat
            };
            btnStart.Click += (_, _) => TrainingStartRound();

            // ── Mode switcher (2 rows) ─────────────────────────────────
            var pnlModeSwitcher = new Panel { Dock = DockStyle.Top, Height = 66 };
            _btnSqMode = new Button
            {
                Text = "♟ Square", Font = F(9f, true),
                Location = new Point(0, 4), Size = new Size(96, 26), FlatStyle = FlatStyle.Flat
            };
            _btnOpMode = new Button
            {
                Text = "♜ Openings", Font = F(9f, true),
                Location = new Point(100, 4), Size = new Size(96, 26), FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleMode = new Button
            {
                Text = "★ Puzzles", Font = F(9f, true),
                Location = new Point(200, 4), Size = new Size(96, 26), FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnVisionMode = new Button
            {
                Text = "◉ Vision", Font = F(9f, true),
                Location = new Point(0, 36), Size = new Size(96, 26), FlatStyle = FlatStyle.Flat
            };
            _btnDrillMode = new Button
            {
                Text = "⚑ Drills", Font = F(9f, true),
                Location = new Point(100, 36), Size = new Size(96, 26), FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnSqMode.Click     += (_, _) => SetTrainingMode("square");
            _btnOpMode.Click     += (_, _) => SetTrainingMode("opening");
            _btnPuzzleMode.Click += (_, _) => SetTrainingMode("puzzle");
            _btnVisionMode.Click += (_, _) => SetTrainingMode("vision");
            _btnDrillMode.Click  += (_, _) => SetTrainingMode("drill");
            pnlModeSwitcher.Controls.AddRange(new Control[] { _btnSqMode, _btnOpMode, _btnPuzzleMode, _btnVisionMode, _btnDrillMode });

            _lblSquareSettingsPB = new Label
            {
                Dock = DockStyle.Top, Height = 18, Font = F(8.5f),
                TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(130, 130, 130)
            };

            var lnkResetSquarePBs = new LinkLabel
            {
                Text = "Reset personal bests", Dock = DockStyle.Top, Height = 18,
                Font = F(8f), TextAlign = ContentAlignment.MiddleLeft, LinkColor = Color.FromArgb(140, 140, 140)
            };
            lnkResetSquarePBs.LinkClicked += (_, _) =>
            {
                if (config == null) return;
                foreach (var key in new[] { "Easy-White", "Easy-Black", "Easy-Random", "Challenge-White", "Challenge-Black", "Challenge-Random" })
                    config.TrainingPersonalBests.Remove(key);
                config.Save();
                UpdateSquarePBLabel();
            };

            // ── Wrap square settings into collapsible panel ────────────
            _pnlSquareSettings = new Panel { Dock = DockStyle.Top, Height = 288 };
            // DockStyle.Top: last = topmost visually
            _pnlSquareSettings.Controls.AddRange(new Control[]
                { lnkResetSquarePBs, pnlTime, pnlCount, pnlPerspective, pnlMode, _lblSquareSettingsPB, lblDesc });

            // ── Opening settings ───────────────────────────────────────
            _pnlOpeningSettings = new Panel { Dock = DockStyle.Top, Height = 110, Visible = false };

            var pnlOpModeRow = new Panel { Dock = DockStyle.Top, Height = 30 };
            var lblOpMode = new Label
            {
                Text = "Opening:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 6)
            };
            _rbOpRandom = new RadioButton
            {
                Text = "Random", Font = F(10f),
                AutoSize = true, Location = new Point(82, 5), Checked = true
            };
            _rbOpSelected = new RadioButton
            {
                Text = "Selected", Font = F(10f),
                AutoSize = true, Location = new Point(168, 5)
            };
            var btnChoose = new Button
            {
                Text = "Choose…", Font = F(9f),
                Location = new Point(268, 3), Size = new Size(76, 22), FlatStyle = FlatStyle.Flat
            };
            btnChoose.Click += (_, _) => SelectOpeningForTraining();
            pnlOpModeRow.Controls.AddRange(new Control[] { lblOpMode, _rbOpRandom, _rbOpSelected, btnChoose });

            _lblSelectedOpening = new Label
            {
                Text = "(random opening each round)",
                Font = F(8.5f),
                Dock = DockStyle.Top, Height = 22, AutoEllipsis = true
            };
            _rbOpRandom.CheckedChanged += (_, _) =>
            {
                if (_rbOpRandom.Checked && _lblSelectedOpening != null)
                    _lblSelectedOpening.Text = "(random opening each round)";
            };

            var pnlWatchesRow = new Panel { Dock = DockStyle.Top, Height = 30 };
            var lblWatches = new Label
            {
                Text = "Watches:", Font = F(10f, true),
                AutoSize = true, Location = new Point(0, 6)
            };
            _rbWatchesNone = new RadioButton
            {
                Text = "None", Font = F(10f),
                AutoSize = true, Location = new Point(82, 5)
            };
            _rbWatches1 = new RadioButton
            {
                Text = "1", Font = F(10f),
                AutoSize = true, Location = new Point(150, 5)
            };
            _rbWatches2 = new RadioButton
            {
                Text = "2", Font = F(10f),
                AutoSize = true, Location = new Point(188, 5), Checked = true
            };
            _rbWatches3 = new RadioButton
            {
                Text = "3", Font = F(10f),
                AutoSize = true, Location = new Point(226, 5)
            };
            pnlWatchesRow.Controls.AddRange(new Control[]
                { lblWatches, _rbWatchesNone, _rbWatches1, _rbWatches2, _rbWatches3 });

            var lblOpDesc = new Label
            {
                Text = "Watch the opening autoplay, then recreate it from memory.",
                Font = F(9.5f),
                Dock = DockStyle.Top, Height = 22
            };
            // DockStyle.Top: last = topmost visually
            _pnlOpeningSettings.Controls.AddRange(new Control[]
                { pnlWatchesRow, _lblSelectedOpening, pnlOpModeRow, lblOpDesc });

            // ── Puzzle settings (sub-mode + theme + rush time) ─────────────
            _pnlPuzzleSettings = new Panel { Dock = DockStyle.Top, Height = 98, Visible = false };

            // Sub-mode switcher: Training · Rush · Gauntlet · Daily
            var pnlPuzzleSub = new Panel { Dock = DockStyle.Top, Height = 32 };
            _btnPuzzleSubTraining = new Button
            {
                Text = "Training", Font = F(9f, true),
                Location = new Point(0, 4), Size = new Size(68, 24), FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSubRush = new Button
            {
                Text = "Rush", Font = F(9f, true),
                Location = new Point(72, 4), Size = new Size(68, 24), FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSubGauntlet = new Button
            {
                Text = "Gauntlet", Font = F(9f, true),
                Location = new Point(144, 4), Size = new Size(68, 24), FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSubDaily = new Button
            {
                Text = "Daily", Font = F(9f, true),
                Location = new Point(216, 4), Size = new Size(68, 24), FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSubTraining.Click += (_, _) => SetPuzzleSubMode("training");
            _btnPuzzleSubRush.Click     += (_, _) => SetPuzzleSubMode("rush");
            _btnPuzzleSubGauntlet.Click += (_, _) => SetPuzzleSubMode("gauntlet");
            _btnPuzzleSubDaily.Click    += (_, _) => SetPuzzleSubMode("daily");
            pnlPuzzleSub.Controls.AddRange(new Control[] { _btnPuzzleSubTraining, _btnPuzzleSubRush, _btnPuzzleSubGauntlet, _btnPuzzleSubDaily });

            // Rush time row: 1 2 3 4 5 min
            _pnlRushTimeRow = new Panel { Dock = DockStyle.Top, Height = 30, Visible = false };
            var lblRushTimeLabel = new Label
            {
                Text = "Duration:", Font = F(9f, true),
                AutoSize = true, Location = new Point(0, 7)
            };
            _rushTimeBtn1 = new Button { Text = "1", Font = F(9f), Location = new Point(74, 3), Size = new Size(28, 22), FlatStyle = FlatStyle.Flat };
            _rushTimeBtn2 = new Button { Text = "2", Font = F(9f), Location = new Point(106, 3), Size = new Size(28, 22), FlatStyle = FlatStyle.Flat };
            _rushTimeBtn3 = new Button { Text = "3", Font = F(9f), Location = new Point(138, 3), Size = new Size(28, 22), FlatStyle = FlatStyle.Flat };
            _rushTimeBtn4 = new Button { Text = "4", Font = F(9f), Location = new Point(170, 3), Size = new Size(28, 22), FlatStyle = FlatStyle.Flat };
            _rushTimeBtn5 = new Button { Text = "5", Font = F(9f), Location = new Point(202, 3), Size = new Size(28, 22), FlatStyle = FlatStyle.Flat };
            var lblMinSuffix = new Label { Text = "min", Font = F(9f), AutoSize = true, Location = new Point(234, 7) };
            _rushTimeBtn1.Click += (_, _) => SelectRushTime(1);
            _rushTimeBtn2.Click += (_, _) => SelectRushTime(2);
            _rushTimeBtn3.Click += (_, _) => SelectRushTime(3);
            _rushTimeBtn4.Click += (_, _) => SelectRushTime(4);
            _rushTimeBtn5.Click += (_, _) => SelectRushTime(5);
            _pnlRushTimeRow.Controls.AddRange(new Control[] { lblRushTimeLabel, _rushTimeBtn1, _rushTimeBtn2, _rushTimeBtn3, _rushTimeBtn4, _rushTimeBtn5, lblMinSuffix });

            // Theme filter row (Training only)
            _pnlThemeFilterRow = new Panel { Dock = DockStyle.Top, Height = 52 };
            var lblThemeFilter = new Label
            {
                Text = "Theme Filter", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 20
            };
            _cmbPuzzleTheme = new ComboBox
            {
                Dock = DockStyle.Top, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F(9f)
            };
            foreach (var (label, _) in _puzzleThemeOptions)
                _cmbPuzzleTheme.Items.Add(label);
            _cmbPuzzleTheme.SelectedIndex = 0;
            _cmbPuzzleTheme.SelectedIndexChanged += (_, _) =>
            {
                int i = _cmbPuzzleTheme.SelectedIndex;
                _puzzleThemeFilter = i >= 0 ? _puzzleThemeOptions[i].Theme : null;
            };
            _pnlThemeFilterRow.Controls.AddRange(new Control[] { _cmbPuzzleTheme, lblThemeFilter });

            // Opening filter row (Training only)
            _pnlOpeningFilterRow = new Panel { Dock = DockStyle.Top, Height = 52, Visible = false };
            var lblOpeningFilter = new Label { Text = "Opening", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbPuzzleOpening = new ComboBox { Dock = DockStyle.Top, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            foreach (var (label, _) in _puzzleOpeningOptions)
                _cmbPuzzleOpening.Items.Add(label);
            _cmbPuzzleOpening.SelectedIndex = 0;
            _cmbPuzzleOpening.SelectedIndexChanged += (_, _) =>
            {
                int i = _cmbPuzzleOpening.SelectedIndex;
                _puzzleOpeningFilter = i >= 0 ? _puzzleOpeningOptions[i].Opening : null;
            };
            _pnlOpeningFilterRow.Controls.AddRange(new Control[] { _cmbPuzzleOpening, lblOpeningFilter });

            // Rating range row (all sub-modes)
            _pnlRatingRow = new Panel { Dock = DockStyle.Top, Height = 30 };
            var lblRatingLabel = new Label { Text = "Rating:", Font = F(9f, true), AutoSize = true, Location = new Point(0, 7) };
            _ratingBtnAny    = new Button { Text = "Any",    Font = F(9f), Location = new Point(58,  3), Size = new Size(50, 22), FlatStyle = FlatStyle.Flat };
            _ratingBtnBeg    = new Button { Text = "Beg.",   Font = F(9f), Location = new Point(112, 3), Size = new Size(50, 22), FlatStyle = FlatStyle.Flat };
            _ratingBtnInt    = new Button { Text = "Int.",   Font = F(9f), Location = new Point(166, 3), Size = new Size(46, 22), FlatStyle = FlatStyle.Flat };
            _ratingBtnAdv    = new Button { Text = "Adv.",   Font = F(9f), Location = new Point(216, 3), Size = new Size(50, 22), FlatStyle = FlatStyle.Flat };
            _ratingBtnMaster = new Button { Text = "Master", Font = F(9f), Location = new Point(270, 3), Size = new Size(62, 22), FlatStyle = FlatStyle.Flat };
            _lblRatingRange  = new Label  { Font = F(8.5f),  Location = new Point(338, 7), AutoSize = true, ForeColor = Color.FromArgb(160, 160, 160) };
            _ratingBtnAny.Click    += (_, _) => SelectPuzzleRating(0,    int.MaxValue);
            _ratingBtnBeg.Click    += (_, _) => SelectPuzzleRating(0,    1199);
            _ratingBtnInt.Click    += (_, _) => SelectPuzzleRating(1400, 1799);
            _ratingBtnAdv.Click    += (_, _) => SelectPuzzleRating(1800, 2199);
            _ratingBtnMaster.Click += (_, _) => SelectPuzzleRating(2200, int.MaxValue);
            _pnlRatingRow.Controls.AddRange(new Control[] { lblRatingLabel, _ratingBtnAny, _ratingBtnBeg, _ratingBtnInt, _ratingBtnAdv, _ratingBtnMaster, _lblRatingRange });

            // Gauntlet description
            _lblGauntletDesc = new Label
            {
                Text = "One wrong move ends the run.", Font = F(8.5f),
                Dock = DockStyle.Top, Height = 20, Visible = false
            };

            // Auto-next row (Training only)
            _pnlPuzzleAutoNextRow = new Panel { Dock = DockStyle.Top, Height = 26, Visible = false };
            _chkPuzzleAutoNext = new CheckBox
            {
                Text = "Auto-next", Checked = config?.PuzzleAutoNext != false,
                Font = F(9f), AutoSize = true, Location = new Point(4, 5)
            };
            _chkPuzzleAutoNext.CheckedChanged += (_, _) =>
            {
                if (config != null) { config.PuzzleAutoNext = _chkPuzzleAutoNext.Checked; config.Save(); }
            };
            _pnlPuzzleAutoNextRow.Controls.Add(_chkPuzzleAutoNext);

            var lnkResetPBs = new LinkLabel
            {
                Text = "Reset personal bests", Dock = DockStyle.Top, Height = 18,
                Font = F(8f), TextAlign = ContentAlignment.MiddleLeft, LinkColor = Color.FromArgb(140, 140, 140)
            };
            lnkResetPBs.LinkClicked += (_, _) =>
            {
                if (config == null) return;
                config.PuzzleRushBest           = 0;
                config.PuzzleTrainingBestStreak = 0;
                config.GauntletBestStreak       = 0;
                config.Save();
                _puzzleStreakBest    = 0;
                _gauntletBestStreak = 0;
                UpdatePuzzleStats();
                SetPuzzleSubMode(_puzzleSubMode);
            };

            var pnlPuzzleTopGap = new Panel { Dock = DockStyle.Top, Height = 8 };  // gap: mode switcher → sub-buttons
            var pnlPuzzleSubGap = new Panel { Dock = DockStyle.Top, Height = 6 };  // gap: sub-buttons → content row
            _lblPuzzleSettingsPB = new Label
            {
                Dock = DockStyle.Top, Height = 18, Font = F(8.5f),
                TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(130, 130, 130)
            };

            // DockStyle.Top: last = topmost visually — pnlPuzzleTopGap must be last
            _pnlPuzzleSettings.Controls.AddRange(new Control[]
                { lnkResetPBs, _lblPuzzleSettingsPB, _pnlPuzzleAutoNextRow, _lblGauntletDesc, _pnlRushTimeRow, _pnlRatingRow, _pnlOpeningFilterRow, _pnlThemeFilterRow, pnlPuzzleSubGap, pnlPuzzleSub, pnlPuzzleTopGap });

            // ── Vision settings ────────────────────────────────────────────
            _pnlVisionSettings = new Panel { Dock = DockStyle.Top, Height = 84, Visible = false };

            // Sub-mode switcher: Training · Timed · Survival
            var pnlVisionSub = new Panel { Dock = DockStyle.Top, Height = 32 };
            _btnVisionSubTraining = new Button
            {
                Text = "Training", Font = F(9f, true),
                Location = new Point(0, 4), Size = new Size(82, 24), FlatStyle = FlatStyle.Flat
            };
            _btnVisionSubTimed = new Button
            {
                Text = "Timed", Font = F(9f, true),
                Location = new Point(88, 4), Size = new Size(82, 24), FlatStyle = FlatStyle.Flat
            };
            _btnVisionSubSurvival = new Button
            {
                Text = "Survival", Font = F(9f, true),
                Location = new Point(176, 4), Size = new Size(82, 24), FlatStyle = FlatStyle.Flat
            };
            _btnVisionSubTraining.Click += (_, _) => SetVisionSubMode("training");
            _btnVisionSubTimed.Click    += (_, _) => SetVisionSubMode("timed");
            _btnVisionSubSurvival.Click += (_, _) => SetVisionSubMode("survival");
            pnlVisionSub.Controls.AddRange(new Control[] { _btnVisionSubTraining, _btnVisionSubTimed, _btnVisionSubSurvival });

            // Time picker row (Timed / Survival)
            _pnlVisionTimeRow = new Panel { Dock = DockStyle.Top, Height = 30, Visible = false };
            var lblVisionTimeLabel = new Label
            {
                Text = "Per Q:", Font = F(9f, true),
                AutoSize = true, Location = new Point(0, 7)
            };
            _visionTimeBtn3  = new Button { Text = "3s",  Font = F(9f), Location = new Point(50,  3), Size = new Size(34, 22), FlatStyle = FlatStyle.Flat };
            _visionTimeBtn5  = new Button { Text = "5s",  Font = F(9f), Location = new Point(88,  3), Size = new Size(34, 22), FlatStyle = FlatStyle.Flat };
            _visionTimeBtn10 = new Button { Text = "10s", Font = F(9f), Location = new Point(126, 3), Size = new Size(38, 22), FlatStyle = FlatStyle.Flat };
            _visionTimeBtn3.Click  += (_, _) => SelectVisionTime(3);
            _visionTimeBtn5.Click  += (_, _) => SelectVisionTime(5);
            _visionTimeBtn10.Click += (_, _) => SelectVisionTime(10);
            _pnlVisionTimeRow.Controls.AddRange(new Control[] { lblVisionTimeLabel, _visionTimeBtn3, _visionTimeBtn5, _visionTimeBtn10 });

            // Global timer row (Timed only)
            _pnlVisionGlobalTimeRow = new Panel { Dock = DockStyle.Top, Height = 30, Visible = false };
            var lblVisionGlobalLabel = new Label
            {
                Text = "Total:", Font = F(9f, true),
                AutoSize = true, Location = new Point(0, 7)
            };
            _visionGlobalBtn1 = new Button { Text = "1m", Font = F(9f), Location = new Point(50,  3), Size = new Size(34, 22), FlatStyle = FlatStyle.Flat };
            _visionGlobalBtn3 = new Button { Text = "3m", Font = F(9f), Location = new Point(88,  3), Size = new Size(34, 22), FlatStyle = FlatStyle.Flat };
            _visionGlobalBtn5 = new Button { Text = "5m", Font = F(9f), Location = new Point(126, 3), Size = new Size(34, 22), FlatStyle = FlatStyle.Flat };
            _visionGlobalBtn1.Click += (_, _) => SelectVisionGlobalTime(60);
            _visionGlobalBtn3.Click += (_, _) => SelectVisionGlobalTime(180);
            _visionGlobalBtn5.Click += (_, _) => SelectVisionGlobalTime(300);
            _pnlVisionGlobalTimeRow.Controls.AddRange(new Control[] { lblVisionGlobalLabel, _visionGlobalBtn1, _visionGlobalBtn3, _visionGlobalBtn5 });

            _lblVisionDesc = new Label
            {
                Text = "Is the square light or dark?\nAll 64 squares — no visual clues.",
                Font = F(8.5f), Dock = DockStyle.Top, Height = 38
            };
            // Auto-next row (Training sub-mode only)
            _pnlVisionAutoNextRow = new Panel { Dock = DockStyle.Top, Height = 26, Visible = false };
            _chkVisionAutoNext = new CheckBox
            {
                Text = "Auto-next", Checked = config?.VisionAutoNext != false,
                Font = F(9f), AutoSize = true, Location = new Point(4, 5)
            };
            _chkVisionAutoNext.CheckedChanged += (_, _) =>
            {
                if (config != null) { config.VisionAutoNext = _chkVisionAutoNext.Checked; config.Save(); }
            };
            _pnlVisionAutoNextRow.Controls.Add(_chkVisionAutoNext);

            _lblVisionSettingsPB = new Label
            {
                Dock = DockStyle.Top, Height = 18, Font = F(8.5f),
                TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(130, 130, 130)
            };

            var lnkResetVisionPBs = new LinkLabel
            {
                Text = "Reset personal bests", Dock = DockStyle.Top, Height = 18,
                Font = F(8f), TextAlign = ContentAlignment.MiddleLeft, LinkColor = Color.FromArgb(140, 140, 140)
            };
            lnkResetVisionPBs.LinkClicked += (_, _) =>
            {
                if (config == null) return;
                foreach (var key in new[] { "Vision-Timed-60", "Vision-Timed-180", "Vision-Timed-300", "Vision-Survival" })
                    config.TrainingPersonalBests.Remove(key);
                config.Save();
                UpdateVisionPBLabel();
            };

            var pnlVisionTopGap = new Panel { Dock = DockStyle.Top, Height = 8 };
            var pnlVisionSubGap = new Panel { Dock = DockStyle.Top, Height = 6 };
            // DockStyle.Top: last = topmost visually
            _pnlVisionSettings.Controls.AddRange(new Control[]
                { lnkResetVisionPBs, _pnlVisionAutoNextRow, _lblVisionSettingsPB, _lblVisionDesc, _pnlVisionGlobalTimeRow, _pnlVisionTimeRow, pnlVisionSubGap, pnlVisionSub, pnlVisionTopGap });

            // ── Drill settings ─────────────────────────────────────────
            _pnlDrillSettings = new Panel { Dock = DockStyle.Top, Height = 154, Visible = false };
            var lblStudy = new Label { Text = "Study", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbDrillStudy = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            var lblChapter = new Label { Text = "Position", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbDrillChapter = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            _lblDrillDesc = new Label
            {
                Dock = DockStyle.Top, Height = 32, Font = F(8f),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoEllipsis = true
            };
            _btnDrillVsBot = new Button
            {
                Text = "⚔ Practice vs Bot", Dock = DockStyle.Top, Height = 28,
                Font = F(9f), FlatStyle = FlatStyle.Flat
            };
            _btnDrillVsBot.Click += (_, _) => _ = DrillPracticeAsync();
            var pnlDrillVsBotGap = new Panel { Dock = DockStyle.Top, Height = 6 };
            _cmbDrillStudy.SelectedIndexChanged += (_, _) => PopulateDrillChapters();
            _cmbDrillChapter.SelectedIndexChanged += (_, _) =>
            {
                var ch = SelectedDrillChapter();
                if (_lblDrillDesc != null)
                    _lblDrillDesc.Text = ch?.Description ?? "";
                if (ch != null)
                    boardControl.LoadFEN(ch.Fen);
            };
            _cmbDrillChapter.DropDown += (_, _) =>
            {
                _drillHoverLastIdx = -1;
                if (_drillHoverTimer == null)
                {
                    _drillHoverTimer = new System.Windows.Forms.Timer { Interval = 50 };
                    _drillHoverTimer.Tick += DrillHoverTimer_Tick;
                }
                _drillHoverTimer.Start();
            };
            _cmbDrillChapter.DropDownClosed += (_, _) => _drillHoverTimer?.Stop();
            // DockStyle.Top: last = topmost
            _pnlDrillSettings.Controls.AddRange(new Control[]
                { _btnDrillVsBot, pnlDrillVsBotGap, _lblDrillDesc, _cmbDrillChapter, lblChapter, _cmbDrillStudy, lblStudy });

            // DockStyle.Top stacks back-to-front: last item in Controls = topmost visually
            _pnlTrainingStart.Controls.AddRange(new Control[]
                { btnStart, pnlStartGap, _pnlPuzzleSettings, _pnlVisionSettings, _pnlOpeningSettings, _pnlSquareSettings, _pnlDrillSettings, pnlModeSwitcher, lblTitle });

            // ── Game panel ────────────────────────────────────────────────────
            _pnlTrainingGame = new Panel { Dock = DockStyle.Fill, Visible = false };
            _lblTrainingRound = new Label
            {
                Font = F(10f),
                Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblTrainingTarget = new Label
            {
                Font = F(44f, true),
                Dock = DockStyle.Top, Height = 80, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblTrainingScore = new Label
            {
                Font = F(15f, true),
                Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblTrainingTimer = new Label
            {
                Font = F(10f),
                Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblOpGameStatus = new Label
            {
                Font = F(9.5f),
                Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleCenter,
                Visible = false, AutoEllipsis = true
            };
            _btnOpHint = new Button
            {
                Text = "💡 Hint", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 30, FlatStyle = FlatStyle.Flat,
                Enabled = false, Visible = true
            };
            _btnOpHint.Click += BtnOpHint_Click;
            // DockStyle.Top: last = topmost visually; _btnOpHint first = bottommost
            _pnlTrainingGame.Controls.AddRange(new Control[]
                { _btnOpHint, _lblOpGameStatus, _lblTrainingTimer, _lblTrainingScore, _lblTrainingTarget, _lblTrainingRound });

            // ── Puzzle Game panel ─────────────────────────────────────────────
            _pnlPuzzleGame = new Panel { Dock = DockStyle.Fill, Visible = false };
            _lblRushTimer = new Label
            {
                Font = F(22f, true),
                Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _lblPuzzleFeedback = new Label
            {
                Font = F(20f, true),
                Dock = DockStyle.Top, Height = 46, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblPuzzleRating = new Label
            {
                Font = F(10f, true),
                Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblPuzzleThemes = new Label
            {
                Font = F(8.5f),
                Dock = DockStyle.Top, Height = 34,
                TextAlign = ContentAlignment.TopCenter,
                AutoEllipsis = true
            };
            _lblPuzzleStats = new Label
            {
                Font = F(9.5f),
                Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleCenter
            };
            _btnPuzzleHint = new Button
            {
                Text = "💡 Hint", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnPuzzleHint.Click += BtnPuzzleHint_Click;
            _btnPuzzleSkip = new Button
            {
                Text = "Skip →", Font = F(9f),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSkip.Click += (_, _) => PuzzleSkip();
            _btnPuzzleNext = new Button
            {
                Text = "Next  →", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnPuzzleNext.Click += (_, _) => { if (_btnPuzzleNext != null) _btnPuzzleNext.Visible = false; PuzzleLoadNext(); };
            _btnPuzzleAnalyze = new Button
            {
                Text = "Analyze", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnPuzzleAnalyze.Click += (_, _) =>
            {
                var puzzle = _currentPuzzle;
                if (puzzle == null) return;

                // FEN side-to-move is the opponent (makes the first move) — player is the other side
                string[] fenParts = puzzle.Fen.Split(' ');
                bool playerIsBlack = fenParts.Length > 1 && fenParts[1] == "w";

                StopTraining();

                // Set perspective to player's side before loading
                boardControl.IsFlipped = playerIsBlack;

                // Reset to puzzle starting position
                boardControl.LoadFEN(puzzle.Fen);
                moveTree = new MoveTree(puzzle.Fen);
                _classificationLookup = null;
                _analysisCache.Clear();

                // Replay full solution into the move tree
                foreach (string uci in puzzle.Moves)
                {
                    string san = ConvertUciToSan(uci, moveTree.CurrentNode.FEN);
                    isNavigating = true;
                    boardControl.MakeMove(uci);
                    isNavigating = false;
                    moveTree.AddMove(uci, san, boardControl.GetFEN());
                }

                // Position at the player's first move (skip the opponent's opening move)
                moveTree.GoToStart();
                if (puzzle.Moves.Length > 0 && moveTree.GoForward())
                {
                    isNavigating = true;
                    boardControl.LoadFEN(moveTree.CurrentNode.FEN);
                    isNavigating = false;
                }

                isNavigating = true;
                try { UpdateMoveList(); UpdateMoveListSelection(); }
                finally { isNavigating = false; }
                UpdateFenDisplay();
                UpdateTurnLabel();
                UpdateMaterialStrips();

                _ = TriggerAutoAnalysis();
            };
            // DockStyle.Top: last = topmost visually
            _pnlPuzzleGame.Controls.AddRange(new Control[]
                { _btnPuzzleAnalyze, _btnPuzzleNext, _btnPuzzleSkip, _btnPuzzleHint, _lblPuzzleStats, _lblPuzzleFeedback, _lblPuzzleThemes, _lblPuzzleRating, _lblRushTimer });

            // ── Vision Game panel ─────────────────────────────────────────────
            _pnlVisionGame = new Panel { Dock = DockStyle.Fill, Visible = false };
            _lblVisionQuestion = new Label
            {
                Font = F(18f, true),
                Dock = DockStyle.Top, Height = 52, TextAlign = ContentAlignment.MiddleCenter
            };
            var pnlVisionButtons = new Panel { Dock = DockStyle.Top, Height = 44 };
            _btnVisionLight = new Button
            {
                Text = "Light", Font = F(13f, true),
                Location = new Point(0, 2), Size = new Size(120, 38), FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            _btnVisionDark = new Button
            {
                Text = "Dark", Font = F(13f, true),
                Location = new Point(130, 2), Size = new Size(120, 38), FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            _btnVisionLight.Click += (_, _) => VisionAnswer(true);
            _btnVisionDark.Click  += (_, _) => VisionAnswer(false);
            _btnVisionNext = new Button
            {
                Text = "Next  →", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnVisionNext.Click += (_, _) => { if (_btnVisionNext != null) _btnVisionNext.Visible = false; VisionLoadNext(); };
            pnlVisionButtons.Controls.AddRange(new Control[] { _btnVisionLight, _btnVisionDark });
            pnlVisionButtons.Resize += (_, _) =>
            {
                const int btnW = 120, gap = 10;
                int total = btnW * 2 + gap;
                int startX = (pnlVisionButtons.Width - total) / 2;
                _btnVisionLight!.Location = new Point(startX, 2);
                _btnVisionDark!.Location  = new Point(startX + btnW + gap, 2);
            };
            _lblVisionTimer = new Label
            {
                Font = F(26f, true),
                Dock = DockStyle.Top, Height = 44, TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _lblVisionLives = new Label
            {
                Font = F(16f),
                Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _lblVisionScore = new Label
            {
                Font = F(10f),
                Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblVisionGlobalTimer = new Label
            {
                Font = F(14f, true),
                Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            // DockStyle.Top: last = topmost visually — global timer at top, score at bottom
            _pnlVisionGame.Controls.AddRange(new Control[]
                { _lblVisionScore, _lblVisionLives, _btnVisionNext, pnlVisionButtons, _lblVisionTimer, _lblVisionQuestion, _lblVisionGlobalTimer });

            // ── Result panel ──────────────────────────────────────────────────
            _pnlTrainingResult = new Panel { Dock = DockStyle.Fill, Visible = false };
            var lblComplete = new Label
            {
                Text = "Round Complete!",
                Font = F(16f, true),
                Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblTrainingFinalScore = new Label
            {
                Font = F(12f),
                Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter
            };
            _lblTrainingPB = new Label
            {
                Font = F(10f),
                Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleCenter
            };
            var btnTryAgain = new Button
            {
                Text = "Try Again", Font = F(11f, true),
                Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 0)
            };
            btnTryAgain.Click += (_, _) => TrainingShowStartPanel();
            var btnDone = new Button
            {
                Text = "Done", Font = F(10f),
                Dock = DockStyle.Top, Height = 34, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 6, 0, 0)
            };
            btnDone.Click += (_, _) => StopTraining();
            _lblOpMissedMoves = new Label
            {
                Font = F(9f),
                Dock = DockStyle.Top, Height = 48,
                TextAlign = ContentAlignment.TopCenter,
                Visible = false
            };
            _pnlTrainingResult.Controls.AddRange(new Control[]
                { btnDone, btnTryAgain, _lblOpMissedMoves, _lblTrainingPB, _lblTrainingFinalScore, lblComplete });

            _pnlTraining.Controls.AddRange(new Control[]
                { _pnlTrainingStart, _pnlTrainingGame, _pnlPuzzleGame, _pnlVisionGame, _pnlTrainingResult });
            rightPanel.Controls.Add(_pnlTraining);

            _visionQuestionTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _visionQuestionTimer.Tick += VisionQuestionTimer_Tick;
            _visionGlobalTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _visionGlobalTimer.Tick += VisionGlobalTimer_Tick;

            SetTrainingMode("square"); // initialize Square mode as default
            SetPuzzleSubMode("training"); // initialize puzzle sub-mode
            SelectRushTime(3);            // default 3 min
            SelectPuzzleRating(0, int.MaxValue); // default Any
            SetVisionSubMode("training");
            SelectVisionTime(5);
            SelectVisionGlobalTime(180);
            ApplyTrainingTheme();
        }

        // ── BtnTraining_Click (replaces the old ShowDialog version) ──────────

        private void BtnTraining_Click(object? sender, EventArgs e)
        {
            if (_trainingUiVisible)
            {
                bool puzzleTrainingActive = _pnlPuzzleGame?.Visible == true
                    && _puzzleSubMode == "training"
                    && (_puzzlesClean + _puzzlesStruggled) > 0
                    && _puzzleSubMode != "daily";
                if (puzzleTrainingActive)
                    PuzzleTrainingShowResults();
                else
                    StopTraining();
            }
            else
            {
                // Mutually exclusive with Match panel
                if (_matchPanelActive)
                {
                    _matchPanelActive = false;
                    grpEngineMatch.Visible = false;
                    HighlightButton(btnMatch, false);
                }
                StartTrainingUI();
            }
        }

        private void BtnMatch_Click(object? sender, EventArgs e)
        {
            if (matchRunning) { lblStatus.Text = "Stop the match first"; return; }

            _matchPanelActive = !_matchPanelActive;
            grpEngineMatch.Visible = _matchPanelActive;
            HighlightButton(btnMatch, _matchPanelActive);

            if (_matchPanelActive && _trainingUiVisible)
                StopTraining();
        }

        private void PuzzleTrainingShowResults()
        {
            _puzzleActive = false;
            _puzzleLocked = false;
            boardControl.ClearTrainingHighlight();
            if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = false;
            if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = false;

            int total = _puzzlesClean + _puzzlesStruggled;
            int acc   = total > 0 ? _puzzlesClean * 100 / total : 0;

            if (_lblTrainingFinalScore != null)
                _lblTrainingFinalScore.Text = $"Solved: {total}   ·   {acc}% clean   ·   {_puzzlesClean} perfect";
            if (_lblTrainingPB != null)
                _lblTrainingPB.Text = _puzzleStreakBest > 0 ? $"Best streak: {_puzzleStreakBest}" : "";
            if (_lblOpMissedMoves != null) _lblOpMissedMoves.Visible = false;

            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen     = null;
            _trainingGameActive = false;

            _pnlPuzzleGame!.Visible     = false;
            _pnlTrainingResult!.Visible = true;
        }

        private void StartTrainingUI()
        {
            _puzzlesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Puzzles");
            if (_btnPuzzleMode != null)
                _btnPuzzleMode.Visible = LichessPuzzleService.HasPuzzles(_puzzlesFolder);

            _drillsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Endgames");
            if (EndgameDrillService.HasDrills(_drillsFolder))
            {
                _drillChapters = EndgameDrillService.LoadFromFolder(_drillsFolder);
                if (_btnDrillMode != null) _btnDrillMode.Visible = true;
                PopulateDrillStudies();
            }

            autoAnalysisCts?.Cancel();
            lblStatus.Text = "";

            _trainingUiVisible = true;
            analysisOutput.Visible = false;
            grpEngineMatch.Visible = false;
            lblAnalysis.Visible = false;
            evalBar.Visible = false;
            if (_evalGraph != null) _evalGraph.Visible = false;
            _pnlTraining!.Visible = true;
            _pnlTraining.BringToFront();
            boardControl.InteractionEnabled = false;
            TrainingShowStartPanel();

            btnTraining.Text = "⏹";
            toolTip.SetToolTip(btnTraining, "Stop Training");
            SetTrainingButtonsEnabled(false);
        }

        private void StopTraining()
        {
            _trainingClockTimer?.Stop();
            _trainingFlashTimer?.Stop();
            _openingAutoplayTimer?.Stop();
            _puzzleRushTimer?.Stop();
            _visionQuestionTimer?.Stop();
            _visionGlobalTimer?.Stop();
            _openingRecreatePhase = false;
            _puzzleActive = false;
            _puzzleLocked = false;
            if (_btnOpHint     != null) _btnOpHint.Enabled     = false;
            if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;
            if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = false;
            if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = false;
            if (_btnVisionNext    != null) _btnVisionNext.Visible    = false;
            if (_pnlPuzzleGame != null) _pnlPuzzleGame.Visible = false;
            if (_pnlVisionGame != null) _pnlVisionGame.Visible = false;
            boardControl.MonochromeMode  = config?.MonochromeBoard == true;
            boardControl.HideCoordinates = false;
            bool showStrips = config?.ShowMaterialStrips != false;
            _materialTop.Visible    = showStrips;
            _materialBottom.Visible = showStrips;

            if (_trainingGameActive)
            {
                boardControl.ClearTrainingHighlight();
                boardControl.TrainingMode = false;
                boardControl.IsFlipped = _trainingPreFlipped;
                if (_trainingPreFen != null)
                    boardControl.LoadFEN(_trainingPreFen);
                _trainingPreFen = null;
                _trainingGameActive = false;
            }

            _trainingUiVisible = false;
            _pnlTraining!.Visible = false;
            _pnlTraining.SendToBack();
            analysisOutput.Visible = true;
            grpEngineMatch.Visible = _matchPanelActive;
            lblAnalysis.Visible = true;
            evalBar.Visible = config?.ShowEvalBar != false;
            if (_evalGraph != null) _evalGraph.Visible = config?.ShowEvalGraph ?? true;

            boardControl.InteractionEnabled = true;
            btnTraining.Text = "♟";
            toolTip.SetToolTip(btnTraining, "Training");
            SetTrainingButtonsEnabled(true);

            _ = TriggerAutoAnalysis();
        }

        private void SetTrainingButtonsEnabled(bool enabled)
        {
            foreach (var btn in new[] { btnNewGame, btnFlipBoard, btnTakeBack, btnPrevMove,
                                        btnNextMove, btnAutoPlay, btnPlayBot, btnEditPosition })
                btn.Enabled = enabled;
        }

        private void TrainingShowStartPanel()
        {
            _trainingClockTimer?.Stop();
            _trainingFlashTimer?.Stop();
            _openingAutoplayTimer?.Stop();
            _puzzleRushTimer?.Stop();
            _openingRecreatePhase = false;
            _puzzleActive = false;
            _puzzleLocked = false;
            if (_btnOpHint     != null) _btnOpHint.Enabled     = false;
            if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;
            if (_pnlPuzzleGame != null) _pnlPuzzleGame.Visible = false;
            if (_pnlVisionGame != null) _pnlVisionGame.Visible = false;

            if (_trainingGameActive)
            {
                boardControl.ClearTrainingHighlight();
                boardControl.TrainingMode = false;
                boardControl.MonochromeMode = false;
                boardControl.IsFlipped = _trainingPreFlipped;
                if (_trainingPreFen != null)
                    boardControl.LoadFEN(_trainingPreFen);
                _trainingPreFen = null;
                _trainingGameActive = false;
            }

            if (_lblOpGameStatus != null) _lblOpGameStatus.Visible = false;
            if (_lblTrainingTimer != null) _lblTrainingTimer.Visible = true;

            _pnlTrainingStart!.Visible = true;
            _pnlTrainingGame!.Visible = false;
            _pnlPuzzleGame!.Visible = false;
            _pnlVisionGame!.Visible = false;
            _pnlTrainingResult!.Visible = false;
            boardControl.InteractionEnabled = false;
        }

        private string TrainingModeKey()
        {
            string diff = _rbTrainingEasy?.Checked == true ? "Easy" : "Challenge";
            string side = _rbTrainingRandom?.Checked == true ? "Random"
                        : _rbTrainingBlack?.Checked == true  ? "Black" : "White";
            return $"{diff}-{side}";
        }

        private void TrainingStartRound()
        {
            boardControl.InteractionEnabled = true;
            if (_openingModeSelected) { OpeningTrainingStart();  return; }
            if (_visionModeSelected)  { VisionTrainingStart();   return; }
            if (_puzzleModeSelected)
            {
                if (_puzzleSubMode == "rush")     { PuzzleRushStart();     return; }
                if (_puzzleSubMode == "gauntlet") { PuzzleGauntletStart(); return; }
                if (_puzzleSubMode == "daily")    { PuzzleDailyStart();    return; }
                PuzzleTrainingStart(); return;
            }
            if (_drillModeSelected) { DrillStart(); return; }

            _trainingPreFen = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingQuestions   = (int)(_numQuestions?.Value ?? 10);
            _trainingTimeLimitSec = (int)(_numTimeLimit?.Value ?? 0);
            autoAnalysisCts?.Cancel();

            bool playAsBlack = _rbTrainingBlack?.Checked == true ||
                               (_rbTrainingRandom?.Checked == true && _trainingRng.Next(2) == 1);
            boardControl.IsFlipped = playAsBlack;
            boardControl.TrainingMode = true;
            boardControl.HoverSquareLabelEnabled = _rbTrainingEasy?.Checked == true;
            boardControl.LoadFEN(TRAINING_EMPTY_FEN);

            _trainingCorrect = 0;
            _trainingWrong = 0;
            _trainingAwaitingNext = false;
            _trainingInWrongFlash = false;
            _trainingInCorrectFlash = false;
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible = false;
            _pnlTrainingResult!.Visible = false;
            _pnlTrainingGame!.Visible = true;

            _trainingRoundStart = DateTime.Now;
            _trainingClockTimer?.Start();
            TrainingNextQuestion();
        }

        private void TrainingNextQuestion()
        {
            _trainingAwaitingNext = false;
            boardControl.ClearTrainingHighlight();

            if (_rbTrainingRandom?.Checked == true)
                boardControl.IsFlipped = _trainingRng.Next(2) == 1;

            _trainingTargetRow = _trainingRng.Next(0, 8);
            _trainingTargetCol = _trainingRng.Next(0, 8);
            string name = $"{(char)('a' + _trainingTargetCol)}{8 - _trainingTargetRow}";

            if (_lblTrainingRound != null)
                _lblTrainingRound.Text = $"Question  {_trainingCorrect + _trainingWrong + 1}  /  {_trainingQuestions}";
            if (_lblTrainingTarget != null)
                _lblTrainingTarget.Text = name;

            TrainingUpdateStats();
        }

        private void TrainingSquareClicked(int row, int col)
        {
            if (!_trainingGameActive || _trainingAwaitingNext) return;
            _trainingAwaitingNext = true;
            _trainingFlashMs = 0;

            if (row == _trainingTargetRow && col == _trainingTargetCol)
            {
                _trainingCorrect++;
                _trainingInWrongFlash = false;
                _trainingInCorrectFlash = true;
                boardControl.SetTrainingHighlight(row, col, TrainingOkColor);
            }
            else
            {
                _trainingWrong++;
                _trainingInWrongFlash = true;
                _trainingInCorrectFlash = false;
                _trainingCorrectRow = _trainingTargetRow;
                _trainingCorrectCol = _trainingTargetCol;
                boardControl.SetTrainingHighlight(row, col, TrainingErrorColor);
            }

            TrainingUpdateStats();
            _trainingFlashTimer?.Start();
        }

        private void TrainingFlashTimer_Tick(object? sender, EventArgs e)
        {
            _trainingFlashMs += 16;

            if (_trainingInWrongFlash && _trainingFlashMs >= TRAINING_WRONG_MS)
            {
                _trainingInWrongFlash = false;
                _trainingInCorrectFlash = true;
                _trainingFlashMs = 0;
                boardControl.SetTrainingHighlight(_trainingCorrectRow, _trainingCorrectCol,
                    TrainingOkColor);
                return;
            }

            if (_trainingInCorrectFlash && _trainingFlashMs >= TRAINING_CORRECT_MS)
            {
                _trainingInCorrectFlash = false;
                _trainingFlashTimer?.Stop();
                boardControl.ClearTrainingHighlight();

                if (_trainingCorrect + _trainingWrong >= _trainingQuestions || TrainingCheckTimeExpired())
                    TrainingShowResult();
                else
                    TrainingNextQuestion();
            }
        }

        private void TrainingUpdateStats()
        {
            if (_lblTrainingScore != null)
                _lblTrainingScore.Text = $"{_trainingCorrect} ✓     {_trainingWrong} ✗";

            if (_lblTrainingTimer != null)
            {
                double elapsed = (DateTime.Now - _trainingRoundStart).TotalSeconds;
                if (_trainingTimeLimitSec > 0)
                {
                    double remaining = _trainingTimeLimitSec - elapsed;
                    if (remaining < 0) remaining = 0;
                    _lblTrainingTimer.Text = $"{remaining:F1}s remaining";
                    _lblTrainingTimer.ForeColor = remaining <= 10 ? Color.OrangeRed : SystemColors.ControlText;
                }
                else
                {
                    _lblTrainingTimer.Text = $"{elapsed:F1}s";
                    _lblTrainingTimer.ForeColor = SystemColors.ControlText;
                }
            }
        }

        private bool TrainingCheckTimeExpired()
        {
            if (_trainingTimeLimitSec <= 0) return false;
            return (DateTime.Now - _trainingRoundStart).TotalSeconds >= _trainingTimeLimitSec;
        }

        private void TrainingShowResult()
        {
            _trainingClockTimer?.Stop();
            double elapsed = (DateTime.Now - _trainingRoundStart).TotalSeconds;

            boardControl.ClearTrainingHighlight();
            boardControl.TrainingMode = false;
            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null)
                boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen = null;
            _trainingGameActive = false;

            if (_lblTrainingFinalScore != null)
                _lblTrainingFinalScore.Text = $"{_trainingCorrect} / {_trainingQuestions} correct  ·  {elapsed:F1}s";

            // Personal best
            string key = TrainingModeKey();
            bool newPB = false;
            if (config != null)
            {
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();

                if (_trainingCorrect > pb.BestCorrect ||
                    (_trainingCorrect == pb.BestCorrect && elapsed < pb.BestTime))
                {
                    pb.BestCorrect = _trainingCorrect;
                    pb.BestQuestions = _trainingQuestions;
                    pb.BestTime = elapsed;
                    config.TrainingPersonalBests[key] = pb;
                    config.Save();
                    newPB = true;
                }

                if (_lblTrainingPB != null)
                {
                    string pbText = pb.BestTime == double.MaxValue
                        ? ""
                        : $"PB ({key}): {pb.BestCorrect}/{pb.BestQuestions} in {pb.BestTime:F1}s";
                    _lblTrainingPB.Text = newPB ? $"New personal best!  {pbText}" : pbText;
                }
            }

            if (_lblOpMissedMoves != null) _lblOpMissedMoves.Visible = false;
            _pnlTrainingGame!.Visible = false;
            _pnlTrainingResult!.Visible = true;

            _ = TriggerAutoAnalysis();
        }

        // ── Opening Training ───────────────────────────────────────────────

        private void SetTrainingMode(string mode) // "square" | "opening" | "puzzle" | "vision" | "drill"
        {
            _openingModeSelected = mode == "opening";
            _puzzleModeSelected  = mode == "puzzle";
            _visionModeSelected  = mode == "vision";
            _drillModeSelected   = mode == "drill";
            if (_lblTrainingTitle != null)
                _lblTrainingTitle.Text = mode == "opening" ? "Opening Training"
                                       : mode == "puzzle"  ? "Puzzle Training"
                                       : mode == "vision"  ? "Board Vision"
                                       : mode == "drill"   ? "Endgame Drills"
                                       : "Square Training";
            if (_pnlSquareSettings  != null) _pnlSquareSettings.Visible  = mode == "square";
            if (_pnlOpeningSettings != null) _pnlOpeningSettings.Visible = mode == "opening";
            if (_pnlPuzzleSettings  != null) _pnlPuzzleSettings.Visible  = mode == "puzzle";
            if (_pnlVisionSettings  != null) _pnlVisionSettings.Visible  = mode == "vision";
            if (_pnlDrillSettings   != null) _pnlDrillSettings.Visible   = mode == "drill";
            HighlightButton(_btnSqMode,     mode == "square");
            HighlightButton(_btnOpMode,     mode == "opening");
            HighlightButton(_btnPuzzleMode, mode == "puzzle");
            HighlightButton(_btnVisionMode, mode == "vision");
            HighlightButton(_btnDrillMode,  mode == "drill");
            if (mode == "square")  UpdateSquarePBLabel();
            if (mode == "puzzle")  SetPuzzleSubMode(_puzzleSubMode);
            if (mode == "vision")  UpdateVisionPBLabel();
        }

        private void SetPuzzleSubMode(string subMode) // "training" | "rush" | "gauntlet"
        {
            _puzzleSubMode = subMode;
            bool isTraining = subMode == "training";
            bool isDaily    = subMode == "daily";
            if (_pnlThemeFilterRow    != null) _pnlThemeFilterRow.Visible    = isTraining;
            if (_pnlOpeningFilterRow  != null) _pnlOpeningFilterRow.Visible  = isTraining;
            if (_pnlPuzzleAutoNextRow != null) _pnlPuzzleAutoNextRow.Visible = isTraining;
            if (_pnlRushTimeRow       != null) _pnlRushTimeRow.Visible       = subMode == "rush";
            if (_lblGauntletDesc      != null) _lblGauntletDesc.Visible      = subMode == "gauntlet";
            if (_pnlRatingRow         != null) _pnlRatingRow.Visible         = !isDaily;
            HighlightButton(_btnPuzzleSubTraining, isTraining);
            HighlightButton(_btnPuzzleSubRush,     subMode == "rush");
            HighlightButton(_btnPuzzleSubGauntlet, subMode == "gauntlet");
            HighlightButton(_btnPuzzleSubDaily,    isDaily);
            if (_pnlPuzzleSettings != null)
                _pnlPuzzleSettings.Height = subMode == "rush" ? 142 : subMode == "gauntlet" ? 132 : isDaily ? 110 : 242;
            if (_lblPuzzleSettingsPB != null)
            {
                if (isDaily)
                {
                    string today  = DateTime.Today.ToString("MMM d");
                    int streak    = config?.DailyPuzzleStreak ?? 0;
                    string pb     = config?.DailyPuzzleBestStreak > 1 ? $"  ·  Best: {config.DailyPuzzleBestStreak}" : "";
                    string solved = config?.DailyPuzzleLastSolvedDate == DateTime.Today.ToString("yyyy-MM-dd") ? "  ·  Solved ✓" : "";
                    _lblPuzzleSettingsPB.Text = $"{today}  ·  Streak: {streak}{pb}{solved}";
                }
                else
                {
                    _lblPuzzleSettingsPB.Text = subMode switch
                    {
                        "rush"     => config?.PuzzleRushBest > 0           ? $"Best: {config.PuzzleRushBest} puzzles" : "",
                        "gauntlet" => config?.GauntletBestStreak > 0       ? $"Best streak: {config.GauntletBestStreak}" : "",
                        _          => config?.PuzzleTrainingBestStreak > 0 ? $"Best streak: {config.PuzzleTrainingBestStreak}" : "",
                    };
                }
            }
        }

        private void SelectRushTime(int minutes)
        {
            _rushDurationSeconds = minutes * 60;
            HighlightButton(_rushTimeBtn1, minutes == 1);
            HighlightButton(_rushTimeBtn2, minutes == 2);
            HighlightButton(_rushTimeBtn3, minutes == 3);
            HighlightButton(_rushTimeBtn4, minutes == 4);
            HighlightButton(_rushTimeBtn5, minutes == 5);
        }

        private void SelectPuzzleRating(int min, int max)
        {
            _puzzleRatingMin = min;
            _puzzleRatingMax = max;
            HighlightButton(_ratingBtnAny,    min == 0 && max == int.MaxValue);
            HighlightButton(_ratingBtnBeg,    min == 0 && max == 1199);
            HighlightButton(_ratingBtnInt,    min == 1400 && max == 1799);
            HighlightButton(_ratingBtnAdv,    min == 1800 && max == 2199);
            HighlightButton(_ratingBtnMaster, min == 2200 && max == int.MaxValue);
            if (_lblRatingRange != null)
                _lblRatingRange.Text = (min == 0 && max == int.MaxValue) ? "" :
                                       max == int.MaxValue                ? $"≥ {min}" :
                                       min == 0                           ? $"< {max + 1}" :
                                                                            $"{min} – {max + 1}";
        }

        private void SelectOpeningForTraining()
        {
            string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
            var entries = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
            using var selDialog = new OpeningExplorerDialog(entries, pgn =>
            {
                // Parse ECO+name from PGN [Opening] tag: "[Opening "ECO — Name"]"
                var match = System.Text.RegularExpressions.Regex.Match(pgn, @"\[Opening ""([^""]+)""\]");
                if (match.Success)
                {
                    string openingTag = match.Groups[1].Value;
                    int dash = openingTag.IndexOf(" — ", StringComparison.Ordinal); // em-dash
                    if (dash >= 0)
                    {
                        string eco = openingTag.Substring(0, dash);
                        string name = openingTag.Substring(dash + 3);
                        _selectedTrainingOpening = entries.FirstOrDefault(e =>
                            e.Eco == eco && e.Name == name) ?? entries.FirstOrDefault(e => e.Eco == eco);
                    }
                }
                if (_selectedTrainingOpening != null && _lblSelectedOpening != null)
                    _lblSelectedOpening.Text = $"{_selectedTrainingOpening.Eco}  {_selectedTrainingOpening.Name}";
                if (_rbOpSelected != null) _rbOpSelected.Checked = true;
            }, ThemeService.IsDarkTheme(config?.Theme));
            selDialog.ShowDialog(this);
        }

        private void PrepareOpeningLine(string sanMoves)
        {
            _openingUciMoves.Clear();
            _openingFens.Clear();

            const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            _openingFens.Add(startFen);

            string savedFen = boardControl.GetFEN();
            bool savedNav = isNavigating;
            isNavigating = true;

            string currentFen = startFen;
            var tokens = sanMoves.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d+\.") &&
                             t != "1-0" && t != "0-1" && t != "1/2-1/2" && t != "*")
                .ToList();

            foreach (var san in tokens)
            {
                string? uci = ConvertSanToUci(san, currentFen);
                if (uci == null) break;

                boardControl.LoadFEN(currentFen);
                if (!boardControl.MakeMove(uci)) break;

                currentFen = boardControl.GetFEN();
                _openingUciMoves.Add(uci);
                _openingFens.Add(currentFen);
            }

            boardControl.LoadFEN(savedFen);
            isNavigating = savedNav;
        }

        private void OpeningTrainingStart()
        {
            OpeningEntry? entry;
            if (_rbOpRandom?.Checked == true)
            {
                string booksFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books");
                var all = ChessDroid.Services.EcoBookService.LoadAll(booksFolder);
                if (all.Count == 0) return;
                entry = all[_trainingRng.Next(all.Count)];
            }
            else
            {
                if (_selectedTrainingOpening == null) { SelectOpeningForTraining(); if (_selectedTrainingOpening == null) return; }
                entry = _selectedTrainingOpening;
            }

            PrepareOpeningLine(entry.Moves);
            if (_openingUciMoves.Count == 0)
            {
                MessageBox.Show("Could not parse the opening line.", "Opening Training", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _trainingPreFen = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _selectedTrainingOpening = entry;
            _selectedWatches = _rbWatches3?.Checked == true ? 3
                             : _rbWatches2?.Checked == true ? 2
                             : _rbWatches1?.Checked == true ? 1 : 0;
            _openingWatchesLeft = _selectedWatches;

            autoAnalysisCts?.Cancel();
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible = false;
            _pnlTrainingResult!.Visible = false;
            _pnlTrainingGame!.Visible = true;

            if (_selectedWatches == 0)
                OpeningTrainingStartRecreate();
            else
                OpeningTrainingStartWatch();
        }

        private void OpeningTrainingStartWatch()
        {
            _openingPlayIndex = 0;
            _openingRecreatePhase = false;
            _openingAutoplayTimer?.Stop();

            isNavigating = true;
            boardControl.IsFlipped = false;
            boardControl.LoadFEN(_openingFens[0]);
            isNavigating = false;
            boardControl.ClearTrainingHighlight();

            if (_lblOpGameStatus != null) _lblOpGameStatus.Visible = true;
            if (_lblTrainingTimer != null) _lblTrainingTimer.Visible = false;

            UpdateOpeningWatchDisplay();
            _openingAutoplayTimer?.Start();
        }

        private void OpeningAutoplayTimer_Tick(object? sender, EventArgs e)
        {
            if (_openingPlayIndex >= _openingUciMoves.Count)
            {
                _openingAutoplayTimer?.Stop();
                _openingWatchesLeft--;

                if (_openingWatchesLeft > 0)
                    ScheduleInvoke(800, OpeningTrainingStartWatch);
                else
                    ScheduleInvoke(800, OpeningTrainingStartRecreate);
                return;
            }

            string uci = _openingUciMoves[_openingPlayIndex];
            string fen = _openingFens[_openingPlayIndex + 1];
            string preFen = _openingFens[_openingPlayIndex];

            string san = ConvertUciToSan(uci, preFen);

            isNavigating = true;
            boardControl.LoadFEN(fen);
            isNavigating = false;

            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(uci);
            PlayMoveSound(san.Contains('x'), san);

            SetText(_lblTrainingTarget, san);

            _openingPlayIndex++;
            UpdateOpeningWatchDisplay();
        }

        private void UpdateOpeningWatchDisplay()
        {
            int watchNum = _selectedWatches - _openingWatchesLeft + 1;
            SetText(_lblTrainingRound, $"Watch  {watchNum} / {_selectedWatches}");
            SetText(_lblTrainingScore, $"{_selectedTrainingOpening?.Eco}  {StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
            SetText(_lblOpGameStatus,  $"Move  {_openingPlayIndex} / {_openingUciMoves.Count}");
            if (_openingPlayIndex == 0) SetText(_lblTrainingTarget, "…");
        }

        private void OpeningTrainingStartRecreate()
        {
            _openingRecreateIndex = 0;
            _openingHintsUsed     = 0;
            _openingPosMistakes.Clear();
            _openingRecreateLocked = false;
            _openingRecreatePhase  = true;
            if (_btnOpHint != null) _btnOpHint.Enabled = true;
            boardControl.ClearTrainingHighlight();

            isNavigating = true;
            boardControl.LoadFEN(_openingFens[0]);
            isNavigating = false;

            SetText(_lblTrainingRound,  "Recreate");
            SetText(_lblTrainingTarget, "?");
            SetText(_lblTrainingScore,  $"{_selectedTrainingOpening?.Eco}  {StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
            SetText(_lblOpGameStatus,   $"Move  1 / {_openingUciMoves.Count}");
        }

        private void BtnOpHint_Click(object? sender, EventArgs e)
        {
            if (!_openingRecreatePhase || _openingRecreateLocked) return;
            if (_openingRecreateIndex >= _openingUciMoves.Count) return;
            _openingHintsUsed++;
            string uci = _openingUciMoves[_openingRecreateIndex];
            ShowTrainingHint(uci, _btnOpHint, () =>
            {
                if (_openingRecreatePhase && !_openingRecreateLocked && _btnOpHint != null)
                    _btnOpHint.Enabled = true;
            });
        }

        private void OpeningTrainingValidateMove(string uciMove)
        {
            if (_openingRecreateIndex >= _openingUciMoves.Count || _openingRecreateLocked) return;

            string expected = _openingUciMoves[_openingRecreateIndex];
            int toCol = uciMove[2] - 'a';
            int toRow = 7 - (uciMove[3] - '1');

            if (uciMove == expected)
            {
                _openingRecreateIndex++;
                if (_btnOpHint != null) _btnOpHint.Enabled = true;

                if (_openingRecreateIndex >= _openingUciMoves.Count)
                {
                    // Last move — lock briefly then show result
                    _openingRecreateLocked = true;
                    ScheduleInvoke(700, OpeningTrainingEnd);
                }
                else
                {
                    // Correct but not last
                    UpdateOpeningRecreateDisplay();
                }
            }
            else
            {
                _openingPosMistakes[_openingRecreateIndex] =
                    _openingPosMistakes.GetValueOrDefault(_openingRecreateIndex) + 1;
                _openingRecreateLocked = true;
                boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);

                string correctFen = _openingFens[_openingRecreateIndex];
                Task.Delay(600).ContinueWith(_ =>
                {
                    if (IsDisposed) return;
                    Invoke(() =>
                    {
                        boardControl.ClearTrainingHighlight();
                        isNavigating = true;
                        boardControl.LoadFEN(correctFen);
                        isNavigating = false;
                        _openingRecreateLocked = false;
                        if (_btnOpHint != null) _btnOpHint.Enabled = true;
                    });
                });
                UpdateOpeningRecreateDisplay();
            }
        }

        private void UpdateOpeningRecreateDisplay()
        {
            int total = _openingUciMoves.Count;
            int done = _openingRecreateIndex;
            SetText(_lblOpGameStatus,   $"Move  {Math.Min(done + 1, total)} / {total}    ✗ {_openingPosMistakes.Count}");
            SetText(_lblTrainingTarget, "?");
        }

        private void OpeningTrainingEnd()
        {
            _openingAutoplayTimer?.Stop();
            _openingRecreatePhase = false;
            _openingRecreateLocked = false;
            _trainingGameActive = false;
            if (_btnOpHint != null) _btnOpHint.Enabled = false;

            boardControl.ClearTrainingHighlight();
            isNavigating = true;
            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen = null;
            isNavigating = false;

            int total = _openingUciMoves.Count;
            int wrongPositions = _openingPosMistakes.Count;
            int correct = total - wrongPositions;

            _openingSessionRuns++;
            if (wrongPositions == 0 && _openingHintsUsed == 0) _openingSessionPerfect++;

            if (_lblTrainingFinalScore != null)
                _lblTrainingFinalScore.Text = $"{correct} / {total} correct"
                    + (wrongPositions == 0 ? "  —  Perfect!" : "")
                    + (_openingHintsUsed > 0 ? $"  ({_openingHintsUsed} hint{(_openingHintsUsed == 1 ? "" : "s")})" : "");
            if (_lblTrainingPB != null)
            {
                string openingName = $"{_selectedTrainingOpening?.Eco}  {_selectedTrainingOpening?.Name}";
                string sessionStr  = $"  ·  {_openingSessionRuns} run{(_openingSessionRuns == 1 ? "" : "s")}  ·  {_openingSessionPerfect} perfect";
                _lblTrainingPB.Text = openingName + sessionStr;
            }

            if (_lblOpMissedMoves != null)
            {
                if (wrongPositions == 0)
                {
                    _lblOpMissedMoves.Visible = false;
                }
                else
                {
                    var parts = _openingPosMistakes.OrderBy(kv => kv.Key).Select(kv =>
                    {
                        string san = ConvertUciToSan(_openingUciMoves[kv.Key], _openingFens[kv.Key]);
                        int moveNum = kv.Key / 2 + 1;
                        string prefix = kv.Key % 2 == 0 ? $"{moveNum}." : $"{moveNum}…";
                        return $"{prefix}{san} ×{kv.Value}";
                    });
                    _lblOpMissedMoves.Text = "Missed:  " + string.Join("   ", parts);
                    _lblOpMissedMoves.Visible = true;
                }
            }

            _pnlTrainingGame!.Visible = false;
            _pnlTrainingResult!.Visible = true;

            if (_lblOpGameStatus != null) _lblOpGameStatus.Visible = false;
            if (_lblTrainingTimer != null) _lblTrainingTimer.Visible = true;

            _ = TriggerAutoAnalysis();
        }

        // ── Puzzle Training ────────────────────────────────────────────────────

        // ── Endgame Drills ─────────────────────────────────────────────────────

        private void PopulateDrillStudies()
        {
            if (_cmbDrillStudy == null) return;
            _cmbDrillStudy.Items.Clear();
            foreach (var name in EndgameDrillService.GetStudyNames(_drillChapters))
                _cmbDrillStudy.Items.Add(name);
            if (_cmbDrillStudy.Items.Count > 0) _cmbDrillStudy.SelectedIndex = 0;
        }

        private void PopulateDrillChapters()
        {
            if (_cmbDrillStudy == null || _cmbDrillChapter == null) return;
            string? study = _cmbDrillStudy.SelectedItem?.ToString();
            _cmbDrillChapter.Items.Clear();
            foreach (var ch in _drillChapters.Where(c => c.StudyName == study))
                _cmbDrillChapter.Items.Add(ch.ChapterName);
            if (_cmbDrillChapter.Items.Count > 0) _cmbDrillChapter.SelectedIndex = 0;
        }

        private EndgameChapter? SelectedDrillChapter()
        {
            if (_cmbDrillStudy == null || _cmbDrillChapter == null) return null;
            string? study   = _cmbDrillStudy.SelectedItem?.ToString();
            string? chapter = _cmbDrillChapter.SelectedItem?.ToString();
            return _drillChapters.FirstOrDefault(c => c.StudyName == study && c.ChapterName == chapter);
        }

        private void DrillStart()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }

            StopTraining();

            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip board to match the active side
            if (!chapter.WhiteToMove && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (chapter.WhiteToMove && boardControl.IsFlipped) boardControl.FlipBoard();

            lblStatus.Text = chapter.ChapterName;
            _ = TriggerAutoAnalysis();
        }

        private async Task DrillPracticeAsync()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }
            if (_botModeActive) { StopBotMode(); return; }
            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
            if (string.IsNullOrEmpty(config?.SelectedEngine)) { lblStatus.Text = "No engine configured — click ⚙ to set up"; return; }

            string[] availableEngines = Directory.Exists(config.GetEnginesPath())
                ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();
            using var dialog = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _botSettings = dialog.Settings;
            // User always plays the active side in the drill position
            _botSettings.BotPlaysWhite = !chapter.WhiteToMove;

            StopTraining();
            CancelClassification();
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear();
            _currentClassification = null;
            _classificationLookup = null;
            consoleFormatter?.SetActiveClassification(null);
            analysisOutput.Clear();
            evalBar?.Reset();
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip board: user plays the active side
            bool userPlaysBlack = _botSettings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped) boardControl.FlipBoard();

            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string engineFile = !string.IsNullOrEmpty(_botSettings.EngineFileName)
                    ? _botSettings.EngineFileName : config.SelectedEngine;
                string enginePath = Path.Combine(enginesPath, engineFile);

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(enginePath);

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose();
                    _botEngine = null;
                    return;
                }

                await _botEngine.SetEloTargetAsync(_botSettings.EloTarget, _botSettings.GetSkillLevel());
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
            btnStartMatch.Enabled = false;

            string diffLabel = _botSettings.GetDifficultyLabel();
            string colorLabel = userPlaysBlack ? "Black" : "White";
            analysisOutput.AppendText($"Endgame Drill: {chapter.ChapterName}\n");
            analysisOutput.AppendText($"You play {colorLabel} — {diffLabel}\n\n");
            lblStatus.Text = $"Drill vs Bot — {diffLabel}";

            // It's always user's turn in the drill FEN — just start analysis
            _ = TriggerAutoAnalysis();
        }

        private static readonly (string Label, string? Theme)[] _puzzleThemeOptions =
        {
            ("Any",                  null),
            ("Mate in 1",            "mateIn1"),
            ("Mate in 2",            "mateIn2"),
            ("Mate in 3",            "mateIn3"),
            ("Fork",                 "fork"),
            ("Pin",                  "pin"),
            ("Skewer",               "skewer"),
            ("Sacrifice",            "sacrifice"),
            ("Discovered Attack",    "discoveredAttack"),
            ("Back Rank Mate",       "backRankMate"),
            ("Endgame",              "endgame"),
        };

        private static readonly (string Label, string? Opening)[] _puzzleOpeningOptions =
        {
            ("Any Opening",             null),
            ("Sicilian Defense",        "Sicilian_Defense"),
            ("French Defense",          "French_Defense"),
            ("Ruy Lopez",               "Ruy_Lopez"),
            ("Italian Game",            "Italian_Game"),
            ("Caro-Kann Defense",       "Caro-Kann_Defense"),
            ("King's Indian Defense",   "Kings_Indian_Defense"),
            ("Queen's Gambit",          "Queens_Gambit"),
            ("English Opening",         "English_Opening"),
            ("Nimzo-Indian Defense",    "Nimzo-Indian_Defense"),
            ("Slav Defense",            "Slav_Defense"),
            ("Dutch Defense",           "Dutch_Defense"),
            ("King's Gambit",           "Kings_Gambit"),
            ("Scandinavian Defense",    "Scandinavian_Defense"),
            ("Grunfeld Defense",        "Grunfeld_Defense"),
            ("Scotch Game",             "Scotch_Game"),
            ("Pirc Defense",            "Pirc_Defense"),
            ("Modern Defense",          "Modern_Defense"),
            ("Benoni Defense",          "Benoni_Defense"),
            ("Four Knights Game",       "Four_Knights_Game"),
            ("Petrov's Defense",        "Petrovs_Defense"),
            ("Indian Defense",          "Indian_Defense"),
        };

        private void PuzzleTrainingStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _puzzleStreak     = 0;
            _puzzleStreakBest  = config?.PuzzleTrainingBestStreak ?? 0;
            _puzzleQueue.Clear();
            _puzzleQueue.AddRange(LichessPuzzleService.GetRandomBatch(_puzzlesFolder, 200, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible  = false;
            _pnlTrainingResult!.Visible = false;
            _pnlPuzzleGame!.Visible     = true;

            PuzzleLoadNext();
        }

        private void PuzzleDailyStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            var daily = LichessPuzzleService.GetDailyPuzzle(_puzzlesFolder);
            if (daily == null) { lblStatus.Text = "No puzzles found."; return; }

            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _puzzleStreak     = 0;
            _puzzleStreakBest  = 0;
            _puzzleQueue.Clear();
            _puzzleQueue.Add(daily);

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible  = false;
            _pnlTrainingResult!.Visible = false;
            _pnlPuzzleGame!.Visible     = true;

            PuzzleLoadNext();
        }

        private void DailyPuzzleCompleted()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (config != null && config.DailyPuzzleLastSolvedDate != today)
            {
                // Update streak
                bool consecutive = config.DailyPuzzleLastSolvedDate ==
                                   DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                config.DailyPuzzleStreak = consecutive ? config.DailyPuzzleStreak + 1 : 1;
                if (config.DailyPuzzleStreak > config.DailyPuzzleBestStreak)
                    config.DailyPuzzleBestStreak = config.DailyPuzzleStreak;
                config.DailyPuzzleLastSolvedDate = today;
                config.Save();
            }

            bool clean = _puzzlesStruggled == 0 && _puzzleHintsUsed == 0;
            string streakStr = (config?.DailyPuzzleStreak ?? 0) > 0
                ? $"  ·  Streak: {config!.DailyPuzzleStreak}" : "";
            string result = clean ? $"Solved clean!{streakStr}" : $"Solved{streakStr}";
            SetText(_lblPuzzleFeedback, result);

            if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = true;
            if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = false;
            if (_btnPuzzleSkip    != null) _btnPuzzleSkip.Visible    = false;
            _puzzleLocked = true;
        }

        private void PuzzleLoadNext()
        {
            if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = false;
            if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = false;
            if (_btnPuzzleSkip    != null) _btnPuzzleSkip.Visible    = _puzzleSubMode == "training";
            boardControl.ClearTrainingHighlight();
            _puzzleLocked      = false;
            _wrongThisPuzzle   = false;
            _currentPuzzle     = null;

            if (_puzzleQueue.Count == 0)
            {
                if (_puzzleSubMode == "daily") { DailyPuzzleCompleted(); return; }
                _puzzleQueue.AddRange(LichessPuzzleService.GetRandomBatch(_puzzlesFolder!, 200, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));
            }

            _currentPuzzle = _puzzleQueue[0];
            _puzzleQueue.RemoveAt(0);
            _puzzleMoveIndex  = 1;
            _puzzlesAttempted++;
            _puzzleActive     = true;

            SetText(_lblPuzzleFeedback, "");
            if (_lblPuzzleThemes  != null) _lblPuzzleThemes.Text =
                _puzzleThemeFilter != null ? $"▸ {_cmbPuzzleTheme?.Text}" : "";
            if (_lblPuzzleRating  != null) _lblPuzzleRating.Text =
                $"Puzzle #{_puzzlesAttempted}  ·  Rating {_currentPuzzle.Rating}";
            if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;  // enabled after trigger plays
            UpdatePuzzleStats();

            // Determine player side from FEN side-to-move (player is opposite of trigger mover)
            bool fenWhiteToMove = _currentPuzzle.Fen.Contains(" w ");
            isNavigating = true;
            boardControl.LoadFEN(_currentPuzzle.Fen);
            boardControl.IsFlipped = fenWhiteToMove; // fenWhiteToMove=true → opponent is white → player is black → flip
            isNavigating = false;

            // Short pause then play trigger move
            _puzzleLocked = true;
            ScheduleInvoke(500, PuzzlePlayTrigger);
        }

        private void PuzzlePlayTrigger()
        {
            if (_currentPuzzle == null) return;
            string trigger = _currentPuzzle.Moves[0];
            string preFen  = boardControl.GetFEN();

            isNavigating = true;
            boardControl.MakeMove(trigger);
            isNavigating = false;

            string san = ConvertUciToSan(trigger, preFen);
            PlayMoveSound(san.Contains('x'), san);
            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(trigger);

            int delay = config?.ShowAnimations == true ? (config.AnimationDurationMs + 100) : 150;
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    _puzzleStartTime = DateTime.Now;
                    _puzzleLocked    = false;
                    if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = true;
                });
            });
        }

        private void BtnPuzzleHint_Click(object? sender, EventArgs e)
        {
            if (!_puzzleActive || _puzzleLocked || _currentPuzzle == null) return;
            if (_puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            _puzzleHintsUsed++;
            _wrongThisPuzzle = true;
            _puzzleStreak    = 0;
            UpdatePuzzleStats();
            string uci = _currentPuzzle.Moves[_puzzleMoveIndex];
            ShowTrainingHint(uci, _btnPuzzleHint, () =>
            {
                if (_puzzleActive && !_puzzleLocked && _btnPuzzleHint != null)
                    _btnPuzzleHint.Enabled = true;
            });
        }

        private void PuzzleValidateMove(string uciMove)
        {
            if (_currentPuzzle == null || _puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            string expected = _currentPuzzle.Moves[_puzzleMoveIndex];

            if (uciMove == expected)
            {
                _puzzleMoveIndex++;
                if (_puzzleMoveIndex >= _currentPuzzle.Moves.Length)
                {
                    // Puzzle solved
                    if (_wrongThisPuzzle) { _puzzlesStruggled++; _puzzleStreak = 0; }
                    else
                    {
                        _puzzlesClean++;
                        if (_puzzleSubMode == "training")
                        {
                            _puzzleStreak++;
                            if (_puzzleStreak > _puzzleStreakBest)
                            {
                                _puzzleStreakBest = _puzzleStreak;
                                if (config != null) { config.PuzzleTrainingBestStreak = _puzzleStreakBest; config.Save(); }
                            }
                        }
                    }
                    if (_puzzleSubMode == "gauntlet") _gauntletStreak++;
                    _puzzleLocked = true;
                    if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;
                    double elapsed = (DateTime.Now - _puzzleStartTime).TotalSeconds;
                    string timeStr = _puzzleSubMode == "gauntlet" ? "" : elapsed < 60
                        ? $"  {elapsed:F1}s"
                        : $"  {(int)(elapsed/60)}m {(int)(elapsed%60)}s";
                    SetText(_lblPuzzleFeedback, $"✓ Correct!{timeStr}");
                    PuzzleRevealThemes();
                    UpdatePuzzleStats();
                    int solveDelay = _puzzleSubMode == "gauntlet" ? 700 : 1400;
                    bool manualNext = _puzzleSubMode == "training" && _chkPuzzleAutoNext?.Checked == false;
                    if (manualNext)
                        BeginInvoke(() =>
                        {
                            if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = true;
                            if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = true;
                            if (_btnPuzzleSkip    != null) _btnPuzzleSkip.Visible    = false;
                        });
                    else
                        ScheduleInvoke(solveDelay, PuzzleLoadNext);
                }
                else
                {
                    // Correct move, opponent responds
                    _puzzleLocked = true;
                    SetText(_lblPuzzleFeedback, "");
                    ScheduleInvoke(400, PuzzlePlayOpponentResponse);
                }
            }
            else
            {
                _wrongThisPuzzle = true;
                _puzzleLocked    = true;
                int toRow = 7 - (uciMove[3] - '1');
                int toCol = uciMove[2] - 'a';
                boardControl.SetTrainingHighlight(toRow, toCol, TrainingErrorColor);
                SetText(_lblPuzzleFeedback, "✗ Wrong");

                if (_puzzleSubMode == "gauntlet")
                {
                    // Gauntlet: end run after brief flash
                    ScheduleInvoke(900, GauntletEnd);
                    return;
                }

                // Training / Rush: restore position and let player retry
                isNavigating = true;
                boardControl.LoadFEN(_currentPuzzle.Fen);
                for (int i = 0; i < _puzzleMoveIndex; i++)
                    boardControl.MakeMove(_currentPuzzle.Moves[i]);
                isNavigating = false;

                Task.Delay(800).ContinueWith(_ =>
                {
                    if (!IsDisposed) Invoke(() =>
                    {
                        boardControl.ClearTrainingHighlight();
                        SetText(_lblPuzzleFeedback, "");
                        _puzzleLocked = false;
                        if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = true;
                    });
                });
            }
        }

        private void PuzzlePlayOpponentResponse()
        {
            if (_currentPuzzle == null || _puzzleMoveIndex >= _currentPuzzle.Moves.Length) return;
            string response = _currentPuzzle.Moves[_puzzleMoveIndex];
            string preFen   = boardControl.GetFEN();

            isNavigating = true;
            boardControl.MakeMove(response);
            isNavigating = false;

            string san = ConvertUciToSan(response, preFen);
            PlayMoveSound(san.Contains('x'), san);
            if (config?.ShowAnimations == true)
                boardControl.StartAnimation(response);

            _puzzleMoveIndex++;
            int delay = config?.ShowAnimations == true ? (config.AnimationDurationMs + 100) : 150;
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    _puzzleLocked = false;
                    if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = true;
                });
            });
        }

        private void PuzzleSkip()
        {
            if (!_puzzleActive) return;
            _puzzlesAttempted = Math.Max(0, _puzzlesAttempted - 1); // don't count skips
            _puzzleLocked = true;
            if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;
            PuzzleRevealThemes();
            SetText(_lblPuzzleFeedback, "");
            bool manualNext = _puzzleSubMode == "training" && _chkPuzzleAutoNext?.Checked == false;
            if (manualNext)
            {
                if (_btnPuzzleNext    != null) _btnPuzzleNext.Visible    = true;
                if (_btnPuzzleAnalyze != null) _btnPuzzleAnalyze.Visible = true;
                if (_btnPuzzleSkip    != null) _btnPuzzleSkip.Visible    = false;
            }
            else
                ScheduleInvoke(900, PuzzleLoadNext);
        }

        private void PuzzleRevealThemes()
        {
            if (_lblPuzzleThemes == null || _currentPuzzle == null) return;
            string themes  = string.Join(" · ", _currentPuzzle.Themes.Take(5));
            string opening = _currentPuzzle.OpeningTags.Replace('_', ' ').Trim();
            _lblPuzzleThemes.Text = string.IsNullOrEmpty(opening)
                ? themes
                : $"{themes}\n{opening}";
        }

        private void UpdatePuzzleStats()
        {
            if (_lblPuzzleStats == null) return;
            string s;
            if (_puzzleSubMode == "gauntlet")
            {
                s = $"Streak: {_gauntletStreak}   Best: {_gauntletBestStreak}";
            }
            else if (_puzzleSubMode == "rush")
            {
                int pb = config?.PuzzleRushBest ?? 0;
                string pbStr = pb > 0 ? $"   ·   PB: {pb}" : "";
                s = $"Solved: {_puzzlesClean + _puzzlesStruggled}   ~ Helped: {_puzzlesStruggled}{pbStr}";
            }
            else
            {
                int total = _puzzlesClean + _puzzlesStruggled;
                string accStr = total > 0 ? $"   ·   {_puzzlesClean * 100 / total}% clean" : "";
                string streakStr = _puzzleStreak > 0 ? $"   ·   Streak {_puzzleStreak}" : "";
                string pbStr = _puzzleStreakBest > 1 ? $" (best {_puzzleStreakBest})" : "";
                s = $"✓ {_puzzlesClean}   ~ {_puzzlesStruggled}{accStr}{streakStr}{pbStr}";
            }
            _lblPuzzleStats.Text = s;
        }

        // ── Puzzle Rush ────────────────────────────────────────────────────────

        private void PuzzleRushStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _puzzleQueue.Clear();
            _puzzleQueue.AddRange(LichessPuzzleService.GetRandomBatch(_puzzlesFolder, 300, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible  = false;
            _pnlTrainingResult!.Visible = false;
            _pnlPuzzleGame!.Visible     = true;

            // Show rush timer, hide skip (rushing = no time to skip)
            if (_lblRushTimer    != null) _lblRushTimer.Visible    = true;
            if (_btnPuzzleSkip   != null) _btnPuzzleSkip.Visible   = false;
            if (_btnPuzzleHint   != null) _btnPuzzleHint.Visible   = true;

            _rushSecondsRemaining = _rushDurationSeconds;
            UpdateRushDisplay();

            _puzzleRushTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _puzzleRushTimer.Tick += PuzzleRushTick;
            _puzzleRushTimer.Start();

            PuzzleLoadNext();
        }

        private void PuzzleRushTick(object? sender, EventArgs e)
        {
            _rushSecondsRemaining--;
            UpdateRushDisplay();
            if (_rushSecondsRemaining <= 0)
            {
                _puzzleRushTimer?.Stop();
                _puzzleLocked = true;
                if (_btnPuzzleHint != null) _btnPuzzleHint.Enabled = false;
                ScheduleInvoke(300, PuzzleRushEnd);
            }
        }

        private void UpdateRushDisplay()
        {
            if (_lblRushTimer == null) return;
            int m = _rushSecondsRemaining / 60;
            int s = _rushSecondsRemaining % 60;
            _lblRushTimer.Text = $"{m}:{s:D2}";
            _lblRushTimer.ForeColor = _rushSecondsRemaining <= 10
                ? Color.FromArgb(220, 80, 80)
                : ThemeService.GetColorScheme(config?.Theme ?? "Dark").TextColor;
        }

        private void PuzzleRushEnd()
        {
            _puzzleActive = false;
            _puzzleRushTimer?.Stop();
            boardControl.ClearTrainingHighlight();
            if (_lblRushTimer  != null) _lblRushTimer.Visible  = false;
            if (_btnPuzzleSkip != null) _btnPuzzleSkip.Visible = true;

            int solved = _puzzlesClean + _puzzlesStruggled;
            if (_lblTrainingFinalScore != null)
                _lblTrainingFinalScore.Text = $"Puzzles solved: {solved}  ({_puzzlesClean} clean)";
            if (_lblTrainingPB != null)
            {
                int pb = config?.PuzzleRushBest ?? 0;
                bool newPb = solved > pb;
                if (newPb && config != null) { config.PuzzleRushBest = solved; config.Save(); }
                _lblTrainingPB.Text = newPb ? $"New best!  {solved}" : pb > 0 ? $"Best: {pb}" : "";
            }
            if (_lblOpMissedMoves != null) _lblOpMissedMoves.Visible = false;

            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen    = null;
            _trainingGameActive = false;

            _pnlPuzzleGame!.Visible    = false;
            _pnlTrainingResult!.Visible = true;
        }

        // ── Puzzle Gauntlet ────────────────────────────────────────────────────

        private void PuzzleGauntletStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _gauntletStreak     = 0;
            _gauntletBestStreak = config?.GauntletBestStreak ?? 0;
            _puzzleQueue.Clear();
            _puzzleQueue.AddRange(LichessPuzzleService.GetRandomBatch(_puzzlesFolder, 300, _puzzleThemeFilter, _puzzleRatingMin, _puzzleRatingMax, _puzzleOpeningFilter));

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;

            _pnlTrainingStart!.Visible  = false;
            _pnlTrainingResult!.Visible = false;
            _pnlPuzzleGame!.Visible     = true;

            if (_lblRushTimer  != null) _lblRushTimer.Visible  = false;
            if (_btnPuzzleHint != null) _btnPuzzleHint.Visible = false; // no hints in gauntlet
            if (_btnPuzzleSkip != null) _btnPuzzleSkip.Visible = false; // no skips in gauntlet

            PuzzleLoadNext();
        }

        private void GauntletEnd()
        {
            _puzzleActive = false;
            boardControl.ClearTrainingHighlight();

            if (_gauntletStreak > _gauntletBestStreak)
            {
                _gauntletBestStreak = _gauntletStreak;
                if (config != null) { config.GauntletBestStreak = _gauntletBestStreak; config.Save(); }
            }

            if (_btnPuzzleHint != null) _btnPuzzleHint.Visible = true;
            if (_btnPuzzleSkip != null) _btnPuzzleSkip.Visible = true;

            if (_lblTrainingFinalScore != null)
                _lblTrainingFinalScore.Text = $"Streak: {_gauntletStreak}";
            if (_lblTrainingPB != null)
                _lblTrainingPB.Text = _gauntletBestStreak > _gauntletStreak
                    ? $"Best streak: {_gauntletBestStreak}"
                    : _gauntletStreak > 0 ? "New personal best!" : "";
            if (_lblOpMissedMoves != null) _lblOpMissedMoves.Visible = false;

            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen    = null;
            _trainingGameActive = false;

            _pnlPuzzleGame!.Visible    = false;
            _pnlTrainingResult!.Visible = true;
        }

        // ── Board Vision ───────────────────────────────────────────────────────

        private static readonly string[] _allSquares = Enumerable.Range(0, 64)
            .Select(i => $"{(char)('a' + i % 8)}{i / 8 + 1}").ToArray();

        private static bool IsLightSquareName(string sq)
        {
            int col  = sq[0] - 'a'; // 0-7
            int rank = sq[1] - '0'; // 1-8
            return (col + rank) % 2 == 0;
        }

        private void VisionTrainingStart()
        {
            _visionCorrect  = 0;
            _visionWrong    = 0;
            _visionStreak   = 0;
            _visionLives    = 3;

            _trainingPreFen     = boardControl.GetFEN();
            _trainingPreFlipped = boardControl.IsFlipped;
            _trainingGameActive = true;

            boardControl.TrainingMode      = false;
            boardControl.MonochromeMode    = true;
            boardControl.HideCoordinates   = true;
            boardControl.IsFlipped         = false;
            isNavigating = true;
            boardControl.LoadFEN(TRAINING_EMPTY_FEN);
            isNavigating = false;

            _materialTop.Visible    = false;
            _materialBottom.Visible = false;

            bool hasTimed = _visionSubMode != "training";
            if (_lblVisionTimer       != null) { _lblVisionTimer.Visible       = hasTimed;                        _lblVisionTimer.Text = ""; }
            if (_lblVisionLives       != null) { _lblVisionLives.Visible       = _visionSubMode == "survival";    UpdateVisionLivesLabel(); }
            if (_lblVisionGlobalTimer != null) { _lblVisionGlobalTimer.Visible = _visionSubMode == "timed";       _lblVisionGlobalTimer.Text = ""; }

            if (_visionSubMode == "timed")
            {
                _visionGlobalSecondsRemaining = _visionGlobalDurationSeconds;
                UpdateVisionGlobalTimerLabel();
                _visionGlobalTimer?.Start();
            }

            _pnlTrainingStart!.Visible  = false;
            _pnlTrainingResult!.Visible = false;
            _pnlVisionGame!.Visible     = true;

            VisionLoadNext();
        }

        private void VisionLoadNext()
        {
            if (_btnVisionNext != null) _btnVisionNext.Visible = false;
            _visionCurrentSquare = _allSquares[_trainingRng.Next(_allSquares.Length)];
            if (_lblVisionQuestion != null)
                _lblVisionQuestion.Text = $"Is  {_visionCurrentSquare}  light or dark?";
            if (_lblVisionScore != null)
                _lblVisionScore.Text = $"✓ {_visionCorrect}   ✗ {_visionWrong}   Streak: {_visionStreak}";
            if (_btnVisionLight != null) _btnVisionLight.Enabled = true;
            if (_btnVisionDark  != null) _btnVisionDark.Enabled  = true;

            if (_visionSubMode != "training")
            {
                _visionSecondsRemaining = _visionTimedSeconds;
                UpdateVisionTimerLabel();
                _visionQuestionTimer?.Start();
            }
        }

        private void VisionAnswer(bool guessedLight)
        {
            _visionQuestionTimer?.Stop();
            if (_btnVisionLight != null) _btnVisionLight.Enabled = false;
            if (_btnVisionDark  != null) _btnVisionDark.Enabled  = false;
            VisionHandleResult(IsLightSquareName(_visionCurrentSquare) == guessedLight, timeout: false);
        }

        private void VisionHandleResult(bool correct, bool timeout)
        {
            string squareAnswer = IsLightSquareName(_visionCurrentSquare) ? "Light" : "Dark";
            if (correct)
            {
                _visionCorrect++;
                _visionStreak++;
                if (_lblVisionQuestion != null)
                    _lblVisionQuestion.Text = $"{_visionCurrentSquare} — {squareAnswer} ✓";
            }
            else
            {
                _visionWrong++;
                _visionStreak = 0;
                if (_visionSubMode == "survival") { _visionLives--; UpdateVisionLivesLabel(); }
                if (_lblVisionQuestion != null)
                    _lblVisionQuestion.Text = $"{_visionCurrentSquare} — {squareAnswer}  {(timeout ? "⏱" : "✗")}";
            }

            if (_lblVisionScore != null)
                _lblVisionScore.Text = $"✓ {_visionCorrect}   ✗ {_visionWrong}   Streak: {_visionStreak}";
            if (_lblVisionTimer != null) _lblVisionTimer.Text = "";
            if (_visionStreak > _visionBestStreak) _visionBestStreak = _visionStreak;

            if (_visionSubMode == "survival" && _visionLives <= 0)
            {
                ScheduleInvoke(900, VisionSurvivalEnd);
                return;
            }

            bool manualNext = _visionSubMode == "training" && _chkVisionAutoNext?.Checked == false;
            if (manualNext)
                { if (_btnVisionNext != null) _btnVisionNext.Visible = true; }
            else
                ScheduleInvoke(correct ? 350 : 700, VisionLoadNext);
        }

        private void SetVisionSubMode(string subMode) // "training" | "timed" | "survival"
        {
            _visionSubMode = subMode;
            bool hasTimed = subMode != "training";
            if (_pnlVisionTimeRow       != null) _pnlVisionTimeRow.Visible       = hasTimed;
            if (_pnlVisionGlobalTimeRow != null) _pnlVisionGlobalTimeRow.Visible = subMode == "timed";
            if (_pnlVisionAutoNextRow   != null) _pnlVisionAutoNextRow.Visible   = subMode == "training";
            if (_pnlVisionSettings != null)
                _pnlVisionSettings.Height = subMode == "timed" ? 180 : hasTimed ? 150 : 146;
            if (_lblVisionDesc != null)
                _lblVisionDesc.Text = subMode == "survival"
                    ? "3 lives — wrong or timeout costs a life.\nRun ends when lives reach zero."
                    : subMode == "timed"
                    ? "Answer before time runs out.\nTimeout counts as wrong."
                    : "Is the square light or dark?\nAll 64 squares — no visual clues.";
            HighlightButton(_btnVisionSubTraining, subMode == "training");
            HighlightButton(_btnVisionSubTimed,    subMode == "timed");
            HighlightButton(_btnVisionSubSurvival, subMode == "survival");
            UpdateVisionPBLabel();
        }

        private void UpdateSquarePBLabel()
        {
            if (_lblSquareSettingsPB == null || config == null) return;
            string key = TrainingModeKey();
            _lblSquareSettingsPB.Text = config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestTime != double.MaxValue
                ? $"Best: {pb.BestCorrect}/{pb.BestQuestions} correct  ·  {pb.BestTime:F1}s"
                : "";
        }

        private void UpdateVisionPBLabel()
        {
            if (_lblVisionSettingsPB == null || config == null) return;
            string key = _visionSubMode == "survival" ? "Vision-Survival"
                : $"Vision-Timed-{_visionGlobalDurationSeconds}";
            if (_visionSubMode == "training")
                { _lblVisionSettingsPB.Text = ""; return; }
            _lblVisionSettingsPB.Text = config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0
                ? $"Best: {pb.BestCorrect} correct"
                : "";
        }

        private void SelectVisionTime(int seconds)
        {
            _visionTimedSeconds = seconds;
            HighlightButton(_visionTimeBtn3,  seconds == 3);
            HighlightButton(_visionTimeBtn5,  seconds == 5);
            HighlightButton(_visionTimeBtn10, seconds == 10);
        }

        private void SelectVisionGlobalTime(int seconds)
        {
            _visionGlobalDurationSeconds = seconds;
            HighlightButton(_visionGlobalBtn1, seconds == 60);
            HighlightButton(_visionGlobalBtn3, seconds == 180);
            HighlightButton(_visionGlobalBtn5, seconds == 300);
            UpdateVisionPBLabel();
        }

        private void VisionGlobalTimer_Tick(object? sender, EventArgs e)
        {
            _visionGlobalSecondsRemaining--;
            UpdateVisionGlobalTimerLabel();
            if (_visionGlobalSecondsRemaining <= 0)
            {
                _visionGlobalTimer!.Stop();
                _visionQuestionTimer?.Stop();
                if (_btnVisionLight != null) _btnVisionLight.Enabled = false;
                if (_btnVisionDark  != null) _btnVisionDark.Enabled  = false;
                if (!IsDisposed) Invoke((Action)VisionTimedEnd);
            }
        }

        private void UpdateVisionGlobalTimerLabel()
        {
            if (_lblVisionGlobalTimer == null) return;
            int m = _visionGlobalSecondsRemaining / 60;
            int s = _visionGlobalSecondsRemaining % 60;
            _lblVisionGlobalTimer.Text = $"⏱ {m}:{s:D2}";
            _lblVisionGlobalTimer.ForeColor = _visionGlobalSecondsRemaining <= 10
                ? Color.FromArgb(220, 80, 80)
                : (Parent?.ForeColor ?? Color.White);
        }

        private void VisionTimedEnd()
        {
            if (config != null)
            {
                string key = $"Vision-Timed-{_visionGlobalDurationSeconds}";
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();
                if (_visionCorrect > pb.BestCorrect)
                {
                    pb.BestCorrect = _visionCorrect;
                    config.TrainingPersonalBests[key] = pb;
                    config.Save();
                }
            }
            VisionEnd($"✓ {_visionCorrect} correct   ✗ {_visionWrong} wrong", $"Best streak: {_visionBestStreak}");
        }

        private void VisionQuestionTimer_Tick(object? sender, EventArgs e)
        {
            _visionSecondsRemaining--;
            UpdateVisionTimerLabel();
            if (_visionSecondsRemaining <= 0)
            {
                _visionQuestionTimer!.Stop();
                if (_btnVisionLight != null) _btnVisionLight.Enabled = false;
                if (_btnVisionDark  != null) _btnVisionDark.Enabled  = false;
                VisionHandleResult(false, timeout: true);
            }
        }

        private void UpdateVisionTimerLabel()
        {
            if (_lblVisionTimer == null) return;
            _lblVisionTimer.Text = _visionSecondsRemaining > 0 ? $"⏱ {_visionSecondsRemaining}" : "";
            _lblVisionTimer.ForeColor = _visionSecondsRemaining <= 2
                ? Color.FromArgb(220, 80, 80)
                : (Parent?.ForeColor ?? Color.White);
        }

        private void UpdateVisionLivesLabel()
        {
            if (_lblVisionLives == null) return;
            _lblVisionLives.Text = _visionLives switch { >= 3 => "♥ ♥ ♥", 2 => "♥ ♥", 1 => "♥", _ => "✗" };
            _lblVisionLives.ForeColor = _visionLives <= 1 ? Color.FromArgb(220, 80, 80) : Color.FromArgb(200, 80, 80);
        }

        private void VisionSurvivalEnd()
        {
            if (config != null)
            {
                string key = "Vision-Survival";
                if (!config.TrainingPersonalBests.TryGetValue(key, out var pb))
                    pb = new TrainingPersonalBest();
                if (_visionCorrect > pb.BestCorrect)
                {
                    pb.BestCorrect = _visionCorrect;
                    config.TrainingPersonalBests[key] = pb;
                    config.Save();
                }
            }
            VisionEnd($"✓ {_visionCorrect} correct   Best streak: {_visionBestStreak}", "");
        }

        // ── Training helpers ───────────────────────────────────────────────────

        private void VisionEnd(string scoreText, string pbText)
        {
            _pnlVisionGame!.Visible     = false;
            _pnlTrainingResult!.Visible = true;
            if (_lblTrainingFinalScore != null) _lblTrainingFinalScore.Text = scoreText;
            if (_lblTrainingPB        != null) _lblTrainingPB.Text = pbText;
            if (_lblOpMissedMoves     != null) _lblOpMissedMoves.Visible = false;
        }

        private void ScheduleInvoke(int delayMs, Action action)
        {
            Task.Delay(delayMs).ContinueWith(_ => { if (!IsDisposed) Invoke(action); });
        }

        private void HighlightButton(Button? btn, bool active)
        {
            if (btn == null) return;
            var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");
            btn.BackColor = active ? scheme.TextColor : scheme.ButtonBackColor;
            btn.ForeColor = active ? scheme.FormBackColor : scheme.ButtonForeColor;
        }

        private static void SetText(Label? lbl, string text)  { if (lbl != null) lbl.Text = text; }
        private static void SetText(Button? btn, string text) { if (btn != null) btn.Text = text; }

        private void ShowTrainingHint(string uci, Button? hintButton, Action? onExpire = null)
        {
            if (hintButton != null) hintButton.Enabled = false;
            var (fromRow, fromCol, _, _) = UciToSquares(uci);
            boardControl.SetTrainingHighlight(fromRow, fromCol, TrainingHintColor);
            Task.Delay(1500).ContinueWith(_ =>
            {
                if (!IsDisposed) Invoke(() =>
                {
                    boardControl.ClearTrainingHighlight();
                    onExpire?.Invoke();
                });
            });
        }

        private void ApplyTrainingTheme()
        {
            if (_pnlTraining == null) return;
            var scheme = ThemeService.GetColorScheme(config?.Theme ?? "Dark");

            _pnlTraining.BackColor = scheme.FormBackColor;
            ApplyThemeToChildren(_pnlTraining, scheme);
            // Re-apply mode + sub-mode + rush time button highlights with current theme
            string mode = _visionModeSelected ? "vision" : _puzzleModeSelected ? "puzzle" : _openingModeSelected ? "opening" : "square";
            SetTrainingMode(mode);
            SetPuzzleSubMode(_puzzleSubMode);
            SetVisionSubMode(_visionSubMode);
            int rushMins = _rushDurationSeconds / 60;
            SelectRushTime(rushMins > 0 ? rushMins : 3);
            SelectPuzzleRating(_puzzleRatingMin, _puzzleRatingMax);
            SelectVisionTime(_visionTimedSeconds);
            SelectVisionGlobalTime(_visionGlobalDurationSeconds);
        }

        private void ApplyThemeToChildren(Control parent, ColorScheme scheme)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    lbl.ForeColor = scheme.TextColor;
                }
                else if (c is RadioButton rb)
                {
                    rb.BackColor = Color.Transparent;
                    rb.ForeColor = scheme.TextColor;
                }
                else if (c is CheckBox chk)
                {
                    chk.BackColor = Color.Transparent;
                    chk.ForeColor = scheme.TextColor;
                }
                else if (c is Button btn)
                {
                    btn.BackColor = scheme.ButtonBackColor;
                    btn.ForeColor = scheme.ButtonForeColor;
                }
                else if (c is ComboBox cmb)
                {
                    cmb.BackColor = scheme.ButtonBackColor;
                    cmb.ForeColor = scheme.TextColor;
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BackColor = scheme.ButtonBackColor;
                    nud.ForeColor = scheme.TextColor;
                }
                else if (c is Panel pnl)
                {
                    pnl.BackColor = Color.Transparent;
                    ApplyThemeToChildren(pnl, scheme);
                }
            }
        }

        #endregion


    }
}
