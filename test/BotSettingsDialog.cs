using ChessDroid.Models;

namespace ChessDroid
{
    public class BotSettingsDialog : Form
    {
        private TrackBar trkDifficulty = null!;
        private Label lblDifficultyValue = null!;
        private RadioButton rbPlayWhite = null!;
        private RadioButton rbPlayBlack = null!;
        private RadioButton rbFriendly = null!;
        private RadioButton rbChallenge = null!;
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
            Size = new Size(280, 365);
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

            // Live level label (right side)
            lblDifficultyValue = new Label
            {
                Text = "Level 8",
                Location = new Point(95, 18),
                Size = new Size(160, 20),
                Font = new Font("Courier New", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Trackbar (1–20)
            trkDifficulty = new TrackBar
            {
                Minimum = 1,
                Maximum = 20,
                Value = 8,
                TickFrequency = 1,
                LargeChange = 5,
                SmallChange = 1,
                Location = new Point(10, 40),
                Size = new Size(250, 40),
                AutoSize = false
            };
            trkDifficulty.Scroll += (s, e) =>
            {
                lblDifficultyValue.Text = $"Level {trkDifficulty.Value}";
            };

            // Range labels
            var lblMin = new Label
            {
                Text = "1",
                Location = new Point(10, 80),
                Size = new Size(20, 16),
                Font = new Font("Courier New", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblMax = new Label
            {
                Text = "20",
                Location = new Point(240, 80),
                Size = new Size(24, 16),
                Font = new Font("Courier New", 8F),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Color group
            var grpColor = new GroupBox
            {
                Text = "Play as",
                Location = new Point(15, 105),
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

            // Type group
            var grpType = new GroupBox
            {
                Text = "Type",
                Location = new Point(15, 185),
                Size = new Size(240, 65),
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };

            rbFriendly = new RadioButton
            {
                Text = "Friendly",
                Location = new Point(15, 25),
                Size = new Size(95, 25),
                Checked = true,
                Font = new Font("Courier New", 9F, FontStyle.Regular)
            };

            rbChallenge = new RadioButton
            {
                Text = "Challenge",
                Location = new Point(120, 25),
                Size = new Size(110, 25),
                Font = new Font("Courier New", 9F, FontStyle.Regular)
            };

            grpType.Controls.Add(rbFriendly);
            grpType.Controls.Add(rbChallenge);

            // Start button
            btnStart = new Button
            {
                Text = "Start",
                Location = new Point(55, 265),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold)
            };
            btnStart.Click += (s, e) =>
            {
                Settings = new BotSettings
                {
                    SkillLevel = trkDifficulty.Value,
                    BotPlaysWhite = rbPlayBlack.Checked,
                    ChallengeMode = rbChallenge.Checked
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            // Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(145, 265),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                lblDifficulty, lblDifficultyValue, trkDifficulty,
                lblMin, lblMax, grpColor, grpType, btnStart, btnCancel
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
