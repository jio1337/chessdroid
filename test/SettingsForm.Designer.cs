namespace ChessDroid
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            tabControl1 = new TabControl();
            tabEngine = new TabPage();
            tabAnalysis = new TabPage();
            tabBoard = new TabPage();
            tabEffects = new TabPage();
            tabStyle = new TabPage();
            lblEngine = new Label();
            cmbEngine = new ComboBox();
            lblEngineTimeout = new Label();
            numEngineTimeout = new NumericUpDown();
            lblMaxRetries = new Label();
            numMaxRetries = new NumericUpDown();
            lblMoveTimeout = new Label();
            numMoveTimeout = new NumericUpDown();
            lblEngineDepth = new Label();
            numEngineDepth = new NumericUpDown();
            lblMinAnalysisTime = new Label();
            numMinAnalysisTime = new NumericUpDown();
            chkContinuousAnalysis = new CheckBox();
            lblAutoPlayInterval = new Label();
            numAutoPlayInterval = new NumericUpDown();
            lblSyzygy = new Label();
            txtSyzygyPath = new TextBox();
            btnBrowseSyzygy = new Button();
            lblBest = new Label();
            chkShowBest = new CheckBox();
            lblSecond = new Label();
            chkShowSecond = new CheckBox();
            lblThird = new Label();
            chkShowThird = new CheckBox();
            lblEngineArrows = new Label();
            cmbArrowCount = new ComboBox();
            lblEvalBar = new Label();
            chkEvalBar = new CheckBox();
            lblConsoleFont = new Label();
            btnChooseFont = new Button();
            chkShowExplanations = new CheckBox();
            chkTactical = new CheckBox();
            chkPositional = new CheckBox();
            chkEndgame = new CheckBox();
            chkOpening = new CheckBox();
            chkThreats = new CheckBox();
            chkWDL = new CheckBox();
            chkOpeningName = new CheckBox();
            chkMoveQuality = new CheckBox();
            chkBookMoves = new CheckBox();
            chkPlayStyleEnabled = new CheckBox();
            lblAggressiveness = new Label();
            trkAggressiveness = new TrackBar();
            lblAggressivenessValue = new Label();
            lblLightSquares = new Label();
            btnLightColor = new Button();
            lblDarkSquares = new Label();
            btnDarkColor = new Button();
            lblSquareLabels = new Label();
            chkSquareLabels = new CheckBox();
            lblCoordinates = new Label();
            chkCoordinates = new CheckBox();
            lblLastMoveHighlight = new Label();
            chkLastMoveHighlight = new CheckBox();
            lblShowLegalMoves = new Label();
            chkShowLegalMoves = new CheckBox();
            lblMovementMode = new Label();
            cmbMovementMode = new ComboBox();
            lblMaterialStrips = new Label();
            chkMaterialStrips = new CheckBox();
            lblEvalGraph = new Label();
            chkEvalGraph = new CheckBox();
            lblAnimations = new Label();
            chkAnimations = new CheckBox();
            lblAnimationSpeed = new Label();
            numAnimationMs = new NumericUpDown();
            chkGradient = new CheckBox();
            chkVignette = new CheckBox();
            numVigAlpha = new NumericUpDown();
            chkPieceGlow = new CheckBox();
            chkBoardFrame = new CheckBox();
            btnFrameColor = new Button();
            lblBookArrows = new Label();
            chkBookArrows = new CheckBox();
            lblThreatArrows = new Label();
            chkThreatArrows = new CheckBox();
            chkSoundEffects = new CheckBox();
            lblTheme = new Label();
            cmbTheme = new ComboBox();
            btnSave = new Button();
            btnReset = new Button();
            btnHelp = new Button();
            btnCancel = new Button();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEngineDepth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAnimationMs).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numVigAlpha).BeginInit();
            tabControl1.SuspendLayout();
            tabEngine.SuspendLayout();
            tabAnalysis.SuspendLayout();
            tabBoard.SuspendLayout();
            tabEffects.SuspendLayout();
            tabStyle.SuspendLayout();
            SuspendLayout();

            // tabControl1
            tabControl1.Controls.Add(tabEngine);
            tabControl1.Controls.Add(tabAnalysis);
            tabControl1.Controls.Add(tabBoard);
            tabControl1.Controls.Add(tabEffects);
            tabControl1.Controls.Add(tabStyle);
            tabControl1.Location = new Point(8, 8);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(644, 422);
            tabControl1.TabIndex = 0;

            // tabEngine
            tabEngine.Controls.Add(lblEngine);
            tabEngine.Controls.Add(cmbEngine);
            tabEngine.Controls.Add(lblEngineTimeout);
            tabEngine.Controls.Add(numEngineTimeout);
            tabEngine.Controls.Add(lblMaxRetries);
            tabEngine.Controls.Add(numMaxRetries);
            tabEngine.Controls.Add(lblMoveTimeout);
            tabEngine.Controls.Add(numMoveTimeout);
            tabEngine.Controls.Add(lblEngineDepth);
            tabEngine.Controls.Add(numEngineDepth);
            tabEngine.Controls.Add(lblMinAnalysisTime);
            tabEngine.Controls.Add(numMinAnalysisTime);
            tabEngine.Controls.Add(chkContinuousAnalysis);
            tabEngine.Controls.Add(lblAutoPlayInterval);
            tabEngine.Controls.Add(numAutoPlayInterval);
            tabEngine.Controls.Add(lblSyzygy);
            tabEngine.Controls.Add(txtSyzygyPath);
            tabEngine.Controls.Add(btnBrowseSyzygy);
            tabEngine.Location = new Point(4, 23);
            tabEngine.Name = "tabEngine";
            tabEngine.Padding = new Padding(3);
            tabEngine.Size = new Size(636, 395);
            tabEngine.TabIndex = 0;
            tabEngine.Text = "Engine";
            tabEngine.UseVisualStyleBackColor = true;

            lblEngine.Location = new Point(10, 18);
            lblEngine.Name = "lblEngine";
            lblEngine.Size = new Size(155, 19);
            lblEngine.TabIndex = 0;
            lblEngine.Text = "Chess Engine:";

            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.FormattingEnabled = true;
            cmbEngine.Location = new Point(170, 16);
            cmbEngine.Name = "cmbEngine";
            cmbEngine.Size = new Size(200, 22);
            cmbEngine.TabIndex = 1;
            toolTip1.SetToolTip(cmbEngine, "Select chess engine to use for analysis");

            lblEngineTimeout.Location = new Point(10, 46);
            lblEngineTimeout.Name = "lblEngineTimeout";
            lblEngineTimeout.Size = new Size(155, 19);
            lblEngineTimeout.TabIndex = 2;
            lblEngineTimeout.Text = "Response Timeout (ms):";
            toolTip1.SetToolTip(lblEngineTimeout, "Maximum time to wait for engine response (1000-60000ms).");

            numEngineTimeout.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
            numEngineTimeout.Location = new Point(170, 44);
            numEngineTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numEngineTimeout.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            numEngineTimeout.Name = "numEngineTimeout";
            numEngineTimeout.Size = new Size(120, 20);
            numEngineTimeout.TabIndex = 3;
            numEngineTimeout.Value = new decimal(new int[] { 5000, 0, 0, 0 });

            lblMaxRetries.Location = new Point(10, 72);
            lblMaxRetries.Name = "lblMaxRetries";
            lblMaxRetries.Size = new Size(155, 19);
            lblMaxRetries.TabIndex = 4;
            lblMaxRetries.Text = "Max Retries:";

            numMaxRetries.Location = new Point(170, 70);
            numMaxRetries.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numMaxRetries.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxRetries.Name = "numMaxRetries";
            numMaxRetries.Size = new Size(120, 20);
            numMaxRetries.TabIndex = 5;
            numMaxRetries.Value = new decimal(new int[] { 3, 0, 0, 0 });

            lblMoveTimeout.Location = new Point(10, 98);
            lblMoveTimeout.Name = "lblMoveTimeout";
            lblMoveTimeout.Size = new Size(155, 19);
            lblMoveTimeout.TabIndex = 6;
            lblMoveTimeout.Text = "Move Timeout (ms):";

            numMoveTimeout.Increment = new decimal(new int[] { 5000, 0, 0, 0 });
            numMoveTimeout.Location = new Point(170, 96);
            numMoveTimeout.Maximum = new decimal(new int[] { 120000, 0, 0, 0 });
            numMoveTimeout.Minimum = new decimal(new int[] { 5000, 0, 0, 0 });
            numMoveTimeout.Name = "numMoveTimeout";
            numMoveTimeout.Size = new Size(120, 20);
            numMoveTimeout.TabIndex = 7;
            numMoveTimeout.Value = new decimal(new int[] { 30000, 0, 0, 0 });

            lblEngineDepth.Location = new Point(10, 124);
            lblEngineDepth.Name = "lblEngineDepth";
            lblEngineDepth.Size = new Size(155, 19);
            lblEngineDepth.TabIndex = 8;
            lblEngineDepth.Text = "Engine Depth:";
            toolTip1.SetToolTip(lblEngineDepth, "Analysis depth (1-40). Higher = stronger but slower.");

            numEngineDepth.Location = new Point(170, 122);
            numEngineDepth.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
            numEngineDepth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEngineDepth.Name = "numEngineDepth";
            numEngineDepth.Size = new Size(120, 20);
            numEngineDepth.TabIndex = 9;
            toolTip1.SetToolTip(numEngineDepth, "Analysis depth (1-40). Higher = stronger but slower.");
            numEngineDepth.Value = new decimal(new int[] { 15, 0, 0, 0 });

            lblMinAnalysisTime.Location = new Point(10, 150);
            lblMinAnalysisTime.Name = "lblMinAnalysisTime";
            lblMinAnalysisTime.Size = new Size(155, 19);
            lblMinAnalysisTime.TabIndex = 10;
            lblMinAnalysisTime.Text = "Min Analysis Time (ms):";
            toolTip1.SetToolTip(lblMinAnalysisTime, "Minimum analysis time in ms (0 = no minimum).");

            numMinAnalysisTime.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numMinAnalysisTime.Location = new Point(170, 148);
            numMinAnalysisTime.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numMinAnalysisTime.Name = "numMinAnalysisTime";
            numMinAnalysisTime.Size = new Size(120, 20);
            numMinAnalysisTime.TabIndex = 11;
            toolTip1.SetToolTip(numMinAnalysisTime, "Minimum analysis time in ms (0 = no minimum).");
            numMinAnalysisTime.Value = new decimal(new int[] { 500, 0, 0, 0 });

            chkContinuousAnalysis.Location = new Point(10, 175);
            chkContinuousAnalysis.Name = "chkContinuousAnalysis";
            chkContinuousAnalysis.Size = new Size(280, 18);
            chkContinuousAnalysis.TabIndex = 12;
            chkContinuousAnalysis.Text = "Continuous analysis (go infinite)";
            toolTip1.SetToolTip(chkContinuousAnalysis, "When enabled, engine runs go infinite and updates PV lines live.");

            lblAutoPlayInterval.Location = new Point(10, 202);
            lblAutoPlayInterval.Name = "lblAutoPlayInterval";
            lblAutoPlayInterval.Size = new Size(155, 19);
            lblAutoPlayInterval.TabIndex = 13;
            lblAutoPlayInterval.Text = "Auto-play speed (ms):";
            toolTip1.SetToolTip(lblAutoPlayInterval, "Time between moves during auto-play (200-2000ms).");

            numAutoPlayInterval.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numAutoPlayInterval.Location = new Point(170, 200);
            numAutoPlayInterval.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            numAutoPlayInterval.Minimum = new decimal(new int[] { 200, 0, 0, 0 });
            numAutoPlayInterval.Name = "numAutoPlayInterval";
            numAutoPlayInterval.Size = new Size(120, 20);
            numAutoPlayInterval.TabIndex = 14;
            numAutoPlayInterval.Value = new decimal(new int[] { 600, 0, 0, 0 });

            lblSyzygy.Location = new Point(10, 228);
            lblSyzygy.Name = "lblSyzygy";
            lblSyzygy.Size = new Size(400, 16);
            lblSyzygy.TabIndex = 15;
            lblSyzygy.Text = "Syzygy tablebase path (optional):";
            toolTip1.SetToolTip(lblSyzygy, "Folder containing Syzygy .rtbw/.rtbz files. Leave blank to disable.");

            txtSyzygyPath.Location = new Point(10, 248);
            txtSyzygyPath.Name = "txtSyzygyPath";
            txtSyzygyPath.Size = new Size(380, 20);
            txtSyzygyPath.TabIndex = 16;

            btnBrowseSyzygy.Location = new Point(398, 246);
            btnBrowseSyzygy.Name = "btnBrowseSyzygy";
            btnBrowseSyzygy.Size = new Size(72, 24);
            btnBrowseSyzygy.TabIndex = 17;
            btnBrowseSyzygy.Text = "Browse...";
            btnBrowseSyzygy.Click += BtnBrowseSyzygy_Click;

            // tabAnalysis
            tabAnalysis.Controls.Add(lblBest);
            tabAnalysis.Controls.Add(chkShowBest);
            tabAnalysis.Controls.Add(lblSecond);
            tabAnalysis.Controls.Add(chkShowSecond);
            tabAnalysis.Controls.Add(lblThird);
            tabAnalysis.Controls.Add(chkShowThird);
            tabAnalysis.Controls.Add(lblEngineArrows);
            tabAnalysis.Controls.Add(cmbArrowCount);
            tabAnalysis.Controls.Add(lblEvalBar);
            tabAnalysis.Controls.Add(chkEvalBar);
            tabAnalysis.Controls.Add(lblConsoleFont);
            tabAnalysis.Controls.Add(btnChooseFont);
            tabAnalysis.Controls.Add(chkShowExplanations);
            tabAnalysis.Controls.Add(chkTactical);
            tabAnalysis.Controls.Add(chkPositional);
            tabAnalysis.Controls.Add(chkEndgame);
            tabAnalysis.Controls.Add(chkOpening);
            tabAnalysis.Controls.Add(chkThreats);
            tabAnalysis.Controls.Add(chkWDL);
            tabAnalysis.Controls.Add(chkOpeningName);
            tabAnalysis.Controls.Add(chkMoveQuality);
            tabAnalysis.Controls.Add(chkBookMoves);
            tabAnalysis.Controls.Add(chkPlayStyleEnabled);
            tabAnalysis.Controls.Add(lblAggressiveness);
            tabAnalysis.Controls.Add(trkAggressiveness);
            tabAnalysis.Controls.Add(lblAggressivenessValue);
            tabAnalysis.Location = new Point(4, 23);
            tabAnalysis.Name = "tabAnalysis";
            tabAnalysis.Padding = new Padding(3);
            tabAnalysis.Size = new Size(636, 395);
            tabAnalysis.TabIndex = 1;
            tabAnalysis.Text = "Analysis";
            tabAnalysis.UseVisualStyleBackColor = true;

            // Left column: PV lines / display
            lblBest.Location = new Point(10, 15);
            lblBest.Name = "lblBest";
            lblBest.Size = new Size(155, 19);
            lblBest.TabIndex = 0;
            lblBest.Text = "Show Best Line:";

            chkShowBest.AutoSize = true;
            chkShowBest.Checked = true;
            chkShowBest.CheckState = CheckState.Checked;
            chkShowBest.Location = new Point(214, 17);
            chkShowBest.Name = "chkShowBest";
            chkShowBest.Size = new Size(15, 14);
            chkShowBest.TabIndex = 1;
            toolTip1.SetToolTip(chkShowBest, "Show best move line");
            chkShowBest.UseVisualStyleBackColor = true;

            lblSecond.Location = new Point(10, 39);
            lblSecond.Name = "lblSecond";
            lblSecond.Size = new Size(155, 19);
            lblSecond.TabIndex = 2;
            lblSecond.Text = "Show 2nd Best Line:";

            chkShowSecond.AutoSize = true;
            chkShowSecond.Location = new Point(214, 41);
            chkShowSecond.Name = "chkShowSecond";
            chkShowSecond.Size = new Size(15, 14);
            chkShowSecond.TabIndex = 3;
            toolTip1.SetToolTip(chkShowSecond, "Show 2nd best move line");
            chkShowSecond.UseVisualStyleBackColor = true;

            lblThird.Location = new Point(10, 63);
            lblThird.Name = "lblThird";
            lblThird.Size = new Size(155, 19);
            lblThird.TabIndex = 4;
            lblThird.Text = "Show 3rd Best Line:";

            chkShowThird.AutoSize = true;
            chkShowThird.Location = new Point(214, 65);
            chkShowThird.Name = "chkShowThird";
            chkShowThird.Size = new Size(15, 14);
            chkShowThird.TabIndex = 5;
            toolTip1.SetToolTip(chkShowThird, "Show 3rd best move line");
            chkShowThird.UseVisualStyleBackColor = true;

            lblEngineArrows.Location = new Point(10, 87);
            lblEngineArrows.Name = "lblEngineArrows";
            lblEngineArrows.Size = new Size(155, 19);
            lblEngineArrows.TabIndex = 6;
            lblEngineArrows.Text = "Engine Arrows:";

            cmbArrowCount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbArrowCount.Items.AddRange(new object[] { "None", "Best only", "Best + 2nd", "All 3" });
            cmbArrowCount.Location = new Point(170, 85);
            cmbArrowCount.Name = "cmbArrowCount";
            cmbArrowCount.Size = new Size(130, 22);
            cmbArrowCount.TabIndex = 7;
            toolTip1.SetToolTip(cmbArrowCount, "Number of engine move arrows shown on the board");

            lblEvalBar.Location = new Point(10, 113);
            lblEvalBar.Name = "lblEvalBar";
            lblEvalBar.Size = new Size(155, 19);
            lblEvalBar.TabIndex = 8;
            lblEvalBar.Text = "Show Eval Bar:";

            chkEvalBar.AutoSize = true;
            chkEvalBar.Checked = true;
            chkEvalBar.CheckState = CheckState.Checked;
            chkEvalBar.Location = new Point(214, 115);
            chkEvalBar.Name = "chkEvalBar";
            chkEvalBar.Size = new Size(15, 14);
            chkEvalBar.TabIndex = 9;
            toolTip1.SetToolTip(chkEvalBar, "Show the evaluation bar on the left of the board");
            chkEvalBar.UseVisualStyleBackColor = true;

            lblConsoleFont.Location = new Point(10, 137);
            lblConsoleFont.Name = "lblConsoleFont";
            lblConsoleFont.Size = new Size(104, 19);
            lblConsoleFont.TabIndex = 10;
            lblConsoleFont.Text = "Console Font:";
            toolTip1.SetToolTip(lblConsoleFont, "Font used in analysis output and move list");

            btnChooseFont.FlatStyle = FlatStyle.Flat;
            btnChooseFont.Location = new Point(118, 135);
            btnChooseFont.Name = "btnChooseFont";
            btnChooseFont.Size = new Size(160, 22);
            btnChooseFont.TabIndex = 11;
            btnChooseFont.Text = "Consolas 10pt";
            btnChooseFont.TextAlign = ContentAlignment.MiddleLeft;
            toolTip1.SetToolTip(btnChooseFont, "Click to choose font for analysis output and move list");
            btnChooseFont.Click += BtnChooseFont_Click;

            chkShowExplanations.AutoSize = true;
            chkShowExplanations.Checked = true;
            chkShowExplanations.CheckState = CheckState.Checked;
            chkShowExplanations.Location = new Point(10, 165);
            chkShowExplanations.Name = "chkShowExplanations";
            chkShowExplanations.Size = new Size(220, 18);
            chkShowExplanations.TabIndex = 12;
            chkShowExplanations.Text = "Show Explanations";
            toolTip1.SetToolTip(chkShowExplanations, "When off, fixed-depth analysis shows only the depth header and clickable PV lines.");
            chkShowExplanations.UseVisualStyleBackColor = true;

            // Right column: explanation toggles
            chkTactical.AutoSize = true;
            chkTactical.Checked = true;
            chkTactical.CheckState = CheckState.Checked;
            chkTactical.Location = new Point(330, 15);
            chkTactical.Name = "chkTactical";
            chkTactical.Size = new Size(145, 18);
            chkTactical.TabIndex = 13;
            chkTactical.Text = "Tactical Analysis";
            toolTip1.SetToolTip(chkTactical, "Pins, forks, skewers, sacrifices, overloading, desperado, and other tactical patterns");
            chkTactical.UseVisualStyleBackColor = true;

            chkPositional.AutoSize = true;
            chkPositional.Checked = true;
            chkPositional.CheckState = CheckState.Checked;
            chkPositional.Location = new Point(330, 39);
            chkPositional.Name = "chkPositional";
            chkPositional.Size = new Size(159, 18);
            chkPositional.TabIndex = 14;
            chkPositional.Text = "Positional Analysis";
            toolTip1.SetToolTip(chkPositional, "Pawn structure, piece activity, central control, and king safety");
            chkPositional.UseVisualStyleBackColor = true;

            chkEndgame.AutoSize = true;
            chkEndgame.Checked = true;
            chkEndgame.CheckState = CheckState.Checked;
            chkEndgame.Location = new Point(330, 63);
            chkEndgame.Name = "chkEndgame";
            chkEndgame.Size = new Size(138, 18);
            chkEndgame.TabIndex = 15;
            chkEndgame.Text = "Endgame Analysis";
            toolTip1.SetToolTip(chkEndgame, "King opposition, fortress detection, unstoppable pawns, drawn positions");
            chkEndgame.UseVisualStyleBackColor = true;

            chkOpening.AutoSize = true;
            chkOpening.Checked = true;
            chkOpening.CheckState = CheckState.Checked;
            chkOpening.Location = new Point(330, 87);
            chkOpening.Name = "chkOpening";
            chkOpening.Size = new Size(152, 18);
            chkOpening.TabIndex = 16;
            chkOpening.Text = "Opening Principles";
            toolTip1.SetToolTip(chkOpening, "Center control and piece development advice (first 20 moves)");
            chkOpening.UseVisualStyleBackColor = true;

            chkThreats.AutoSize = true;
            chkThreats.Checked = true;
            chkThreats.CheckState = CheckState.Checked;
            chkThreats.Location = new Point(330, 111);
            chkThreats.Name = "chkThreats";
            chkThreats.Size = new Size(138, 18);
            chkThreats.TabIndex = 17;
            chkThreats.Text = "Threats Analysis";
            toolTip1.SetToolTip(chkThreats, "Show threats created by the best move and opponent threats against your pieces");
            chkThreats.UseVisualStyleBackColor = true;

            chkWDL.AutoSize = true;
            chkWDL.Checked = true;
            chkWDL.CheckState = CheckState.Checked;
            chkWDL.Location = new Point(330, 135);
            chkWDL.Name = "chkWDL";
            chkWDL.Size = new Size(131, 18);
            chkWDL.TabIndex = 18;
            chkWDL.Text = "WDL + Sharpness";
            toolTip1.SetToolTip(chkWDL, "Show Win/Draw/Loss probabilities and position sharpness");
            chkWDL.UseVisualStyleBackColor = true;

            chkOpeningName.AutoSize = true;
            chkOpeningName.Checked = true;
            chkOpeningName.CheckState = CheckState.Checked;
            chkOpeningName.Location = new Point(330, 159);
            chkOpeningName.Name = "chkOpeningName";
            chkOpeningName.Size = new Size(145, 18);
            chkOpeningName.TabIndex = 19;
            chkOpeningName.Text = "Show Opening Name";
            toolTip1.SetToolTip(chkOpeningName, "Display opening name when in known theory (ECO codes)");
            chkOpeningName.UseVisualStyleBackColor = true;

            chkMoveQuality.AutoSize = true;
            chkMoveQuality.Checked = true;
            chkMoveQuality.CheckState = CheckState.Checked;
            chkMoveQuality.Location = new Point(330, 183);
            chkMoveQuality.Name = "chkMoveQuality";
            chkMoveQuality.Size = new Size(229, 18);
            chkMoveQuality.TabIndex = 20;
            chkMoveQuality.Text = "Move Quality (Brilliant/Best)";
            toolTip1.SetToolTip(chkMoveQuality, "Brilliant (!!) detection and quality labels on alternatives");
            chkMoveQuality.UseVisualStyleBackColor = true;

            chkBookMoves.AutoSize = true;
            chkBookMoves.Checked = true;
            chkBookMoves.CheckState = CheckState.Checked;
            chkBookMoves.Location = new Point(330, 207);
            chkBookMoves.Name = "chkBookMoves";
            chkBookMoves.Size = new Size(131, 18);
            chkBookMoves.TabIndex = 21;
            chkBookMoves.Text = "Show Book Moves";
            toolTip1.SetToolTip(chkBookMoves, "Show opening book move suggestions from Polyglot books");
            chkBookMoves.UseVisualStyleBackColor = true;

            // Play style (bottom of Analysis tab)
            chkPlayStyleEnabled.AutoSize = true;
            chkPlayStyleEnabled.Location = new Point(10, 237);
            chkPlayStyleEnabled.Name = "chkPlayStyleEnabled";
            chkPlayStyleEnabled.Size = new Size(90, 18);
            chkPlayStyleEnabled.TabIndex = 22;
            chkPlayStyleEnabled.Text = "Play style";
            toolTip1.SetToolTip(chkPlayStyleEnabled, "Enable play style recommendations based on aggressiveness preference");
            chkPlayStyleEnabled.UseVisualStyleBackColor = true;
            chkPlayStyleEnabled.CheckedChanged += ChkPlayStyleEnabled_CheckedChanged;

            lblAggressiveness.Location = new Point(10, 262);
            lblAggressiveness.Name = "lblAggressiveness";
            lblAggressiveness.Size = new Size(120, 19);
            lblAggressiveness.TabIndex = 23;
            lblAggressiveness.Text = "Aggressiveness:";
            toolTip1.SetToolTip(lblAggressiveness, "0=Very Solid, 50=Balanced, 100=Very Aggressive");

            trkAggressiveness.Location = new Point(10, 282);
            trkAggressiveness.Maximum = 100;
            trkAggressiveness.Name = "trkAggressiveness";
            trkAggressiveness.Size = new Size(610, 45);
            trkAggressiveness.TabIndex = 24;
            trkAggressiveness.TickFrequency = 25;
            toolTip1.SetToolTip(trkAggressiveness, "0=Very Solid (avoid risk), 50=Balanced, 100=Very Aggressive (seek complications)");
            trkAggressiveness.Value = 50;
            trkAggressiveness.Scroll += TrkAggressiveness_Scroll;

            lblAggressivenessValue.Location = new Point(42, 328);
            lblAggressivenessValue.Name = "lblAggressivenessValue";
            lblAggressivenessValue.Size = new Size(200, 19);
            lblAggressivenessValue.TabIndex = 25;
            lblAggressivenessValue.Text = "50 (Balanced)";

            // tabBoard
            tabBoard.Controls.Add(lblLightSquares);
            tabBoard.Controls.Add(btnLightColor);
            tabBoard.Controls.Add(lblDarkSquares);
            tabBoard.Controls.Add(btnDarkColor);
            tabBoard.Controls.Add(lblSquareLabels);
            tabBoard.Controls.Add(chkSquareLabels);
            tabBoard.Controls.Add(lblCoordinates);
            tabBoard.Controls.Add(chkCoordinates);
            tabBoard.Controls.Add(lblLastMoveHighlight);
            tabBoard.Controls.Add(chkLastMoveHighlight);
            tabBoard.Controls.Add(lblShowLegalMoves);
            tabBoard.Controls.Add(chkShowLegalMoves);
            tabBoard.Controls.Add(lblMovementMode);
            tabBoard.Controls.Add(cmbMovementMode);
            tabBoard.Controls.Add(lblMaterialStrips);
            tabBoard.Controls.Add(chkMaterialStrips);
            tabBoard.Controls.Add(lblEvalGraph);
            tabBoard.Controls.Add(chkEvalGraph);
            tabBoard.Location = new Point(4, 23);
            tabBoard.Name = "tabBoard";
            tabBoard.Padding = new Padding(3);
            tabBoard.Size = new Size(636, 395);
            tabBoard.TabIndex = 2;
            tabBoard.Text = "Board";
            tabBoard.UseVisualStyleBackColor = true;

            lblLightSquares.Location = new Point(10, 15);
            lblLightSquares.Name = "lblLightSquares";
            lblLightSquares.Size = new Size(160, 19);
            lblLightSquares.TabIndex = 0;
            lblLightSquares.Text = "Light Squares:";

            btnLightColor.BackColor = Color.FromArgb(240, 217, 181);
            btnLightColor.Cursor = Cursors.Hand;
            btnLightColor.FlatAppearance.BorderColor = Color.Gray;
            btnLightColor.FlatStyle = FlatStyle.Flat;
            btnLightColor.Location = new Point(175, 13);
            btnLightColor.Name = "btnLightColor";
            btnLightColor.Size = new Size(165, 22);
            btnLightColor.TabIndex = 1;
            toolTip1.SetToolTip(btnLightColor, "Click to choose light square color");
            btnLightColor.UseVisualStyleBackColor = false;
            btnLightColor.Click += BtnLightColor_Click;

            lblDarkSquares.Location = new Point(10, 43);
            lblDarkSquares.Name = "lblDarkSquares";
            lblDarkSquares.Size = new Size(160, 19);
            lblDarkSquares.TabIndex = 2;
            lblDarkSquares.Text = "Dark Squares:";

            btnDarkColor.BackColor = Color.FromArgb(181, 136, 99);
            btnDarkColor.Cursor = Cursors.Hand;
            btnDarkColor.FlatAppearance.BorderColor = Color.Gray;
            btnDarkColor.FlatStyle = FlatStyle.Flat;
            btnDarkColor.Location = new Point(175, 41);
            btnDarkColor.Name = "btnDarkColor";
            btnDarkColor.Size = new Size(165, 22);
            btnDarkColor.TabIndex = 3;
            toolTip1.SetToolTip(btnDarkColor, "Click to choose dark square color");
            btnDarkColor.UseVisualStyleBackColor = false;
            btnDarkColor.Click += BtnDarkColor_Click;

            lblSquareLabels.Location = new Point(10, 71);
            lblSquareLabels.Name = "lblSquareLabels";
            lblSquareLabels.Size = new Size(160, 19);
            lblSquareLabels.TabIndex = 4;
            lblSquareLabels.Text = "Square Labels:";
            toolTip1.SetToolTip(lblSquareLabels, "Show square names (e4, d5...) in the center of each square");

            chkSquareLabels.AutoSize = true;
            chkSquareLabels.Location = new Point(210, 73);
            chkSquareLabels.Name = "chkSquareLabels";
            chkSquareLabels.Size = new Size(15, 14);
            chkSquareLabels.TabIndex = 5;
            toolTip1.SetToolTip(chkSquareLabels, "Show square names (e4, d5...) in the center of each square");
            chkSquareLabels.UseVisualStyleBackColor = true;

            lblCoordinates.Location = new Point(10, 95);
            lblCoordinates.Name = "lblCoordinates";
            lblCoordinates.Size = new Size(160, 19);
            lblCoordinates.TabIndex = 6;
            lblCoordinates.Text = "Coordinates:";
            toolTip1.SetToolTip(lblCoordinates, "Show board coordinates (A-H / 1-8)");

            chkCoordinates.AutoSize = true;
            chkCoordinates.Checked = true;
            chkCoordinates.CheckState = CheckState.Checked;
            chkCoordinates.Location = new Point(210, 97);
            chkCoordinates.Name = "chkCoordinates";
            chkCoordinates.Size = new Size(15, 14);
            chkCoordinates.TabIndex = 7;
            toolTip1.SetToolTip(chkCoordinates, "Show board coordinates (A-H / 1-8)");
            chkCoordinates.UseVisualStyleBackColor = true;

            lblLastMoveHighlight.Location = new Point(10, 119);
            lblLastMoveHighlight.Name = "lblLastMoveHighlight";
            lblLastMoveHighlight.Size = new Size(160, 19);
            lblLastMoveHighlight.TabIndex = 8;
            lblLastMoveHighlight.Text = "Move Highlight:";
            toolTip1.SetToolTip(lblLastMoveHighlight, "Highlight the last move's from/to squares on the board");

            chkLastMoveHighlight.AutoSize = true;
            chkLastMoveHighlight.Location = new Point(210, 121);
            chkLastMoveHighlight.Name = "chkLastMoveHighlight";
            chkLastMoveHighlight.Size = new Size(15, 14);
            chkLastMoveHighlight.TabIndex = 9;
            toolTip1.SetToolTip(chkLastMoveHighlight, "Highlight the last move's from/to squares on the board");
            chkLastMoveHighlight.UseVisualStyleBackColor = true;

            lblShowLegalMoves.Location = new Point(10, 143);
            lblShowLegalMoves.Name = "lblShowLegalMoves";
            lblShowLegalMoves.Size = new Size(160, 19);
            lblShowLegalMoves.TabIndex = 10;
            lblShowLegalMoves.Text = "Legal Move Dots:";
            toolTip1.SetToolTip(lblShowLegalMoves, "Show dots/rings on legal destination squares when a piece is selected");

            chkShowLegalMoves.AutoSize = true;
            chkShowLegalMoves.Checked = true;
            chkShowLegalMoves.CheckState = CheckState.Checked;
            chkShowLegalMoves.Location = new Point(210, 145);
            chkShowLegalMoves.Name = "chkShowLegalMoves";
            chkShowLegalMoves.Size = new Size(15, 14);
            chkShowLegalMoves.TabIndex = 11;
            toolTip1.SetToolTip(chkShowLegalMoves, "Show dots/rings on legal destination squares when a piece is selected");
            chkShowLegalMoves.UseVisualStyleBackColor = true;

            lblMovementMode.Location = new Point(10, 167);
            lblMovementMode.Name = "lblMovementMode";
            lblMovementMode.Size = new Size(160, 19);
            lblMovementMode.TabIndex = 12;
            lblMovementMode.Text = "Piece Movement:";
            toolTip1.SetToolTip(lblMovementMode, "How pieces are moved: drag, click origin then destination, or both");

            cmbMovementMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMovementMode.Items.AddRange(new object[] { "Both", "Drag", "Click" });
            cmbMovementMode.Location = new Point(175, 165);
            cmbMovementMode.Name = "cmbMovementMode";
            cmbMovementMode.Size = new Size(120, 22);
            cmbMovementMode.TabIndex = 13;
            toolTip1.SetToolTip(cmbMovementMode, "How pieces are moved: drag, click origin then destination, or both");

            lblMaterialStrips.Location = new Point(10, 195);
            lblMaterialStrips.Name = "lblMaterialStrips";
            lblMaterialStrips.Size = new Size(160, 19);
            lblMaterialStrips.TabIndex = 14;
            lblMaterialStrips.Text = "Material Strips:";
            toolTip1.SetToolTip(lblMaterialStrips, "Show captured pieces and material advantage above/below the board");

            chkMaterialStrips.AutoSize = true;
            chkMaterialStrips.Location = new Point(210, 197);
            chkMaterialStrips.Name = "chkMaterialStrips";
            chkMaterialStrips.Size = new Size(15, 14);
            chkMaterialStrips.TabIndex = 15;
            toolTip1.SetToolTip(chkMaterialStrips, "Show captured pieces and material advantage above/below the board");
            chkMaterialStrips.UseVisualStyleBackColor = true;

            lblEvalGraph.Location = new Point(10, 219);
            lblEvalGraph.Name = "lblEvalGraph";
            lblEvalGraph.Size = new Size(160, 19);
            lblEvalGraph.TabIndex = 16;
            lblEvalGraph.Text = "Eval Graph:";
            toolTip1.SetToolTip(lblEvalGraph, "Show the evaluation graph above the analysis output");

            chkEvalGraph.AutoSize = true;
            chkEvalGraph.Location = new Point(210, 221);
            chkEvalGraph.Name = "chkEvalGraph";
            chkEvalGraph.Size = new Size(15, 14);
            chkEvalGraph.TabIndex = 17;
            toolTip1.SetToolTip(chkEvalGraph, "Show the evaluation graph above the analysis output");
            chkEvalGraph.UseVisualStyleBackColor = true;

            // tabEffects
            tabEffects.Controls.Add(lblAnimations);
            tabEffects.Controls.Add(chkAnimations);
            tabEffects.Controls.Add(lblAnimationSpeed);
            tabEffects.Controls.Add(numAnimationMs);
            tabEffects.Controls.Add(chkGradient);
            tabEffects.Controls.Add(chkVignette);
            tabEffects.Controls.Add(numVigAlpha);
            tabEffects.Controls.Add(chkPieceGlow);
            tabEffects.Controls.Add(chkBoardFrame);
            tabEffects.Controls.Add(btnFrameColor);
            tabEffects.Controls.Add(lblBookArrows);
            tabEffects.Controls.Add(chkBookArrows);
            tabEffects.Controls.Add(lblThreatArrows);
            tabEffects.Controls.Add(chkThreatArrows);
            tabEffects.Controls.Add(chkSoundEffects);
            tabEffects.Location = new Point(4, 23);
            tabEffects.Name = "tabEffects";
            tabEffects.Padding = new Padding(3);
            tabEffects.Size = new Size(636, 395);
            tabEffects.TabIndex = 3;
            tabEffects.Text = "Effects";
            tabEffects.UseVisualStyleBackColor = true;

            lblAnimations.Location = new Point(10, 15);
            lblAnimations.Name = "lblAnimations";
            lblAnimations.Size = new Size(160, 19);
            lblAnimations.TabIndex = 0;
            lblAnimations.Text = "Animations:";
            toolTip1.SetToolTip(lblAnimations, "Enable smooth piece move animations");

            chkAnimations.AutoSize = true;
            chkAnimations.Location = new Point(210, 17);
            chkAnimations.Name = "chkAnimations";
            chkAnimations.Size = new Size(15, 14);
            chkAnimations.TabIndex = 1;
            toolTip1.SetToolTip(chkAnimations, "Enable smooth piece move animations");
            chkAnimations.UseVisualStyleBackColor = true;

            lblAnimationSpeed.Location = new Point(10, 39);
            lblAnimationSpeed.Name = "lblAnimationSpeed";
            lblAnimationSpeed.Size = new Size(160, 19);
            lblAnimationSpeed.TabIndex = 2;
            lblAnimationSpeed.Text = "Speed (ms):";
            toolTip1.SetToolTip(lblAnimationSpeed, "Animation duration in milliseconds (50=very fast, 500=slow)");

            numAnimationMs.Increment = new decimal(new int[] { 25, 0, 0, 0 });
            numAnimationMs.Location = new Point(175, 37);
            numAnimationMs.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            numAnimationMs.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numAnimationMs.Name = "numAnimationMs";
            numAnimationMs.Size = new Size(70, 20);
            numAnimationMs.TabIndex = 3;
            toolTip1.SetToolTip(numAnimationMs, "Animation duration in milliseconds (50=very fast, 500=slow)");
            numAnimationMs.Value = new decimal(new int[] { 150, 0, 0, 0 });

            chkGradient.AutoSize = true;
            chkGradient.Location = new Point(10, 63);
            chkGradient.Name = "chkGradient";
            chkGradient.Size = new Size(117, 18);
            chkGradient.TabIndex = 4;
            chkGradient.Text = "Gradient fill";
            toolTip1.SetToolTip(chkGradient, "Add a soft gradient shading to each square for a 3D feel");
            chkGradient.UseVisualStyleBackColor = true;

            chkVignette.AutoSize = true;
            chkVignette.Location = new Point(10, 87);
            chkVignette.Name = "chkVignette";
            chkVignette.Size = new Size(82, 18);
            chkVignette.TabIndex = 5;
            chkVignette.Text = "Vignette";
            toolTip1.SetToolTip(chkVignette, "Darken the board edges for a dramatic cinematic effect");
            chkVignette.UseVisualStyleBackColor = true;

            numVigAlpha.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numVigAlpha.Location = new Point(175, 85);
            numVigAlpha.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
            numVigAlpha.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numVigAlpha.Name = "numVigAlpha";
            numVigAlpha.Size = new Size(70, 20);
            numVigAlpha.TabIndex = 6;
            toolTip1.SetToolTip(numVigAlpha, "Vignette darkness (10=subtle, 240=very dark)");
            numVigAlpha.Value = new decimal(new int[] { 110, 0, 0, 0 });

            chkPieceGlow.AutoSize = true;
            chkPieceGlow.Location = new Point(10, 111);
            chkPieceGlow.Name = "chkPieceGlow";
            chkPieceGlow.Size = new Size(96, 18);
            chkPieceGlow.TabIndex = 7;
            chkPieceGlow.Text = "Piece glow";
            toolTip1.SetToolTip(chkPieceGlow, "Add a golden/blue glow halo around white/black pieces");
            chkPieceGlow.UseVisualStyleBackColor = true;

            chkBoardFrame.AutoSize = true;
            chkBoardFrame.Location = new Point(10, 135);
            chkBoardFrame.Name = "chkBoardFrame";
            chkBoardFrame.Size = new Size(103, 18);
            chkBoardFrame.TabIndex = 8;
            chkBoardFrame.Text = "Board frame";
            toolTip1.SetToolTip(chkBoardFrame, "Add a border around the board");
            chkBoardFrame.UseVisualStyleBackColor = true;

            btnFrameColor.BackColor = Color.FromArgb(80, 50, 25);
            btnFrameColor.Cursor = Cursors.Hand;
            btnFrameColor.FlatAppearance.BorderColor = Color.Gray;
            btnFrameColor.FlatStyle = FlatStyle.Flat;
            btnFrameColor.Location = new Point(120, 133);
            btnFrameColor.Name = "btnFrameColor";
            btnFrameColor.Size = new Size(140, 22);
            btnFrameColor.TabIndex = 9;
            btnFrameColor.Text = "Frame color";
            toolTip1.SetToolTip(btnFrameColor, "Click to choose the board frame color");
            btnFrameColor.UseVisualStyleBackColor = false;
            btnFrameColor.Click += BtnFrameColor_Click;

            lblBookArrows.Location = new Point(10, 163);
            lblBookArrows.Name = "lblBookArrows";
            lblBookArrows.Size = new Size(160, 19);
            lblBookArrows.TabIndex = 10;
            lblBookArrows.Text = "Book Arrows:";
            toolTip1.SetToolTip(lblBookArrows, "Show opening book move arrows on the board");

            chkBookArrows.AutoSize = true;
            chkBookArrows.Location = new Point(210, 165);
            chkBookArrows.Name = "chkBookArrows";
            chkBookArrows.Size = new Size(15, 14);
            chkBookArrows.TabIndex = 11;
            toolTip1.SetToolTip(chkBookArrows, "Show opening book move arrows on the board");
            chkBookArrows.UseVisualStyleBackColor = true;

            lblThreatArrows.Location = new Point(10, 187);
            lblThreatArrows.Name = "lblThreatArrows";
            lblThreatArrows.Size = new Size(160, 19);
            lblThreatArrows.TabIndex = 12;
            lblThreatArrows.Text = "Threat Arrows:";
            toolTip1.SetToolTip(lblThreatArrows, "Show red arrows warning about opponent threats against your pieces");

            chkThreatArrows.AutoSize = true;
            chkThreatArrows.Location = new Point(210, 189);
            chkThreatArrows.Name = "chkThreatArrows";
            chkThreatArrows.Size = new Size(15, 14);
            chkThreatArrows.TabIndex = 13;
            toolTip1.SetToolTip(chkThreatArrows, "Show red arrows warning about opponent threats against your pieces");
            chkThreatArrows.UseVisualStyleBackColor = true;

            chkSoundEffects.AutoSize = true;
            chkSoundEffects.Location = new Point(10, 213);
            chkSoundEffects.Name = "chkSoundEffects";
            chkSoundEffects.Size = new Size(166, 18);
            chkSoundEffects.TabIndex = 14;
            chkSoundEffects.Text = "Enable sound effects";
            toolTip1.SetToolTip(chkSoundEffects, "Play sounds for piece moves, captures, checks and checkmate");
            chkSoundEffects.UseVisualStyleBackColor = true;

            // tabStyle
            tabStyle.Controls.Add(lblTheme);
            tabStyle.Controls.Add(cmbTheme);
            tabStyle.Location = new Point(4, 23);
            tabStyle.Name = "tabStyle";
            tabStyle.Padding = new Padding(3);
            tabStyle.Size = new Size(636, 395);
            tabStyle.TabIndex = 4;
            tabStyle.Text = "Style";
            tabStyle.UseVisualStyleBackColor = true;

            lblTheme.Font = new Font("Courier New", 10F);
            lblTheme.Location = new Point(10, 18);
            lblTheme.Name = "lblTheme";
            lblTheme.Size = new Size(160, 20);
            lblTheme.TabIndex = 0;
            lblTheme.Text = "chessdroid theme:";
            lblTheme.TextAlign = ContentAlignment.MiddleLeft;

            cmbTheme.BackColor = Color.FromArgb(60, 60, 65);
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Font = new Font("Courier New", 10F);
            cmbTheme.ForeColor = Color.White;
            cmbTheme.Location = new Point(175, 16);
            cmbTheme.Name = "cmbTheme";
            cmbTheme.Size = new Size(160, 24);
            cmbTheme.TabIndex = 1;
            cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;

            // Action buttons (below tab control)
            btnSave.FlatStyle = FlatStyle.Popup;
            btnSave.Font = new Font("Courier New", 14.25F);
            btnSave.ForeColor = SystemColors.ControlLightLight;
            btnSave.Location = new Point(8, 438);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(155, 28);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save && Apply";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnReset.FlatStyle = FlatStyle.Popup;
            btnReset.Font = new Font("Courier New", 14.25F);
            btnReset.ForeColor = SystemColors.ControlLightLight;
            btnReset.Location = new Point(171, 438);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(155, 28);
            btnReset.TabIndex = 2;
            btnReset.Text = "Defaults";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += BtnReset_Click;

            btnHelp.FlatStyle = FlatStyle.Popup;
            btnHelp.Font = new Font("Courier New", 14.25F);
            btnHelp.ForeColor = SystemColors.ControlLightLight;
            btnHelp.Location = new Point(334, 438);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(155, 28);
            btnHelp.TabIndex = 3;
            btnHelp.Text = "Help";
            btnHelp.UseVisualStyleBackColor = true;
            btnHelp.Click += BtnHelp_Click;

            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatStyle = FlatStyle.Popup;
            btnCancel.Font = new Font("Courier New", 14.25F);
            btnCancel.ForeColor = SystemColors.ControlLightLight;
            btnCancel.Location = new Point(497, 438);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(155, 28);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;

            // SettingsForm
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(45, 45, 48);
            CancelButton = btnCancel;
            ClientSize = new Size(660, 474);
            Controls.Add(tabControl1);
            Controls.Add(btnSave);
            Controls.Add(btnReset);
            Controls.Add(btnHelp);
            Controls.Add(btnCancel);
            Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "chessdroid://settings";

            tabEngine.ResumeLayout(false);
            tabEngine.PerformLayout();
            tabAnalysis.ResumeLayout(false);
            tabAnalysis.PerformLayout();
            tabBoard.ResumeLayout(false);
            tabBoard.PerformLayout();
            tabEffects.ResumeLayout(false);
            tabEffects.PerformLayout();
            tabStyle.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEngineDepth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAnimationMs).EndInit();
            ((System.ComponentModel.ISupportInitialize)numVigAlpha).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabEngine;
        private System.Windows.Forms.TabPage tabAnalysis;
        private System.Windows.Forms.TabPage tabBoard;
        private System.Windows.Forms.TabPage tabEffects;
        private System.Windows.Forms.TabPage tabStyle;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.ComboBox cmbEngine;
        private System.Windows.Forms.Label lblEngineTimeout;
        private System.Windows.Forms.NumericUpDown numEngineTimeout;
        private System.Windows.Forms.Label lblMaxRetries;
        private System.Windows.Forms.NumericUpDown numMaxRetries;
        private System.Windows.Forms.Label lblMoveTimeout;
        private System.Windows.Forms.NumericUpDown numMoveTimeout;
        private System.Windows.Forms.Label lblEngineDepth;
        private System.Windows.Forms.NumericUpDown numEngineDepth;
        private System.Windows.Forms.Label lblMinAnalysisTime;
        private System.Windows.Forms.NumericUpDown numMinAnalysisTime;
        private System.Windows.Forms.CheckBox chkContinuousAnalysis;
        private System.Windows.Forms.Label lblAutoPlayInterval;
        private System.Windows.Forms.NumericUpDown numAutoPlayInterval;
        private System.Windows.Forms.Label lblSyzygy;
        private System.Windows.Forms.TextBox txtSyzygyPath;
        private System.Windows.Forms.Button btnBrowseSyzygy;
        private System.Windows.Forms.Label lblBest;
        private System.Windows.Forms.CheckBox chkShowBest;
        private System.Windows.Forms.Label lblSecond;
        private System.Windows.Forms.CheckBox chkShowSecond;
        private System.Windows.Forms.Label lblThird;
        private System.Windows.Forms.CheckBox chkShowThird;
        private System.Windows.Forms.Label lblEngineArrows;
        private System.Windows.Forms.ComboBox cmbArrowCount;
        private System.Windows.Forms.Label lblEvalBar;
        private System.Windows.Forms.CheckBox chkEvalBar;
        private System.Windows.Forms.Label lblConsoleFont;
        private System.Windows.Forms.Button btnChooseFont;
        private System.Windows.Forms.CheckBox chkShowExplanations;
        private System.Windows.Forms.CheckBox chkTactical;
        private System.Windows.Forms.CheckBox chkPositional;
        private System.Windows.Forms.CheckBox chkEndgame;
        private System.Windows.Forms.CheckBox chkOpening;
        private System.Windows.Forms.CheckBox chkThreats;
        private System.Windows.Forms.CheckBox chkWDL;
        private System.Windows.Forms.CheckBox chkOpeningName;
        private System.Windows.Forms.CheckBox chkMoveQuality;
        private System.Windows.Forms.CheckBox chkBookMoves;
        private System.Windows.Forms.CheckBox chkPlayStyleEnabled;
        private System.Windows.Forms.Label lblAggressiveness;
        private System.Windows.Forms.TrackBar trkAggressiveness;
        private System.Windows.Forms.Label lblAggressivenessValue;
        private System.Windows.Forms.Label lblLightSquares;
        private System.Windows.Forms.Button btnLightColor;
        private System.Windows.Forms.Label lblDarkSquares;
        private System.Windows.Forms.Button btnDarkColor;
        private System.Windows.Forms.Label lblSquareLabels;
        private System.Windows.Forms.CheckBox chkSquareLabels;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.CheckBox chkCoordinates;
        private System.Windows.Forms.Label lblLastMoveHighlight;
        private System.Windows.Forms.CheckBox chkLastMoveHighlight;
        private System.Windows.Forms.Label lblShowLegalMoves;
        private System.Windows.Forms.CheckBox chkShowLegalMoves;
        private System.Windows.Forms.Label lblMovementMode;
        private System.Windows.Forms.ComboBox cmbMovementMode;
        private System.Windows.Forms.Label lblMaterialStrips;
        private System.Windows.Forms.CheckBox chkMaterialStrips;
        private System.Windows.Forms.Label lblEvalGraph;
        private System.Windows.Forms.CheckBox chkEvalGraph;
        private System.Windows.Forms.Label lblAnimations;
        private System.Windows.Forms.CheckBox chkAnimations;
        private System.Windows.Forms.Label lblAnimationSpeed;
        private System.Windows.Forms.NumericUpDown numAnimationMs;
        private System.Windows.Forms.CheckBox chkGradient;
        private System.Windows.Forms.CheckBox chkVignette;
        private System.Windows.Forms.NumericUpDown numVigAlpha;
        private System.Windows.Forms.CheckBox chkPieceGlow;
        private System.Windows.Forms.CheckBox chkBoardFrame;
        private System.Windows.Forms.Button btnFrameColor;
        private System.Windows.Forms.Label lblBookArrows;
        private System.Windows.Forms.CheckBox chkBookArrows;
        private System.Windows.Forms.Label lblThreatArrows;
        private System.Windows.Forms.CheckBox chkThreatArrows;
        private System.Windows.Forms.CheckBox chkSoundEffects;
        private System.Windows.Forms.Label lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
