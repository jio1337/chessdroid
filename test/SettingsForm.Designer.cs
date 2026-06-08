namespace ChessDroid
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            grpEngine = new GroupBox();
            lblSyzygy = new Label();
            txtSyzygyPath = new TextBox();
            btnBrowseSyzygy = new Button();
            numEngineDepth = new NumericUpDown();
            numMinAnalysisTime = new NumericUpDown();
            lblMinAnalysisTime = new Label();
            chkContinuousAnalysis = new CheckBox();
            lblAutoPlayInterval = new Label();
            numAutoPlayInterval = new NumericUpDown();
            numMoveTimeout = new NumericUpDown();
            numMaxRetries = new NumericUpDown();
            numEngineTimeout = new NumericUpDown();
            lblEngineDepth = new Label();
            lblMoveTimeout = new Label();
            lblMaxRetries = new Label();
            lblEngineTimeout = new Label();
            grpDisplay = new GroupBox();
            cmbEngine = new ComboBox();
            lblEngine = new Label();
            cmbArrowCount = new ComboBox();
            lblEngineArrows = new Label();
            chkEvalBar = new CheckBox();
            lblEvalBar = new Label();
            chkShowThird = new CheckBox();
            lblThird = new Label();
            chkShowSecond = new CheckBox();
            lblSecond = new Label();
            chkShowBest = new CheckBox();
            lblBookArrows = new Label();
            lblBest = new Label();
            chkBookArrows = new CheckBox();
            lblConsoleFont = new Label();
            btnChooseFont = new Button();
            grpBoardColors = new GroupBox();
            lblLightSquares = new Label();
            btnLightColor = new Button();
            lblDarkSquares = new Label();
            btnDarkColor = new Button();
            lblSquareLabels = new Label();
            chkSquareLabels = new CheckBox();
            lblCoordinates = new Label();
            chkCoordinates = new CheckBox();
            lblThreatArrows = new Label();
            chkThreatArrows = new CheckBox();
            lblLastMoveHighlight = new Label();
            chkLastMoveHighlight = new CheckBox();
            lblEvalGraph = new Label();
            chkEvalGraph = new CheckBox();
            lblAnimations = new Label();
            chkAnimations = new CheckBox();
            lblAnimationSpeed = new Label();
            numAnimationMs = new NumericUpDown();
            lblMaterialStrips = new Label();
            chkMaterialStrips = new CheckBox();
            grpExplanations = new GroupBox();
            chkTactical = new CheckBox();
            chkPositional = new CheckBox();
            chkEndgame = new CheckBox();
            chkOpening = new CheckBox();
            chkThreats = new CheckBox();
            chkWDL = new CheckBox();
            chkOpeningName = new CheckBox();
            chkMoveQuality = new CheckBox();
            chkBookMoves = new CheckBox();
            chkShowExplanations = new CheckBox();
            grpLc0Features = new GroupBox();
            chkPlayStyleEnabled = new CheckBox();
            lblAggressiveness = new Label();
            trkAggressiveness = new TrackBar();
            lblAggressivenessValue = new Label();
            grpBoardEffects = new GroupBox();
            chkGradient = new CheckBox();
            chkVignette = new CheckBox();
            numVigAlpha = new NumericUpDown();
            chkPieceGlow = new CheckBox();
            chkBoardFrame = new CheckBox();
            btnFrameColor = new Button();
            grpSounds = new GroupBox();
            chkSoundEffects = new CheckBox();
            btnSave = new Button();
            btnReset = new Button();
            btnHelp = new Button();
            btnCancel = new Button();
            lblTheme = new Label();
            cmbTheme = new ComboBox();
            toolTip1 = new ToolTip(components);
            grpEngine.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numEngineDepth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).BeginInit();
            grpDisplay.SuspendLayout();
            grpBoardColors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numAnimationMs).BeginInit();
            grpExplanations.SuspendLayout();
            grpLc0Features.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).BeginInit();
            grpBoardEffects.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numVigAlpha).BeginInit();
            grpSounds.SuspendLayout();
            SuspendLayout();
            // 
            // grpEngine
            // 
            grpEngine.Controls.Add(numEngineDepth);
            grpEngine.Controls.Add(numMinAnalysisTime);
            grpEngine.Controls.Add(lblMinAnalysisTime);
            grpEngine.Controls.Add(chkContinuousAnalysis);
            grpEngine.Controls.Add(lblAutoPlayInterval);
            grpEngine.Controls.Add(numAutoPlayInterval);
            grpEngine.Controls.Add(numMoveTimeout);
            grpEngine.Controls.Add(numMaxRetries);
            grpEngine.Controls.Add(numEngineTimeout);
            grpEngine.Controls.Add(lblEngineDepth);
            grpEngine.Controls.Add(lblMoveTimeout);
            grpEngine.Controls.Add(lblMaxRetries);
            grpEngine.Controls.Add(lblEngineTimeout);
            grpEngine.Controls.Add(lblSyzygy);
            grpEngine.Controls.Add(txtSyzygyPath);
            grpEngine.Controls.Add(btnBrowseSyzygy);
            grpEngine.ForeColor = Color.White;
            grpEngine.Location = new Point(12, 9);
            grpEngine.Name = "grpEngine";
            grpEngine.Size = new Size(298, 288);
            grpEngine.TabIndex = 1;
            grpEngine.TabStop = false;
            grpEngine.Text = "Chess Engine";
            // 
            // numEngineDepth
            // 
            numEngineDepth.Location = new Point(182, 117);
            numEngineDepth.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
            numEngineDepth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numEngineDepth.Name = "numEngineDepth";
            numEngineDepth.Size = new Size(100, 20);
            numEngineDepth.TabIndex = 6;
            toolTip1.SetToolTip(numEngineDepth, "Analysis depth (1-40). Higher = stronger but slower.");
            numEngineDepth.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // numMinAnalysisTime
            // 
            numMinAnalysisTime.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numMinAnalysisTime.Location = new Point(182, 150);
            numMinAnalysisTime.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numMinAnalysisTime.Name = "numMinAnalysisTime";
            numMinAnalysisTime.Size = new Size(100, 20);
            numMinAnalysisTime.TabIndex = 8;
            toolTip1.SetToolTip(numMinAnalysisTime, "Minimum analysis time in ms (0 = no minimum). Ensures engine thinks for at least this long.");
            numMinAnalysisTime.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // lblMinAnalysisTime
            // 
            lblMinAnalysisTime.Location = new Point(10, 152);
            lblMinAnalysisTime.Name = "lblMinAnalysisTime";
            lblMinAnalysisTime.Size = new Size(170, 19);
            lblMinAnalysisTime.TabIndex = 7;
            lblMinAnalysisTime.Text = "Min Analysis Time (ms):";
            toolTip1.SetToolTip(lblMinAnalysisTime, "Minimum analysis time in ms (0 = no minimum). Ensures engine thinks for at least this long.");
            // 
            // chkContinuousAnalysis
            // 
            chkContinuousAnalysis.Location = new Point(12, 178);
            chkContinuousAnalysis.Name = "chkContinuousAnalysis";
            chkContinuousAnalysis.Size = new Size(170, 18);
            chkContinuousAnalysis.TabIndex = 9;
            chkContinuousAnalysis.Text = "Continuous analysis";
            toolTip1.SetToolTip(chkContinuousAnalysis, "When enabled, engine runs go infinite and updates PV lines live. When disabled, engine stops at the configured depth and shows full explained output.");
            // 
            // lblAutoPlayInterval
            // 
            lblAutoPlayInterval.Location = new Point(10, 206);
            lblAutoPlayInterval.Name = "lblAutoPlayInterval";
            lblAutoPlayInterval.Size = new Size(171, 15);
            lblAutoPlayInterval.TabIndex = 12;
            lblAutoPlayInterval.Text = "Auto-play speed (ms):";
            toolTip1.SetToolTip(lblAutoPlayInterval, "Time between moves during auto-play (200-2000ms).");
            // 
            // numAutoPlayInterval
            // 
            numAutoPlayInterval.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numAutoPlayInterval.Location = new Point(182, 202);
            numAutoPlayInterval.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            numAutoPlayInterval.Minimum = new decimal(new int[] { 200, 0, 0, 0 });
            numAutoPlayInterval.Name = "numAutoPlayInterval";
            numAutoPlayInterval.Size = new Size(100, 20);
            numAutoPlayInterval.TabIndex = 13;
            toolTip1.SetToolTip(numAutoPlayInterval, "Time between moves during auto-play (200-2000ms).");
            numAutoPlayInterval.Value = new decimal(new int[] { 600, 0, 0, 0 });
            //
            // lblSyzygy
            //
            lblSyzygy.Location = new Point(10, 232);
            lblSyzygy.Name = "lblSyzygy";
            lblSyzygy.Size = new Size(278, 16);
            lblSyzygy.TabIndex = 14;
            lblSyzygy.Text = "Syzygy tablebase path (optional):";
            toolTip1.SetToolTip(lblSyzygy, "Folder containing Syzygy .rtbw/.rtbz files. Stockfish uses these for perfect endgame play. Leave blank to disable.");
            //
            // txtSyzygyPath
            //
            txtSyzygyPath.Location = new Point(10, 251);
            txtSyzygyPath.Name = "txtSyzygyPath";
            txtSyzygyPath.Size = new Size(200, 20);
            txtSyzygyPath.TabIndex = 15;
            toolTip1.SetToolTip(txtSyzygyPath, "Path to your Syzygy tablebase folder. Download .rtbw/.rtbz files from online sources and point here.");
            //
            // btnBrowseSyzygy
            //
            btnBrowseSyzygy.Location = new Point(215, 249);
            btnBrowseSyzygy.Name = "btnBrowseSyzygy";
            btnBrowseSyzygy.Size = new Size(72, 24);
            btnBrowseSyzygy.TabIndex = 16;
            btnBrowseSyzygy.Text = "Browse...";
            btnBrowseSyzygy.Click += BtnBrowseSyzygy_Click;
            //
            // numMoveTimeout
            // 
            numMoveTimeout.Increment = new decimal(new int[] { 5000, 0, 0, 0 });
            numMoveTimeout.Location = new Point(182, 84);
            numMoveTimeout.Maximum = new decimal(new int[] { 120000, 0, 0, 0 });
            numMoveTimeout.Minimum = new decimal(new int[] { 5000, 0, 0, 0 });
            numMoveTimeout.Name = "numMoveTimeout";
            numMoveTimeout.Size = new Size(100, 20);
            numMoveTimeout.TabIndex = 5;
            toolTip1.SetToolTip(numMoveTimeout, "Maximum time to execute a move sequence (5000-120000ms).");
            numMoveTimeout.Value = new decimal(new int[] { 30000, 0, 0, 0 });
            // 
            // numMaxRetries
            // 
            numMaxRetries.Location = new Point(182, 51);
            numMaxRetries.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numMaxRetries.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxRetries.Name = "numMaxRetries";
            numMaxRetries.Size = new Size(100, 20);
            numMaxRetries.TabIndex = 4;
            toolTip1.SetToolTip(numMaxRetries, "Number of retry attempts when engine fails.");
            numMaxRetries.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // numEngineTimeout
            // 
            numEngineTimeout.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
            numEngineTimeout.Location = new Point(182, 19);
            numEngineTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numEngineTimeout.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            numEngineTimeout.Name = "numEngineTimeout";
            numEngineTimeout.Size = new Size(100, 20);
            numEngineTimeout.TabIndex = 3;
            toolTip1.SetToolTip(numEngineTimeout, "Maximum time to wait for engine response (1000-60000ms).");
            numEngineTimeout.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // lblEngineDepth
            // 
            lblEngineDepth.Location = new Point(10, 120);
            lblEngineDepth.Name = "lblEngineDepth";
            lblEngineDepth.Size = new Size(104, 19);
            lblEngineDepth.TabIndex = 3;
            lblEngineDepth.Text = "Engine Depth:";
            toolTip1.SetToolTip(lblEngineDepth, "Analysis depth (1-40). Higher = stronger but slower.");
            // 
            // lblMoveTimeout
            // 
            lblMoveTimeout.Location = new Point(10, 87);
            lblMoveTimeout.Name = "lblMoveTimeout";
            lblMoveTimeout.Size = new Size(200, 19);
            lblMoveTimeout.TabIndex = 2;
            lblMoveTimeout.Text = "Move Timeout (ms):";
            toolTip1.SetToolTip(lblMoveTimeout, "Maximum time to execute a move sequence (5000-120000ms).");
            // 
            // lblMaxRetries
            // 
            lblMaxRetries.Location = new Point(10, 54);
            lblMaxRetries.Name = "lblMaxRetries";
            lblMaxRetries.Size = new Size(200, 19);
            lblMaxRetries.TabIndex = 1;
            lblMaxRetries.Text = "Max Retries:";
            toolTip1.SetToolTip(lblMaxRetries, "Number of retry attempts when engine fails.");
            // 
            // lblEngineTimeout
            // 
            lblEngineTimeout.Location = new Point(10, 21);
            lblEngineTimeout.Name = "lblEngineTimeout";
            lblEngineTimeout.Size = new Size(200, 19);
            lblEngineTimeout.TabIndex = 0;
            lblEngineTimeout.Text = "Response Timeout (ms):";
            toolTip1.SetToolTip(lblEngineTimeout, "Maximum time to wait for engine response (1000-60000ms).");
            // 
            // grpDisplay
            // 
            grpDisplay.Controls.Add(cmbEngine);
            grpDisplay.Controls.Add(lblEngine);
            grpDisplay.Controls.Add(cmbArrowCount);
            grpDisplay.Controls.Add(lblEngineArrows);
            grpDisplay.Controls.Add(chkEvalBar);
            grpDisplay.Controls.Add(lblEvalBar);
            grpDisplay.Controls.Add(chkShowThird);
            grpDisplay.Controls.Add(lblThird);
            grpDisplay.Controls.Add(chkShowSecond);
            grpDisplay.Controls.Add(lblSecond);
            grpDisplay.Controls.Add(chkShowBest);
            grpDisplay.Controls.Add(lblBookArrows);
            grpDisplay.Controls.Add(lblBest);
            grpDisplay.Controls.Add(chkBookArrows);
            grpDisplay.Controls.Add(lblConsoleFont);
            grpDisplay.Controls.Add(btnChooseFont);
            grpDisplay.ForeColor = Color.White;
            grpDisplay.Location = new Point(12, 245);
            grpDisplay.Name = "grpDisplay";
            grpDisplay.Size = new Size(298, 199);
            grpDisplay.TabIndex = 2;
            grpDisplay.TabStop = false;
            grpDisplay.Text = "Display Options";
            // 
            // cmbEngine
            // 
            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.FormattingEnabled = true;
            cmbEngine.Location = new Point(166, 18);
            cmbEngine.Name = "cmbEngine";
            cmbEngine.Size = new Size(116, 22);
            cmbEngine.TabIndex = 7;
            toolTip1.SetToolTip(cmbEngine, "Select chess engine to use for analysis");
            // 
            // lblEngine
            // 
            lblEngine.Location = new Point(10, 21);
            lblEngine.Name = "lblEngine";
            lblEngine.Size = new Size(150, 19);
            lblEngine.TabIndex = 6;
            lblEngine.Text = "Chess Engine:";
            // 
            // cmbArrowCount
            // 
            cmbArrowCount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbArrowCount.Items.AddRange(new object[] { "None", "Best only", "Best + 2nd", "All 3" });
            cmbArrowCount.Location = new Point(166, 102);
            cmbArrowCount.Name = "cmbArrowCount";
            cmbArrowCount.Size = new Size(116, 22);
            cmbArrowCount.TabIndex = 9;
            toolTip1.SetToolTip(cmbArrowCount, "Number of engine move arrows shown on the board (independent of lines in output)");
            // 
            // lblEngineArrows
            // 
            lblEngineArrows.Location = new Point(10, 104);
            lblEngineArrows.Name = "lblEngineArrows";
            lblEngineArrows.Size = new Size(140, 19);
            lblEngineArrows.TabIndex = 8;
            lblEngineArrows.Text = "Engine Arrows:";
            // 
            // chkEvalBar
            // 
            chkEvalBar.AutoSize = true;
            chkEvalBar.Checked = true;
            chkEvalBar.CheckState = CheckState.Checked;
            chkEvalBar.Location = new Point(166, 148);
            chkEvalBar.Name = "chkEvalBar";
            chkEvalBar.Size = new Size(15, 14);
            chkEvalBar.TabIndex = 11;
            toolTip1.SetToolTip(chkEvalBar, "Show the evaluation bar on the left of the board");
            chkEvalBar.UseVisualStyleBackColor = true;
            // 
            // lblEvalBar
            // 
            lblEvalBar.Location = new Point(10, 146);
            lblEvalBar.Name = "lblEvalBar";
            lblEvalBar.Size = new Size(150, 19);
            lblEvalBar.TabIndex = 10;
            lblEvalBar.Text = "Show Eval Bar:";
            // 
            // chkShowThird
            // 
            chkShowThird.AutoSize = true;
            chkShowThird.Location = new Point(166, 83);
            chkShowThird.Name = "chkShowThird";
            chkShowThird.Size = new Size(15, 14);
            chkShowThird.TabIndex = 5;
            toolTip1.SetToolTip(chkShowThird, "Show 3rd best move line");
            chkShowThird.UseVisualStyleBackColor = true;
            // 
            // lblThird
            // 
            lblThird.Location = new Point(10, 82);
            lblThird.Name = "lblThird";
            lblThird.Size = new Size(150, 19);
            lblThird.TabIndex = 4;
            lblThird.Text = "Show 3rd Best Line:";
            // 
            // chkShowSecond
            // 
            chkShowSecond.AutoSize = true;
            chkShowSecond.Location = new Point(166, 65);
            chkShowSecond.Name = "chkShowSecond";
            chkShowSecond.Size = new Size(15, 14);
            chkShowSecond.TabIndex = 3;
            toolTip1.SetToolTip(chkShowSecond, "Show 2nd best move line");
            chkShowSecond.UseVisualStyleBackColor = true;
            // 
            // lblSecond
            // 
            lblSecond.Location = new Point(10, 64);
            lblSecond.Name = "lblSecond";
            lblSecond.Size = new Size(150, 19);
            lblSecond.TabIndex = 2;
            lblSecond.Text = "Show 2nd Best Line:";
            // 
            // chkShowBest
            // 
            chkShowBest.AutoSize = true;
            chkShowBest.Checked = true;
            chkShowBest.CheckState = CheckState.Checked;
            chkShowBest.Location = new Point(166, 47);
            chkShowBest.Name = "chkShowBest";
            chkShowBest.Size = new Size(15, 14);
            chkShowBest.TabIndex = 1;
            toolTip1.SetToolTip(chkShowBest, "Show best move line");
            chkShowBest.UseVisualStyleBackColor = true;
            // 
            // lblBookArrows
            // 
            lblBookArrows.Location = new Point(11, 129);
            lblBookArrows.Name = "lblBookArrows";
            lblBookArrows.Size = new Size(116, 19);
            lblBookArrows.TabIndex = 17;
            lblBookArrows.Text = "Book Arrows:";
            toolTip1.SetToolTip(lblBookArrows, "Show opening book move arrows on the board");
            // 
            // lblBest
            // 
            lblBest.Location = new Point(10, 46);
            lblBest.Name = "lblBest";
            lblBest.Size = new Size(150, 19);
            lblBest.TabIndex = 0;
            lblBest.Text = "Show Best Line:";
            // 
            // chkBookArrows
            // 
            chkBookArrows.AutoSize = true;
            chkBookArrows.Location = new Point(166, 130);
            chkBookArrows.Name = "chkBookArrows";
            chkBookArrows.Size = new Size(15, 14);
            chkBookArrows.TabIndex = 18;
            toolTip1.SetToolTip(chkBookArrows, "Show opening book move arrows on the board");
            chkBookArrows.UseVisualStyleBackColor = true;
            // 
            // lblConsoleFont
            // 
            lblConsoleFont.Location = new Point(10, 168);
            lblConsoleFont.Name = "lblConsoleFont";
            lblConsoleFont.Size = new Size(104, 19);
            lblConsoleFont.TabIndex = 12;
            lblConsoleFont.Text = "Console Font:";
            toolTip1.SetToolTip(lblConsoleFont, "Font used in analysis output and move list");
            // 
            // btnChooseFont
            // 
            btnChooseFont.FlatStyle = FlatStyle.Flat;
            btnChooseFont.Location = new Point(166, 166);
            btnChooseFont.Name = "btnChooseFont";
            btnChooseFont.Size = new Size(116, 22);
            btnChooseFont.TabIndex = 13;
            btnChooseFont.Text = "Consolas 10pt";
            btnChooseFont.TextAlign = ContentAlignment.MiddleLeft;
            toolTip1.SetToolTip(btnChooseFont, "Click to choose font for analysis output and move list");
            btnChooseFont.Click += BtnChooseFont_Click;
            // 
            // grpBoardColors
            // 
            grpBoardColors.Controls.Add(lblLightSquares);
            grpBoardColors.Controls.Add(btnLightColor);
            grpBoardColors.Controls.Add(lblDarkSquares);
            grpBoardColors.Controls.Add(btnDarkColor);
            grpBoardColors.Controls.Add(lblSquareLabels);
            grpBoardColors.Controls.Add(chkSquareLabels);
            grpBoardColors.Controls.Add(lblCoordinates);
            grpBoardColors.Controls.Add(chkCoordinates);
            grpBoardColors.Controls.Add(lblThreatArrows);
            grpBoardColors.Controls.Add(chkThreatArrows);
            grpBoardColors.Controls.Add(lblLastMoveHighlight);
            grpBoardColors.Controls.Add(chkLastMoveHighlight);
            grpBoardColors.Controls.Add(lblEvalGraph);
            grpBoardColors.Controls.Add(chkEvalGraph);
            grpBoardColors.Controls.Add(lblAnimations);
            grpBoardColors.Controls.Add(chkAnimations);
            grpBoardColors.Controls.Add(lblAnimationSpeed);
            grpBoardColors.Controls.Add(numAnimationMs);
            grpBoardColors.Controls.Add(lblMaterialStrips);
            grpBoardColors.Controls.Add(chkMaterialStrips);
            grpBoardColors.ForeColor = Color.White;
            grpBoardColors.Location = new Point(12, 450);
            grpBoardColors.Name = "grpBoardColors";
            grpBoardColors.Size = new Size(298, 240);
            grpBoardColors.TabIndex = 12;
            grpBoardColors.TabStop = false;
            grpBoardColors.Text = "Board Colors";
            // 
            // lblLightSquares
            // 
            lblLightSquares.Location = new Point(10, 22);
            lblLightSquares.Name = "lblLightSquares";
            lblLightSquares.Size = new Size(105, 19);
            lblLightSquares.TabIndex = 0;
            lblLightSquares.Text = "Light Squares:";
            // 
            // btnLightColor
            // 
            btnLightColor.BackColor = Color.FromArgb(240, 217, 181);
            btnLightColor.Cursor = Cursors.Hand;
            btnLightColor.FlatAppearance.BorderColor = Color.Gray;
            btnLightColor.FlatStyle = FlatStyle.Flat;
            btnLightColor.Location = new Point(120, 18);
            btnLightColor.Name = "btnLightColor";
            btnLightColor.Size = new Size(162, 22);
            btnLightColor.TabIndex = 1;
            toolTip1.SetToolTip(btnLightColor, "Click to choose light square color");
            btnLightColor.UseVisualStyleBackColor = false;
            btnLightColor.Click += BtnLightColor_Click;
            // 
            // lblDarkSquares
            // 
            lblDarkSquares.Location = new Point(10, 50);
            lblDarkSquares.Name = "lblDarkSquares";
            lblDarkSquares.Size = new Size(105, 19);
            lblDarkSquares.TabIndex = 2;
            lblDarkSquares.Text = "Dark Squares:";
            // 
            // btnDarkColor
            // 
            btnDarkColor.BackColor = Color.FromArgb(181, 136, 99);
            btnDarkColor.Cursor = Cursors.Hand;
            btnDarkColor.FlatAppearance.BorderColor = Color.Gray;
            btnDarkColor.FlatStyle = FlatStyle.Flat;
            btnDarkColor.Location = new Point(120, 46);
            btnDarkColor.Name = "btnDarkColor";
            btnDarkColor.Size = new Size(162, 22);
            btnDarkColor.TabIndex = 3;
            toolTip1.SetToolTip(btnDarkColor, "Click to choose dark square color");
            btnDarkColor.UseVisualStyleBackColor = false;
            btnDarkColor.Click += BtnDarkColor_Click;
            // 
            // lblSquareLabels
            // 
            lblSquareLabels.Location = new Point(10, 76);
            lblSquareLabels.Name = "lblSquareLabels";
            lblSquareLabels.Size = new Size(116, 19);
            lblSquareLabels.TabIndex = 7;
            lblSquareLabels.Text = "Square Labels:";
            toolTip1.SetToolTip(lblSquareLabels, "Show square names (e4, d5...) in the center of each square");
            // 
            // chkSquareLabels
            // 
            chkSquareLabels.AutoSize = true;
            chkSquareLabels.Location = new Point(166, 78);
            chkSquareLabels.Name = "chkSquareLabels";
            chkSquareLabels.Size = new Size(15, 14);
            chkSquareLabels.TabIndex = 8;
            toolTip1.SetToolTip(chkSquareLabels, "Show square names (e4, d5...) in the center of each square");
            chkSquareLabels.UseVisualStyleBackColor = true;
            // 
            // lblCoordinates
            // 
            lblCoordinates.Location = new Point(10, 215);
            lblCoordinates.Name = "lblCoordinates";
            lblCoordinates.Size = new Size(116, 19);
            lblCoordinates.TabIndex = 30;
            lblCoordinates.Text = "Coordinates:";
            toolTip1.SetToolTip(lblCoordinates, "Show board coordinates (A-H / 1-8)");
            // 
            // chkCoordinates
            // 
            chkCoordinates.AutoSize = true;
            chkCoordinates.Checked = true;
            chkCoordinates.CheckState = CheckState.Checked;
            chkCoordinates.Location = new Point(166, 217);
            chkCoordinates.Name = "chkCoordinates";
            chkCoordinates.Size = new Size(15, 14);
            chkCoordinates.TabIndex = 31;
            toolTip1.SetToolTip(chkCoordinates, "Show board coordinates (A-H / 1-8)");
            chkCoordinates.UseVisualStyleBackColor = true;
            // 
            // lblThreatArrows
            // 
            lblThreatArrows.Location = new Point(10, 95);
            lblThreatArrows.Name = "lblThreatArrows";
            lblThreatArrows.Size = new Size(116, 19);
            lblThreatArrows.TabIndex = 9;
            lblThreatArrows.Text = "Threat Arrows:";
            toolTip1.SetToolTip(lblThreatArrows, "Show red arrows warning about opponent threats against your pieces");
            // 
            // chkThreatArrows
            // 
            chkThreatArrows.AutoSize = true;
            chkThreatArrows.Location = new Point(166, 97);
            chkThreatArrows.Name = "chkThreatArrows";
            chkThreatArrows.Size = new Size(15, 14);
            chkThreatArrows.TabIndex = 10;
            toolTip1.SetToolTip(chkThreatArrows, "Show red arrows warning about opponent threats against your pieces");
            chkThreatArrows.UseVisualStyleBackColor = true;
            // 
            // lblLastMoveHighlight
            // 
            lblLastMoveHighlight.Location = new Point(11, 114);
            lblLastMoveHighlight.Name = "lblLastMoveHighlight";
            lblLastMoveHighlight.Size = new Size(116, 19);
            lblLastMoveHighlight.TabIndex = 19;
            lblLastMoveHighlight.Text = "Move Highlight:";
            toolTip1.SetToolTip(lblLastMoveHighlight, "Highlight the last move's from/to squares on the board");
            // 
            // chkLastMoveHighlight
            // 
            chkLastMoveHighlight.AutoSize = true;
            chkLastMoveHighlight.Location = new Point(166, 116);
            chkLastMoveHighlight.Name = "chkLastMoveHighlight";
            chkLastMoveHighlight.Size = new Size(15, 14);
            chkLastMoveHighlight.TabIndex = 20;
            toolTip1.SetToolTip(chkLastMoveHighlight, "Highlight the last move's from/to squares on the board");
            chkLastMoveHighlight.UseVisualStyleBackColor = true;
            // 
            // lblEvalGraph
            // 
            lblEvalGraph.Location = new Point(11, 133);
            lblEvalGraph.Name = "lblEvalGraph";
            lblEvalGraph.Size = new Size(104, 19);
            lblEvalGraph.TabIndex = 11;
            lblEvalGraph.Text = "Eval Graph:";
            toolTip1.SetToolTip(lblEvalGraph, "Show the evaluation graph above the analysis output");
            // 
            // chkEvalGraph
            // 
            chkEvalGraph.AutoSize = true;
            chkEvalGraph.Location = new Point(166, 136);
            chkEvalGraph.Name = "chkEvalGraph";
            chkEvalGraph.Size = new Size(15, 14);
            chkEvalGraph.TabIndex = 12;
            toolTip1.SetToolTip(chkEvalGraph, "Show the evaluation graph above the analysis output");
            chkEvalGraph.UseVisualStyleBackColor = true;
            // 
            // lblAnimations
            // 
            lblAnimations.Location = new Point(11, 152);
            lblAnimations.Name = "lblAnimations";
            lblAnimations.Size = new Size(86, 19);
            lblAnimations.TabIndex = 13;
            lblAnimations.Text = "Animations:";
            toolTip1.SetToolTip(lblAnimations, "Enable smooth piece move animations");
            // 
            // chkAnimations
            // 
            chkAnimations.AutoSize = true;
            chkAnimations.Location = new Point(166, 155);
            chkAnimations.Name = "chkAnimations";
            chkAnimations.Size = new Size(15, 14);
            chkAnimations.TabIndex = 14;
            toolTip1.SetToolTip(chkAnimations, "Enable smooth piece move animations");
            chkAnimations.UseVisualStyleBackColor = true;
            // 
            // lblAnimationSpeed
            // 
            lblAnimationSpeed.Location = new Point(10, 173);
            lblAnimationSpeed.Name = "lblAnimationSpeed";
            lblAnimationSpeed.Size = new Size(104, 19);
            lblAnimationSpeed.TabIndex = 15;
            lblAnimationSpeed.Text = "Speed (ms):";
            toolTip1.SetToolTip(lblAnimationSpeed, "Animation duration in milliseconds (50 = very fast, 500 = slow)");
            // 
            // numAnimationMs
            // 
            numAnimationMs.Increment = new decimal(new int[] { 25, 0, 0, 0 });
            numAnimationMs.Location = new Point(166, 174);
            numAnimationMs.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            numAnimationMs.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numAnimationMs.Name = "numAnimationMs";
            numAnimationMs.Size = new Size(60, 20);
            numAnimationMs.TabIndex = 16;
            toolTip1.SetToolTip(numAnimationMs, "Animation duration in milliseconds (50 = very fast, 500 = slow)");
            numAnimationMs.Value = new decimal(new int[] { 150, 0, 0, 0 });
            // 
            // lblMaterialStrips
            // 
            lblMaterialStrips.Location = new Point(11, 196);
            lblMaterialStrips.Name = "lblMaterialStrips";
            lblMaterialStrips.Size = new Size(143, 19);
            lblMaterialStrips.TabIndex = 21;
            lblMaterialStrips.Text = "Material Strips:";
            toolTip1.SetToolTip(lblMaterialStrips, "Show captured pieces and material advantage above/below the board");
            // 
            // chkMaterialStrips
            // 
            chkMaterialStrips.AutoSize = true;
            chkMaterialStrips.Location = new Point(166, 199);
            chkMaterialStrips.Name = "chkMaterialStrips";
            chkMaterialStrips.Size = new Size(15, 14);
            chkMaterialStrips.TabIndex = 22;
            toolTip1.SetToolTip(chkMaterialStrips, "Show captured pieces and material advantage above/below the board");
            chkMaterialStrips.UseVisualStyleBackColor = true;
            // 
            // grpExplanations
            // 
            grpExplanations.Controls.Add(chkTactical);
            grpExplanations.Controls.Add(chkPositional);
            grpExplanations.Controls.Add(chkEndgame);
            grpExplanations.Controls.Add(chkOpening);
            grpExplanations.Controls.Add(chkThreats);
            grpExplanations.Controls.Add(chkWDL);
            grpExplanations.Controls.Add(chkOpeningName);
            grpExplanations.Controls.Add(chkMoveQuality);
            grpExplanations.Controls.Add(chkBookMoves);
            grpExplanations.Controls.Add(chkShowExplanations);
            grpExplanations.ForeColor = Color.White;
            grpExplanations.Location = new Point(316, 9);
            grpExplanations.Name = "grpExplanations";
            grpExplanations.Size = new Size(255, 268);
            grpExplanations.TabIndex = 3;
            grpExplanations.TabStop = false;
            grpExplanations.Text = "Explanation Settings";
            // 
            // chkTactical
            // 
            chkTactical.AutoSize = true;
            chkTactical.Checked = true;
            chkTactical.CheckState = CheckState.Checked;
            chkTactical.ForeColor = Color.White;
            chkTactical.Location = new Point(10, 21);
            chkTactical.Name = "chkTactical";
            chkTactical.Size = new Size(145, 18);
            chkTactical.TabIndex = 1;
            chkTactical.Text = "Tactical Analysis";
            toolTip1.SetToolTip(chkTactical, "Pins, forks, skewers, sacrifices, overloading, desperado, and other tactical patterns");
            chkTactical.UseVisualStyleBackColor = true;
            // 
            // chkPositional
            // 
            chkPositional.AutoSize = true;
            chkPositional.Checked = true;
            chkPositional.CheckState = CheckState.Checked;
            chkPositional.ForeColor = Color.White;
            chkPositional.Location = new Point(10, 46);
            chkPositional.Name = "chkPositional";
            chkPositional.Size = new Size(159, 18);
            chkPositional.TabIndex = 2;
            chkPositional.Text = "Positional Analysis";
            toolTip1.SetToolTip(chkPositional, "Pawn structure, piece activity, central control, and king safety");
            chkPositional.UseVisualStyleBackColor = true;
            // 
            // chkEndgame
            // 
            chkEndgame.AutoSize = true;
            chkEndgame.Checked = true;
            chkEndgame.CheckState = CheckState.Checked;
            chkEndgame.ForeColor = Color.White;
            chkEndgame.Location = new Point(10, 71);
            chkEndgame.Name = "chkEndgame";
            chkEndgame.Size = new Size(138, 18);
            chkEndgame.TabIndex = 3;
            chkEndgame.Text = "Endgame Analysis";
            toolTip1.SetToolTip(chkEndgame, "King opposition, fortress detection, unstoppable pawns, drawn positions, and endgame type identification");
            chkEndgame.UseVisualStyleBackColor = true;
            // 
            // chkOpening
            // 
            chkOpening.AutoSize = true;
            chkOpening.Checked = true;
            chkOpening.CheckState = CheckState.Checked;
            chkOpening.ForeColor = Color.White;
            chkOpening.Location = new Point(10, 96);
            chkOpening.Name = "chkOpening";
            chkOpening.Size = new Size(152, 18);
            chkOpening.TabIndex = 4;
            chkOpening.Text = "Opening Principles";
            toolTip1.SetToolTip(chkOpening, "Center control and piece development advice (active for the first 20 moves)");
            chkOpening.UseVisualStyleBackColor = true;
            // 
            // chkThreats
            // 
            chkThreats.AutoSize = true;
            chkThreats.Checked = true;
            chkThreats.CheckState = CheckState.Checked;
            chkThreats.ForeColor = Color.White;
            chkThreats.Location = new Point(10, 121);
            chkThreats.Name = "chkThreats";
            chkThreats.Size = new Size(138, 18);
            chkThreats.TabIndex = 9;
            chkThreats.Text = "Threats Analysis";
            toolTip1.SetToolTip(chkThreats, "Show threats created by the best move (⚔) and opponent threats against your pieces (🛡 defenses)");
            chkThreats.UseVisualStyleBackColor = true;
            // 
            // chkWDL
            // 
            chkWDL.AutoSize = true;
            chkWDL.Checked = true;
            chkWDL.CheckState = CheckState.Checked;
            chkWDL.ForeColor = Color.White;
            chkWDL.Location = new Point(10, 146);
            chkWDL.Name = "chkWDL";
            chkWDL.Size = new Size(131, 18);
            chkWDL.TabIndex = 10;
            chkWDL.Text = "WDL + Sharpness";
            toolTip1.SetToolTip(chkWDL, "Show Win/Draw/Loss probabilities and position sharpness (Lc0-inspired)");
            chkWDL.UseVisualStyleBackColor = true;
            // 
            // chkOpeningName
            // 
            chkOpeningName.AutoSize = true;
            chkOpeningName.Checked = true;
            chkOpeningName.CheckState = CheckState.Checked;
            chkOpeningName.ForeColor = Color.White;
            chkOpeningName.Location = new Point(10, 171);
            chkOpeningName.Name = "chkOpeningName";
            chkOpeningName.Size = new Size(145, 18);
            chkOpeningName.TabIndex = 11;
            chkOpeningName.Text = "Show Opening Name";
            toolTip1.SetToolTip(chkOpeningName, "Display opening name when in known theory (ECO codes)");
            chkOpeningName.UseVisualStyleBackColor = true;
            // 
            // chkMoveQuality
            // 
            chkMoveQuality.AutoSize = true;
            chkMoveQuality.Checked = true;
            chkMoveQuality.CheckState = CheckState.Checked;
            chkMoveQuality.ForeColor = Color.White;
            chkMoveQuality.Location = new Point(10, 196);
            chkMoveQuality.Name = "chkMoveQuality";
            chkMoveQuality.Size = new Size(229, 18);
            chkMoveQuality.TabIndex = 12;
            chkMoveQuality.Text = "Move Quality (Brilliant/Best)";
            toolTip1.SetToolTip(chkMoveQuality, "Brilliant (!!) detection, ⚡ only winning move indicator, and quality labels on alternatives (Inaccuracy, Mistake, Blunder)");
            chkMoveQuality.UseVisualStyleBackColor = true;
            // 
            // chkBookMoves
            // 
            chkBookMoves.AutoSize = true;
            chkBookMoves.Checked = true;
            chkBookMoves.CheckState = CheckState.Checked;
            chkBookMoves.ForeColor = Color.White;
            chkBookMoves.Location = new Point(10, 221);
            chkBookMoves.Name = "chkBookMoves";
            chkBookMoves.Size = new Size(131, 18);
            chkBookMoves.TabIndex = 13;
            chkBookMoves.Text = "Show Book Moves";
            toolTip1.SetToolTip(chkBookMoves, "Show opening book move suggestions from Polyglot books");
            chkBookMoves.UseVisualStyleBackColor = true;
            // 
            // chkShowExplanations
            // 
            chkShowExplanations.AutoSize = true;
            chkShowExplanations.Checked = true;
            chkShowExplanations.CheckState = CheckState.Checked;
            chkShowExplanations.ForeColor = Color.White;
            chkShowExplanations.Location = new Point(10, 243);
            chkShowExplanations.Name = "chkShowExplanations";
            chkShowExplanations.Size = new Size(145, 18);
            chkShowExplanations.TabIndex = 14;
            chkShowExplanations.Text = "Show Explanations";
            toolTip1.SetToolTip(chkShowExplanations, "When off, fixed-depth analysis shows only the depth header and clickable PV lines — no tactical/positional explanation text. Faster rendering.");
            chkShowExplanations.UseVisualStyleBackColor = true;
            // 
            // grpLc0Features
            // 
            grpLc0Features.Controls.Add(chkPlayStyleEnabled);
            grpLc0Features.Controls.Add(lblAggressiveness);
            grpLc0Features.Controls.Add(trkAggressiveness);
            grpLc0Features.Controls.Add(lblAggressivenessValue);
            grpLc0Features.ForeColor = Color.White;
            grpLc0Features.Location = new Point(316, 279);
            grpLc0Features.Name = "grpLc0Features";
            grpLc0Features.Size = new Size(255, 146);
            grpLc0Features.TabIndex = 9;
            grpLc0Features.TabStop = false;
            grpLc0Features.Text = "Play Style";
            // 
            // chkPlayStyleEnabled
            // 
            chkPlayStyleEnabled.AutoSize = true;
            chkPlayStyleEnabled.Location = new Point(10, 23);
            chkPlayStyleEnabled.Name = "chkPlayStyleEnabled";
            chkPlayStyleEnabled.Size = new Size(75, 18);
            chkPlayStyleEnabled.TabIndex = 0;
            chkPlayStyleEnabled.Text = "Enabled";
            toolTip1.SetToolTip(chkPlayStyleEnabled, "Enable play style recommendations based on aggressiveness preference");
            chkPlayStyleEnabled.UseVisualStyleBackColor = true;
            chkPlayStyleEnabled.CheckedChanged += ChkPlayStyleEnabled_CheckedChanged;
            // 
            // lblAggressiveness
            // 
            lblAggressiveness.Location = new Point(10, 47);
            lblAggressiveness.Name = "lblAggressiveness";
            lblAggressiveness.Size = new Size(100, 19);
            lblAggressiveness.TabIndex = 1;
            lblAggressiveness.Text = "Choose:";
            toolTip1.SetToolTip(lblAggressiveness, "0=Very Solid (avoid risk), 50=Balanced, 100=Very Aggressive (seek complications)");
            // 
            // trkAggressiveness
            // 
            trkAggressiveness.Location = new Point(10, 66);
            trkAggressiveness.Maximum = 100;
            trkAggressiveness.Name = "trkAggressiveness";
            trkAggressiveness.Size = new Size(229, 45);
            trkAggressiveness.TabIndex = 2;
            trkAggressiveness.TickFrequency = 25;
            toolTip1.SetToolTip(trkAggressiveness, "0=Very Solid (avoid risk), 50=Balanced, 100=Very Aggressive (seek complications)");
            trkAggressiveness.Value = 50;
            trkAggressiveness.Scroll += TrkAggressiveness_Scroll;
            // 
            // lblAggressivenessValue
            // 
            lblAggressivenessValue.Location = new Point(42, 108);
            lblAggressivenessValue.Name = "lblAggressivenessValue";
            lblAggressivenessValue.Size = new Size(191, 19);
            lblAggressivenessValue.TabIndex = 3;
            lblAggressivenessValue.Text = "50 (Balanced)";
            // 
            // grpBoardEffects
            // 
            grpBoardEffects.Controls.Add(chkGradient);
            grpBoardEffects.Controls.Add(chkVignette);
            grpBoardEffects.Controls.Add(numVigAlpha);
            grpBoardEffects.Controls.Add(chkPieceGlow);
            grpBoardEffects.Controls.Add(chkBoardFrame);
            grpBoardEffects.Controls.Add(btnFrameColor);
            grpBoardEffects.ForeColor = Color.White;
            grpBoardEffects.Location = new Point(316, 429);
            grpBoardEffects.Name = "grpBoardEffects";
            grpBoardEffects.Size = new Size(255, 108);
            grpBoardEffects.TabIndex = 14;
            grpBoardEffects.TabStop = false;
            grpBoardEffects.Text = "Board Effects";
            // 
            // chkGradient
            // 
            chkGradient.AutoSize = true;
            chkGradient.ForeColor = Color.White;
            chkGradient.Location = new Point(10, 19);
            chkGradient.Name = "chkGradient";
            chkGradient.Size = new Size(117, 18);
            chkGradient.TabIndex = 0;
            chkGradient.Text = "Gradient fill";
            toolTip1.SetToolTip(chkGradient, "Add a soft gradient shading to each square for a 3D feel");
            chkGradient.UseVisualStyleBackColor = true;
            // 
            // chkVignette
            // 
            chkVignette.AutoSize = true;
            chkVignette.ForeColor = Color.White;
            chkVignette.Location = new Point(10, 40);
            chkVignette.Name = "chkVignette";
            chkVignette.Size = new Size(82, 18);
            chkVignette.TabIndex = 1;
            chkVignette.Text = "Vignette";
            toolTip1.SetToolTip(chkVignette, "Darken the board edges for a dramatic cinematic effect");
            chkVignette.UseVisualStyleBackColor = true;
            // 
            // numVigAlpha
            // 
            numVigAlpha.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numVigAlpha.Location = new Point(150, 38);
            numVigAlpha.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
            numVigAlpha.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numVigAlpha.Name = "numVigAlpha";
            numVigAlpha.Size = new Size(50, 20);
            numVigAlpha.TabIndex = 5;
            toolTip1.SetToolTip(numVigAlpha, "Vignette darkness (10=subtle, 240=very dark)");
            numVigAlpha.Value = new decimal(new int[] { 110, 0, 0, 0 });
            // 
            // chkPieceGlow
            // 
            chkPieceGlow.AutoSize = true;
            chkPieceGlow.ForeColor = Color.White;
            chkPieceGlow.Location = new Point(10, 61);
            chkPieceGlow.Name = "chkPieceGlow";
            chkPieceGlow.Size = new Size(96, 18);
            chkPieceGlow.TabIndex = 2;
            chkPieceGlow.Text = "Piece glow";
            toolTip1.SetToolTip(chkPieceGlow, "Add a golden/blue glow halo around white/black pieces");
            chkPieceGlow.UseVisualStyleBackColor = true;
            // 
            // chkBoardFrame
            // 
            chkBoardFrame.AutoSize = true;
            chkBoardFrame.ForeColor = Color.White;
            chkBoardFrame.Location = new Point(10, 82);
            chkBoardFrame.Name = "chkBoardFrame";
            chkBoardFrame.Size = new Size(103, 18);
            chkBoardFrame.TabIndex = 3;
            chkBoardFrame.Text = "Board frame";
            toolTip1.SetToolTip(chkBoardFrame, "Add a border around the board");
            chkBoardFrame.UseVisualStyleBackColor = true;
            // 
            // btnFrameColor
            // 
            btnFrameColor.BackColor = Color.FromArgb(80, 50, 25);
            btnFrameColor.Cursor = Cursors.Hand;
            btnFrameColor.FlatAppearance.BorderColor = Color.Gray;
            btnFrameColor.FlatStyle = FlatStyle.Flat;
            btnFrameColor.Location = new Point(115, 78);
            btnFrameColor.Name = "btnFrameColor";
            btnFrameColor.Size = new Size(128, 22);
            btnFrameColor.TabIndex = 6;
            btnFrameColor.Text = "Frame color";
            toolTip1.SetToolTip(btnFrameColor, "Click to choose the board frame color");
            btnFrameColor.UseVisualStyleBackColor = false;
            btnFrameColor.Click += BtnFrameColor_Click;
            // 
            // grpSounds
            // 
            grpSounds.Controls.Add(chkSoundEffects);
            grpSounds.ForeColor = Color.White;
            grpSounds.Location = new Point(316, 542);
            grpSounds.Name = "grpSounds";
            grpSounds.Size = new Size(255, 45);
            grpSounds.TabIndex = 15;
            grpSounds.TabStop = false;
            grpSounds.Text = "Sound Effects";
            // 
            // chkSoundEffects
            // 
            chkSoundEffects.AutoSize = true;
            chkSoundEffects.ForeColor = Color.White;
            chkSoundEffects.Location = new Point(10, 19);
            chkSoundEffects.Name = "chkSoundEffects";
            chkSoundEffects.Size = new Size(166, 18);
            chkSoundEffects.TabIndex = 0;
            chkSoundEffects.Text = "Enable sound effects";
            toolTip1.SetToolTip(chkSoundEffects, "Play sounds for piece moves, captures, checks and checkmate");
            chkSoundEffects.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.FlatStyle = FlatStyle.Popup;
            btnSave.Font = new Font("Courier New", 14.25F);
            btnSave.ForeColor = SystemColors.ControlLightLight;
            btnSave.Location = new Point(316, 590);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(255, 29);
            btnSave.TabIndex = 4;
            btnSave.Text = "Save && Apply";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnReset
            // 
            btnReset.FlatStyle = FlatStyle.Popup;
            btnReset.Font = new Font("Courier New", 14.25F);
            btnReset.ForeColor = SystemColors.ControlLightLight;
            btnReset.Location = new Point(316, 625);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(255, 28);
            btnReset.TabIndex = 5;
            btnReset.Text = "Defaults";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += BtnReset_Click;
            // 
            // btnHelp
            // 
            btnHelp.FlatStyle = FlatStyle.Popup;
            btnHelp.Font = new Font("Courier New", 14.25F);
            btnHelp.ForeColor = SystemColors.ControlLightLight;
            btnHelp.Location = new Point(316, 659);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(255, 28);
            btnHelp.TabIndex = 6;
            btnHelp.Text = "Help";
            btnHelp.UseVisualStyleBackColor = true;
            btnHelp.Click += BtnHelp_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatStyle = FlatStyle.Popup;
            btnCancel.Font = new Font("Courier New", 14.25F);
            btnCancel.ForeColor = SystemColors.ControlLightLight;
            btnCancel.Location = new Point(317, 693);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(255, 28);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // lblTheme
            // 
            lblTheme.Font = new Font("Courier New", 10F);
            lblTheme.ForeColor = Color.White;
            lblTheme.Location = new Point(17, 697);
            lblTheme.Name = "lblTheme";
            lblTheme.Size = new Size(150, 20);
            lblTheme.TabIndex = 0;
            lblTheme.Text = "chessdroid theme:";
            lblTheme.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbTheme
            // 
            cmbTheme.BackColor = Color.FromArgb(60, 60, 65);
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Font = new Font("Courier New", 10F);
            cmbTheme.ForeColor = Color.White;
            cmbTheme.Location = new Point(173, 696);
            cmbTheme.Name = "cmbTheme";
            cmbTheme.Size = new Size(137, 24);
            cmbTheme.TabIndex = 8;
            cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;
            // 
            // SettingsForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(45, 45, 48);
            CancelButton = btnCancel;
            ClientSize = new Size(585, 731);
            Controls.Add(lblTheme);
            Controls.Add(cmbTheme);
            Controls.Add(btnCancel);
            Controls.Add(btnHelp);
            Controls.Add(btnReset);
            Controls.Add(btnSave);
            Controls.Add(grpSounds);
            Controls.Add(grpBoardEffects);
            Controls.Add(grpLc0Features);
            Controls.Add(grpExplanations);
            Controls.Add(grpBoardColors);
            Controls.Add(grpDisplay);
            Controls.Add(grpEngine);
            Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "chessdroid://settings";
            grpEngine.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numEngineDepth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).EndInit();
            grpDisplay.ResumeLayout(false);
            grpDisplay.PerformLayout();
            grpBoardColors.ResumeLayout(false);
            grpBoardColors.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numAnimationMs).EndInit();
            grpExplanations.ResumeLayout(false);
            grpExplanations.PerformLayout();
            grpLc0Features.ResumeLayout(false);
            grpLc0Features.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).EndInit();
            grpBoardEffects.ResumeLayout(false);
            grpBoardEffects.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numVigAlpha).EndInit();
            grpSounds.ResumeLayout(false);
            grpSounds.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpEngine;
        private System.Windows.Forms.Label lblSyzygy;
        private System.Windows.Forms.TextBox txtSyzygyPath;
        private System.Windows.Forms.Button btnBrowseSyzygy;
        private System.Windows.Forms.NumericUpDown numEngineDepth;
        private System.Windows.Forms.NumericUpDown numMinAnalysisTime;
        private System.Windows.Forms.Label lblMinAnalysisTime;
        private System.Windows.Forms.NumericUpDown numMoveTimeout;
        private System.Windows.Forms.NumericUpDown numMaxRetries;
        private System.Windows.Forms.NumericUpDown numEngineTimeout;
        private System.Windows.Forms.Label lblEngineDepth;
        private System.Windows.Forms.Label lblMoveTimeout;
        private System.Windows.Forms.Label lblMaxRetries;
        private System.Windows.Forms.Label lblEngineTimeout;
        private System.Windows.Forms.GroupBox grpDisplay;
        private System.Windows.Forms.ComboBox cmbEngine;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.CheckBox chkShowThird;
        private System.Windows.Forms.Label lblThird;
        private System.Windows.Forms.CheckBox chkShowSecond;
        private System.Windows.Forms.Label lblSecond;
        private System.Windows.Forms.CheckBox chkShowBest;
        private System.Windows.Forms.Label lblBest;
        private System.Windows.Forms.ComboBox cmbArrowCount;
        private System.Windows.Forms.Label lblEngineArrows;
        private System.Windows.Forms.CheckBox chkEvalBar;
        private System.Windows.Forms.Label lblEvalBar;
        private System.Windows.Forms.Label lblConsoleFont;
        private System.Windows.Forms.Button btnChooseFont;
        private System.Windows.Forms.GroupBox grpBoardColors;
        private System.Windows.Forms.Label lblLightSquares;
        private System.Windows.Forms.Button btnLightColor;
        private System.Windows.Forms.Label lblDarkSquares;
        private System.Windows.Forms.Button btnDarkColor;
        private System.Windows.Forms.Label lblSquareLabels;
        private System.Windows.Forms.CheckBox chkSquareLabels;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.CheckBox chkCoordinates;
        private System.Windows.Forms.Label lblThreatArrows;
        private System.Windows.Forms.CheckBox chkThreatArrows;
        private System.Windows.Forms.Label lblLastMoveHighlight;
        private System.Windows.Forms.CheckBox chkLastMoveHighlight;
        private System.Windows.Forms.Label lblBookArrows;
        private System.Windows.Forms.CheckBox chkBookArrows;
        private System.Windows.Forms.Label lblEvalGraph;
        private System.Windows.Forms.CheckBox chkEvalGraph;
        private System.Windows.Forms.Label lblAnimations;
        private System.Windows.Forms.CheckBox chkAnimations;
        private System.Windows.Forms.Label lblAnimationSpeed;
        private System.Windows.Forms.NumericUpDown numAnimationMs;
        private System.Windows.Forms.Label lblMaterialStrips;
        private System.Windows.Forms.CheckBox chkMaterialStrips;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox grpExplanations;
        private System.Windows.Forms.CheckBox chkTactical;
        private System.Windows.Forms.CheckBox chkPositional;
        private System.Windows.Forms.CheckBox chkEndgame;
        private System.Windows.Forms.CheckBox chkOpening;
        private System.Windows.Forms.CheckBox chkThreats;
        private System.Windows.Forms.CheckBox chkWDL;
        private System.Windows.Forms.GroupBox grpLc0Features;
        private System.Windows.Forms.CheckBox chkPlayStyleEnabled;
        private System.Windows.Forms.Label lblAggressiveness;
        private System.Windows.Forms.TrackBar trkAggressiveness;
        private System.Windows.Forms.Label lblAggressivenessValue;
        private System.Windows.Forms.CheckBox chkOpeningName;
        private System.Windows.Forms.CheckBox chkMoveQuality;
        private System.Windows.Forms.CheckBox chkBookMoves;
        private System.Windows.Forms.CheckBox chkShowExplanations;
        private System.Windows.Forms.CheckBox chkContinuousAnalysis;

        private System.Windows.Forms.Label lblAutoPlayInterval;
        private System.Windows.Forms.NumericUpDown numAutoPlayInterval;
        private System.Windows.Forms.GroupBox grpBoardEffects;
        private System.Windows.Forms.CheckBox chkGradient;
        private System.Windows.Forms.CheckBox chkVignette;
        private System.Windows.Forms.NumericUpDown numVigAlpha;
        private System.Windows.Forms.CheckBox chkPieceGlow;
        private System.Windows.Forms.CheckBox chkBoardFrame;
        private System.Windows.Forms.Button btnFrameColor;
        private System.Windows.Forms.GroupBox grpSounds;
        private System.Windows.Forms.CheckBox chkSoundEffects;
    }
}
