using ChessDroid.Models;

namespace ChessDroid
{
    public class BotSettingsDialog : Form
    {
        private ComboBox cmbDifficulty = null!;
        private RadioButton rbPlayWhite = null!;
        private RadioButton rbPlayBlack = null!;
        private Button btnStart = null!;
        private Button btnCancel = null!;

        public BotSettings Settings { get; private set; } = new();

        public BotSettingsDialog(bool isDarkMode)
        {
            InitializeControls();
            ApplyTheme(isDarkMode);
        }

        private void InitializeControls()
        {
            Text = "Play vs Bot";
            Size = new Size(280, 230);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Difficulty label
            var lblDifficulty = new Label
            {
                Text = "Difficulty:",
                Location = new Point(15, 18),
                Size = new Size(75, 20),
                Font = new Font("Courier New", 9F)
            };

            // Difficulty combo
            cmbDifficulty = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(95, 15),
                Size = new Size(160, 23),
                Font = new Font("Courier New", 9F)
            };
            cmbDifficulty.Items.AddRange(new object[] { "Easy", "Medium", "Hard", "Expert", "Master" });
            cmbDifficulty.SelectedIndex = 1; // Default: Medium

            // Color group
            var grpColor = new GroupBox
            {
                Text = "Play as",
                Location = new Point(15, 50),
                Size = new Size(240, 70),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            rbPlayWhite = new RadioButton
            {
                Text = "White",
                Location = new Point(15, 25),
                Size = new Size(90, 25),
                Checked = true,
                Font = new Font("Courier New", 9F, FontStyle.Regular)
            };

            rbPlayBlack = new RadioButton
            {
                Text = "Black",
                Location = new Point(130, 25),
                Size = new Size(90, 25),
                Font = new Font("Courier New", 9F, FontStyle.Regular)
            };

            grpColor.Controls.Add(rbPlayWhite);
            grpColor.Controls.Add(rbPlayBlack);

            // Start button
            btnStart = new Button
            {
                Text = "Start",
                Location = new Point(55, 135),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };
            btnStart.Click += (s, e) =>
            {
                Settings = new BotSettings
                {
                    Difficulty = (BotDifficulty)cmbDifficulty.SelectedIndex,
                    BotPlaysWhite = rbPlayBlack.Checked
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            // Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(145, 135),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[] { lblDifficulty, cmbDifficulty, grpColor, btnStart, btnCancel });
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

                if (c is ComboBox cmb)
                {
                    cmb.BackColor = Color.FromArgb(60, 60, 65);
                    cmb.ForeColor = Color.White;
                }
                else if (c is Button btn)
                {
                    btn.BackColor = Color.FromArgb(60, 60, 65);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
                }
                else if (c is GroupBox grp)
                {
                    grp.ForeColor = Color.White;
                    grp.BackColor = Color.FromArgb(45, 45, 48);
                    foreach (Control gc in grp.Controls)
                    {
                        gc.ForeColor = Color.White;
                        gc.BackColor = Color.FromArgb(45, 45, 48);
                    }
                }
            }
        }
    }
}
