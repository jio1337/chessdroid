using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ChessDroid.Controls;
using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Views
{
    /// <summary>
    /// Training mode's control tree, extracted out of AnalysisBoardForm.Training.cs per
    /// CHESSDROID_AUDIT.md §4.2 (construction only — InitTrainingPanel()'s ~870 lines of
    /// hand-built controls). All training MODE LOGIC (scoring, puzzle/vision/opening/drill
    /// algorithms, timers) stays on AnalysisBoardForm and reaches into this control's
    /// internal fields exactly as it reaches into ChessBoardControl today.
    ///
    /// This control itself plays the role the old _pnlTraining sub-panel used to (Dock=Fill,
    /// initially hidden, same padding) rather than nesting a redundant extra panel inside
    /// itself — AnalysisBoardForm adds this control directly to rightPanel and toggles its
    /// own Visible/BringToFront/SendToBack exactly as it used to for _pnlTraining.
    /// </summary>
    internal sealed class TrainingPanel : UserControl
    {
        private readonly ChessDroid.AnalysisBoardForm _form;

        internal Panel?          _pnlTrainingStart;
        internal Panel?          _pnlTrainingGame;
        internal Panel?          _pnlTrainingResult;
        internal RadioButton?    _rbTrainingEasy;
        internal RadioButton?    _rbTrainingBlack;
        internal RadioButton?    _rbTrainingRandom;
        internal NumericUpDown?  _numQuestions;
        internal NumericUpDown?  _numTimeLimit;
        internal Label?       _lblTrainingRound;
        internal Label?       _lblTrainingTarget;
        internal Label?       _lblTrainingScore;
        internal Label?       _lblTrainingTimer;
        internal Label?       _lblTrainingFinalScore;
        internal Label?       _lblTrainingPB;
        internal Label?  _lblTrainingTitle;
        internal Label?  _lblSquareDesc;
        internal Label?  _lblSquareSettingsPB;
        internal Label?  _lblPuzzleSettingsPB;
        internal Label?  _lblVisionSettingsPB;
        internal Button? _btnSqMode;
        internal Button? _btnOpMode;
        internal Button? _btnDrillMode;
        internal Button? _btnTrainingStart;
        internal Panel?  _pnlDrillSettings;
        internal ComboBox? _cmbDrillStudy;
        internal ComboBox? _cmbDrillChapter;
        internal RichTextBox? _lblDrillDesc;
        internal Button?   _btnDrillVsBot;
        internal Button?   _btnDrillWatchEngines;
        internal Panel? _pnlSquareSettings;
        internal Panel? _pnlOpeningSettings;
        internal RadioButton? _rbOpRandom;
        internal RadioButton? _rbOpSelected;
        internal Label? _lblSelectedOpening;
        internal RadioButton? _rbWatchesNone;
        internal RadioButton? _rbWatches1;
        internal RadioButton? _rbWatches2;
        internal RadioButton? _rbWatches3;
        internal Label?  _lblOpGameStatus;
        internal Label?  _lblOpMissedMoves;
        internal Button? _btnOpHint;
        internal Button? _btnPuzzleSubTraining;
        internal Button? _btnPuzzleSubRush;
        internal Button? _btnPuzzleSubGauntlet;
        internal Button? _btnPuzzleSubDaily;
        internal Panel?  _pnlRushTimeRow;
        internal Panel?  _pnlThemeFilterRow;
        internal Panel?  _pnlOpeningFilterRow;
        internal ComboBox? _cmbPuzzleOpening;
        internal Panel?  _pnlRatingRow;
        internal Button? _ratingBtnAny, _ratingBtnBeg, _ratingBtnInt, _ratingBtnAdv, _ratingBtnMaster;
        internal Label?  _lblRatingRange;
        internal Label?  _lblGauntletDesc;
        internal Button? _rushTimeBtn1, _rushTimeBtn2, _rushTimeBtn3, _rushTimeBtn4, _rushTimeBtn5;
        internal Label?  _lblRushTimer;
        internal Button?   _btnPuzzleMode;
        internal Panel?    _pnlPuzzleGame;
        internal Panel?    _pnlPuzzleSettings;
        internal Panel?    _pnlPuzzleAutoNextRow;
        internal CheckBox? _chkPuzzleAutoNext;
        internal Button?   _btnPuzzleNext;
        internal Button?   _btnPuzzleAnalyze;
        internal ComboBox? _cmbPuzzleTheme;
        internal Label?    _lblPuzzleRating;
        internal Label?  _lblPuzzleThemes;
        internal Label?  _lblPuzzleFeedback;
        internal Label?  _lblPuzzleStats;
        internal Button? _btnPuzzleHint;
        internal Button? _btnPuzzleSkip;
        internal Button? _btnVisionMode;
        internal Panel?   _pnlVisionAutoNextRow;
        internal CheckBox? _chkVisionAutoNext;
        internal Button?   _btnVisionNext;
        internal Panel?  _pnlVisionGame;
        internal Panel?  _pnlVisionSettings;
        internal Label?  _lblVisionQuestion;
        internal Label?  _lblVisionScore;
        internal Button? _btnVisionLight;
        internal Button? _btnVisionDark;
        internal Label?  _lblVisionTimer;
        internal Label?  _lblVisionLives;
        internal Button? _btnVisionSubTraining, _btnVisionSubTimed, _btnVisionSubSurvival;
        internal Panel?  _pnlVisionTimeRow;
        internal Panel?  _pnlVisionGlobalTimeRow;
        internal Label?  _lblVisionDesc;
        internal Button? _visionTimeBtn3, _visionTimeBtn5, _visionTimeBtn10;
        internal Button? _visionGlobalBtn1, _visionGlobalBtn3, _visionGlobalBtn5;
        internal Label?  _lblVisionGlobalTimer;

        internal TrainingPanel(ChessDroid.AnalysisBoardForm form)
        {
            _form = form;
            // BuildUi() ends by calling back into _form.SetTrainingMode()/ApplyTrainingTheme()/
            // etc., which reach _form._trainingPanel — assign it now, before BuildUi() runs,
            // since the caller's own "_trainingPanel = new TrainingPanel(this)" assignment
            // doesn't happen until this constructor returns.
            _form._trainingPanel = this;
            Dock = DockStyle.Fill;
            Visible = false;
            Padding = new Padding(16, 12, 14, 10);
            BuildUi();
        }

        private void BuildUi()
        {
            _form._trainingClockTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _form._trainingClockTimer.Tick += (_, _) =>
            {
                _form.TrainingUpdateStats();
                if (_form._trainingGameActive && !_form._trainingAwaitingNext && _form.TrainingCheckTimeExpired())
                {
                    _form._trainingAwaitingNext = true;
                    _form._trainingFlashTimer?.Stop();
                    _form.boardControl.ClearTrainingHighlight();
                    _form.TrainingShowResult();
                }
            };
            _form._trainingFlashTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _form._trainingFlashTimer.Tick += _form.TrainingFlashTimer_Tick;

            _form._openingAutoplayTimer = new System.Windows.Forms.Timer { Interval = 900 };
            _form._openingAutoplayTimer.Tick += _form.OpeningAutoplayTimer_Tick;

            _form.boardControl.SquareClicked += _form.TrainingSquareClicked;

            // Local font factory — every call returns a new instance (safe for WinForms disposal)
            Font F(float size, bool bold = false) =>
                new Font("Courier New", size, bold ? FontStyle.Bold : FontStyle.Regular);


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
            _rbTrainingEasy.CheckedChanged    += (_, _) => _form.UpdateSquarePBLabel();
            rbTrainingChallenge.CheckedChanged += (_, _) => _form.UpdateSquarePBLabel();
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
            rbTrainingWhite.CheckedChanged   += (_, _) => _form.UpdateSquarePBLabel();
            _rbTrainingBlack.CheckedChanged  += (_, _) => _form.UpdateSquarePBLabel();
            _rbTrainingRandom.CheckedChanged += (_, _) => _form.UpdateSquarePBLabel();
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
            _btnTrainingStart.Click += (_, _) => _form.TrainingStartRound();
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
            _btnSqMode.Click     += (_, _) => _form.SetTrainingMode(TrainingSubMode.Square);
            _btnOpMode.Click     += (_, _) => _form.SetTrainingMode(TrainingSubMode.Opening);
            _btnPuzzleMode.Click += (_, _) => _form.SetTrainingMode(TrainingSubMode.Puzzle);
            _btnVisionMode.Click += (_, _) => _form.SetTrainingMode(TrainingSubMode.Vision);
            _btnDrillMode.Click  += (_, _) => _form.SetTrainingMode(TrainingSubMode.Drill);
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
                if (_form.config == null) return;
                foreach (var key in new[] { "Easy-White", "Easy-Black", "Easy-Random", "Challenge-White", "Challenge-Black", "Challenge-Random" })
                    _form.config.TrainingPersonalBests.Remove(key);
                _form.config.Save();
                _form.UpdateSquarePBLabel();
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
            btnChoose.Click += (_, _) => _form.SelectOpeningForTraining();
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
            _btnPuzzleSubTraining.Click += (_, _) => _form.SetPuzzleSubMode("training");
            _btnPuzzleSubRush.Click     += (_, _) => _form.SetPuzzleSubMode("rush");
            _btnPuzzleSubGauntlet.Click += (_, _) => _form.SetPuzzleSubMode("gauntlet");
            _btnPuzzleSubDaily.Click    += (_, _) => _form.SetPuzzleSubMode("daily");
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
            _rushTimeBtn1.Click += (_, _) => _form.SelectRushTime(1);
            _rushTimeBtn2.Click += (_, _) => _form.SelectRushTime(2);
            _rushTimeBtn3.Click += (_, _) => _form.SelectRushTime(3);
            _rushTimeBtn4.Click += (_, _) => _form.SelectRushTime(4);
            _rushTimeBtn5.Click += (_, _) => _form.SelectRushTime(5);
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
            foreach (var (label, _) in AnalysisBoardForm._puzzleThemeOptions)
                _cmbPuzzleTheme.Items.Add(label);
            _cmbPuzzleTheme.SelectedIndex = 0;
            _cmbPuzzleTheme.SelectedIndexChanged += (_, _) =>
            {
                int i = _cmbPuzzleTheme.SelectedIndex;
                _form._puzzleThemeFilter = i >= 0 ? AnalysisBoardForm._puzzleThemeOptions[i].Theme : null;
            };
            _pnlThemeFilterRow.Controls.AddRange(new Control[] { _cmbPuzzleTheme, lblThemeFilter });

            // Opening filter row (Training only)
            _pnlOpeningFilterRow = new Panel { Dock = DockStyle.Top, Height = 52, Visible = false };
            var lblOpeningFilter = new Label { Text = "Opening", Font = F(9f, true), Dock = DockStyle.Top, Height = 20 };
            _cmbPuzzleOpening = new ComboBox { Dock = DockStyle.Top, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, Font = F(9f) };
            foreach (var (label, _) in AnalysisBoardForm._puzzleOpeningOptions)
                _cmbPuzzleOpening.Items.Add(label);
            _cmbPuzzleOpening.SelectedIndex = 0;
            _cmbPuzzleOpening.SelectedIndexChanged += (_, _) =>
            {
                int i = _cmbPuzzleOpening.SelectedIndex;
                _form._puzzleOpeningFilter = i >= 0 ? AnalysisBoardForm._puzzleOpeningOptions[i].Opening : null;
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
            _ratingBtnAny.Click    += (_, _) => _form.SelectPuzzleRating(0,    int.MaxValue);
            _ratingBtnBeg.Click    += (_, _) => _form.SelectPuzzleRating(0,    1199);
            _ratingBtnInt.Click    += (_, _) => _form.SelectPuzzleRating(1400, 1799);
            _ratingBtnAdv.Click    += (_, _) => _form.SelectPuzzleRating(1800, 2199);
            _ratingBtnMaster.Click += (_, _) => _form.SelectPuzzleRating(2200, int.MaxValue);
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
                Text = "Auto-next", Checked = _form.config?.PuzzleAutoNext != false,
                Font = F(9f), AutoSize = true, Location = new Point(4, 5)
            };
            _chkPuzzleAutoNext.CheckedChanged += (_, _) =>
            {
                if (_form.config != null) { _form.config.PuzzleAutoNext = _chkPuzzleAutoNext.Checked; _form.config.Save(); }
            };
            _pnlPuzzleAutoNextRow.Controls.Add(_chkPuzzleAutoNext);

            var lnkResetPBs = new LinkLabel
            {
                Text = "Reset personal bests", Dock = DockStyle.Top, Height = 18,
                Font = F(8f), TextAlign = ContentAlignment.MiddleLeft, LinkColor = Color.FromArgb(140, 140, 140)
            };
            lnkResetPBs.LinkClicked += (_, _) =>
            {
                if (_form.config == null) return;
                _form.config.PuzzleRushBest                = 0;
                _form.config.PuzzleRushBestByMinutes.Clear();
                _form.config.PuzzleTrainingBestStreak      = 0;
                _form.config.PuzzleTrainingBestStreakDate   = "";
                _form.config.PuzzleTrainingTotalAttempted   = 0;
                _form.config.PuzzleTrainingTotalClean       = 0;
                _form.config.GauntletBestStreak             = 0;
                _form.config.GauntletBestStreakDate         = "";
                _form.config.GauntletRecentScores.Clear();
                _form.config.VisionBestStreak               = 0;
                _form.config.Save();
                _form._puzzleStreakBest    = 0;
                _form._gauntletBestStreak = 0;
                _form.UpdatePuzzleStats();
                _form.SetPuzzleSubMode(_form._puzzleSubMode);
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
            _btnVisionSubTraining.Click += (_, _) => _form.SetVisionSubMode("training");
            _btnVisionSubTimed.Click    += (_, _) => _form.SetVisionSubMode("timed");
            _btnVisionSubSurvival.Click += (_, _) => _form.SetVisionSubMode("survival");
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
            _visionTimeBtn3.Click  += (_, _) => _form.SelectVisionTime(3);
            _visionTimeBtn5.Click  += (_, _) => _form.SelectVisionTime(5);
            _visionTimeBtn10.Click += (_, _) => _form.SelectVisionTime(10);
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
            _visionGlobalBtn1.Click += (_, _) => _form.SelectVisionGlobalTime(60);
            _visionGlobalBtn3.Click += (_, _) => _form.SelectVisionGlobalTime(180);
            _visionGlobalBtn5.Click += (_, _) => _form.SelectVisionGlobalTime(300);
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
                Text = "Auto-next", Checked = _form.config?.VisionAutoNext != false,
                Font = F(9f), AutoSize = true, Location = new Point(4, 5)
            };
            _chkVisionAutoNext.CheckedChanged += (_, _) =>
            {
                if (_form.config != null) { _form.config.VisionAutoNext = _chkVisionAutoNext.Checked; _form.config.Save(); }
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
                if (_form.config == null) return;
                foreach (var key in new[] { "Vision-Timed-60", "Vision-Timed-180", "Vision-Timed-300", "Vision-Survival" })
                    _form.config.TrainingPersonalBests.Remove(key);
                _form.config.Save();
                _form.UpdateVisionPBLabel();
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
            _btnDrillVsBot.Click += (_, _) => _ = _form.DrillPracticeAsync();
            var pnlDrillVsBotGap = new Panel { Dock = DockStyle.Top, Height = 4 };
            _btnDrillWatchEngines = new Button
            {
                Text = "⚙ Watch engines", Dock = DockStyle.Top, Height = 28,
                Font = F(9f), FlatStyle = FlatStyle.Flat
            };
            _btnDrillWatchEngines.Click += (_, _) =>
            {
                if (_form._drillWatchActive) { _form.matchService?.StopMatch(); return; }
                _ = _form.DrillWatchEnginesAsync();
            };
            var pnlDrillWatchGap = new Panel { Dock = DockStyle.Top, Height = 6 };
            _cmbDrillStudy.SelectedIndexChanged += (_, _) => _form.PopulateDrillChapters();
            _cmbDrillChapter.SelectedIndexChanged += (_, _) =>
            {
                var ch = _form.SelectedDrillChapter();
                _form.SetDrillDescription(ch?.Description ?? "");
                if (ch != null && _form._trainingSubMode == TrainingSubMode.Drill)
                    _form.boardControl.LoadFEN(ch.Fen);
            };
            _cmbDrillChapter.DropDown += (_, _) =>
            {
                _form._drillHoverLastIdx = -1;
                if (_form._drillHoverTimer == null)
                {
                    _form._drillHoverTimer = new System.Windows.Forms.Timer { Interval = 50 };
                    _form._drillHoverTimer.Tick += _form.DrillHoverTimer_Tick;
                }
                _form._drillHoverTimer.Start();
            };
            _cmbDrillChapter.DropDownClosed += (_, _) => _form._drillHoverTimer?.Stop();
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
            _btnOpHint.Click += _form.BtnOpHint_Click;
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
            _btnPuzzleHint.Click += _form.BtnPuzzleHint_Click;
            _btnPuzzleSkip = new Button
            {
                Text = "Skip →", Font = F(9f),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat
            };
            _btnPuzzleSkip.Click += (_, _) => _form.PuzzleSkip();
            _btnPuzzleNext = new Button
            {
                Text = "Next  →", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnPuzzleNext.Click += (_, _) => { if (_btnPuzzleNext != null) _btnPuzzleNext.Visible = false; _ = _form.PuzzleLoadNext(); };
            _btnPuzzleAnalyze = new Button
            {
                Text = "Analyze", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnPuzzleAnalyze.Click += (_, _) =>
            {
                var puzzle = _form._currentPuzzle;
                if (puzzle == null) return;

                // FEN side-to-move is the opponent (makes the first move) — player is the other side
                string[] fenParts = puzzle.Fen.Split(' ');
                bool playerIsBlack = fenParts.Length > 1 && fenParts[1] == "w";

                _form.StopTraining();

                // Set perspective to player's side before loading
                _form.boardControl.IsFlipped = playerIsBlack;

                // Reset to puzzle starting position
                _form.boardControl.LoadFEN(puzzle.Fen);
                _form.moveTree = new MoveTree(puzzle.Fen);
                _form._classificationLookup = null;
                _form._analysisCache.Clear();

                // Replay full solution into the move tree
                foreach (string uci in puzzle.Moves)
                {
                    string san = AnalysisBoardForm.ConvertUciToSan(uci, _form.moveTree.CurrentNode.FEN);
                    _form.isNavigating = true;
                    _form.boardControl.MakeMove(uci);
                    _form.isNavigating = false;
                    _form.moveTree.AddMove(uci, san, _form.boardControl.GetFEN());
                }

                // Position at the player's first move (skip the opponent's opening move)
                _form.moveTree.GoToStart();
                if (puzzle.Moves.Length > 0 && _form.moveTree.GoForward())
                {
                    _form.isNavigating = true;
                    _form.boardControl.LoadFEN(_form.moveTree.CurrentNode.FEN);
                    _form.isNavigating = false;
                }

                _form.isNavigating = true;
                try { _form.UpdateMoveList(); _form.UpdateMoveListSelection(); }
                finally { _form.isNavigating = false; }
                _form.UpdateFenDisplay();
                _form.UpdateTurnLabel();
                _form.UpdateMaterialStrips();

                _ = _form.TriggerAutoAnalysis();
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
            _btnVisionLight.Click += (_, _) => _form.VisionAnswer(true);
            _btnVisionDark.Click  += (_, _) => _form.VisionAnswer(false);
            _btnVisionNext = new Button
            {
                Text = "Next  →", Font = F(9f, true),
                Dock = DockStyle.Top, Height = 28, FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            _btnVisionNext.Click += (_, _) => { if (_btnVisionNext != null) _btnVisionNext.Visible = false; _form.VisionLoadNext(); };
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
                Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter
            };
            var btnTryAgain = new Button
            {
                Text = "Try Again", Font = F(11f, true),
                Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 0)
            };
            btnTryAgain.Click += (_, _) => _form.TrainingShowStartPanel();
            var btnDone = new Button
            {
                Text = "Done", Font = F(10f),
                Dock = DockStyle.Top, Height = 34, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 6, 0, 0)
            };
            btnDone.Click += (_, _) => _form.StopTraining();
            _lblOpMissedMoves = new Label
            {
                Font = F(9f),
                Dock = DockStyle.Top, Height = 48,
                TextAlign = ContentAlignment.TopCenter,
                Visible = false
            };
            _pnlTrainingResult.Controls.AddRange(new Control[]
                { btnDone, btnTryAgain, _lblOpMissedMoves, _lblTrainingPB, _lblTrainingFinalScore, lblComplete });

            Controls.AddRange(new Control[]
                { _pnlTrainingStart, _pnlTrainingGame, _pnlPuzzleGame, _pnlVisionGame, _pnlTrainingResult });

            _form._visionQuestionTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _form._visionQuestionTimer.Tick += _form.VisionQuestionTimer_Tick;
            _form._visionGlobalTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _form._visionGlobalTimer.Tick += _form.VisionGlobalTimer_Tick;

            _form.SetTrainingMode(TrainingSubMode.Square); // initialize Square mode as default
            _form.SetPuzzleSubMode("training"); // initialize puzzle sub-mode
            _form.SelectRushTime(3);            // default 3 min
            _form.SelectPuzzleRating(0, int.MaxValue); // default Any
            _form.SetVisionSubMode("training");
            _form.SelectVisionTime(5);
            _form.SelectVisionGlobalTime(180);
            _form.ApplyTrainingTheme();
        }
    }
}
