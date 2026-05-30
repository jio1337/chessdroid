using ChessDroid.Models;

namespace ChessDroid
{
    public class DrillWatchDialog : Form
    {
        private ComboBox cmbWhite = null!;
        private ComboBox cmbBlack = null!;
        private Button btnStart  = null!;
        private Button btnCancel = null!;

        private readonly string[] _engineFileNames;

        public string WhiteEngine => _engineFileNames[cmbWhite.SelectedIndex];
        public string BlackEngine => _engineFileNames[cmbBlack.SelectedIndex];

        public DrillWatchDialog(bool isDarkMode, string[] engines,
            Dictionary<string, EngineProfile> profiles, string defaultEngine)
        {
            _engineFileNames = engines;
            InitializeControls(engines, profiles, defaultEngine);
            ApplyTheme(isDarkMode);
        }

        private void InitializeControls(string[] engines,
            Dictionary<string, EngineProfile> profiles, string defaultEngine)
        {
            Text = "Watch Engines Play";
            Size = new Size(280, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var lblWhite = new Label
            {
                Text = "White:", Location = new Point(15, 18),
                Size = new Size(50, 20), Font = new Font("Courier New", 9F)
            };
            var lblBlack = new Label
            {
                Text = "Black:", Location = new Point(15, 55),
                Size = new Size(50, 20), Font = new Font("Courier New", 9F)
            };

            cmbWhite = BuildEngineCombo(engines, profiles, defaultEngine, new Point(70, 15));
            cmbBlack = BuildEngineCombo(engines, profiles, defaultEngine, new Point(70, 52));

            // Default: if 2+ engines, make Black the second one
            if (cmbBlack.Items.Count > 1) cmbBlack.SelectedIndex = 1;

            btnStart = new Button
            {
                Text = "Start", Location = new Point(45, 110),
                Size = new Size(80, 35), FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };
            btnStart.Click += (_, _) =>
            {
                if (cmbWhite.SelectedIndex < 0 || cmbBlack.SelectedIndex < 0) return;
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel = new Button
            {
                Text = "Cancel", Location = new Point(145, 110),
                Size = new Size(80, 35), FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblWhite, lblBlack, cmbWhite, cmbBlack, btnStart, btnCancel });
            AcceptButton = btnStart;
            CancelButton = btnCancel;
        }

        private ComboBox BuildEngineCombo(string[] engines,
            Dictionary<string, EngineProfile> profiles, string defaultEngine, Point location)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Courier New", 9F),
                Location = location,
                Size = new Size(195, 23)
            };
            int defaultIdx = 0;
            for (int i = 0; i < engines.Length; i++)
            {
                profiles.TryGetValue(engines[i], out var prof);
                string name = !string.IsNullOrEmpty(prof?.DisplayName)
                    ? prof.DisplayName : Path.GetFileNameWithoutExtension(engines[i]);
                if (prof?.Elo > 0) name += $" ({prof.Elo})";
                cmb.Items.Add(name);
                if (engines[i].Equals(defaultEngine, StringComparison.OrdinalIgnoreCase))
                    defaultIdx = i;
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = defaultIdx;
            return cmb;
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
                else if (c is ComboBox cmb)
                    cmb.BackColor = Color.FromArgb(60, 60, 65);
            }
        }
    }
}
