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
            grpEngine = new GroupBox();
            cmbEngineDepth = new ComboBox();
            numMinAnalysisTime = new NumericUpDown();
            lblMinAnalysisTime = new Label();
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
            chkShowThird = new CheckBox();
            lblThird = new Label();
            chkShowSecond = new CheckBox();
            lblSecond = new Label();
            chkShowBest = new CheckBox();
            lblBest = new Label();
            grpExplanations = new GroupBox();
            cmbComplexity = new ComboBox();
            lblComplexity = new Label();
            chkTactical = new CheckBox();
            chkPositional = new CheckBox();
            chkEndgame = new CheckBox();
            chkOpening = new CheckBox();
            chkSEE = new CheckBox();
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
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).BeginInit();
            grpDisplay.SuspendLayout();
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
            grpEngine.Controls.Add(numMoveTimeout);
            grpEngine.Controls.Add(numMaxRetries);
            grpEngine.Controls.Add(numEngineTimeout);
            grpEngine.Controls.Add(lblEngineDepth);
            grpEngine.Controls.Add(lblMoveTimeout);
            grpEngine.Controls.Add(lblMaxRetries);
            grpEngine.Controls.Add(lblEngineTimeout);
            grpEngine.ForeColor = Color.White;
            grpEngine.Location = new Point(13, 13);
            grpEngine.Name = "grpEngine";
            grpEngine.Size = new Size(298, 185);
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
            grpDisplay.Controls.Add(chkShowThird);
            grpDisplay.Controls.Add(lblThird);
            grpDisplay.Controls.Add(chkShowSecond);
            grpDisplay.Controls.Add(lblSecond);
            grpDisplay.Controls.Add(chkShowBest);
            grpDisplay.Controls.Add(lblBest);
            grpDisplay.ForeColor = Color.White;
            grpDisplay.Location = new Point(13, 204);
            grpDisplay.Name = "grpDisplay";
            grpDisplay.Size = new Size(298, 117);
            grpDisplay.TabIndex = 2;
            grpDisplay.TabStop = false;
            grpDisplay.Text = "Display Options";
            // 
            // cmbEngine
            // 
            cmbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEngine.FormattingEnabled = true;
            cmbEngine.Location = new Point(182, 18);
            cmbEngine.Name = "cmbEngine";
            cmbEngine.Size = new Size(100, 22);
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
            // chkShowThird
            // 
            chkShowThird.AutoSize = true;
            chkShowThird.Location = new Point(182, 88);
            chkShowThird.Name = "chkShowThird";
            chkShowThird.Size = new Size(15, 14);
            chkShowThird.TabIndex = 5;
            toolTip1.SetToolTip(chkShowThird, "Show 3rd best move line");
            chkShowThird.UseVisualStyleBackColor = true;
            // 
            // lblThird
            // 
            lblThird.Location = new Point(10, 86);
            lblThird.Name = "lblThird";
            lblThird.Size = new Size(150, 19);
            lblThird.TabIndex = 4;
            lblThird.Text = "Show 3rd Best Line:";
            // 
            // chkShowSecond
            // 
            chkShowSecond.AutoSize = true;
            chkShowSecond.Location = new Point(182, 70);
            chkShowSecond.Name = "chkShowSecond";
            chkShowSecond.Size = new Size(15, 14);
            chkShowSecond.TabIndex = 3;
            toolTip1.SetToolTip(chkShowSecond, "Show 2nd best move line");
            chkShowSecond.UseVisualStyleBackColor = true;
            // 
            // lblSecond
            // 
            lblSecond.Location = new Point(10, 68);
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
            chkShowBest.Location = new Point(182, 52);
            chkShowBest.Name = "chkShowBest";
            chkShowBest.Size = new Size(15, 14);
            chkShowBest.TabIndex = 1;
            toolTip1.SetToolTip(chkShowBest, "Show best move line");
            chkShowBest.UseVisualStyleBackColor = true;
            // 
            // lblBest
            // 
            lblBest.Location = new Point(10, 50);
            lblBest.Name = "lblBest";
            lblBest.Size = new Size(150, 19);
            lblBest.TabIndex = 0;
            lblBest.Text = "Show Best Line:";
            // 
            // grpExplanations
            // 
            grpExplanations.Controls.Add(cmbComplexity);
            grpExplanations.Controls.Add(lblComplexity);
            grpExplanations.Controls.Add(chkTactical);
            grpExplanations.Controls.Add(chkPositional);
            grpExplanations.Controls.Add(chkEndgame);
            grpExplanations.Controls.Add(chkOpening);
            grpExplanations.Controls.Add(chkSEE);
            grpExplanations.Controls.Add(chkThreats);
            grpExplanations.Controls.Add(chkWDL);
            grpExplanations.Controls.Add(chkOpeningName);
            grpExplanations.Controls.Add(chkMoveQuality);
            grpExplanations.Controls.Add(chkBookMoves);
            grpExplanations.ForeColor = Color.White;
            grpExplanations.Location = new Point(317, 13);
            grpExplanations.Name = "grpExplanations";
            grpExplanations.Size = new Size(255, 308);
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
            chkTactical.Location = new Point(10, 52);
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
            chkPositional.Location = new Point(10, 77);
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
            chkEndgame.Location = new Point(10, 102);
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
            chkOpening.Location = new Point(10, 127);
            chkOpening.Name = "chkOpening";
            chkOpening.Size = new Size(152, 18);
            chkOpening.TabIndex = 4;
            chkOpening.Text = "Opening Principles";
            toolTip1.SetToolTip(chkOpening, "Show center control, development");
            chkOpening.UseVisualStyleBackColor = true;
            // 
            // chkSEE
            // 
            chkSEE.AutoSize = true;
            chkSEE.Checked = true;
            chkSEE.CheckState = CheckState.Checked;
            chkSEE.ForeColor = Color.White;
            chkSEE.Location = new Point(10, 152);
            chkSEE.Name = "chkSEE";
            chkSEE.Size = new Size(96, 18);
            chkSEE.TabIndex = 8;
            chkSEE.Text = "SEE Values";
            toolTip1.SetToolTip(chkSEE, "Static Exchange Evaluation for captures");
            chkSEE.UseVisualStyleBackColor = true;
            // 
            // chkThreats
            // 
            chkThreats.AutoSize = true;
            chkThreats.Checked = true;
            chkThreats.CheckState = CheckState.Checked;
            chkThreats.ForeColor = Color.White;
            chkThreats.Location = new Point(10, 177);
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
            chkWDL.Location = new Point(10, 202);
            chkWDL.Name = "chkWDL";
            chkWDL.Size = new Size(166, 18);
            chkWDL.TabIndex = 10;
            chkWDL.Text = "WDL & Sharpness (Lc0)";
            toolTip1.SetToolTip(chkWDL, "Show Win/Draw/Loss probabilities and position sharpness (Lc0-inspired)");
            chkWDL.UseVisualStyleBackColor = true;
            // 
            // chkOpeningName
            // 
            chkOpeningName.AutoSize = true;
            chkOpeningName.Checked = true;
            chkOpeningName.CheckState = CheckState.Checked;
            chkOpeningName.ForeColor = Color.White;
            chkOpeningName.Location = new Point(10, 227);
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
            chkMoveQuality.Location = new Point(10, 252);
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
            chkBookMoves.Location = new Point(10, 277);
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
            grpLc0Features.Location = new Point(317, 327);
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
            chkPlayStyleEnabled.Size = new Size(69, 18);
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
            btnSave.Location = new Point(12, 386);
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
            btnReset.Location = new Point(12, 421);
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
            btnHelp.Location = new Point(195, 386);
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
            btnCancel.Location = new Point(195, 421);
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
            chkDarkMode.Location = new Point(13, 348);
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
            ClientSize = new Size(585, 461);
            Controls.Add(chkDarkMode);
            Controls.Add(btnCancel);
            Controls.Add(btnHelp);
            Controls.Add(btnReset);
            Controls.Add(btnSave);
            Controls.Add(grpLc0Features);
            Controls.Add(grpExplanations);
            Controls.Add(grpDisplay);
            Controls.Add(grpEngine);
            Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "chessdroid://settings";
            grpEngine.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMinAnalysisTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).EndInit();
            grpDisplay.ResumeLayout(false);
            grpDisplay.PerformLayout();
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
        private System.Windows.Forms.CheckBox chkSEE;
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
    }
}
