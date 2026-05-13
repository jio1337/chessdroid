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
            cmbEngineDepth = new ComboBox();
            numMinAnalysisTime = new NumericUpDown();
            lblMinAnalysisTime = new Label();
            chkContinuousAnalysis = new CheckBox();
            lblContinuousMaxDepth = new Label();
            numContinuousMaxDepth = new NumericUpDown();
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
            lblBest = new Label();
            lblConsoleFont = new Label();
            btnChooseFont = new Button();
            grpBoardColors = new GroupBox();
            lblLightSquares = new Label();
            btnLightColor = new Button();
            lblDarkSquares = new Label();
            btnDarkColor = new Button();
            lblColorPreset = new Label();
            cmbColorPreset = new ComboBox();
            btnResetColors = new Button();
            lblSquareLabels = new Label();
            chkSquareLabels = new CheckBox();
            grpExplanations = new GroupBox();
            cmbComplexity = new ComboBox();
            lblComplexity = new Label();
            chkTactical = new CheckBox();
            chkPositional = new CheckBox();
            chkEndgame = new CheckBox();
            chkOpening = new CheckBox();
            chkThreats = new CheckBox();
            chkWDL = new CheckBox();
            chkOpeningName = new CheckBox();
            chkMoveQuality = new CheckBox();
            chkBookMoves = new CheckBox();
            grpLc0Features = new GroupBox();
            chkPlayStyleEnabled = new CheckBox();
            lblAggressiveness = new Label();
            trkAggressiveness = new TrackBar();
            lblAggressivenessValue = new Label();
            btnSave = new Button();
            btnReset = new Button();
            btnHelp = new Button();
            btnCancel = new Button();
            chkDarkMode = new CheckBox();
            toolTip1 = new ToolTip(components);
            grpEngine.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numContinuousMaxDepth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).BeginInit();
            grpDisplay.SuspendLayout();
            grpBoardColors.SuspendLayout();
            grpExplanations.SuspendLayout();
            grpLc0Features.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).BeginInit();
            SuspendLayout();
            // 
            // grpEngine
            // 
            grpEngine.Controls.Add(cmbEngineDepth);
            grpEngine.Controls.Add(numMinAnalysisTime);
            grpEngine.Controls.Add(lblMinAnalysisTime);
            grpEngine.Controls.Add(chkContinuousAnalysis);
            grpEngine.Controls.Add(lblContinuousMaxDepth);
            grpEngine.Controls.Add(numContinuousMaxDepth);
            grpEngine.Controls.Add(lblAutoPlayInterval);
            grpEngine.Controls.Add(numAutoPlayInterval);
            grpEngine.Controls.Add(numMoveTimeout);
            grpEngine.Controls.Add(numMaxRetries);
            grpEngine.Controls.Add(numEngineTimeout);
            grpEngine.Controls.Add(lblEngineDepth);
            grpEngine.Controls.Add(lblMoveTimeout);
            grpEngine.Controls.Add(lblMaxRetries);
            grpEngine.Controls.Add(lblEngineTimeout);
            grpEngine.ForeColor = Color.White;
            grpEngine.Location = new Point(12, 10);
            grpEngine.Name = "grpEngine";
            grpEngine.Size = new Size(298, 230);
            grpEngine.TabIndex = 1;
            grpEngine.TabStop = false;
            grpEngine.Text = "Chess Engine";
            // 
            // cmbEngineDepth
            // 
            cmbEngineDepth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngineDepth.FormattingEnabled = true;
            cmbEngineDepth.Location = new Point(182, 117);
            cmbEngineDepth.Name = "cmbEngineDepth";
            cmbEngineDepth.Size = new Size(100, 22);
            cmbEngineDepth.TabIndex = 6;
            toolTip1.SetToolTip(cmbEngineDepth, "Analysis depth (1-20). Higher = stronger but slower.");
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
            toolTip1.SetToolTip(chkContinuousAnalysis, "When enabled, engine analyzes to max depth and updates results live as depth increases. When disabled, engine stops at the configured depth.");
            // 
            // lblContinuousMaxDepth
            // 
            lblContinuousMaxDepth.Font = new Font("Courier New", 8.25F);
            lblContinuousMaxDepth.Location = new Point(186, 178);
            lblContinuousMaxDepth.Name = "lblContinuousMaxDepth";
            lblContinuousMaxDepth.Size = new Size(36, 15);
            lblContinuousMaxDepth.TabIndex = 10;
            lblContinuousMaxDepth.Text = "Max:";
            toolTip1.SetToolTip(lblContinuousMaxDepth, "Maximum depth for continuous analysis (10-100).");
            // 
            // numContinuousMaxDepth
            // 
            numContinuousMaxDepth.Location = new Point(228, 176);
            numContinuousMaxDepth.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numContinuousMaxDepth.Name = "numContinuousMaxDepth";
            numContinuousMaxDepth.Size = new Size(54, 20);
            numContinuousMaxDepth.TabIndex = 11;
            toolTip1.SetToolTip(numContinuousMaxDepth, "Maximum depth for continuous analysis (10-100).");
            numContinuousMaxDepth.Value = new decimal(new int[] { 50, 0, 0, 0 });
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
            toolTip1.SetToolTip(lblEngineDepth, "Analysis depth (1-20). Higher = stronger but slower.");
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
            grpDisplay.Controls.Add(lblBest);
            grpDisplay.Controls.Add(lblConsoleFont);
            grpDisplay.Controls.Add(btnChooseFont);
            grpDisplay.ForeColor = Color.White;
            grpDisplay.Location = new Point(12, 246);
            grpDisplay.Name = "grpDisplay";
            grpDisplay.Size = new Size(298, 181);
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
            chkEvalBar.Location = new Point(166, 130);
            chkEvalBar.Name = "chkEvalBar";
            chkEvalBar.Size = new Size(15, 14);
            chkEvalBar.TabIndex = 11;
            toolTip1.SetToolTip(chkEvalBar, "Show the evaluation bar on the left of the board");
            chkEvalBar.UseVisualStyleBackColor = true;
            // 
            // lblEvalBar
            // 
            lblEvalBar.Location = new Point(10, 128);
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
            // lblBest
            // 
            lblBest.Location = new Point(10, 46);
            lblBest.Name = "lblBest";
            lblBest.Size = new Size(150, 19);
            lblBest.TabIndex = 0;
            lblBest.Text = "Show Best Line:";
            // 
            // lblConsoleFont
            // 
            lblConsoleFont.Location = new Point(10, 150);
            lblConsoleFont.Name = "lblConsoleFont";
            lblConsoleFont.Size = new Size(104, 19);
            lblConsoleFont.TabIndex = 12;
            lblConsoleFont.Text = "Console Font:";
            toolTip1.SetToolTip(lblConsoleFont, "Font used in analysis output and move list");
            // 
            // btnChooseFont
            // 
            btnChooseFont.FlatStyle = FlatStyle.Flat;
            btnChooseFont.Location = new Point(166, 148);
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
            grpBoardColors.Controls.Add(lblColorPreset);
            grpBoardColors.Controls.Add(cmbColorPreset);
            grpBoardColors.Controls.Add(btnResetColors);
            grpBoardColors.Controls.Add(lblSquareLabels);
            grpBoardColors.Controls.Add(chkSquareLabels);
            grpBoardColors.ForeColor = Color.White;
            grpBoardColors.Location = new Point(12, 433);
            grpBoardColors.Name = "grpBoardColors";
            grpBoardColors.Size = new Size(298, 130);
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
            // lblColorPreset
            //
            lblColorPreset.Location = new Point(10, 76);
            lblColorPreset.Name = "lblColorPreset";
            lblColorPreset.Size = new Size(80, 19);
            lblColorPreset.TabIndex = 4;
            lblColorPreset.Text = "Theme:";
            //
            // cmbColorPreset
            //
            cmbColorPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbColorPreset.FlatStyle = FlatStyle.Flat;
            cmbColorPreset.Location = new Point(95, 73);
            cmbColorPreset.Name = "cmbColorPreset";
            cmbColorPreset.Size = new Size(130, 22);
            cmbColorPreset.TabIndex = 5;
            toolTip1.SetToolTip(cmbColorPreset, "Select a board color theme preset");
            cmbColorPreset.SelectedIndexChanged += CmbColorPreset_SelectedIndexChanged;
            //
            // btnResetColors
            //
            btnResetColors.Cursor = Cursors.Hand;
            btnResetColors.FlatAppearance.BorderColor = Color.Gray;
            btnResetColors.FlatStyle = FlatStyle.Flat;
            btnResetColors.Location = new Point(235, 73);
            btnResetColors.Name = "btnResetColors";
            btnResetColors.Size = new Size(55, 22);
            btnResetColors.TabIndex = 6;
            btnResetColors.Text = "Reset";
            toolTip1.SetToolTip(btnResetColors, "Reset board colors to default brown");
            btnResetColors.UseVisualStyleBackColor = false;
            btnResetColors.Click += BtnResetColors_Click;
            //
            // lblSquareLabels
            //
            lblSquareLabels.Location = new Point(10, 106);
            lblSquareLabels.Name = "lblSquareLabels";
            lblSquareLabels.Size = new Size(150, 19);
            lblSquareLabels.TabIndex = 7;
            lblSquareLabels.Text = "Square Labels:";
            toolTip1.SetToolTip(lblSquareLabels, "Show square names (e4, d5...) in the center of each square");
            //
            // chkSquareLabels
            //
            chkSquareLabels.AutoSize = true;
            chkSquareLabels.Location = new Point(166, 105);
            chkSquareLabels.Name = "chkSquareLabels";
            chkSquareLabels.Size = new Size(15, 14);
            chkSquareLabels.TabIndex = 8;
            toolTip1.SetToolTip(chkSquareLabels, "Show square names (e4, d5...) in the center of each square");
            chkSquareLabels.UseVisualStyleBackColor = true;
            //
            // grpExplanations
            // 
            grpExplanations.Controls.Add(cmbComplexity);
            grpExplanations.Controls.Add(lblComplexity);
            grpExplanations.Controls.Add(chkTactical);
            grpExplanations.Controls.Add(chkPositional);
            grpExplanations.Controls.Add(chkEndgame);
            grpExplanations.Controls.Add(chkOpening);
            grpExplanations.Controls.Add(chkThreats);
            grpExplanations.Controls.Add(chkWDL);
            grpExplanations.Controls.Add(chkOpeningName);
            grpExplanations.Controls.Add(chkMoveQuality);
            grpExplanations.Controls.Add(chkBookMoves);
            grpExplanations.ForeColor = Color.White;
            grpExplanations.Location = new Point(316, 10);
            grpExplanations.Name = "grpExplanations";
            grpExplanations.Size = new Size(255, 278);
            grpExplanations.TabIndex = 3;
            grpExplanations.TabStop = false;
            grpExplanations.Text = "Explanation Settings";
            // 
            // cmbComplexity
            // 
            cmbComplexity.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbComplexity.FormattingEnabled = true;
            cmbComplexity.Location = new Point(120, 20);
            cmbComplexity.Name = "cmbComplexity";
            cmbComplexity.Size = new Size(124, 22);
            cmbComplexity.TabIndex = 0;
            toolTip1.SetToolTip(cmbComplexity, "Explanation detail level: Beginner (simple) to Master (technical)");
            cmbComplexity.SelectedIndexChanged += CmbComplexity_SelectedIndexChanged;
            // 
            // lblComplexity
            // 
            lblComplexity.Location = new Point(10, 23);
            lblComplexity.Name = "lblComplexity";
            lblComplexity.Size = new Size(110, 19);
            lblComplexity.TabIndex = 0;
            lblComplexity.Text = "Complexity:";
            toolTip1.SetToolTip(lblComplexity, "Explanation detail level: Beginner (simple) to Master (technical)");
            // 
            // chkTactical
            // 
            chkTactical.AutoSize = true;
            chkTactical.Checked = true;
            chkTactical.CheckState = CheckState.Checked;
            chkTactical.ForeColor = Color.White;
            chkTactical.Location = new Point(10, 51);
            chkTactical.Name = "chkTactical";
            chkTactical.Size = new Size(145, 18);
            chkTactical.TabIndex = 1;
            chkTactical.Text = "Tactical Analysis";
            toolTip1.SetToolTip(chkTactical, "Show pins, forks, skewers, discovered attacks");
            chkTactical.UseVisualStyleBackColor = true;
            // 
            // chkPositional
            // 
            chkPositional.AutoSize = true;
            chkPositional.Checked = true;
            chkPositional.CheckState = CheckState.Checked;
            chkPositional.ForeColor = Color.White;
            chkPositional.Location = new Point(10, 76);
            chkPositional.Name = "chkPositional";
            chkPositional.Size = new Size(159, 18);
            chkPositional.TabIndex = 2;
            chkPositional.Text = "Positional Analysis";
            toolTip1.SetToolTip(chkPositional, "Show pawn structure, outposts, mobility");
            chkPositional.UseVisualStyleBackColor = true;
            // 
            // chkEndgame
            // 
            chkEndgame.AutoSize = true;
            chkEndgame.Checked = true;
            chkEndgame.CheckState = CheckState.Checked;
            chkEndgame.ForeColor = Color.White;
            chkEndgame.Location = new Point(10, 101);
            chkEndgame.Name = "chkEndgame";
            chkEndgame.Size = new Size(138, 18);
            chkEndgame.TabIndex = 3;
            chkEndgame.Text = "Endgame Analysis";
            toolTip1.SetToolTip(chkEndgame, "Show endgame patterns, zugzwang");
            chkEndgame.UseVisualStyleBackColor = true;
            // 
            // chkOpening
            // 
            chkOpening.AutoSize = true;
            chkOpening.Checked = true;
            chkOpening.CheckState = CheckState.Checked;
            chkOpening.ForeColor = Color.White;
            chkOpening.Location = new Point(10, 126);
            chkOpening.Name = "chkOpening";
            chkOpening.Size = new Size(152, 18);
            chkOpening.TabIndex = 4;
            chkOpening.Text = "Opening Principles";
            toolTip1.SetToolTip(chkOpening, "Show center control, development");
            chkOpening.UseVisualStyleBackColor = true;
            // 
            // chkThreats
            // 
            chkThreats.AutoSize = true;
            chkThreats.Checked = true;
            chkThreats.CheckState = CheckState.Checked;
            chkThreats.ForeColor = Color.White;
            chkThreats.Location = new Point(10, 151);
            chkThreats.Name = "chkThreats";
            chkThreats.Size = new Size(138, 18);
            chkThreats.TabIndex = 9;
            chkThreats.Text = "Threats Analysis";
            toolTip1.SetToolTip(chkThreats, "Show threats we create and opponent threats against us");
            chkThreats.UseVisualStyleBackColor = true;
            // 
            // chkWDL
            // 
            chkWDL.AutoSize = true;
            chkWDL.Checked = true;
            chkWDL.CheckState = CheckState.Checked;
            chkWDL.ForeColor = Color.White;
            chkWDL.Location = new Point(10, 176);
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
            chkOpeningName.Location = new Point(10, 201);
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
            chkMoveQuality.Location = new Point(10, 226);
            chkMoveQuality.Name = "chkMoveQuality";
            chkMoveQuality.Size = new Size(229, 18);
            chkMoveQuality.TabIndex = 12;
            chkMoveQuality.Text = "Move Quality (Brilliant/Best)";
            toolTip1.SetToolTip(chkMoveQuality, "Show move quality indicators like chess.com (!!, !, ?!, ?, ??)");
            chkMoveQuality.UseVisualStyleBackColor = true;
            // 
            // chkBookMoves
            // 
            chkBookMoves.AutoSize = true;
            chkBookMoves.Checked = true;
            chkBookMoves.CheckState = CheckState.Checked;
            chkBookMoves.ForeColor = Color.White;
            chkBookMoves.Location = new Point(10, 251);
            chkBookMoves.Name = "chkBookMoves";
            chkBookMoves.Size = new Size(131, 18);
            chkBookMoves.TabIndex = 13;
            chkBookMoves.Text = "Show Book Moves";
            toolTip1.SetToolTip(chkBookMoves, "Show opening book move suggestions from Polyglot books");
            chkBookMoves.UseVisualStyleBackColor = true;
            // 
            // grpLc0Features
            // 
            grpLc0Features.Controls.Add(chkPlayStyleEnabled);
            grpLc0Features.Controls.Add(lblAggressiveness);
            grpLc0Features.Controls.Add(trkAggressiveness);
            grpLc0Features.Controls.Add(lblAggressivenessValue);
            grpLc0Features.ForeColor = Color.White;
            grpLc0Features.Location = new Point(316, 298);
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
            // btnSave
            // 
            btnSave.FlatStyle = FlatStyle.Popup;
            btnSave.ForeColor = SystemColors.ControlLightLight;
            btnSave.Location = new Point(316, 450);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 28);
            btnSave.TabIndex = 4;
            btnSave.Text = "Save && Apply";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnReset
            // 
            btnReset.FlatStyle = FlatStyle.Popup;
            btnReset.ForeColor = SystemColors.ControlLightLight;
            btnReset.Location = new Point(316, 484);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(100, 28);
            btnReset.TabIndex = 5;
            btnReset.Text = "Defaults";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += BtnReset_Click;
            // 
            // btnHelp
            // 
            btnHelp.FlatStyle = FlatStyle.Popup;
            btnHelp.ForeColor = SystemColors.ControlLightLight;
            btnHelp.Location = new Point(471, 451);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(100, 28);
            btnHelp.TabIndex = 6;
            btnHelp.Text = "Help";
            btnHelp.UseVisualStyleBackColor = true;
            btnHelp.Click += BtnHelp_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatStyle = FlatStyle.Popup;
            btnCancel.ForeColor = SystemColors.ControlLightLight;
            btnCancel.Location = new Point(472, 484);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 28);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // chkDarkMode
            // 
            chkDarkMode.AutoSize = true;
            chkDarkMode.Checked = true;
            chkDarkMode.CheckState = CheckState.Checked;
            chkDarkMode.ForeColor = Color.White;
            chkDarkMode.Location = new Point(12, 569);
            chkDarkMode.Name = "chkDarkMode";
            chkDarkMode.Size = new Size(89, 18);
            chkDarkMode.TabIndex = 8;
            chkDarkMode.Text = "Dark Mode";
            chkDarkMode.UseVisualStyleBackColor = true;
            chkDarkMode.CheckedChanged += ChkDarkMode_CheckedChanged;
            // 
            // SettingsForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(45, 45, 48);
            CancelButton = btnCancel;
            ClientSize = new Size(585, 596);
            Controls.Add(chkDarkMode);
            Controls.Add(btnCancel);
            Controls.Add(btnHelp);
            Controls.Add(btnReset);
            Controls.Add(btnSave);
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
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numContinuousMaxDepth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAutoPlayInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).EndInit();
            grpDisplay.ResumeLayout(false);
            grpDisplay.PerformLayout();
            grpBoardColors.ResumeLayout(false);
            grpExplanations.ResumeLayout(false);
            grpExplanations.PerformLayout();
            grpLc0Features.ResumeLayout(false);
            grpLc0Features.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkAggressiveness).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpEngine;
        private System.Windows.Forms.ComboBox cmbEngineDepth;
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
        private System.Windows.Forms.Label lblColorPreset;
        private System.Windows.Forms.ComboBox cmbColorPreset;
        private System.Windows.Forms.Button btnResetColors;
        private System.Windows.Forms.Label lblSquareLabels;
        private System.Windows.Forms.CheckBox chkSquareLabels;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkDarkMode;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox grpExplanations;
        private System.Windows.Forms.ComboBox cmbComplexity;
        private System.Windows.Forms.Label lblComplexity;
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
        private System.Windows.Forms.CheckBox chkContinuousAnalysis;
        private System.Windows.Forms.Label lblContinuousMaxDepth;
        private System.Windows.Forms.NumericUpDown numContinuousMaxDepth;
        private System.Windows.Forms.Label lblAutoPlayInterval;
        private System.Windows.Forms.NumericUpDown numAutoPlayInterval;
    }
}
