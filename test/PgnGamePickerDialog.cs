using System.Text;
using System.Text.RegularExpressions;

namespace ChessDroid
{
    /// <summary>
    /// Shows a list of games parsed from a multi-game PGN and lets the user pick one.
    /// </summary>
    public class PgnGamePickerDialog : Form
    {
        private readonly List<string> _games;
        private readonly ListView     _listView;

        public string? SelectedPgn { get; private set; }

        public PgnGamePickerDialog(List<string> games, bool isDark)
        {
            _games = games;

            Text            = "Select Game";
            Size            = new Size(740, 400);
            MinimumSize     = new Size(520, 300);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = false;
            BackColor       = isDark ? Color.FromArgb(28, 28, 33) : Color.White;
            ForeColor       = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;
            Font            = new Font("Courier New", 9f);

            // ── Top label ────────────────────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                BackColor = isDark ? Color.FromArgb(35, 35, 42) : Color.FromArgb(245, 245, 245),
                Padding   = new Padding(10, 0, 0, 0)
            };
            var lblInfo = new Label
            {
                Text      = $"{games.Count} games found — double-click or select then Load",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = isDark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(80, 80, 80),
                Font      = new Font("Courier New", 8.5f)
            };
            pnlTop.Controls.Add(lblInfo);

            // ── Button bar ───────────────────────────────────────────────────
            var pnlButtons = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 42,
                BackColor = isDark ? Color.FromArgb(22, 22, 27) : Color.FromArgb(238, 238, 238)
            };
            var btnLoad = new Button
            {
                Text      = "Load",
                Size      = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(55, 90, 145) : Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (_, _) => ConfirmSelection();

            var btnCancel = new Button
            {
                Text         = "Cancel",
                Size         = new Size(80, 28),
                FlatStyle    = FlatStyle.Flat,
                BackColor    = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(220, 220, 220),
                ForeColor    = isDark ? Color.White : Color.Black,
                DialogResult = DialogResult.Cancel,
                Cursor       = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlButtons.Controls.Add(btnLoad);
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Resize += (_, _) =>
            {
                btnLoad.Location   = new Point(pnlButtons.Width - 174, 7);
                btnCancel.Location = new Point(pnlButtons.Width - 88,  7);
            };

            // ── Game list ────────────────────────────────────────────────────
            _listView = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = false,
                MultiSelect   = false,
                BackColor     = isDark ? Color.FromArgb(22, 22, 28) : Color.White,
                ForeColor     = isDark ? Color.FromArgb(210, 210, 210) : Color.Black,
                BorderStyle   = BorderStyle.None,
                HeaderStyle   = ColumnHeaderStyle.Nonclickable,
                Font          = new Font("Courier New", 8.5f)
            };
            _listView.Columns.Add("#",       38);
            _listView.Columns.Add("White",  155);
            _listView.Columns.Add("Black",  155);
            _listView.Columns.Add("Result",  58);
            _listView.Columns.Add("Moves",   52);
            _listView.Columns.Add("Date",    90);
            _listView.Columns.Add("Event",  130);

            for (int i = 0; i < games.Count; i++)
            {
                var h    = ParsePgnHeaders(games[i]);
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(h.GetValueOrDefault("White",  "?"));
                item.SubItems.Add(h.GetValueOrDefault("Black",  "?"));
                item.SubItems.Add(h.GetValueOrDefault("Result", "?"));
                item.SubItems.Add(CountMoves(games[i]).ToString());
                item.SubItems.Add(h.GetValueOrDefault("Date", "").Replace(".??", "").Replace("??", "").TrimEnd('.'));
                item.SubItems.Add(h.GetValueOrDefault("Event", ""));
                _listView.Items.Add(item);
            }
            if (_listView.Items.Count > 0)
                _listView.Items[0].Selected = true;

            _listView.DoubleClick += (_, _) => ConfirmSelection();

            Controls.Add(_listView);
            Controls.Add(pnlButtons);
            Controls.Add(pnlTop);

            AcceptButton = btnLoad;
            CancelButton = btnCancel;
        }

        private void ConfirmSelection()
        {
            if (_listView.SelectedIndices.Count == 0) return;
            SelectedPgn  = _games[_listView.SelectedIndices[0]];
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Splits a PGN string containing one or more games into individual game strings.
        /// A new game is detected when a header line ([...]) appears after move text was seen.
        /// </summary>
        public static List<string> SplitPgnGames(string pgn)
        {
            var games   = new List<string>();
            var current = new StringBuilder();
            bool inMoveText = false;

            foreach (string rawLine in pgn.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.TrimStart().StartsWith("["))
                {
                    if (inMoveText)
                    {
                        string g = current.ToString().Trim();
                        if (g.Length > 0) games.Add(g);
                        current.Clear();
                        inMoveText = false;
                    }
                    current.AppendLine(line);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(line)) inMoveText = true;
                    current.AppendLine(line);
                }
            }

            string last = current.ToString().Trim();
            if (last.Length > 0) games.Add(last);
            return games;
        }

        public static Dictionary<string, string> ParsePgnHeaders(string pgn)
        {
            var h = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in pgn.Split('\n'))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("[")) break;
                int keyEnd = line.IndexOf(' ');
                if (keyEnd < 1) continue;
                string key = line.Substring(1, keyEnd - 1);
                int vs = line.IndexOf('"') + 1;
                int ve = line.LastIndexOf('"');
                if (vs > 0 && ve > vs) h[key] = line.Substring(vs, ve - vs);
            }
            return h;
        }

        private static int CountMoves(string pgn)
        {
            // Skip header lines, then find the highest move number in the move text
            var moveLines = pgn.Split('\n')
                .SkipWhile(l => l.Trim().StartsWith("[") || string.IsNullOrWhiteSpace(l));
            string moveText = string.Join(" ", moveLines);
            var matches = Regex.Matches(moveText, @"\b(\d+)\.");
            if (matches.Count == 0) return 0;
            return int.TryParse(matches[^1].Groups[1].Value, out int n) ? n : 0;
        }
    }
}
