namespace ChessDroid
{
    public class EngineProfilesDialog : Form
    {
        private readonly AppConfig config;
        private ListBox lstEngines = null!;
        private TextBox txtDisplayName = null!;
        private NumericUpDown numElo = null!;
        private Label lblChessdroidValue = null!;
        private Button btnResetChessdroid = null!;
        private Button btnSave = null!;
        private Button btnClose = null!;
        private string? selectedEngine;

        public EngineProfilesDialog(AppConfig config, bool isDarkMode)
        {
            this.config = config;
            InitializeControls();
            LoadEngines();
            ApplyTheme(isDarkMode);
        }

        private void InitializeControls()
        {
            Text = "Engine Profiles";
            Size = new Size(400, 355);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblList = new Label
            {
                Text = "Engine:",
                Location = new Point(15, 18),
                Size = new Size(170, 18),
                Font = new Font("Courier New", 9F)
            };

            lstEngines = new ListBox
            {
                Location = new Point(15, 38),
                Size = new Size(170, 225),
                Font = new Font("Courier New", 9F),
                IntegralHeight = false
            };
            lstEngines.SelectedIndexChanged += LstEngines_SelectedIndexChanged;

            var lblName = new Label
            {
                Text = "Display Name:",
                Location = new Point(200, 38),
                Size = new Size(160, 18),
                Font = new Font("Courier New", 9F)
            };

            txtDisplayName = new TextBox
            {
                Location = new Point(200, 58),
                Size = new Size(160, 22),
                Font = new Font("Courier New", 9F),
                MaxLength = 40,
                Enabled = false
            };

            var lblElo = new Label
            {
                Text = "CCRL Rating:",
                Location = new Point(200, 92),
                Size = new Size(160, 18),
                Font = new Font("Courier New", 9F)
            };

            numElo = new NumericUpDown
            {
                Location = new Point(200, 112),
                Size = new Size(100, 22),
                Font = new Font("Courier New", 9F),
                Minimum = 0,
                Maximum = 5000,
                Value = 0,
                Enabled = false
            };

            var lblHint = new Label
            {
                Text = "(0 = unknown)",
                Location = new Point(200, 138),
                Size = new Size(160, 16),
                Font = new Font("Courier New", 8F),
                ForeColor = Color.Gray
            };

            var lblChessdroidHeader = new Label
            {
                Text = "Chessdroid Rating:",
                Location = new Point(200, 163),
                Size = new Size(160, 18),
                Font = new Font("Courier New", 9F)
            };

            lblChessdroidValue = new Label
            {
                Text = "—",
                Location = new Point(200, 183),
                Size = new Size(160, 18),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            btnResetChessdroid = new Button
            {
                Text = "Reset Chessdroid",
                Location = new Point(200, 207),
                Size = new Size(160, 24),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 8F),
                Enabled = false
            };
            btnResetChessdroid.Click += BtnResetChessdroid_Click;

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(200, 245),
                Size = new Size(75, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnSave.Click += BtnSave_Click;

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(285, 245),
                Size = new Size(75, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblList, lstEngines,
                lblName, txtDisplayName,
                lblElo, numElo, lblHint,
                lblChessdroidHeader, lblChessdroidValue, btnResetChessdroid,
                btnSave, btnClose
            });

            CancelButton = btnClose;
        }

        private void LoadEngines()
        {
            lstEngines.Items.Clear();
            string enginesPath = config.GetEnginesPath();
            if (!Directory.Exists(enginesPath)) return;

            foreach (var file in Directory.GetFiles(enginesPath, "*.exe").OrderBy(Path.GetFileName))
                lstEngines.Items.Add(Path.GetFileName(file));
        }

        private void LstEngines_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedEngine = lstEngines.SelectedItem?.ToString();
            bool hasSelection = !string.IsNullOrEmpty(selectedEngine);

            txtDisplayName.Enabled = hasSelection;
            numElo.Enabled = hasSelection;
            btnSave.Enabled = hasSelection;
            btnResetChessdroid.Enabled = hasSelection;

            if (!hasSelection)
            {
                lblChessdroidValue.Text = "—";
                return;
            }

            if (config.EngineProfiles.TryGetValue(selectedEngine!, out var profile))
            {
                txtDisplayName.Text = profile.DisplayName;
                numElo.Value = Math.Clamp(profile.Elo, 0, 5000);
                RefreshChessdroidDisplay(profile);
            }
            else
            {
                txtDisplayName.Text = Path.GetFileNameWithoutExtension(selectedEngine);
                numElo.Value = 0;
                lblChessdroidValue.Text = "—";
            }
        }

        private void RefreshChessdroidDisplay(EngineProfile? profile = null)
        {
            if (profile == null && selectedEngine != null)
                config.EngineProfiles.TryGetValue(selectedEngine, out profile);

            if (profile == null || profile.ChessdroidElo == 0)
            {
                lblChessdroidValue.Text = "— (no games yet)";
                return;
            }

            string games = profile.GamesPlayed == 1 ? "1 game" : $"{profile.GamesPlayed} games";
            lblChessdroidValue.Text = $"{profile.ChessdroidElo}  ({games})";
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedEngine)) return;

            config.EngineProfiles.TryGetValue(selectedEngine, out var existing);
            config.EngineProfiles[selectedEngine] = new EngineProfile
            {
                DisplayName = txtDisplayName.Text.Trim(),
                Elo = (int)numElo.Value,
                ChessdroidElo = existing?.ChessdroidElo ?? 0,
                GamesPlayed = existing?.GamesPlayed ?? 0
            };
            config.Save();

            btnSave.Text = "Saved!";
            btnSave.Enabled = false;
            Task.Delay(800).ContinueWith(_ => BeginInvoke(() =>
            {
                btnSave.Text = "Save";
                btnSave.Enabled = selectedEngine != null;
            }));
        }

        private void BtnResetChessdroid_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedEngine)) return;
            if (!config.EngineProfiles.TryGetValue(selectedEngine, out var existing)) return;

            config.EngineProfiles[selectedEngine] = new EngineProfile
            {
                DisplayName = existing.DisplayName,
                Elo = existing.Elo,
                ChessdroidElo = 0,
                GamesPlayed = 0
            };
            config.Save();
            RefreshChessdroidDisplay();
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
                    btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
                }
                else if (c is TextBox txt)
                    txt.BackColor = Color.FromArgb(60, 60, 65);
                else if (c is ListBox lb)
                    lb.BackColor = Color.FromArgb(60, 60, 65);
                else if (c is NumericUpDown nud)
                    nud.BackColor = Color.FromArgb(60, 60, 65);
            }

            lblChessdroidValue.ForeColor = Color.FromArgb(100, 200, 255);
        }
    }
}
