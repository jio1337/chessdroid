namespace ChessDroid
{
    public partial class SettingsForm : Form
    {
        private AppConfig config;
        private readonly Action? onConfigChanged;

        public SettingsForm(AppConfig config, Action? onConfigChanged = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.onConfigChanged = onConfigChanged;
            InitializeComponent();
            PopulateEngineDepthComboBox();
            LoadSettings();
        }

        private void PopulateEngineDepthComboBox()
        {
            cmbEngineDepth.Items.Clear();
            for (int i = 1; i <= 20; i++)
            {
                cmbEngineDepth.Items.Add(i);
            }
        }

        private void PopulateComplexityComboBox()
        {
            cmbComplexity.Items.Clear();
            cmbComplexity.Items.Add("Beginner");
            cmbComplexity.Items.Add("Intermediate");
            cmbComplexity.Items.Add("Advanced");
            cmbComplexity.Items.Add("Master");
        }

        private void PopulateEnginesComboBox()
        {
            try
            {
                string enginesFolder = config.GetEnginesPath();
                if (Directory.Exists(enginesFolder))
                {
                    string[] engineFiles = Directory.GetFiles(enginesFolder, "*.exe");
                    cmbEngine.Items.Clear();
                    foreach (string file in engineFiles)
                    {
                        cmbEngine.Items.Add(Path.GetFileName(file));
                    }

                    // Select configured engine or default
                    if (!string.IsNullOrEmpty(config.SelectedEngine) && cmbEngine.Items.Contains(config.SelectedEngine))
                    {
                        cmbEngine.SelectedItem = config.SelectedEngine;
                    }
                    else if (cmbEngine.Items.Count > 0)
                    {
                        cmbEngine.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading engines: " + ex.Message, "Engine Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            this.TopMost = true;

            // Engine settings
            numEngineTimeout.Value = config.EngineResponseTimeoutMs;
            numMaxRetries.Value = config.MaxEngineRetries;
            numMoveTimeout.Value = config.MoveTimeoutMs;

            // Engine depth
            if (config.EngineDepth >= 1 && config.EngineDepth <= 20)
            {
                cmbEngineDepth.SelectedItem = config.EngineDepth;
            }
            else
            {
                cmbEngineDepth.SelectedItem = 15; // Default
            }

            // Min analysis time
            numMinAnalysisTime.Value = config.MinAnalysisTimeMs;

            // Display options
            PopulateEnginesComboBox();

            // Best lines checkboxes
            chkShowBest.Checked = config.ShowBestLine;
            chkShowSecond.Checked = config.ShowSecondLine;
            chkShowThird.Checked = config.ShowThirdLine;
            chkEngineArrows.Checked = config.ShowEngineArrows;

            // Explanation settings
            PopulateComplexityComboBox();
            cmbComplexity.SelectedItem = config.ExplanationComplexity ?? "Intermediate";
            chkTactical.Checked = config.ShowTacticalAnalysis;
            chkPositional.Checked = config.ShowPositionalAnalysis;
            chkEndgame.Checked = config.ShowEndgameAnalysis;
            chkOpening.Checked = config.ShowOpeningPrinciples;
            chkThreats.Checked = config.ShowThreats;
            chkWDL.Checked = config.ShowWDL;

            // Play style and display features
            chkPlayStyleEnabled.Checked = config.PlayStyleEnabled;
            trkAggressiveness.Value = Math.Clamp(config.Aggressiveness, 0, 100);
            trkAggressiveness.Enabled = config.PlayStyleEnabled;
            UpdateAggressivenessLabel();
            chkOpeningName.Checked = config.ShowOpeningName;
            chkMoveQuality.Checked = config.ShowMoveQuality;
            chkBookMoves.Checked = config.ShowBookMoves;

            // Load theme preference
            chkDarkMode.Checked = config.Theme == "Dark";
            ApplyTheme(config.Theme == "Dark");
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Validate Engine Depth selection
            if (cmbEngineDepth.SelectedItem == null)
            {
                MessageBox.Show("Please select an Engine Depth value.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save engine values
            config.EngineResponseTimeoutMs = (int)numEngineTimeout.Value;
            config.MaxEngineRetries = (int)numMaxRetries.Value;
            config.MoveTimeoutMs = (int)numMoveTimeout.Value;
            config.EngineDepth = (int)cmbEngineDepth.SelectedItem;
            config.MinAnalysisTimeMs = (int)numMinAnalysisTime.Value;
            config.Theme = chkDarkMode.Checked ? "Dark" : "Light";

            // Save display options
            config.SelectedEngine = cmbEngine.SelectedItem?.ToString() ?? "";
            config.ShowBestLine = chkShowBest.Checked;
            config.ShowSecondLine = chkShowSecond.Checked;
            config.ShowThirdLine = chkShowThird.Checked;
            config.ShowEngineArrows = chkEngineArrows.Checked;

            // Save explanation settings
            config.ExplanationComplexity = cmbComplexity.SelectedItem?.ToString() ?? "Intermediate";
            config.ShowTacticalAnalysis = chkTactical.Checked;
            config.ShowPositionalAnalysis = chkPositional.Checked;
            config.ShowEndgameAnalysis = chkEndgame.Checked;
            config.ShowOpeningPrinciples = chkOpening.Checked;
            config.ShowThreats = chkThreats.Checked;
            config.ShowWDL = chkWDL.Checked;

            // Play style and display features
            config.PlayStyleEnabled = chkPlayStyleEnabled.Checked;
            config.Aggressiveness = trkAggressiveness.Value;
            config.ShowOpeningName = chkOpeningName.Checked;
            config.ShowMoveQuality = chkMoveQuality.Checked;
            config.ShowBookMoves = chkBookMoves.Checked;

            config.Save();

            // Notify that config changed
            onConfigChanged?.Invoke();

            MessageBox.Show("Settings saved successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all settings to default values?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                config = new AppConfig();
                LoadSettings();
            }
        }

        private void BtnHelp_Click(object? sender, EventArgs e)
        {
            using (var helpForm = new HelpForm())
            {
                helpForm.ShowDialog(this);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void ChkDarkMode_CheckedChanged(object? sender, EventArgs e)
        {
            ApplyTheme(chkDarkMode.Checked);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            // Suspend layout to prevent flickering and improve performance
            this.SuspendLayout();

            if (isDarkMode)
            {
                // Dark Mode Colors
                this.BackColor = Color.FromArgb(45, 45, 48);

                // GroupBoxes
                grpEngine.ForeColor = Color.White;
                grpEngine.BackColor = Color.FromArgb(45, 45, 48);

                foreach (Control ctrl in grpEngine.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.White;
                        lbl.BackColor = Color.FromArgb(45, 45, 48);
                    }
                    else if (ctrl is NumericUpDown num)
                    {
                        num.BackColor = Color.FromArgb(60, 60, 65);
                        num.ForeColor = Color.White;
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.FromArgb(60, 60, 65);
                        cmb.ForeColor = Color.White;
                    }
                }

                // Display Options GroupBox
                grpDisplay.ForeColor = Color.White;
                grpDisplay.BackColor = Color.FromArgb(45, 45, 48);

                foreach (Control ctrl in grpDisplay.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.White;
                        lbl.BackColor = Color.FromArgb(45, 45, 48);
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.FromArgb(60, 60, 65);
                        cmb.ForeColor = Color.White;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.White;
                        chk.BackColor = Color.FromArgb(45, 45, 48);
                    }
                }

                // Explanation Settings GroupBox
                grpExplanations.ForeColor = Color.White;
                grpExplanations.BackColor = Color.FromArgb(45, 45, 48);

                foreach (Control ctrl in grpExplanations.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.White;
                        lbl.BackColor = Color.FromArgb(45, 45, 48);
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.FromArgb(60, 60, 65);
                        cmb.ForeColor = Color.White;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.White;
                        chk.BackColor = Color.FromArgb(45, 45, 48);
                    }
                }

                // Play Style GroupBox
                grpLc0Features.ForeColor = Color.Cyan;
                grpLc0Features.BackColor = Color.FromArgb(45, 45, 48);

                foreach (Control ctrl in grpLc0Features.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.White;
                        lbl.BackColor = Color.FromArgb(45, 45, 48);
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.White;
                        chk.BackColor = Color.FromArgb(45, 45, 48);
                    }
                    else if (ctrl is TrackBar)
                    {
                        ctrl.BackColor = Color.FromArgb(45, 45, 48);
                    }
                }

                // Buttons
                btnSave.ForeColor = Color.LightGreen;
                btnSave.BackColor = Color.FromArgb(45, 45, 48);
                btnReset.ForeColor = Color.LightCoral;
                btnReset.BackColor = Color.FromArgb(45, 45, 48);
                btnHelp.ForeColor = Color.LightBlue;
                btnHelp.BackColor = Color.FromArgb(45, 45, 48);
                btnCancel.ForeColor = Color.LightGray;
                btnCancel.BackColor = Color.FromArgb(45, 45, 48);

                // Dark Mode Checkbox
                chkDarkMode.ForeColor = Color.White;
                chkDarkMode.BackColor = Color.FromArgb(45, 45, 48);
            }
            else
            {
                // Light Mode Colors
                this.BackColor = Color.WhiteSmoke;

                // GroupBoxes
                grpEngine.ForeColor = Color.Black;
                grpEngine.BackColor = Color.WhiteSmoke;

                foreach (Control ctrl in grpEngine.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.Black;
                        lbl.BackColor = Color.WhiteSmoke;
                    }
                    else if (ctrl is NumericUpDown num)
                    {
                        num.BackColor = Color.White;
                        num.ForeColor = Color.Black;
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.White;
                        cmb.ForeColor = Color.Black;
                    }
                }

                // Display Options GroupBox
                grpDisplay.ForeColor = Color.Black;
                grpDisplay.BackColor = Color.WhiteSmoke;

                foreach (Control ctrl in grpDisplay.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.Black;
                        lbl.BackColor = Color.WhiteSmoke;
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.White;
                        cmb.ForeColor = Color.Black;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.Black;
                        chk.BackColor = Color.WhiteSmoke;
                    }
                }

                // Explanation Settings GroupBox
                grpExplanations.ForeColor = Color.Black;
                grpExplanations.BackColor = Color.WhiteSmoke;

                foreach (Control ctrl in grpExplanations.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.Black;
                        lbl.BackColor = Color.WhiteSmoke;
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        cmb.BackColor = Color.White;
                        cmb.ForeColor = Color.Black;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.Black;
                        chk.BackColor = Color.WhiteSmoke;
                    }
                }

                // Play Style GroupBox
                grpLc0Features.ForeColor = Color.DarkCyan;
                grpLc0Features.BackColor = Color.WhiteSmoke;

                foreach (Control ctrl in grpLc0Features.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = Color.Black;
                        lbl.BackColor = Color.WhiteSmoke;
                    }
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.Black;
                        chk.BackColor = Color.WhiteSmoke;
                    }
                    else if (ctrl is TrackBar)
                    {
                        ctrl.BackColor = Color.WhiteSmoke;
                    }
                }

                // Buttons
                btnSave.ForeColor = Color.DarkGreen;
                btnSave.BackColor = Color.Gainsboro;
                btnReset.ForeColor = Color.DarkRed;
                btnReset.BackColor = Color.Gainsboro;
                btnHelp.ForeColor = Color.DarkBlue;
                btnHelp.BackColor = Color.Gainsboro;
                btnCancel.ForeColor = Color.DarkSlateGray;
                btnCancel.BackColor = Color.Gainsboro;

                // Dark Mode Checkbox
                chkDarkMode.ForeColor = Color.Black;
                chkDarkMode.BackColor = Color.WhiteSmoke;
            }

            // Resume layout to apply all changes at once
            this.ResumeLayout();
        }

        private void ChkPlayStyleEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            trkAggressiveness.Enabled = chkPlayStyleEnabled.Checked;
        }

        private void TrkAggressiveness_Scroll(object? sender, EventArgs e)
        {
            UpdateAggressivenessLabel();
        }

        private void UpdateAggressivenessLabel()
        {
            string style = trkAggressiveness.Value switch
            {
                <= 20 => "Very Solid",
                <= 40 => "Solid",
                <= 60 => "Balanced",
                <= 80 => "Aggressive",
                _ => "Very Aggressive"
            };
            lblAggressivenessValue.Text = $"{trkAggressiveness.Value} ({style})";
        }

        private void CmbComplexity_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? complexity = cmbComplexity.SelectedItem?.ToString();

            switch (complexity)
            {
                case "Beginner":
                    // Beginner (<1200): Tactical Analysis, Opening Principles, WDL
                    chkTactical.Checked = true;
                    chkPositional.Checked = false;
                    chkEndgame.Checked = false;
                    chkOpening.Checked = true;

                    chkThreats.Checked = false;
                    chkWDL.Checked = true;
                    chkOpeningName.Checked = true;
                    chkMoveQuality.Checked = false;
                    chkBookMoves.Checked = false;
                    break;

                case "Intermediate":
                    // Intermediate (1200-1800): All tactical/positional, Move Quality, WDL
                    chkTactical.Checked = true;
                    chkPositional.Checked = true;
                    chkEndgame.Checked = false;
                    chkOpening.Checked = true;

                    chkThreats.Checked = true;
                    chkWDL.Checked = true;
                    chkOpeningName.Checked = true;
                    chkMoveQuality.Checked = true;
                    chkBookMoves.Checked = true;
                    break;

                case "Advanced":
                    // Advanced (1800-2200): All features
                    chkTactical.Checked = true;
                    chkPositional.Checked = true;
                    chkEndgame.Checked = true;
                    chkOpening.Checked = true;

                    chkThreats.Checked = true;
                    chkWDL.Checked = true;
                    chkOpeningName.Checked = true;
                    chkMoveQuality.Checked = true;
                    chkBookMoves.Checked = true;
                    break;

                case "Master":
                    // Master (2200+): All features
                    chkTactical.Checked = true;
                    chkPositional.Checked = true;
                    chkEndgame.Checked = true;
                    chkOpening.Checked = true;

                    chkThreats.Checked = true;
                    chkWDL.Checked = true;
                    chkOpeningName.Checked = true;
                    chkMoveQuality.Checked = true;
                    chkBookMoves.Checked = true;
                    break;
            }
        }
    }
}