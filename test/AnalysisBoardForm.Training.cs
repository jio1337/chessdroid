using System.Diagnostics;
using System.Text.RegularExpressions;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public partial class AnalysisBoardForm : Form
    {
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
        private Button? _btnTrainingStart;
        private Panel?  _pnlDrillSettings;
        private ComboBox? _cmbDrillStudy;
        private ComboBox? _cmbDrillChapter;
        private RichTextBox? _lblDrillDesc;
        private Button?   _btnDrillVsBot;
        private Button?   _btnDrillWatchEngines;
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
            _btnTrainingStart = new Button
            {
                Text = "Start", Font = F(11f, true),
                Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat
            };
            _btnTrainingStart.Click += (_, _) => TrainingStartRound();
            var btnStart = _btnTrainingStart;

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
            _pnlDrillSettings = new Panel { Dock = DockStyle.Top, Height = 184, Visible = false };
            var lblStudy = new Label { Text = "Study", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbDrillStudy = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            var lblChapter = new Label { Text = "Position", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbDrillChapter = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            _lblDrillDesc = new RichTextBox
            {
                Dock = DockStyle.Top, Height = 0, Font = F(10.5f),
                ReadOnly = true, BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };
            _btnDrillVsBot = new Button
            {
                Text = "⚔ Practice vs Bot", Dock = DockStyle.Top, Height = 28,
                Font = F(9f), FlatStyle = FlatStyle.Flat
            };
            _btnDrillVsBot.Click += (_, _) => _ = DrillPracticeAsync();
            var pnlDrillVsBotGap = new Panel { Dock = DockStyle.Top, Height = 4 };
            _btnDrillWatchEngines = new Button
            {
                Text = "⚙ Watch engines", Dock = DockStyle.Top, Height = 28,
                Font = F(9f), FlatStyle = FlatStyle.Flat
            };
            _btnDrillWatchEngines.Click += (_, _) =>
            {
                if (_drillWatchActive) { matchService?.StopMatch(); return; }
                _ = DrillWatchEnginesAsync();
            };
            var pnlDrillWatchGap = new Panel { Dock = DockStyle.Top, Height = 6 };
            _cmbDrillStudy.SelectedIndexChanged += (_, _) => PopulateDrillChapters();
            _cmbDrillChapter.SelectedIndexChanged += (_, _) =>
            {
                var ch = SelectedDrillChapter();
                SetDrillDescription(ch?.Description ?? "");
                if (ch != null && _drillModeSelected)
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
                { _btnDrillVsBot, pnlDrillVsBotGap, _btnDrillWatchEngines, pnlDrillWatchGap, _lblDrillDesc, _cmbDrillChapter, lblChapter, _cmbDrillStudy, lblStudy });

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

        private void BtnTournament_Click(object? sender, EventArgs e)
        {
            if (matchRunning) { lblStatus.Text = "Stop the match first"; return; }
            var form = new TournamentForm(config);
            form.Show(this);
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

            RestoreBoardAfterTraining();

            _pnlPuzzleGame!.Visible     = false;
            _pnlTrainingResult!.Visible = true;
        }

        private void StartTrainingUI()
        {
            _puzzlesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Puzzles");
            if (_btnPuzzleMode != null)
                _btnPuzzleMode.Visible = LichessPuzzleService.HasPuzzles(_puzzlesFolder);

            _drillsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drills");
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
                RestoreBoardAfterTraining();
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

        private void RestoreBoardAfterTraining()
        {
            boardControl.IsFlipped = _trainingPreFlipped;
            if (_trainingPreFen != null) boardControl.LoadFEN(_trainingPreFen);
            _trainingPreFen     = null;
            _trainingGameActive = false;
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
                RestoreBoardAfterTraining();
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
            if (_drillWatchActive) { matchService?.StopMatch(); return; }
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
            RestoreBoardAfterTraining();

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
                                       : mode == "drill"   ? "Drills"
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
            if (_btnTrainingStart != null)
                _btnTrainingStart.Text = mode == "drill" ? "Load position" : "Start";
            if (mode == "square")  UpdateSquarePBLabel();
            if (mode == "puzzle")  SetPuzzleSubMode(_puzzleSubMode);
            if (mode == "vision")  UpdateVisionPBLabel();
            // Re-measure description height now that the panel has its real width
            if (mode == "drill" && IsHandleCreated)
                BeginInvoke(() => SetDrillDescription(_lblDrillDesc?.Text ?? ""));
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
                var match = PgnOpeningHeaderRegex.Match(pgn);
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
            var tokens = FilterSanTokens(sanMoves);

            foreach (var san in tokens)
            {
                string? uci = PgnImportService.ConvertSanToUci(san, currentFen);
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
            SetText(_lblTrainingScore, $"{_selectedTrainingOpening?.Eco}  {PgnImportService.StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
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
            SetText(_lblTrainingScore,  $"{_selectedTrainingOpening?.Eco}  {PgnImportService.StripMovesFromOpeningName(_selectedTrainingOpening?.Name ?? "")}");
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

        private void SetDrillControlsEnabled(bool enabled)
        {
            if (_cmbDrillStudy         != null) _cmbDrillStudy.Enabled         = enabled;
            if (_cmbDrillChapter       != null) _cmbDrillChapter.Enabled       = enabled;
            if (_btnTrainingStart      != null) _btnTrainingStart.Enabled      = enabled;
            if (_btnDrillWatchEngines  != null) _btnDrillWatchEngines.Enabled  = enabled;
            // Lock mode-switcher buttons so the user can't leave drill mode mid-game
            if (_btnSqMode     != null) _btnSqMode.Enabled     = enabled;
            if (_btnOpMode     != null) _btnOpMode.Enabled     = enabled;
            if (_btnPuzzleMode != null) _btnPuzzleMode.Enabled = enabled;
            if (_btnVisionMode != null) _btnVisionMode.Enabled = enabled;
            if (_btnDrillMode  != null) _btnDrillMode.Enabled  = enabled;
        }

        private void SetDrillDescription(string text)
        {
            if (_lblDrillDesc == null || _pnlDrillSettings == null) return;
            _lblDrillDesc.Text = text;
            const int baseHeight = 152; // panel height without the description
            const int maxDescHeight = 120; // max visible lines before scrollbar kicks in
            if (string.IsNullOrEmpty(text))
            {
                _lblDrillDesc.Height = 0;
            }
            else
            {
                int w = Math.Max(_pnlDrillSettings.ClientSize.Width, 80);
                var sz = TextRenderer.MeasureText(text, _lblDrillDesc.Font,
                    new Size(w, 9999), TextFormatFlags.WordBreak);
                _lblDrillDesc.Height = Math.Min(sz.Height + 6, maxDescHeight);
            }
            _pnlDrillSettings.Height = baseHeight + _lblDrillDesc.Height;
        }

        private List<EndgameChapter> GetChaptersForStudy(string? study)
            => _drillChapters.Where(c => c.StudyName == study).ToList();

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
            int n = 1;
            foreach (var ch in GetChaptersForStudy(study))
                _cmbDrillChapter.Items.Add($"Ch.{n++}: {ch.ChapterName}");
            if (_cmbDrillChapter.Items.Count > 0) _cmbDrillChapter.SelectedIndex = 0;
        }

        private EndgameChapter? SelectedDrillChapter()
        {
            if (_cmbDrillStudy == null || _cmbDrillChapter == null) return null;
            string? study = _cmbDrillStudy.SelectedItem?.ToString();
            int idx = _cmbDrillChapter.SelectedIndex;
            if (idx < 0) return null;
            var chapters = GetChaptersForStudy(study);
            return idx < chapters.Count ? chapters[idx] : null;
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

            // Button acts as toggle — stop if already running
            if (_botModeActive) { StopBotMode(); return; }

            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
            if (string.IsNullOrEmpty(config?.SelectedEngine)) { lblStatus.Text = "No engine configured — click ⚙ to set up"; return; }

            string[] availableEngines = Directory.Exists(config.GetEnginesPath())
                ? Directory.GetFiles(config.GetEnginesPath(), "*.exe").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();
            using var dialog = new BotSettingsDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "",
                drillMode: true);
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _botSettings = dialog.Settings;
            // User always plays the active side in the drill position
            _botSettings.BotPlaysWhite = !chapter.WhiteToMove;

            // Lock down drill controls — training panel stays visible the whole session
            SetDrillControlsEnabled(false);
            if (_btnDrillVsBot != null) { _btnDrillVsBot.Text = "⏹ Stop Practice"; _btnDrillVsBot.Enabled = true; }

            // Board setup (no StopTraining — we keep the training panel visible)
            CancelClassification();
            boardControl.ClearEngineArrows();
            boardControl.ClearBookArrows();
            _bookArrowsActive = false;
            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            _botPositionCounts.Clear();
            _botPositionCounts[GetPositionKey(chapter.Fen)] = 1;
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

            // Flip board so user plays the active side
            bool userPlaysBlack = _botSettings.BotPlaysWhite;
            if (userPlaysBlack && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (!userPlaysBlack && boardControl.IsFlipped) boardControl.FlipBoard();

            // Start bot engine
            try
            {
                lblStatus.Text = "Starting bot engine...";
                string enginesPath = config!.GetEnginesPath();
                string engineFile  = !string.IsNullOrEmpty(_botSettings.EngineFileName)
                    ? _botSettings.EngineFileName : config.SelectedEngine;
                string enginePath  = Path.Combine(enginesPath, engineFile);

                _botEngine = new ChessEngineService(config);
                await _botEngine.InitializeAsync(enginePath);

                if (_botEngine.State != EngineState.Ready)
                {
                    lblStatus.Text = "Failed to start bot engine";
                    _botEngine.Dispose();
                    _botEngine = null;
                    SetDrillControlsEnabled(true);
                    if (_btnDrillVsBot != null) _btnDrillVsBot.Text = "⚔ Practice vs Bot";
                    return;
                }

                await _botEngine.SetEloTargetAsync(_botSettings.EloTarget, _botSettings.GetSkillLevel());
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Bot engine error: {ex.Message}";
                _botEngine?.Dispose();
                _botEngine = null;
                SetDrillControlsEnabled(true);
                if (_btnDrillVsBot != null) _btnDrillVsBot.Text = "⚔ Practice vs Bot";
                return;
            }

            _drillBotActive = true;
            _botModeActive  = true;
            _botMoveCts     = new CancellationTokenSource();
            btnPlayBot.Text = "⏹";
            toolTip.SetToolTip(btnPlayBot, "Stop Bot");
            boardControl.InteractionEnabled = true;
            btnStartMatch.Enabled = false;

            // Always challenge mode for drills — no eval bar, no engine lines, no hints
            ApplyChallengeMode();

            string diffLabel  = _botSettings.GetDifficultyLabel();
            string colorLabel = userPlaysBlack ? "Black" : "White";
            lblStatus.Text = $"Drill — {chapter.ChapterName}  |  {diffLabel}  |  You play {colorLabel}";

            // Bot moves first only if it's their turn in the drill starting position
            bool botMovesFirst = _botSettings.BotPlaysWhite == chapter.WhiteToMove;
            if (botMovesFirst)
                _ = MakeBotMoveAsync();
            // No TriggerAutoAnalysis — challenge mode suppresses analysis anyway
        }

        private async Task DrillWatchEnginesAsync()
        {
            var chapter = SelectedDrillChapter();
            if (chapter == null) { lblStatus.Text = "Select a position first."; return; }

            if (matchRunning) { lblStatus.Text = "Stop the engine match first"; return; }
            if (_botModeActive) { lblStatus.Text = "Stop bot mode first"; return; }

            string[] availableEngines = Directory.Exists(config?.GetEnginesPath())
                ? Directory.GetFiles(config!.GetEnginesPath(), "*.exe")
                    .Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToArray()
                : Array.Empty<string>();

            if (availableEngines.Length == 0) { lblStatus.Text = "No engines found in Engines folder"; return; }

            using var dlg = new DrillWatchDialog(ThemeService.IsDarkTheme(config?.Theme),
                availableEngines, config?.EngineProfiles ?? new(), config?.SelectedEngine ?? "");
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string enginesPath = config!.GetEnginesPath();
            string whitePath = Path.Combine(enginesPath, dlg.WhiteEngine);
            string blackPath = Path.Combine(enginesPath, dlg.BlackEngine);
            if (!File.Exists(whitePath) || !File.Exists(blackPath))
            { lblStatus.Text = "Engine file not found"; return; }

            // Lock drill controls
            SetDrillControlsEnabled(false);
            _drillWatchActive = true;
            if (_btnDrillWatchEngines != null) { _btnDrillWatchEngines.Text = "⏹ Stop Watch"; _btnDrillWatchEngines.Enabled = true; }

            // Board setup
            boardControl.LoadFEN(chapter.Fen);
            moveTree.Clear(chapter.Fen);
            moveListBox.Items.Clear();
            displayedNodes.Clear();
            _analysisCache.Clear();
            analysisOutput.Clear();
            evalBar?.Reset();
            UpdateMoveList();
            UpdateFenDisplay();
            UpdateTurnLabel();

            // Flip to match active side
            if (!chapter.WhiteToMove && !boardControl.IsFlipped) boardControl.FlipBoard();
            else if (chapter.WhiteToMove && boardControl.IsFlipped) boardControl.FlipBoard();

            // Engine names for display
            config.EngineProfiles.TryGetValue(dlg.WhiteEngine, out var wProf);
            config.EngineProfiles.TryGetValue(dlg.BlackEngine, out var bProf);
            _matchWhiteName     = !string.IsNullOrEmpty(wProf?.DisplayName) ? wProf!.DisplayName : Path.GetFileNameWithoutExtension(dlg.WhiteEngine);
            _matchBlackName     = !string.IsNullOrEmpty(bProf?.DisplayName) ? bProf!.DisplayName : Path.GetFileNameWithoutExtension(dlg.BlackEngine);
            _matchWhiteElo      = wProf?.Elo ?? 0;
            _matchBlackElo      = bProf?.Elo ?? 0;
            _matchWhiteFileName = dlg.WhiteEngine;
            _matchBlackFileName = dlg.BlackEngine;

            // Series init (single game)
            _seriesTotal            = 1;
            _seriesPlayed           = 0;
            _seriesEng1Score        = 0;
            _seriesEng2Score        = 0;
            _seriesEng1File         = dlg.WhiteEngine;
            _seriesEng2File         = dlg.BlackEngine;
            _seriesCurrentWhiteFile = dlg.WhiteEngine;
            lblSeriesScore.Text     = "";

            // Seed position counts for threefold detection
            _watchPositionCounts.Clear();
            _watchPositionCounts[GetPositionKey(chapter.Fen)] = 1;

            // Output header
            analysisOutput.AppendText($"Engine Watch — {chapter.ChapterName}\n");
            analysisOutput.AppendText($"{_matchWhiteName} vs {_matchBlackName}  |  60+0\n\n");

            // Set up match service
            matchService?.Dispose();
            matchService = new Services.EngineMatchService(config);
            _previousMatchEval = null;
            matchService.OnMovePlayed           += MatchService_OnMovePlayed;
            matchService.OnClockUpdated         += MatchService_OnClockUpdated;
            matchService.OnMatchEnded           += MatchService_OnMatchEnded;
            matchService.OnStatusChanged        += MatchService_OnStatusChanged;
            matchService.OnAnnotatorEvalUpdated += MatchService_OnAnnotatorEvalUpdated;
            boardControl.AnimationCompleted     += MatchBoard_AnimationCompleted;

            // Set annotator engine for eval bar
            matchService.AnnotatorEngine = engineService;
            matchService.AnnotatorDepth  = config.EngineDepth;
            SetEngineInfoLabels(wProf, bProf, dlg.WhiteEngine, dlg.BlackEngine);

            // 60+0 time control
            var tc = new EngineMatchTimeControl
            {
                Type        = TimeControlType.TotalPlusIncrement,
                TotalTimeMs = 60_000,
                IncrementMs = 0
            };

            clockTimer.Start();
            matchRunning = true;
            btnStartMatch.Enabled = false;

            await matchService.StartMatchAsync(whitePath, blackPath, _matchWhiteName, _matchBlackName, tc, chapter.Fen);
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

        private void ResetPuzzleCounters()
        {
            _puzzlesClean     = 0;
            _puzzlesStruggled = 0;
            _puzzlesAttempted = 0;
            _puzzleHintsUsed  = 0;
            _puzzleQueue.Clear();
        }

        private void PuzzleTrainingStart()
        {
            if (string.IsNullOrEmpty(_puzzlesFolder)) return;
            ResetPuzzleCounters();
            _puzzleStreak    = 0;
            _puzzleStreakBest = config?.PuzzleTrainingBestStreak ?? 0;
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

            ResetPuzzleCounters();
            _puzzleStreak    = 0;
            _puzzleStreakBest = 0;
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
            ResetPuzzleCounters();
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
            ResetPuzzleCounters();
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
            string mode = _drillModeSelected ? "drill" : _visionModeSelected ? "vision" : _puzzleModeSelected ? "puzzle" : _openingModeSelected ? "opening" : "square";
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
