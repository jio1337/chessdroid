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
            grpDetection = new GroupBox();
            numMinBoardArea = new NumericUpDown();
            numCannyHigh = new NumericUpDown();
            numCannyLow = new NumericUpDown();
            numMatchThreshold = new NumericUpDown();
            chkDebugCells = new CheckBox();
            lblMinBoardArea = new Label();
            lblCannyHigh = new Label();
            lblCannyLow = new Label();
            lblMatchThreshold = new Label();
            grpEngine = new GroupBox();
            cmbEngineDepth = new ComboBox();
            numMoveTimeout = new NumericUpDown();
            numMaxRetries = new NumericUpDown();
            numEngineTimeout = new NumericUpDown();
            lblEngineDepth = new Label();
            lblMoveTimeout = new Label();
            lblMaxRetries = new Label();
            lblEngineTimeout = new Label();
            grpDisplay = new GroupBox();
            cmbSite = new ComboBox();
            lblSite = new Label();
            cmbEngine = new ComboBox();
            lblEngine = new Label();
            chkShowThird = new CheckBox();
            lblThird = new Label();
            chkShowSecond = new CheckBox();
            lblSecond = new Label();
            chkShowBest = new CheckBox();
            lblBest = new Label();
            btnSave = new Button();
            btnReset = new Button();
            btnHelp = new Button();
            btnCancel = new Button();
            chkDarkMode = new CheckBox();
            toolTip1 = new ToolTip(components);
            grpDetection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMinBoardArea).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCannyHigh).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCannyLow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMatchThreshold).BeginInit();
            grpEngine.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).BeginInit();
            grpDisplay.SuspendLayout();
            SuspendLayout();
            // 
            // grpDetection
            //
            grpDetection.Controls.Add(chkDebugCells);
            grpDetection.Controls.Add(numMinBoardArea);
            grpDetection.Controls.Add(numCannyHigh);
            grpDetection.Controls.Add(numCannyLow);
            grpDetection.Controls.Add(numMatchThreshold);
            grpDetection.Controls.Add(lblMinBoardArea);
            grpDetection.Controls.Add(lblCannyHigh);
            grpDetection.Controls.Add(lblCannyLow);
            grpDetection.Controls.Add(lblMatchThreshold);
            grpDetection.ForeColor = Color.White;
            grpDetection.Location = new Point(12, 12);
            grpDetection.Name = "grpDetection";
            grpDetection.Size = new Size(298, 173);
            grpDetection.TabIndex = 0;
            grpDetection.TabStop = false;
            grpDetection.Text = "Board Detection";
            // 
            // numMinBoardArea
            // 
            numMinBoardArea.Increment = new decimal(new int[] { 500, 0, 0, 0 });
            numMinBoardArea.Location = new Point(182, 117);
            numMinBoardArea.Maximum = new decimal(new int[] { 50000, 0, 0, 0 });
            numMinBoardArea.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            numMinBoardArea.Name = "numMinBoardArea";
            numMinBoardArea.Size = new Size(100, 20);
            numMinBoardArea.TabIndex = 7;
            toolTip1.SetToolTip(numMinBoardArea, "Minimum board size to detect. Increase if detecting wrong areas.");
            numMinBoardArea.Value = new decimal(new int[] { 10000, 0, 0, 0 });
            // 
            // numCannyHigh
            // 
            numCannyHigh.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            numCannyHigh.Location = new Point(182, 84);
            numCannyHigh.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numCannyHigh.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numCannyHigh.Name = "numCannyHigh";
            numCannyHigh.Size = new Size(100, 20);
            numCannyHigh.TabIndex = 6;
            toolTip1.SetToolTip(numCannyHigh, "Edge detection upper threshold. Should be 2-3x the low value.");
            numCannyHigh.Value = new decimal(new int[] { 150, 0, 0, 0 });
            // 
            // numCannyLow
            // 
            numCannyLow.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            numCannyLow.Location = new Point(182, 51);
            numCannyLow.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numCannyLow.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numCannyLow.Name = "numCannyLow";
            numCannyLow.Size = new Size(100, 20);
            numCannyLow.TabIndex = 5;
            toolTip1.SetToolTip(numCannyLow, "Edge detection lower threshold. Adjust if board detection fails.");
            numCannyLow.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // numMatchThreshold
            // 
            numMatchThreshold.DecimalPlaces = 2;
            numMatchThreshold.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numMatchThreshold.Location = new Point(182, 19);
            numMatchThreshold.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            numMatchThreshold.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numMatchThreshold.Name = "numMatchThreshold";
            numMatchThreshold.Size = new Size(100, 20);
            numMatchThreshold.TabIndex = 4;
            toolTip1.SetToolTip(numMatchThreshold, "Minimum similarity for piece recognition (0.1-1.0). Higher = stricter matching.");
            numMatchThreshold.Value = new decimal(new int[] { 75, 0, 0, 131072 });
            //
            // chkDebugCells
            //
            chkDebugCells.AutoSize = true;
            chkDebugCells.ForeColor = Color.White;
            chkDebugCells.Location = new Point(10, 145);
            chkDebugCells.Name = "chkDebugCells";
            chkDebugCells.Size = new Size(110, 18);
            chkDebugCells.TabIndex = 8;
            chkDebugCells.Text = "Debug Cells";
            toolTip1.SetToolTip(chkDebugCells, "Show board visualization popup for debugging piece recognition");
            chkDebugCells.UseVisualStyleBackColor = true;
            //
            // lblMinBoardArea
            // 
            lblMinBoardArea.Location = new Point(10, 119);
            lblMinBoardArea.Name = "lblMinBoardArea";
            lblMinBoardArea.Size = new Size(200, 19);
            lblMinBoardArea.TabIndex = 3;
            lblMinBoardArea.Text = "Min Board Area (px²):";
            toolTip1.SetToolTip(lblMinBoardArea, "Minimum board size to detect. Increase if detecting wrong areas.");
            // 
            // lblCannyHigh
            // 
            lblCannyHigh.Location = new Point(10, 87);
            lblCannyHigh.Name = "lblCannyHigh";
            lblCannyHigh.Size = new Size(200, 19);
            lblCannyHigh.TabIndex = 2;
            lblCannyHigh.Text = "Canny High Threshold:";
            toolTip1.SetToolTip(lblCannyHigh, "Edge detection upper threshold. Should be 2-3x the low value.");
            // 
            // lblCannyLow
            // 
            lblCannyLow.Location = new Point(10, 54);
            lblCannyLow.Name = "lblCannyLow";
            lblCannyLow.Size = new Size(200, 19);
            lblCannyLow.TabIndex = 1;
            lblCannyLow.Text = "Canny Low Threshold:";
            toolTip1.SetToolTip(lblCannyLow, "Edge detection lower threshold. Adjust if board detection fails.");
            // 
            // lblMatchThreshold
            // 
            lblMatchThreshold.Location = new Point(10, 21);
            lblMatchThreshold.Name = "lblMatchThreshold";
            lblMatchThreshold.Size = new Size(200, 19);
            lblMatchThreshold.TabIndex = 0;
            lblMatchThreshold.Text = "Match Threshold:";
            toolTip1.SetToolTip(lblMatchThreshold, "Minimum similarity for piece recognition (0.1-1.0). Higher = stricter matching.");
            // 
            // grpEngine
            // 
            grpEngine.Controls.Add(cmbEngineDepth);
            grpEngine.Controls.Add(numMoveTimeout);
            grpEngine.Controls.Add(numMaxRetries);
            grpEngine.Controls.Add(numEngineTimeout);
            grpEngine.Controls.Add(lblEngineDepth);
            grpEngine.Controls.Add(lblMoveTimeout);
            grpEngine.Controls.Add(lblMaxRetries);
            grpEngine.Controls.Add(lblEngineTimeout);
            grpEngine.ForeColor = Color.White;
            grpEngine.Location = new Point(12, 191);
            grpEngine.Name = "grpEngine";
            grpEngine.Size = new Size(298, 153);
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
            grpDisplay.Controls.Add(cmbSite);
            grpDisplay.Controls.Add(lblSite);
            grpDisplay.Controls.Add(cmbEngine);
            grpDisplay.Controls.Add(lblEngine);
            grpDisplay.Controls.Add(chkShowThird);
            grpDisplay.Controls.Add(lblThird);
            grpDisplay.Controls.Add(chkShowSecond);
            grpDisplay.Controls.Add(lblSecond);
            grpDisplay.Controls.Add(chkShowBest);
            grpDisplay.Controls.Add(lblBest);
            grpDisplay.ForeColor = Color.White;
            grpDisplay.Location = new Point(12, 350);
            grpDisplay.Name = "grpDisplay";
            grpDisplay.Size = new Size(298, 146);
            grpDisplay.TabIndex = 2;
            grpDisplay.TabStop = false;
            grpDisplay.Text = "Display Options";
            // 
            // cmbSite
            // 
            cmbSite.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSite.FormattingEnabled = true;
            cmbSite.Location = new Point(182, 50);
            cmbSite.Name = "cmbSite";
            cmbSite.Size = new Size(100, 22);
            cmbSite.TabIndex = 9;
            toolTip1.SetToolTip(cmbSite, "Select chess website for piece templates");
            // 
            // lblSite
            // 
            lblSite.Location = new Point(10, 53);
            lblSite.Name = "lblSite";
            lblSite.Size = new Size(150, 19);
            lblSite.TabIndex = 8;
            lblSite.Text = "Website:";
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
            chkShowThird.Location = new Point(182, 116);
            chkShowThird.Name = "chkShowThird";
            chkShowThird.Size = new Size(15, 14);
            chkShowThird.TabIndex = 5;
            toolTip1.SetToolTip(chkShowThird, "Show 3rd best move line");
            chkShowThird.UseVisualStyleBackColor = true;
            // 
            // lblThird
            // 
            lblThird.Location = new Point(10, 114);
            lblThird.Name = "lblThird";
            lblThird.Size = new Size(150, 19);
            lblThird.TabIndex = 4;
            lblThird.Text = "Show 3rd Best Line:";
            // 
            // chkShowSecond
            // 
            chkShowSecond.AutoSize = true;
            chkShowSecond.Location = new Point(182, 100);
            chkShowSecond.Name = "chkShowSecond";
            chkShowSecond.Size = new Size(15, 14);
            chkShowSecond.TabIndex = 3;
            toolTip1.SetToolTip(chkShowSecond, "Show 2nd best move line");
            chkShowSecond.UseVisualStyleBackColor = true;
            // 
            // lblSecond
            // 
            lblSecond.Location = new Point(10, 98);
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
            chkShowBest.Location = new Point(182, 84);
            chkShowBest.Name = "chkShowBest";
            chkShowBest.Size = new Size(15, 14);
            chkShowBest.TabIndex = 1;
            toolTip1.SetToolTip(chkShowBest, "Show best move line");
            chkShowBest.UseVisualStyleBackColor = true;
            // 
            // lblBest
            // 
            lblBest.Location = new Point(10, 82);
            lblBest.Name = "lblBest";
            lblBest.Size = new Size(150, 19);
            lblBest.TabIndex = 0;
            lblBest.Text = "Show Best Line:";
            // 
            // btnSave
            //
            btnSave.FlatStyle = FlatStyle.Popup;
            btnSave.ForeColor = SystemColors.ControlLightLight;
            btnSave.Location = new Point(12, 502);
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
            btnReset.Location = new Point(210, 502);
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
            btnHelp.Location = new Point(12, 536);
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
            btnCancel.Location = new Point(210, 536);
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
            chkDarkMode.Location = new Point(221, 570);
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
            ClientSize = new Size(322, 597);
            Controls.Add(chkDarkMode);
            Controls.Add(btnCancel);
            Controls.Add(btnHelp);
            Controls.Add(btnReset);
            Controls.Add(btnSave);
            Controls.Add(grpDisplay);
            Controls.Add(grpEngine);
            Controls.Add(grpDetection);
            Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "chessdroid://settings";
            grpDetection.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMinBoardArea).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCannyHigh).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCannyLow).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMatchThreshold).EndInit();
            grpEngine.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMoveTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRetries).EndInit();
            ((System.ComponentModel.ISupportInitialize)numEngineTimeout).EndInit();
            grpDisplay.ResumeLayout(false);
            grpDisplay.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpDetection;
        private System.Windows.Forms.NumericUpDown numMinBoardArea;
        private System.Windows.Forms.NumericUpDown numCannyHigh;
        private System.Windows.Forms.NumericUpDown numCannyLow;
        private System.Windows.Forms.NumericUpDown numMatchThreshold;
        private System.Windows.Forms.Label lblMinBoardArea;
        private System.Windows.Forms.Label lblCannyHigh;
        private System.Windows.Forms.Label lblCannyLow;
        private System.Windows.Forms.Label lblMatchThreshold;
        private System.Windows.Forms.GroupBox grpEngine;
        private System.Windows.Forms.ComboBox cmbEngineDepth;
        private System.Windows.Forms.NumericUpDown numMoveTimeout;
        private System.Windows.Forms.NumericUpDown numMaxRetries;
        private System.Windows.Forms.NumericUpDown numEngineTimeout;
        private System.Windows.Forms.Label lblEngineDepth;
        private System.Windows.Forms.Label lblMoveTimeout;
        private System.Windows.Forms.Label lblMaxRetries;
        private System.Windows.Forms.Label lblEngineTimeout;
        private System.Windows.Forms.GroupBox grpDisplay;
        private System.Windows.Forms.ComboBox cmbSite;
        private System.Windows.Forms.Label lblSite;
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
        private System.Windows.Forms.CheckBox chkDebugCells;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
