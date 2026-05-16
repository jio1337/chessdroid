namespace ChessDroid
{
    public partial class SettingsForm : Form
    {
        private AppConfig config;
        private readonly Action? onConfigChanged;
        private string _selectedFontFamily = "Consolas";
        private float _selectedFontSize = 10.0f;

        private static readonly (string Name, string Light, string Dark)[] ColorPresets =
        {
            ("Brown (default)", "#F0D9B5", "#B58863"),
            ("Green",           "#EEEED2", "#769656"),
            ("Blue",            "#DEE3E6", "#8CA2AD"),
            ("Red",             "#F2D0C3", "#B55239"),
            ("Pink",            "#F5D6E3", "#C0708A"),
            ("Purple",          "#E8DCEF", "#8C69AA"),
            ("Gray",            "#E8E8E8", "#8A8A8A"),
        };

        public SettingsForm(AppConfig config, Action? onConfigChanged = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.onConfigChanged = onConfigChanged;
            InitializeComponent();

            PopulateColorPresets();
            LoadSettings();
        }

        private void PopulateColorPresets()
        {
            cmbColorPreset.Items.Clear();
            foreach (var (name, _, _) in ColorPresets)
                cmbColorPreset.Items.Add(name);
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
            numEngineDepth.Value = Math.Clamp(config.EngineDepth, 1, 40);

            // Min analysis time
            numMinAnalysisTime.Value = config.MinAnalysisTimeMs;

            // Display options
            PopulateEnginesComboBox();

            // Best lines checkboxes
            chkShowBest.Checked = config.ShowBestLine;
            chkShowSecond.Checked = config.ShowSecondLine;
            chkShowThird.Checked = config.ShowThirdLine;
            cmbArrowCount.SelectedIndex = Math.Clamp(config.EngineArrowCount, 0, 3);
            chkEvalBar.Checked = config.ShowEvalBar;

            _selectedFontFamily = config.ConsoleFontFamily;
            _selectedFontSize = config.ConsoleFontSize;
            btnChooseFont.Text = $"{_selectedFontFamily} {_selectedFontSize:0.#}pt";

            // Board colors
            try { btnLightColor.BackColor = ColorTranslator.FromHtml(config.LightSquareColor); } catch { btnLightColor.BackColor = Color.FromArgb(240, 217, 181); }
            try { btnDarkColor.BackColor = ColorTranslator.FromHtml(config.DarkSquareColor); } catch { btnDarkColor.BackColor = Color.FromArgb(181, 136, 99); }
            int presetMatch = Array.FindIndex(ColorPresets, p =>
                string.Equals(p.Light, config.LightSquareColor, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.Dark, config.DarkSquareColor, StringComparison.OrdinalIgnoreCase));
            cmbColorPreset.SelectedIndex = presetMatch;
            chkSquareLabels.Checked = config.ShowSquareLabels;
            chkThreatArrows.Checked = config.ShowThreatArrows;
            chkLastMoveHighlight.Checked = config.ShowLastMoveHighlight;
            chkBookArrows.Checked = config.ShowBookArrows;
            chkEvalGraph.Checked = config.ShowEvalGraph;
            chkAnimations.Checked = config.ShowAnimations;
            numAnimationMs.Value = Math.Max(50, Math.Min(500, config.AnimationDurationMs));
            numAnimationMs.Enabled = config.ShowAnimations;
            chkAnimations.CheckedChanged += (s, e) => numAnimationMs.Enabled = chkAnimations.Checked;
            chkMaterialStrips.Checked = config.ShowMaterialStrips;

            // Explanation settings
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

            chkContinuousAnalysis.Checked = config.ContinuousAnalysis;
            numContinuousMaxDepth.Value = Math.Clamp(config.ContinuousAnalysisMaxDepth, 10, 100);
            numContinuousMaxDepth.Enabled = config.ContinuousAnalysis;
            chkContinuousAnalysis.CheckedChanged += (s, e) => numContinuousMaxDepth.Enabled = chkContinuousAnalysis.Checked;
            numAutoPlayInterval.Value = Math.Clamp(config.AutoPlayInterval, 200, 2000);

            // Load theme preference
            chkDarkMode.Checked = config.Theme == "Dark";
            ApplyTheme(config.Theme == "Dark");
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Save engine values
            config.EngineResponseTimeoutMs = (int)numEngineTimeout.Value;
            config.MaxEngineRetries = (int)numMaxRetries.Value;
            config.MoveTimeoutMs = (int)numMoveTimeout.Value;
            config.EngineDepth = (int)numEngineDepth.Value;
            config.MinAnalysisTimeMs = (int)numMinAnalysisTime.Value;
            config.Theme = chkDarkMode.Checked ? "Dark" : "Light";

            // Save display options
            config.SelectedEngine = cmbEngine.SelectedItem?.ToString() ?? "";
            config.ShowBestLine = chkShowBest.Checked;
            config.ShowSecondLine = chkShowSecond.Checked;
            config.ShowThirdLine = chkShowThird.Checked;
            config.EngineArrowCount = cmbArrowCount.SelectedIndex;
            config.ShowEvalBar = chkEvalBar.Checked;
            config.ConsoleFontFamily = _selectedFontFamily;
            config.ConsoleFontSize = _selectedFontSize;
            config.LightSquareColor = ColorTranslator.ToHtml(btnLightColor.BackColor);
            config.DarkSquareColor = ColorTranslator.ToHtml(btnDarkColor.BackColor);
            config.ShowSquareLabels = chkSquareLabels.Checked;
            config.ShowThreatArrows = chkThreatArrows.Checked;
            config.ShowLastMoveHighlight = chkLastMoveHighlight.Checked;
            config.ShowBookArrows = chkBookArrows.Checked;
            config.ShowEvalGraph = chkEvalGraph.Checked;
            config.ShowAnimations = chkAnimations.Checked;
            config.AnimationDurationMs = (int)numAnimationMs.Value;
            config.ShowMaterialStrips = chkMaterialStrips.Checked;

            // Save explanation settings
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
            config.ContinuousAnalysis = chkContinuousAnalysis.Checked;
            config.ContinuousAnalysisMaxDepth = (int)numContinuousMaxDepth.Value;
            config.AutoPlayInterval = (int)numAutoPlayInterval.Value;

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

        private void BtnChooseFont_Click(object? sender, EventArgs e)
        {
            using var dlg = new FontDialog
            {
                Font = new Font(_selectedFontFamily, _selectedFontSize),
                ShowEffects = false,
                AllowScriptChange = false
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _selectedFontFamily = dlg.Font.FontFamily.Name;
                _selectedFontSize = dlg.Font.Size;
                btnChooseFont.Text = $"{_selectedFontFamily} {_selectedFontSize:0.#}pt";
            }
        }

        private void BtnLightColor_Click(object? sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = btnLightColor.BackColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                btnLightColor.BackColor = dlg.Color;
        }

        private void BtnDarkColor_Click(object? sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = btnDarkColor.BackColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                btnDarkColor.BackColor = dlg.Color;
        }

        private void BtnResetColors_Click(object? sender, EventArgs e)
        {
            btnLightColor.BackColor = Color.FromArgb(240, 217, 181);
            btnDarkColor.BackColor = Color.FromArgb(181, 136, 99);
            cmbColorPreset.SelectedIndex = -1;
        }

        private void CmbColorPreset_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = cmbColorPreset.SelectedIndex;
            if (idx < 0 || idx >= ColorPresets.Length) return;
            var (_, light, dark) = ColorPresets[idx];
            btnLightColor.BackColor = ColorTranslator.FromHtml(light);
            btnDarkColor.BackColor = ColorTranslator.FromHtml(dark);
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
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.White;
                        chk.BackColor = Color.FromArgb(45, 45, 48);
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
                    else if (ctrl is Button btn)
                    {
                        btn.ForeColor = Color.White;
                        btn.BackColor = Color.FromArgb(60, 60, 65);
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

                // Board Colors GroupBox
                grpBoardColors.ForeColor = Color.White;
                grpBoardColors.BackColor = Color.FromArgb(45, 45, 48);
                lblLightSquares.ForeColor = Color.White;
                lblLightSquares.BackColor = Color.FromArgb(45, 45, 48);
                lblDarkSquares.ForeColor = Color.White;
                lblDarkSquares.BackColor = Color.FromArgb(45, 45, 48);
                lblColorPreset.ForeColor = Color.White;
                lblColorPreset.BackColor = Color.FromArgb(45, 45, 48);
                cmbColorPreset.BackColor = Color.FromArgb(60, 60, 65);
                cmbColorPreset.ForeColor = Color.White;
                btnResetColors.ForeColor = Color.White;
                btnResetColors.BackColor = Color.FromArgb(60, 60, 65);
                lblSquareLabels.ForeColor = Color.White;
                lblSquareLabels.BackColor = Color.FromArgb(45, 45, 48);
                chkSquareLabels.ForeColor = Color.White;
                chkSquareLabels.BackColor = Color.FromArgb(45, 45, 48);
                lblThreatArrows.ForeColor = Color.White;
                lblThreatArrows.BackColor = Color.FromArgb(45, 45, 48);
                chkThreatArrows.ForeColor = Color.White;
                chkThreatArrows.BackColor = Color.FromArgb(45, 45, 48);
                lblLastMoveHighlight.ForeColor = Color.White;
                lblLastMoveHighlight.BackColor = Color.FromArgb(45, 45, 48);
                chkLastMoveHighlight.ForeColor = Color.White;
                chkLastMoveHighlight.BackColor = Color.FromArgb(45, 45, 48);
                lblBookArrows.ForeColor = Color.White;
                lblBookArrows.BackColor = Color.FromArgb(45, 45, 48);
                chkBookArrows.ForeColor = Color.White;
                chkBookArrows.BackColor = Color.FromArgb(45, 45, 48);
                lblEvalGraph.ForeColor = Color.White;
                lblEvalGraph.BackColor = Color.FromArgb(45, 45, 48);
                chkEvalGraph.ForeColor = Color.White;
                chkEvalGraph.BackColor = Color.FromArgb(45, 45, 48);
                lblAnimations.ForeColor = Color.White;
                lblAnimations.BackColor = Color.FromArgb(45, 45, 48);
                chkAnimations.ForeColor = Color.White;
                chkAnimations.BackColor = Color.FromArgb(45, 45, 48);
                lblAnimationSpeed.ForeColor = Color.White;
                lblAnimationSpeed.BackColor = Color.FromArgb(45, 45, 48);
                numAnimationMs.ForeColor = Color.White;
                numAnimationMs.BackColor = Color.FromArgb(45, 45, 48);
                lblMaterialStrips.ForeColor = Color.White;
                lblMaterialStrips.BackColor = Color.FromArgb(45, 45, 48);
                chkMaterialStrips.ForeColor = Color.White;
                chkMaterialStrips.BackColor = Color.FromArgb(45, 45, 48);
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
                    else if (ctrl is CheckBox chk)
                    {
                        chk.ForeColor = Color.Black;
                        chk.BackColor = Color.WhiteSmoke;
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
                    else if (ctrl is Button btn)
                    {
                        btn.ForeColor = Color.Black;
                        btn.BackColor = Color.White;
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

                // Board Colors GroupBox
                grpBoardColors.ForeColor = Color.Black;
                grpBoardColors.BackColor = Color.WhiteSmoke;
                lblLightSquares.ForeColor = Color.Black;
                lblLightSquares.BackColor = Color.WhiteSmoke;
                lblDarkSquares.ForeColor = Color.Black;
                lblDarkSquares.BackColor = Color.WhiteSmoke;
                lblColorPreset.ForeColor = Color.Black;
                lblColorPreset.BackColor = Color.WhiteSmoke;
                cmbColorPreset.BackColor = Color.White;
                cmbColorPreset.ForeColor = Color.Black;
                btnResetColors.ForeColor = Color.DarkSlateGray;
                btnResetColors.BackColor = Color.Gainsboro;
                lblSquareLabels.ForeColor = Color.Black;
                lblSquareLabels.BackColor = Color.WhiteSmoke;
                chkSquareLabels.ForeColor = Color.Black;
                chkSquareLabels.BackColor = Color.WhiteSmoke;
                lblThreatArrows.ForeColor = Color.Black;
                lblThreatArrows.BackColor = Color.WhiteSmoke;
                chkThreatArrows.ForeColor = Color.Black;
                chkThreatArrows.BackColor = Color.WhiteSmoke;
                lblLastMoveHighlight.ForeColor = Color.Black;
                lblLastMoveHighlight.BackColor = Color.WhiteSmoke;
                chkLastMoveHighlight.ForeColor = Color.Black;
                chkLastMoveHighlight.BackColor = Color.WhiteSmoke;
                lblBookArrows.ForeColor = Color.Black;
                lblBookArrows.BackColor = Color.WhiteSmoke;
                chkBookArrows.ForeColor = Color.Black;
                chkBookArrows.BackColor = Color.WhiteSmoke;
                lblEvalGraph.ForeColor = Color.Black;
                lblEvalGraph.BackColor = Color.WhiteSmoke;
                chkEvalGraph.ForeColor = Color.Black;
                chkEvalGraph.BackColor = Color.WhiteSmoke;
                lblAnimations.ForeColor = Color.Black;
                lblAnimations.BackColor = Color.WhiteSmoke;
                chkAnimations.ForeColor = Color.Black;
                chkAnimations.BackColor = Color.WhiteSmoke;
                lblAnimationSpeed.ForeColor = Color.Black;
                lblAnimationSpeed.BackColor = Color.WhiteSmoke;
                numAnimationMs.ForeColor = Color.Black;
                numAnimationMs.BackColor = Color.WhiteSmoke;
                lblMaterialStrips.ForeColor = Color.Black;
                lblMaterialStrips.BackColor = Color.WhiteSmoke;
                chkMaterialStrips.ForeColor = Color.Black;
                chkMaterialStrips.BackColor = Color.WhiteSmoke;

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

    }
}