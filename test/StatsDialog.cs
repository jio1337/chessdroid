using System;
using System.Collections.Generic;
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
        private readonly Font _dimFont;
        private readonly Color _headerColor;
        private readonly Color _dimColor;

        public StatsDialog(AppConfig config, string theme)
        {
            _config = config;
            bool isDark = Services.ThemeService.IsDarkTheme(theme);
            _headerFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _dataFont    = new Font("Segoe UI", 9f);
            _dimFont     = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            _headerColor = isDark ? Color.FromArgb(200, 180, 100) : Color.FromArgb(60, 80, 140);
            _dimColor    = isDark ? Color.FromArgb(130, 130, 135) : Color.FromArgb(130, 130, 130);
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

            Color bg       = isDark ? Color.FromArgb(45, 45, 48)   : Color.WhiteSmoke;
            Color fg       = isDark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(30, 30, 30);
            Color sepColor = isDark ? Color.FromArgb(70, 70, 75)   : Color.FromArgb(200, 200, 200);
            BackColor = bg;
            ForeColor = fg;

            var scroll = new Panel
            {
                Location   = new Point(0, 0),
                Size       = new Size(500, 476),
                AutoScroll = true,
                BackColor  = bg
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
                               (string text, bool dim)[] rows, bool last = false)
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

            foreach (var (text, dim) in rows)
            {
                var lbl = new Label
                {
                    Text      = text,
                    Font      = dim ? _dimFont : _dataFont,
                    ForeColor = dim ? _dimColor : fg,
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

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string AgeStr(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate)) return "";
            if (!DateTime.TryParse(isoDate, out var d)) return "";
            int days = (DateTime.Today - d.Date).Days;
            if (days == 0) return "today";
            if (days == 1) return "yesterday";
            if (days < 7)  return $"{days} days ago";
            if (days < 30) return $"{days / 7}w ago";
            return $"{days / 30}mo ago";
        }

        // ── Section builders ────────────────────────────────────────────────

        private (string, bool)[] BuildDailyRows()
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
            string clean = _config.DailyPuzzleCleanSolves > 0
                ? $"{_config.DailyPuzzleCleanSolves} clean solve{(_config.DailyPuzzleCleanSolves == 1 ? "" : "s")} lifetime"
                : "";

            var rows = new List<(string, bool)>
            {
                ($"Streak: {streak}   ·   Best streak: {best}   ·   Last solved: {last}", false)
            };
            if (!string.IsNullOrEmpty(clean))
                rows.Add((clean, true));
            return rows.ToArray();
        }

        private (string, bool)[] BuildPuzzleRows()
        {
            // Training streak
            string trainStreak = _config.PuzzleTrainingBestStreak > 0
                ? _config.PuzzleTrainingBestStreak.ToString() : "—";
            string trainDate = AgeStr(_config.PuzzleTrainingBestStreakDate);
            string trainLine = $"Training best streak: {trainStreak}";
            if (!string.IsNullOrEmpty(trainDate)) trainLine += $"   ·   set {trainDate}";

            // Totals
            string totals = _config.PuzzleTrainingTotalAttempted > 0
                ? $"{_config.PuzzleTrainingTotalAttempted} puzzles attempted   ·   " +
                  $"{(_config.PuzzleTrainingTotalAttempted > 0 ? _config.PuzzleTrainingTotalClean * 100 / _config.PuzzleTrainingTotalAttempted : 0)}% clean"
                : "";

            // Rush — per-duration PBs
            string rushLine;
            if (_config.PuzzleRushBestByMinutes.Count > 0)
            {
                var parts = _config.PuzzleRushBestByMinutes
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key} min: {kv.Value}");
                rushLine = "Rush best:   " + string.Join("   ·   ", parts);
            }
            else if (_config.PuzzleRushBest > 0)
            {
                rushLine = $"Rush best: {_config.PuzzleRushBest} solved";
            }
            else
            {
                rushLine = "Rush best: —";
            }

            // Gauntlet
            string gauntlet = _config.GauntletBestStreak > 0
                ? _config.GauntletBestStreak.ToString() : "—";
            string gauntletDate = AgeStr(_config.GauntletBestStreakDate);
            string gauntletLine = $"Gauntlet best: {gauntlet}";
            if (!string.IsNullOrEmpty(gauntletDate)) gauntletLine += $"   ·   set {gauntletDate}";

            // Recent gauntlet average
            string gauntletAvg = "";
            if (_config.GauntletRecentScores.Count >= 2)
            {
                double avg = _config.GauntletRecentScores.Average();
                gauntletAvg = $"Recent average ({_config.GauntletRecentScores.Count} sessions): {avg:F1}";
            }

            var rows = new List<(string, bool)>
            {
                (trainLine,    false),
                (rushLine,     false),
                (gauntletLine, false),
            };
            if (!string.IsNullOrEmpty(gauntletAvg))
                rows.Add((gauntletAvg, true));
            if (!string.IsNullOrEmpty(totals))
                rows.Add((totals, true));
            return rows.ToArray();
        }

        private (string, bool)[] BuildOpeningRows()
        {
            var stats = _config.OpeningTrainingStats;
            if (stats.Count == 0)
                return new (string, bool)[] { ("No openings studied yet.", false) };

            int studied      = stats.Count;
            int totalRuns    = stats.Values.Sum(s => s.TotalRuns);
            int totalPerfect = stats.Values.Sum(s => s.PerfectRuns);
            // Mastered = at least one clean run (BestRunMistakes == 0)
            int mastered     = stats.Values.Count(s => s.BestRunMistakes == 0);

            var top = stats.OrderByDescending(kv => kv.Value.TotalRuns).First();
            string topName = top.Key.Contains('|')
                ? top.Key.Split('|')[0] + "  " + top.Key.Split('|')[1]
                : top.Key;
            if (topName.Length > 54) topName = topName[..54] + "…";

            // Total mistakes across all runs — shows how much grinding happened
            int totalMistakes = stats.Values.Sum(s => s.TotalMistakes);
            string mistakesLine = totalMistakes > 0
                ? $"Total wrong attempts across all runs: {totalMistakes}"
                : "";

            var rows = new List<(string, bool)>
            {
                ($"{studied} openings studied   ·   {totalRuns} total runs   ·   {totalPerfect} perfect", false),
                ($"Mastered (at least one clean run): {mastered}", false),
                ($"Most studied: {topName} ({top.Value.TotalRuns} runs)", false),
            };
            if (!string.IsNullOrEmpty(mistakesLine))
                rows.Add((mistakesLine, true));
            return rows.ToArray();
        }

        private (string, bool)[] BuildVisionRows()
        {
            string GetCorrect(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0)
                    return pb.BestCorrect.ToString();
                return "—";
            }
            string GetDate(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb) && !string.IsNullOrEmpty(pb.LastSet))
                    return AgeStr(pb.LastSet);
                return "";
            }

            string streak = _config.VisionBestStreak > 0
                ? $"Best streak ever: {_config.VisionBestStreak}" : "";

            var rows = new List<(string, bool)>
            {
                ($"60 sec: {GetCorrect("Vision-Timed-60")}   ·   3 min: {GetCorrect("Vision-Timed-180")}   ·   5 min: {GetCorrect("Vision-Timed-300")}   ·   Survival: {GetCorrect("Vision-Survival")}", false),
            };

            // Show when any timed PB was set (use the most recent one)
            var dates = new[] { "Vision-Timed-60", "Vision-Timed-180", "Vision-Timed-300", "Vision-Survival" }
                .Select(k => GetDate(k))
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();
            if (dates.Count > 0)
                rows.Add(($"Last PB: {dates.First()}", true));

            if (!string.IsNullOrEmpty(streak))
                rows.Add((streak, true));

            return rows.ToArray();
        }

        private (string, bool)[] BuildSquareRows()
        {
            string GetCorrect(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb) && pb.BestCorrect > 0)
                    return pb.BestCorrect.ToString();
                return "—";
            }
            string GetSpeed(string key)
            {
                if (_config.TrainingPersonalBests.TryGetValue(key, out var pb)
                    && pb.BestTimePerQuestion < double.MaxValue && pb.BestTimePerQuestion > 0)
                    return $"{pb.BestTimePerQuestion:F2}s/q";
                return "";
            }

            string easyW  = GetCorrect("Easy-White");
            string easyB  = GetCorrect("Easy-Black");
            string easyR  = GetCorrect("Easy-Random");
            string chalW  = GetCorrect("Challenge-White");
            string chalB  = GetCorrect("Challenge-Black");
            string chalR  = GetCorrect("Challenge-Random");

            // Best speed across any challenge mode
            string bestSpeed = new[] { "Challenge-White", "Challenge-Black", "Challenge-Random" }
                .Select(k => GetSpeed(k))
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => double.Parse(s.Replace("s/q", "")))
                .FirstOrDefault() ?? "";

            var rows = new List<(string, bool)>
            {
                ($"Easy:        White {easyW}  ·  Black {easyB}  ·  Random {easyR}", false),
                ($"Challenge:  White {chalW}  ·  Black {chalB}  ·  Random {chalR}", false),
            };
            if (!string.IsNullOrEmpty(bestSpeed))
                rows.Add(($"Best speed (Challenge): {bestSpeed}", true));

            return rows.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _headerFont?.Dispose();
                _dataFont?.Dispose();
                _dimFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
