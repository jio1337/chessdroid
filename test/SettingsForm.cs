namespace ChessDroid
{
    public partial class SettingsForm : Form
    {
        private AppConfig config;
        private readonly Action? onConfigChanged;
        private string _selectedFontFamily = "Consolas";
        private float _selectedFontSize = 10.0f;

        internal static readonly (string Name, string Light, string Dark)[] ColorPresets =
        {
            ("Brown (default)", "#F0D9B5", "#B58863"),
            ("Green",           "#EEEED2", "#769656"),
            ("Blue",            "#DEE3E6", "#8CA2AD"),
            ("Red",             "#F2D0C3", "#B55239"),
            ("Pink",            "#F5D6E3", "#C0708A"),
            ("Purple",          "#E8DCEF", "#8C69AA"),
            ("Gray",            "#E8E8E8", "#8A8A8A"),
            ("Cyberpunk",       "#C0F2EE", "#8080C0"),
            ("Midnight",        "#D0DCE8", "#607090"),
            ("Forest",          "#D4E8CC", "#5A8A6A"),
            ("Walnut",          "#EDD5A8", "#9C7050"),
            ("Arctic",          "#DCF0F8", "#5890B0"),
            ("Pink Soldier",    "#E070E0", "#942C4F"),
            ("Sungrass",        "#D7E070", "#46942C"),
            ("Space Traveler",  "#8C70E0", "#8B2C94"),
            ("Atlantis",        "#70E0A8", "#2C8394"),
            ("Miami",           "#F7B5CD", "#D9508C"),
        };

        public SettingsForm(AppConfig config, Action? onConfigChanged = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.onConfigChanged = onConfigChanged;
            InitializeComponent();

            LoadSettings();
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
            chkSquareLabels.Checked = config.ShowSquareLabels;
            chkCoordinates.Checked = config.ShowCoordinates;
            chkThreatArrows.Checked = config.ShowThreatArrows;
            chkLastMoveHighlight.Checked = config.ShowLastMoveHighlight;
            chkBookArrows.Checked = config.ShowBookArrows;
            chkEvalGraph.Checked = config.ShowEvalGraph;
            chkAnimations.Checked = config.ShowAnimations;
            numAnimationMs.Value = Math.Max(50, Math.Min(500, config.AnimationDurationMs));
            numAnimationMs.Enabled = config.ShowAnimations;
            chkAnimations.CheckedChanged += (s, e) => numAnimationMs.Enabled = chkAnimations.Checked;
            chkMaterialStrips.Checked = config.ShowMaterialStrips;
            chkShowLegalMoves.Checked = config.ShowLegalMoves;
            cmbMovementMode.SelectedItem = config.MovementMode;
            if (cmbMovementMode.SelectedIndex < 0) cmbMovementMode.SelectedIndex = 0;

            var layoutCodes = new[] { "BMA", "BAM", "MBA", "MAB", "ABM", "AMB" };
            int layoutIdx = Array.IndexOf(layoutCodes, config.PanelLayout ?? "BMA");
            cmbPanelLayout.SelectedIndex = Math.Max(0, layoutIdx);

            // Board effects
            chkGradient.Checked = config.GradientBoard;
            chkVignette.Checked = config.BoardVignette;
            numVigAlpha.Value = Math.Clamp(config.VignetteAlpha, 10, 240);
            chkPieceGlow.Checked = config.PieceGlow;
            chkBoardFrame.Checked = config.BoardFrame;
            try { btnFrameColor.BackColor = ColorTranslator.FromHtml(config.BoardFrameColor); }
            catch { btnFrameColor.BackColor = Color.FromArgb(80, 50, 25); }

            // Sound effects
            chkSoundEffects.Checked = config.SoundEffectsEnabled;

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

            txtSyzygyPath.Text = config.SyzygyPath;

            chkContinuousAnalysis.Checked = config.ContinuousAnalysis;
            chkShowExplanations.Checked = config.ShowExplanations;
            UpdateExplanationSubControls(config.ShowExplanations);
            chkShowExplanations.CheckedChanged += (s, e) => UpdateExplanationSubControls(chkShowExplanations.Checked);
            numAutoPlayInterval.Value = Math.Clamp(config.AutoPlayInterval, 200, 2000);

            // Load theme preference
            cmbTheme.Items.Clear();
            foreach (var name in ChessDroid.Services.ThemeService.ThemeNames)
                cmbTheme.Items.Add(name);
            int themeIdx = Array.IndexOf(ChessDroid.Services.ThemeService.ThemeNames, config.Theme);
            cmbTheme.SelectedIndex = themeIdx >= 0 ? themeIdx : 0;
            ApplyTheme(config.Theme);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Save engine values
            config.EngineResponseTimeoutMs = (int)numEngineTimeout.Value;
            config.MaxEngineRetries = (int)numMaxRetries.Value;
            config.MoveTimeoutMs = (int)numMoveTimeout.Value;
            config.EngineDepth = (int)numEngineDepth.Value;
            config.MinAnalysisTimeMs = (int)numMinAnalysisTime.Value;
            config.Theme = cmbTheme.SelectedItem?.ToString() ?? "Dark";

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
            config.ShowCoordinates = chkCoordinates.Checked;
            config.ShowThreatArrows = chkThreatArrows.Checked;
            config.ShowLastMoveHighlight = chkLastMoveHighlight.Checked;
            config.ShowBookArrows = chkBookArrows.Checked;
            config.ShowEvalGraph = chkEvalGraph.Checked;
            config.ShowAnimations = chkAnimations.Checked;
            config.AnimationDurationMs = (int)numAnimationMs.Value;
            config.ShowMaterialStrips = chkMaterialStrips.Checked;
            config.ShowLegalMoves = chkShowLegalMoves.Checked;
            config.MovementMode = cmbMovementMode.SelectedItem?.ToString() ?? "Both";
            config.PanelLayout = cmbPanelLayout.SelectedIndex switch {
                1 => "BAM", 2 => "MBA", 3 => "MAB", 4 => "ABM", 5 => "AMB", _ => "BMA"
            };

            // Board effects
            config.GradientBoard = chkGradient.Checked;
            config.BoardVignette = chkVignette.Checked;
            config.VignetteAlpha = (int)numVigAlpha.Value;
            config.PieceGlow = chkPieceGlow.Checked;
            config.BoardFrame = chkBoardFrame.Checked;
            config.BoardFrameColor = ColorTranslator.ToHtml(btnFrameColor.BackColor);
            config.SoundEffectsEnabled = chkSoundEffects.Checked;

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
            config.SyzygyPath = txtSyzygyPath.Text.Trim();
            config.ContinuousAnalysis = chkContinuousAnalysis.Checked;
            config.ShowExplanations = chkShowExplanations.Checked;
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

        private void BtnBrowseSyzygy_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select Syzygy tablebase folder (.rtbw / .rtbz files)",
                UseDescriptionForTitle = true
            };
            if (!string.IsNullOrEmpty(txtSyzygyPath.Text) && Directory.Exists(txtSyzygyPath.Text))
                dlg.SelectedPath = txtSyzygyPath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSyzygyPath.Text = dlg.SelectedPath;
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

        private void BtnFrameColor_Click(object? sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = btnFrameColor.BackColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                btnFrameColor.BackColor = dlg.Color;
        }

        private void CmbTheme_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyTheme(cmbTheme.SelectedItem?.ToString() ?? "Dark");
        }

        private void ApplyTheme(string theme)
        {
            bool isDark = ChessDroid.Services.ThemeService.IsDarkTheme(theme);
            this.SuspendLayout();
            Color bg       = isDark ? Color.FromArgb(45, 45, 48) : Color.WhiteSmoke;
            Color fg       = isDark ? Color.White                : Color.Black;
            Color inputBg  = isDark ? Color.FromArgb(60, 60, 65) : Color.White;
            this.BackColor = bg;
            ApplyThemeRecursive(this, fg, bg, inputBg);
            btnSave.ForeColor    = isDark ? Color.LightGreen    : Color.DarkGreen;
            btnSave.BackColor    = isDark ? bg : Color.Gainsboro;
            btnReset.ForeColor   = isDark ? Color.LightCoral    : Color.DarkRed;
            btnReset.BackColor   = isDark ? bg : Color.Gainsboro;
            btnHelp.ForeColor    = isDark ? Color.LightBlue     : Color.DarkBlue;
            btnHelp.BackColor    = isDark ? bg : Color.Gainsboro;
            btnCancel.ForeColor  = isDark ? Color.LightGray     : Color.DarkSlateGray;
            btnCancel.BackColor  = isDark ? bg : Color.Gainsboro;
            btnChooseFont.ForeColor   = isDark ? Color.White : Color.Black;
            btnChooseFont.BackColor   = isDark ? inputBg : Color.White;
            btnBrowseSyzygy.ForeColor = isDark ? Color.White : Color.Black;
            btnBrowseSyzygy.BackColor = isDark ? inputBg : Color.White;
            this.ResumeLayout();
        }

        private static void ApplyThemeRecursive(Control parent, Color fg, Color bg, Color inputBg)
        {
            foreach (Control c in parent.Controls)
            {
                switch (c)
                {
                    case TabControl tc:   tc.BackColor = bg;  ApplyThemeRecursive(tc, fg, bg, inputBg); break;
                    case TabPage tp:      tp.BackColor = bg;  ApplyThemeRecursive(tp, fg, bg, inputBg); break;
                    case GroupBox gb:     gb.ForeColor = fg;  gb.BackColor = bg; ApplyThemeRecursive(gb, fg, bg, inputBg); break;
                    case Panel p:         p.BackColor  = bg;  ApplyThemeRecursive(p,  fg, bg, inputBg); break;
                    case Label lbl:       lbl.ForeColor = fg; lbl.BackColor = bg; break;
                    case CheckBox chk:    chk.ForeColor = fg; chk.BackColor = bg; break;
                    case NumericUpDown n: n.ForeColor   = fg; n.BackColor   = inputBg; break;
                    case ComboBox cmb:    cmb.ForeColor  = fg; cmb.BackColor  = inputBg; break;
                    case TextBox txt:     txt.ForeColor  = fg; txt.BackColor  = inputBg; break;
                    case TrackBar trk:    trk.BackColor  = bg; break;
                }
            }
        }

        private void ChkPlayStyleEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            trkAggressiveness.Enabled = chkPlayStyleEnabled.Checked;
        }

        private void TrkAggressiveness_Scroll(object? sender, EventArgs e)
        {
            UpdateAggressivenessLabel();
        }

        private void UpdateExplanationSubControls(bool enabled)
        {
            chkTactical.Enabled   = enabled;
            chkPositional.Enabled = enabled;
            chkEndgame.Enabled    = enabled;
            chkOpening.Enabled    = enabled;
            chkMoveQuality.Enabled = enabled;
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