using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChessDroid
{
    public class StatsDialog : Form
    {
        private readonly AppConfig _config;
        private readonly Font _headerFont;
        private readonly Font _dataFont;
        private readonly Color _headerColor;

        public StatsDialog(AppConfig config, string theme)
        {
            _config = config;
            bool isDark = Services.ThemeService.IsDarkTheme(theme);
            _headerFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _dataFont    = new Font("Segoe UI", 9f);
            _headerColor = isDark ? Color.FromArgb(200, 180, 100) : Color.FromArgb(60, 80, 140);
            BuildUI(isDark);
        }

        private void BuildUI(bool isDark)
        {
            Text            = "Training Stats";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(500, 520);

            Color bg       = isDark ? Color.FromArgb(45, 45, 48)  : Color.WhiteSmoke;
            Color fg       = isDark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(30, 30, 30);
            Color sepColor = isDark ? Color.FromArgb(70, 70, 75)  : Color.FromArgb(200, 200, 200);
            BackColor = bg;
            ForeColor = fg;

            var scroll = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(500, 476),
                AutoScroll = true,
                BackColor = bg
            };

            var inner = new Panel
            {
                Location  = new Point(16, 12),
                Width     = 462,
                BackColor = bg
            };

            int y = 0;
            y = AddSection(inner, "DAILY PUZZLE",    y, fg, bg, sepColor, BuildDailyRows());
            y = AddSection(inner, "PUZZLES",          y, fg, bg, sepColor, BuildPuzzleRows());
            y = AddSection(inner, "OPENING TRAINING", y, fg, bg, sepColor, BuildOpeningRows());
            y = AddSection(inner, "VISION TRAINING",  y, fg, bg, sepColor, BuildVisionRows());
            y = AddSection(inner, "SQUARE TRAINING",  y, fg, bg, sepColor, BuildSquareRows(), last: true);
            inner.Height = y + 8;

            scroll.Controls.Add(inner);

            var btnClose = new Button
            {
                Text         = "Close",
                DialogResult = DialogResult.OK,
                Size         = new Size(90, 30),
                Location     = new Point(500 - 90 - 12, 482),
                FlatStyle    = FlatStyle.Flat,
                ForeColor    = fg,
                BackColor    = bg
            };
            AcceptButton = btnClose;

            Controls.Add(scroll);
            Controls.Add(btnClose);
        }

        private int AddSection(Panel parent, string title, int startY,
                               Color fg, Color bg, Color sepColor,
                               string[] rows, bool last = false)
        {
            int y = startY + 6;

            var header = new Label
            {
                Text      = title,
                Font      = _headerFont,
                ForeColor = _headerColor,
                BackColor = bg,
                Location  = new Point(0, y),
                AutoSize  = true
            };
            parent.Controls.Add(header);
            y += 22;

            foreach (var row in rows)
            {
                var lbl = new Label
                {
                    Text      = row,
                    Font      = _dataFont,
                    ForeColor = fg,
                    BackColor = bg,
                    Location  = new Point(10, y),
                    AutoSize  = true
                };
                parent.Controls.Add(lbl);
                y += 19;
            }

            y += 6;

            if (!last)
            {
                var sep = new Panel
                {
                    Location  = new Point(0, y),
                    Size      = new Size(462, 1),
                    BackColor = sepColor
                };
                parent.Controls.Add(sep);
                y += 10;
            }

            return y;
        }

        // ── Section builders ────────────────────────────────────────────────

        private string[] BuildDailyRows()
        {
            string streak = _config.DailyPuzzleStreak > 0
                ? $"{_config.DailyPuzzleStreak} day{(_config.DailyPuzzleStreak == 1 ? "" : "s")}"
                : "—";
            string best = _config.DailyPuzzleBestStreak > 0
                ? _config.DailyPuzzleBestStreak.ToString()
                : "—";
            string last = string.IsNullOrEmpty(_config.DailyPuzzleLastSolvedDate)
                ? "never"
                : DateTime.Parse(_config.DailyPuzzleLastSolvedDate).ToString("MMM d, yyyy");

            return new[] { $"Streak: {streak}   ·   Best streak: {best}   ·   Last solved: {last}" };
        }

        private string[] BuildPuzzleRows()
        {
            string trainStreak = _config.PuzzleTrainingBestStreak > 0 ? _config.PuzzleTrainingBestStreak.ToString() : "—";
            string rush        = _config.PuzzleRushBest > 0            ? _config.PuzzleRushBest.ToString()            : "—";
            string gauntlet    = _config.GauntletBestStreak > 0        ? _config.GauntletBestStreak.ToString()        : "—";

            return new[]
            {
                $"Training best streak: {trainStreak}   ·   Rush best: {rush} solved   ·   Gauntlet best: {gauntlet}"
            };
        }

        private string[] BuildOpeningRows()
        {
            var stats = _config.OpeningTrainingStats;
            if (stats.Count == 0)
                return new[] { "No openings studied yet." };

            int studied      = stats.Count;
            int totalRuns    = stats.Values.Sum(s => s.TotalRuns);
            int totalPerfect = stats.Values.Sum(s => s.PerfectRuns);
            int mastered     = stats.Values.Count(s => s.PerfectRuns >= 3);

            var top     = stats.OrderByDescending(kv => kv.Value.TotalRuns).First();
            string topName = top.Key.Contains('|')
                ? top.Key.Split('|')[0] + "  " + top.Key.Split('|')[1]
                : top.Key;
            if (topName.Length > 54) topName = topName[..54] + "…";

            return new[]
            {
                $"{studied} openings studied   ·   {totalRuns} total runs   ·   {totalPerfect} perfect",
                $"Mastered (3+ perfect): {mastered}",
                $"Most studied: {topName} ({top.Value.TotalRuns} runs)"
            };
        }

        private string[] BuildVisionRows()
        {
            string Get(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0)
                    return pb.BestCorrect.ToString();
                return "—";
            }

            return new[]
            {
                $"60 sec: {Get("Vision-Timed-60")}   ·   3 min: {Get("Vision-Timed-180")}   ·   5 min: {Get("Vision-Timed-300")}   ·   Survival: {Get("Vision-Survival")}"
            };
        }

        private string[] BuildSquareRows()
        {
            string Get(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0)
                    return pb.BestCorrect.ToString();
                return "—";
            }

            return new[]
            {
                $"Easy:        White {Get("Easy-White")}  ·  Black {Get("Easy-Black")}  ·  Random {Get("Easy-Random")}",
                $"Challenge:  White {Get("Challenge-White")}  ·  Black {Get("Challenge-Black")}  ·  Random {Get("Challenge-Random")}"
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _headerFont?.Dispose();
                _dataFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
