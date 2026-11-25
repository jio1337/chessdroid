using ChessDroid.Services;

namespace ChessDroid
{
    public partial class OpeningExplorerDialog : Form
    {
        private readonly List<OpeningEntry> _allEntries;
        private readonly Action<string> _onLoad;
        private readonly bool _isDark;

        private List<OpeningEntry> _filtered = new();
        private readonly Random _rng = new();

        // null = default (unsorted/name) order; true/false = ascending/descending by Eval.
        // Deliberately not persisted anywhere (config, instance field surviving a reopen,
        // etc.) — purely a transient view action for this dialog session, per the user's
        // explicit request not to save this as opening-form state.
        private bool? _evalSortAscending;

        // Parameterless constructor for VS Designer only
        public OpeningExplorerDialog() : this(new List<OpeningEntry>(), _ => { }, false) { }

        public OpeningExplorerDialog(List<OpeningEntry> entries, Action<string> onLoad, bool isDark)
        {
            _allEntries = entries;
            _onLoad     = onLoad;
            _isDark     = isDark;

            InitializeComponent();

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            txtSearch.TextChanged     += TxtSearch_TextChanged;
            grid.SelectionChanged     += Grid_SelectionChanged;
            grid.CellDoubleClick      += Grid_CellDoubleClick;
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            btnLoad.Click             += BtnLoad_Click;
            btnLoadRandom.Click       += (_, __) => LoadRandom();
            btnClose.Click            += (_, __) => Close();
            grid.ClientSizeChanged    += (_, __) => AdjustNameColumn();
            this.Resize               += (_, __) => AdjustNameColumn();
            this.Load                 += (_, __) => AdjustNameColumn();

            ApplyTheme(isDark);
            PopulateGrid(_allEntries);
        }

        private void AdjustNameColumn()
        {
            int reserved = colEco.Width + colEval.Width + SystemInformation.VerticalScrollBarWidth + 4;
            colName.Width = Math.Max(80, grid.ClientSize.Width - reserved);
        }

        private void PopulateGrid(List<OpeningEntry> entries)
        {
            _filtered = entries;
            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (var e in entries)
            {
                int rowIdx = grid.Rows.Add(e.Eco, e.Name, e.Eval);
                if (!string.IsNullOrEmpty(e.Eval))
                {
                    bool positive = e.Eval.StartsWith('+');
                    grid.Rows[rowIdx].Cells[2].Style.ForeColor = positive
                        ? (_isDark ? Color.FromArgb(120, 210, 80) : Color.FromArgb(30, 130, 30))
                        : (_isDark ? Color.FromArgb(220, 90, 90)  : Color.FromArgb(180, 30, 30));
                }
            }
            grid.ResumeLayout();
            lblMoves.Text       = "";
            btnLoad.Enabled     = false;
            btnLoadRandom.Enabled = entries.Count > 0;
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            // A fresh search always goes back to default (ECO/name) order — the Eval
            // sort from a header click is a one-off view action, not sticky state.
            _evalSortAscending = null;
            colEval.HeaderCell.SortGlyphDirection = SortOrder.None;

            string q = txtSearch.Text.Trim();
            var filtered = string.IsNullOrEmpty(q)
                ? _allEntries
                : _allEntries.Where(e =>
                    e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Eco.Contains(q,  StringComparison.OrdinalIgnoreCase)).ToList();
            PopulateGrid(filtered);
        }

        private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != colEval.Index) return;

            _evalSortAscending = _evalSortAscending != true;
            colEval.HeaderCell.SortGlyphDirection = _evalSortAscending == true ? SortOrder.Ascending : SortOrder.Descending;

            // Sort the currently-displayed (possibly search-filtered) set only — never
            // _allEntries — so this never leaks into/persists across a later search.
            var withValue = _filtered.Select(entry => (Entry: entry, Value: ParseEvalValue(entry.Eval)));
            var sorted = (_evalSortAscending == true
                ? withValue.OrderBy(x => x.Value ?? double.MaxValue)
                : withValue.OrderByDescending(x => x.Value ?? double.MinValue))
                .Select(x => x.Entry).ToList();

            PopulateGrid(sorted);
        }

        // Ranks a mate score as more extreme than any realistic centipawn eval (same
        // "sort-dominant" spirit as ChessEngineService's own ±100000 scaling elsewhere),
        // so e.g. "Mate in +2" sorts above "+9.00". Blank/unparseable evals return null,
        // which both sort directions push to the end of the list.
        internal static double? ParseEvalValue(string eval)
        {
            if (string.IsNullOrEmpty(eval)) return null;

            if (eval.StartsWith("Mate in ", StringComparison.Ordinal))
            {
                return int.TryParse(eval.AsSpan(8).Trim(), out int mateIn)
                    ? (mateIn > 0 ? 10000 - mateIn : mateIn < 0 ? -10000 - mateIn : 0)
                    : null;
            }

            return double.TryParse(eval, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : null;
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) { lblMoves.Text = ""; btnLoad.Enabled = false; return; }
            int idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= _filtered.Count) return;
            lblMoves.Text   = _filtered[idx].Moves;
            btnLoad.Enabled = true;
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) LoadSelected();
        }

        private void BtnLoad_Click(object? sender, EventArgs e) => LoadSelected();

        private void LoadSelected()
        {
            if (grid.SelectedRows.Count == 0) return;
            int idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= _filtered.Count) return;
            LoadEntry(_filtered[idx]);
        }

        // Picks from the currently-displayed set, so a search filter (e.g. "Sicilian")
        // narrows what "random" can land on, same as it narrows what Load Opening can.
        private void LoadRandom()
        {
            if (_filtered.Count == 0) return;
            LoadEntry(_filtered[_rng.Next(_filtered.Count)]);
        }

        private void LoadEntry(OpeningEntry entry)
        {
            string pgn = $"[Event \"Opening Explorer\"]\n[Opening \"{entry.Eco} — {entry.Name}\"]\n\n{entry.Moves}\n";
            _onLoad(pgn);
            Close();
        }

        private void ApplyTheme(bool isDark)
        {
            Color bg     = isDark ? Color.FromArgb(30, 30, 30)  : SystemColors.Control;
            Color fg     = isDark ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;
            Color btnBg  = isDark ? Color.FromArgb(50, 50, 50)  : SystemColors.Control;
            Color gridBg = isDark ? Color.FromArgb(25, 25, 25)  : SystemColors.Window;
            Color selBg  = isDark ? Color.FromArgb(60, 100, 160) : SystemColors.Highlight;

            BackColor = bg;
            ForeColor = fg;

            foreach (Control c in Controls)
            {
                c.BackColor = bg;
                c.ForeColor = fg;
            }

            txtSearch.BackColor = gridBg;
            txtSearch.ForeColor = fg;

            grid.BackgroundColor                             = gridBg;
            grid.GridColor                                   = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.ActiveBorder;
            grid.DefaultCellStyle.BackColor                  = gridBg;
            grid.DefaultCellStyle.ForeColor                  = fg;
            grid.DefaultCellStyle.SelectionBackColor         = selBg;
            grid.DefaultCellStyle.SelectionForeColor         = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor     = btnBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor     = fg;
            grid.EnableHeadersVisualStyles                   = false;

            foreach (Button btn in new[] { btnLoadRandom, btnLoad, btnClose })
            {
                btn.BackColor = btnBg;
                btn.ForeColor = fg;
                btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : SystemColors.ActiveBorder;
            }
        }
    }
}
