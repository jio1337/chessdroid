using System.Globalization;
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

        /// <param name="saveToLibrary">
        /// When given, a "Save to Library" button saves the selected games through it (returning
        /// how many were saved vs. skipped as duplicates) and the dialog stays open.
        /// </param>
        public PgnGamePickerDialog(List<string> games, bool isDark,
            Func<IReadOnlyList<string>, (int saved, int skipped)>? saveToLibrary = null)
        {
            _games = games;

            Text            = "Select Game";
            Size            = new Size(740, 400);
            MinimumSize     = new Size(520, 300);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = false;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
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
                Text      = saveToLibrary == null
                    ? $"{games.Count} games found — double-click or select then Load"
                    : $"{games.Count} games found — double-click to load; Ctrl/Shift-click or Ctrl+A to pick several for the library",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = isDark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(80, 80, 80),
                Font      = new Font("Courier New", 8.5f)
            };
            pnlTop.Controls.Add(lblInfo);

            // ── Search box (right side of the top bar) ───────────────────────
            var txtSearch = new TextBox
            {
                Dock            = DockStyle.Right,
                Width           = 200,
                PlaceholderText = "Search players / event…",
                BorderStyle     = BorderStyle.FixedSingle,
                BackColor       = isDark ? Color.FromArgb(22, 22, 28) : Color.White,
                ForeColor       = isDark ? Color.FromArgb(220, 220, 220) : Color.Black,
                Font            = new Font("Segoe UI", 9f)
            };
            var pnlSearch = new Panel { Dock = DockStyle.Right, Width = 212, Padding = new Padding(0, 6, 10, 0) };
            pnlSearch.Controls.Add(txtSearch);
            pnlTop.Controls.Add(pnlSearch);

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

            Button? btnSaveLib = null;
            if (saveToLibrary != null)
            {
                btnSaveLib = new Button
                {
                    Text      = "Save to Library",
                    Size      = new Size(130, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isDark ? Color.FromArgb(50, 50, 58) : Color.FromArgb(220, 220, 220),
                    ForeColor = isDark ? Color.White : Color.Black,
                    Cursor    = Cursors.Hand
                };
                btnSaveLib.FlatAppearance.BorderSize = 0;
                btnSaveLib.Click += (_, _) =>
                {
                    var picked = _listView!.SelectedItems.Cast<ListViewItem>()
                        .Select(it => (int)it.Tag!).OrderBy(i => i).Select(i => _games[i]).ToList();
                    if (picked.Count == 0) { lblInfo.Text = "Select one or more games first"; return; }
                    // ~1.5 s for ~200 games (opening detection per game) — show it's working.
                    Cursor = Cursors.WaitCursor;
                    (int saved, int skipped) result;
                    try { result = saveToLibrary(picked); }
                    finally { Cursor = Cursors.Default; }
                    var (saved, skipped) = result;
                    lblInfo.Text = skipped == 0
                        ? $"Saved {saved} game{(saved == 1 ? "" : "s")} to library"
                        : $"Saved {saved} game{(saved == 1 ? "" : "s")} to library ({skipped} already there)";
                };
                pnlButtons.Controls.Add(btnSaveLib);
            }

            pnlButtons.Resize += (_, _) =>
            {
                btnLoad.Location   = new Point(pnlButtons.Width - 174, 7);
                btnCancel.Location = new Point(pnlButtons.Width - 88,  7);
                if (btnSaveLib != null) btnSaveLib.Location = new Point(10, 7);
            };

            // ── Game list ────────────────────────────────────────────────────
            _listView = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = false,
                MultiSelect   = saveToLibrary != null,
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

            // All rows are built once; the search box shows a filtered subset. Tag = index into
            // _games, so selection keeps working whatever is currently visible.
            var allItems = new List<ListViewItem>(games.Count);
            for (int i = 0; i < games.Count; i++)
            {
                var h    = ParsePgnHeaders(games[i]);
                var item = new ListViewItem((i + 1).ToString(CultureInfo.InvariantCulture)) { Tag = i };
                item.SubItems.Add(h.GetValueOrDefault("White",  "?"));
                item.SubItems.Add(h.GetValueOrDefault("Black",  "?"));
                item.SubItems.Add(h.GetValueOrDefault("Result", "?"));
                item.SubItems.Add(CountMoves(games[i]).ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(h.GetValueOrDefault("Date", "").Replace(".??", "").Replace("??", "").TrimEnd('.'));
                item.SubItems.Add(h.GetValueOrDefault("Event", ""));
                allItems.Add(item);
            }
            _listView.Items.AddRange(allItems.ToArray());
            if (_listView.Items.Count > 0)
                _listView.Items[0].Selected = true;

            string baseInfo = lblInfo.Text;
            txtSearch.TextChanged += (_, _) =>
            {
                string q = txtSearch.Text;
                _listView.BeginUpdate();
                _listView.Items.Clear();
                // White, Black, Date, Event (skip #, Result, Moves)
                _listView.Items.AddRange(allItems.Where(it => MatchesSearch(q,
                    it.SubItems[1].Text, it.SubItems[2].Text, it.SubItems[5].Text, it.SubItems[6].Text)).ToArray());
                if (_listView.Items.Count > 0) _listView.Items[0].Selected = true;
                _listView.EndUpdate();
                lblInfo.Text = string.IsNullOrWhiteSpace(q) ? baseInfo : $"{_listView.Items.Count} of {games.Count} games match";
            };
            // Down arrow from the search box jumps into the list; Enter loads the top match.
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Down && _listView.Items.Count > 0) { _listView.Focus(); e.Handled = true; }
            };

            _listView.DoubleClick += (_, _) => ConfirmSelection();
            _listView.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.A && _listView.MultiSelect)
                {
                    _listView.BeginUpdate();
                    foreach (ListViewItem it in _listView.Items) it.Selected = true;
                    _listView.EndUpdate();
                    e.SuppressKeyPress = true;
                }
            };

            Controls.Add(_listView);
            Controls.Add(pnlButtons);
            Controls.Add(pnlTop);

            AcceptButton = btnLoad;
            CancelButton = btnCancel;
            ActiveControl = txtSearch; // start typing straight away; Enter loads the top match
        }

        private void ConfirmSelection()
        {
            if (_listView.SelectedItems.Count == 0) return;
            // With several selected, Load opens the one last clicked (the focused row).
            var row = _listView.FocusedItem is { Selected: true } focused ? focused : _listView.SelectedItems[0];
            SelectedPgn  = _games[(int)row.Tag!];
            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Search-box rule shared by the PGN picker and the Game Library: every whitespace-separated
        /// word in <paramref name="query"/> must appear (case-insensitively) in at least one of
        /// <paramref name="fields"/>. An empty query matches everything.
        /// </summary>
        public static bool MatchesSearch(string? query, params string?[] fields)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).All(term =>
                fields.Any(f => f != null && f.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

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
                // A real PGN tag is always [Name "value"] (quoted); Lichess/chess.com-style
                // move annotations like [%eval 0.31] or [%clk 0:05:00] are never quoted. If a
                // long move-text line word-wraps so one of those lands alone at the start of a
                // line, it still starts and ends with brackets — the quote check is what tells
                // a real new-game header apart from an annotation and avoids a false split.
                string trimmed = line.Trim();
                bool looksLikeHeaderTag = trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Contains('"');
                if (looksLikeHeaderTag)
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
                if (!line.StartsWith('[')) break;
                int keyEnd = line.IndexOf(' ');
                if (keyEnd < 1) continue;
                string key = line.Substring(1, keyEnd - 1);
                int vs = line.IndexOf('"') + 1;
                int ve = line.LastIndexOf('"');
                if (vs > 0 && ve > vs) h[key] = line.Substring(vs, ve - vs);
            }
            return h;
        }

        internal static int CountMoves(string pgn)
        {
            // Skip header lines, then find the highest move number in the move text
            var moveLines = pgn.Split('\n')
                .SkipWhile(l => l.Trim().StartsWith('[') || string.IsNullOrWhiteSpace(l));
            string moveText = string.Join(" ", moveLines);
            var matches = Regex.Matches(moveText, @"\b(\d+)\.");
            if (matches.Count == 0) return 0;
            return int.TryParse(matches[^1].Groups[1].Value, out int n) ? n : 0;
        }
    }
}
