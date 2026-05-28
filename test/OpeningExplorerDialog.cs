using ChessDroid.Services;

namespace ChessDroid
{
    public class OpeningExplorerDialog : Form
    {
        private readonly List<OpeningEntry> _allEntries;
        private readonly Action<string> _onLoad;

        private TextBox txtSearch = null!;
        private DataGridView grid = null!;
        private Label lblMoves = null!;
        private Button btnLoad = null!;
        private Button btnClose = null!;

        private List<OpeningEntry> _filtered = new();

        public OpeningExplorerDialog(List<OpeningEntry> entries, Action<string> onLoad, bool isDark)
        {
            _allEntries = entries;
            _onLoad = onLoad;
            InitializeControls();
            ApplyTheme(isDark);
            PopulateGrid(_allEntries);
        }

        private void InitializeControls()
        {
            Text = "Opening Explorer";
            Size = new Size(720, 540);
            MinimumSize = new Size(500, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // Search bar
            var lblSearch = new Label
            {
                Text = "Search:",
                Location = new Point(10, 14),
                Size = new Size(52, 20),
                Font = new Font("Courier New", 9F)
            };

            txtSearch = new TextBox
            {
                Location = new Point(64, 10),
                Size = new Size(630, 22),
                Font = new Font("Courier New", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // Grid
            grid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(684, 370),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 24,
                RowTemplate = { Height = 22 },
                Font = new Font("Courier New", 9F),
                TabIndex = 1
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Eco",
                HeaderText = "ECO",
                Width = 60,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Opening Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.SelectionChanged += Grid_SelectionChanged;
            grid.CellDoubleClick += Grid_CellDoubleClick;

            // Moves preview
            lblMoves = new Label
            {
                Text = "",
                Location = new Point(10, 418),
                Size = new Size(684, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Courier New", 8.25F),
                AutoEllipsis = true
            };

            // Buttons
            btnLoad = new Button
            {
                Text = "Load Opening",
                Size = new Size(120, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Courier New", 9F),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnLoad.Click += BtnLoad_Click;

            btnClose = new Button
            {
                Text = "Close",
                Size = new Size(80, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Courier New", 9F),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnClose.Click += (_, __) => Close();

            // Layout buttons at bottom-right
            this.Resize += (_, __) =>
            {
                btnClose.Location = new Point(ClientSize.Width - btnClose.Width - 10, ClientSize.Height - btnClose.Height - 10);
                btnLoad.Location  = new Point(btnClose.Left - btnLoad.Width - 8, btnClose.Top);
            };

            Controls.AddRange(new Control[] { lblSearch, txtSearch, grid, lblMoves, btnLoad, btnClose });
            AcceptButton = btnLoad;
            CancelButton = btnClose;
        }

        private void PopulateGrid(List<OpeningEntry> entries)
        {
            _filtered = entries;
            grid.SuspendLayout();
            grid.Rows.Clear();
            foreach (var e in entries)
                grid.Rows.Add(e.Eco, e.Name);
            grid.ResumeLayout();
            lblMoves.Text = "";
            btnLoad.Enabled = false;
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? _allEntries
                : _allEntries.Where(e =>
                    e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Eco.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            PopulateGrid(filtered);
        }

        private void Grid_SelectionChanged(object? sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) { lblMoves.Text = ""; btnLoad.Enabled = false; return; }
            int idx = grid.SelectedRows[0].Index;
            if (idx < 0 || idx >= _filtered.Count) return;
            lblMoves.Text = _filtered[idx].Moves;
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
            Color bg     = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color fg     = isDark ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;
            Color btnBg  = isDark ? Color.FromArgb(50, 50, 50) : SystemColors.Control;
            Color gridBg = isDark ? Color.FromArgb(25, 25, 25) : SystemColors.Window;
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

            grid.BackgroundColor = gridBg;
            grid.GridColor = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.ActiveBorder;
            grid.DefaultCellStyle.BackColor = gridBg;
            grid.DefaultCellStyle.ForeColor = fg;
            grid.DefaultCellStyle.SelectionBackColor = selBg;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = btnBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = fg;
            grid.EnableHeadersVisualStyles = false;

            foreach (Button btn in new[] { btnLoad, btnClose })
            {
                btn.BackColor = btnBg;
                btn.ForeColor = fg;
                btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : SystemColors.ActiveBorder;
            }
        }
    }
}
