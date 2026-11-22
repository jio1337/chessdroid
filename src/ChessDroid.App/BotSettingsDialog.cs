using ChessDroid.Models;

namespace ChessDroid
{
    public class BotSettingsDialog : Form
    {
        private NumericUpDown nudElo = null!;
        private ComboBox cmbEngine = null!;
        private RadioButton rbPlayWhite = null!;
        private RadioButton rbPlayBlack = null!;
        private RadioButton rbFriendly = null!;
        private RadioButton rbChallenge = null!;
        private Button btnStart = null!;
        private Button btnCancel = null!;

        private readonly string[] _engineFileNames;

        private static readonly (string Label, int Elo)[] _presets =
        {
            ("Beginner", 1350),
            ("Club",     1700),
            ("Advanced", 2100),
            ("Expert",   2500),
        };

        public BotSettings Settings { get; private set; } = new();

        public BotSettingsDialog(bool isDarkMode, string[] engines,
            Dictionary<string, EngineProfile> profiles, string defaultEngine,
            bool drillMode = false)
        {
            _engineFileNames = engines;
            InitializeControls(engines, profiles, defaultEngine, drillMode);
            ApplyTheme(isDarkMode);
        }

        private void InitializeControls(string[] engines,
            Dictionary<string, EngineProfile> profiles, string defaultEngine,
            bool drillMode = false)
        {
            Text = "Play vs Bot";
            Size = new Size(280, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // ── Difficulty ────────────────────────────────────────────────
            var lblDifficulty = new Label
            {
                Text = "Target Elo:",
                Location = new Point(15, 18),
                Size = new Size(80, 20),
                Font = new Font("Courier New", 9F)
            };

            nudElo = new NumericUpDown
            {
                Minimum = 1320,
                Maximum = 3190,
                Value   = 1500,
                Increment = 10,
                Location = new Point(100, 15),
                Size = new Size(155, 22),
                Font = new Font("Courier New", 9F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Right
            };

            // Preset buttons row
            var pnlPresets = new Panel
            {
                Location = new Point(10, 45),
                Size = new Size(252, 28)
            };
            int btnW = 60, gap = 4;
            for (int i = 0; i < _presets.Length; i++)
            {
                var (label, elo) = _presets[i];
                var btn = new Button
                {
                    Text = label,
                    Location = new Point(i * (btnW + gap), 0),
                    Size = new Size(btnW, 26),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Courier New", 7.5F),
                    Tag = elo
                };
                btn.Click += (s, _) => nudElo.Value = (int)((Button)s!).Tag!;
                pnlPresets.Controls.Add(btn);
            }

            // ── Engine ────────────────────────────────────────────────────
            var grpEngine = new GroupBox
            {
                Text = "Engine",
                Location = new Point(15, 83),
                Size = new Size(240, 50),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            cmbEngine = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Courier New", 9F),
                Location = new Point(10, 18),
                Size = new Size(220, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            int defaultIndex = 0;
            for (int i = 0; i < engines.Length; i++)
            {
                profiles.TryGetValue(engines[i], out var prof);
                string name = !string.IsNullOrEmpty(prof?.DisplayName)
                    ? prof.DisplayName : Path.GetFileNameWithoutExtension(engines[i]);
                if (prof?.Elo > 0) name += $" ({prof.Elo})";
                cmbEngine.Items.Add(name);
                if (engines[i].Equals(defaultEngine, StringComparison.OrdinalIgnoreCase))
                    defaultIndex = i;
            }
            if (cmbEngine.Items.Count > 0) cmbEngine.SelectedIndex = defaultIndex;

            grpEngine.Controls.Add(cmbEngine);

            // ── Play as ───────────────────────────────────────────────────
            var grpColor = new GroupBox
            {
                Text = "Play as",
                Location = new Point(15, 143),
                Size = new Size(240, 65),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            rbPlayWhite = new RadioButton
            {
                Text = "White",
                Location = new Point(15, 25),
                Size = new Size(90, 25),
                Checked = true,
                Font = new Font("Courier New", 9F)
            };
            rbPlayBlack = new RadioButton
            {
                Text = "Black",
                Location = new Point(130, 25),
                Size = new Size(90, 25),
                Font = new Font("Courier New", 9F)
            };

            grpColor.Controls.Add(rbPlayWhite);
            grpColor.Controls.Add(rbPlayBlack);

            // ── Type ──────────────────────────────────────────────────────
            var grpType = new GroupBox
            {
                Text = "Type",
                Location = new Point(15, 218),
                Size = new Size(240, 65),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            rbFriendly = new RadioButton
            {
                Text = "Friendly",
                Location = new Point(15, 25),
                Size = new Size(95, 25),
                Checked = true,
                Font = new Font("Courier New", 9F)
            };
            rbChallenge = new RadioButton
            {
                Text = "Challenge",
                Location = new Point(120, 25),
                Size = new Size(110, 25),
                Font = new Font("Courier New", 9F)
            };

            grpType.Controls.Add(rbFriendly);
            grpType.Controls.Add(rbChallenge);

            // In drill mode the active side is fixed and challenge mode is always on —
            // hide the Play as and Type groups and compact the form
            if (drillMode)
            {
                grpColor.Visible = false;
                grpType.Visible  = false;
                Size = new Size(280, 240);
            }

            int btnY = drillMode ? 148 : 300;

            // ── Buttons ───────────────────────────────────────────────────
            btnStart = new Button
            {
                Text = "Start",
                Location = new Point(55, btnY),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };
            btnStart.Click += (s, e) =>
            {
                string engineFile = _engineFileNames.Length > 0 && cmbEngine.SelectedIndex >= 0
                    ? _engineFileNames[cmbEngine.SelectedIndex] : "";
                Settings = new BotSettings
                {
                    EloTarget      = (int)nudElo.Value,
                    BotPlaysWhite  = rbPlayBlack.Checked,
                    ChallengeMode  = rbChallenge.Checked,
                    EngineFileName = engineFile
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(145, btnY),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblDifficulty, nudElo, pnlPresets,
                grpEngine, grpColor, grpType, btnStart, btnCancel
            });
            AcceptButton = btnStart;
            CancelButton = btnCancel;
        }

        private void ApplyTheme(bool isDarkMode)
        {
            if (!isDarkMode) return;

            BackColor = Color.FromArgb(45, 45, 48);
            ForeColor = Color.White;

            foreach (Control c in Controls)
            {
                c.ForeColor = Color.White;
                c.BackColor = Color.FromArgb(45, 45, 48);

                if (c is Button btn)
                {
                    btn.BackColor = Color.FromArgb(60, 60, 65);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
                }
                else if (c is NumericUpDown nud)
                {
                    nud.BackColor = Color.FromArgb(60, 60, 65);
                    nud.ForeColor = Color.White;
                }
                else if (c is Panel pnl)
                {
                    pnl.BackColor = Color.FromArgb(45, 45, 48);
                    foreach (Control pc in pnl.Controls)
                    {
                        pc.ForeColor = Color.White;
                        pc.BackColor = Color.FromArgb(45, 45, 48);
                        if (pc is Button pb)
                        {
                            pb.BackColor = Color.FromArgb(60, 60, 65);
                            pb.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
                        }
                    }
                }
                else if (c is GroupBox grp)
                {
                    grp.ForeColor = Color.White;
                    grp.BackColor = Color.FromArgb(45, 45, 48);
                    foreach (Control gc in grp.Controls)
                    {
                        gc.ForeColor = Color.White;
                        gc.BackColor = Color.FromArgb(45, 45, 48);
                        if (gc is ComboBox cmb)
                            cmb.BackColor = Color.FromArgb(60, 60, 65);
                    }
                }
            }
        }
    }
}
