using ChessDroid.Services;

namespace ChessDroid
{
    public partial class OpeningExplorerDialog : Form
    {
        private readonly List<OpeningEntry> _allEntries;
        private readonly Action<string> _onLoad;
        private readonly bool _isDark;

        private List<OpeningEntry> _filtered = new();

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
            btnLoad.Click             += BtnLoad_Click;
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
            lblMoves.Text    = "";
            btnLoad.Enabled  = false;
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim();
            var filtered = string.IsNullOrEmpty(q)
                ? _allEntries
                : _allEntries.Where(e =>
                    e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Eco.Contains(q,  StringComparison.OrdinalIgnoreCase)).ToList();
            PopulateGrid(filtered);
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
            var entry = _filtered[idx];
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

            foreach (Button btn in new[] { btnLoad, btnClose })
            {
                btn.BackColor = btnBg;
                btn.ForeColor = fg;
                btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : SystemColors.ActiveBorder;
            }
        }
    }
}
