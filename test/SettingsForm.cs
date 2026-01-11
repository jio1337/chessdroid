#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace ChessDroid
{
    public partial class SettingsForm : Form
    {
        private AppConfig config;
        private Action? onConfigChanged;

        public SettingsForm(AppConfig config, Action? onConfigChanged = null)
        {
            this.config = config;
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

            // Detection settings
            numMatchThreshold.Value = (decimal)config.MatchThreshold;
            numEngineTimeout.Value = config.EngineResponseTimeoutMs;
            numMaxRetries.Value = config.MaxEngineRetries;
            numMoveTimeout.Value = config.MoveTimeoutMs;
            numCannyLow.Value = config.CannyThresholdLow;
            numCannyHigh.Value = config.CannyThresholdHigh;
            numMinBoardArea.Value = config.MinBoardArea;
            chkDebugCells.Checked = config.ShowDebugCells;

            // Engine depth
            if (config.EngineDepth >= 1 && config.EngineDepth <= 20)
            {
                cmbEngineDepth.SelectedItem = config.EngineDepth;
            }
            else
            {
                cmbEngineDepth.SelectedItem = 15; // Default
            }

            // Display options
            PopulateEnginesComboBox();

            // Site selection
            cmbSite.Items.Clear();
            cmbSite.Items.Add("Lichess");
            cmbSite.Items.Add("Chess.com");
            cmbSite.SelectedItem = config.SelectedSite;

            // Best lines checkboxes
            chkShowBest.Checked = config.ShowBestLine;
            chkShowSecond.Checked = config.ShowSecondLine;
            chkShowThird.Checked = config.ShowThirdLine;

            // Explanation settings
            PopulateComplexityComboBox();
            cmbComplexity.SelectedItem = config.ExplanationComplexity ?? "Intermediate";
            chkTactical.Checked = config.ShowTacticalAnalysis;
            chkPositional.Checked = config.ShowPositionalAnalysis;
            chkEndgame.Checked = config.ShowEndgameAnalysis;
            chkOpening.Checked = config.ShowOpeningPrinciples;
            chkWinRate.Checked = config.ShowWinPercentage;
            chkTablebase.Checked = config.ShowTablebaseInfo;
            chkColorCoding.Checked = config.ShowMoveQualityColor;
            chkSEE.Checked = config.ShowSEEValues;

            // Load theme preference
            chkDarkMode.Checked = config.Theme == "Dark";
            ApplyTheme(config.Theme == "Dark");
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Validate Canny thresholds
            if (numCannyHigh.Value < numCannyLow.Value * 2)
            {
                MessageBox.Show("Canny High Threshold should be at least 2x the Low Threshold.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate Engine Depth selection
            if (cmbEngineDepth.SelectedItem == null)
            {
                MessageBox.Show("Please select an Engine Depth value.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save values
            config.MatchThreshold = (double)numMatchThreshold.Value;
            config.EngineResponseTimeoutMs = (int)numEngineTimeout.Value;
            config.MaxEngineRetries = (int)numMaxRetries.Value;
            config.MoveTimeoutMs = (int)numMoveTimeout.Value;
            config.CannyThresholdLow = (int)numCannyLow.Value;
            config.CannyThresholdHigh = (int)numCannyHigh.Value;
            config.MinBoardArea = (int)numMinBoardArea.Value;
            config.EngineDepth = (int)cmbEngineDepth.SelectedItem;
            config.Theme = chkDarkMode.Checked ? "Dark" : "Light";
            config.ShowDebugCells = chkDebugCells.Checked;

            // Save display options
            config.SelectedEngine = cmbEngine.SelectedItem?.ToString() ?? "";
            config.SelectedSite = cmbSite.SelectedItem?.ToString() ?? "Lichess";
            config.ShowBestLine = chkShowBest.Checked;
            config.ShowSecondLine = chkShowSecond.Checked;
            config.ShowThirdLine = chkShowThird.Checked;

            // Save explanation settings
            config.ExplanationComplexity = cmbComplexity.SelectedItem?.ToString() ?? "Intermediate";
            config.ShowTacticalAnalysis = chkTactical.Checked;
            config.ShowPositionalAnalysis = chkPositional.Checked;
            config.ShowEndgameAnalysis = chkEndgame.Checked;
            config.ShowOpeningPrinciples = chkOpening.Checked;
            config.ShowWinPercentage = chkWinRate.Checked;
            config.ShowTablebaseInfo = chkTablebase.Checked;
            config.ShowMoveQualityColor = chkColorCoding.Checked;
            config.ShowSEEValues = chkSEE.Checked;

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
                grpDetection.ForeColor = Color.White;
                grpDetection.BackColor = Color.FromArgb(45, 45, 48);
                grpEngine.ForeColor = Color.White;
                grpEngine.BackColor = Color.FromArgb(45, 45, 48);

                // Labels
                foreach (Control ctrl in grpDetection.Controls)
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
                }

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
                grpDetection.ForeColor = Color.Black;
                grpDetection.BackColor = Color.WhiteSmoke;
                grpEngine.ForeColor = Color.Black;
                grpEngine.BackColor = Color.WhiteSmoke;

                // Labels
                foreach (Control ctrl in grpDetection.Controls)
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
                }

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
    }
}