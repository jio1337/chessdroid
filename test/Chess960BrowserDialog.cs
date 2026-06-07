using ChessDroid.Services;

namespace ChessDroid
{
    internal class Chess960BrowserDialog : Form
    {
        public int SelectedPosition { get; private set; }
        public bool StartBot { get; private set; }

        private readonly DataGridView _grid;
        private readonly TextBox _filter;
        private readonly List<(int Pos, string Rank)> _allRows;

        internal Chess960BrowserDialog(bool isDark)
        {
            _allRows = Enumerable.Range(0, 960)
                .Select(i => (i, new string(Chess960Service.GetBackRank(i))))
                .ToList();

            Color bg      = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            Color fg      = isDark ? Color.White                 : Color.Black;
            Color inputBg = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Window;
            Color gridBg  = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Window;
            Color hdrBg   = isDark ? Color.FromArgb(38, 38, 38) : SystemColors.ControlDark;
            Color altBg   = isDark ? Color.FromArgb(38, 38, 38) : Color.FromArgb(246, 246, 252);
            Color selBg   = isDark ? Color.FromArgb(50, 90, 150) : SystemColors.Highlight;
            Color btnBg   = isDark ? Color.FromArgb(55, 55, 55) : SystemColors.ButtonFace;

            Text = "Chess 960 — Position Browser";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(428, 533);
            MinimumSize = new Size(428, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            BackColor = bg; ForeColor = fg;
            Font = new Font("Segoe UI", 9f);

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // --- Top panel (filter) ---
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = bg };

            var lblFilter = new Label
            {
                Text = "Filter:", AutoSize = true,
                Location = new Point(10, 11), ForeColor = fg,
            };
            _filter = new TextBox
            {
                Location = new Point(54, 8),
                BackColor = inputBg, ForeColor = fg,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "position number or pieces (e.g. 518 or RNBQKBNR)",
            };
            _filter.Width = pnlTop.Width - 64;
            pnlTop.Resize += (_, _) => _filter.Width = pnlTop.Width - 64;
            pnlTop.Controls.AddRange([lblFilter, _filter]);

            // --- Bottom panel (buttons) ---
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = bg };

            var btnLoad   = new Button { Text = "Load Position", FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = fg };
            var btnBot    = new Button { Text = "vs Bot",        FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = fg };
            var btnRandom = new Button { Text = "Random",        FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = fg };
            var btnCancel = new Button { Text = "Cancel",        FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = fg, DialogResult = DialogResult.Cancel };

            void LayoutButtons()
            {
                const int bh = 26, pad = 8, gap = 6;
                int bw   = (pnlBottom.Width - 2 * pad - gap) / 2;
                int row1 = pad;
                int row2 = pad + bh + gap;
                btnLoad.Bounds   = new Rectangle(pad,          row1, bw, bh);
                btnBot.Bounds    = new Rectangle(pad + bw + gap, row1, bw, bh);
                btnRandom.Bounds = new Rectangle(pad,          row2, bw, bh);
                btnCancel.Bounds = new Rectangle(pad + bw + gap, row2, bw, bh);
            }
            LayoutButtons();
            pnlBottom.Resize += (_, _) => LayoutButtons();
            pnlBottom.Controls.AddRange([btnLoad, btnBot, btnRandom, btnCancel]);

            // --- DataGridView ---
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = gridBg,
                GridColor = isDark ? Color.FromArgb(55, 55, 55) : Color.FromArgb(210, 210, 210),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 26,
                RowTemplate = { Height = 22 },
                EnableHeadersVisualStyles = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            };

            var cellStyle = new DataGridViewCellStyle { BackColor = gridBg, ForeColor = fg, SelectionBackColor = selBg, SelectionForeColor = Color.White };
            _grid.DefaultCellStyle = cellStyle;
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(cellStyle) { BackColor = altBg };
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = hdrBg, ForeColor = fg };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "SP", Width = 60,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Back Rank",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Courier New", 9f),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                },
            });

            PopulateGrid(_allRows);

            // Wire events
            _filter.TextChanged   += (_, _) => ApplyFilter();
            _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Confirm(startBot: false); };
            btnLoad.Click         += (_, _) => Confirm(startBot: false);
            btnBot.Click          += (_, _) => Confirm(startBot: true);
            btnRandom.Click       += (_, _) => SelectRandom();

            // Docking order: Fill first, then Top/Bottom claim their edges
            Controls.Add(_grid);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);

            AcceptButton = btnLoad;
            CancelButton = btnCancel;
        }

        private void PopulateGrid(IReadOnlyList<(int Pos, string Rank)> rows)
        {
            _grid.Rows.Clear();
            foreach (var (pos, rank) in rows)
                _grid.Rows.Add(pos.ToString(), rank);
            if (_grid.Rows.Count > 0)
                _grid.Rows[0].Selected = true;
        }

        private void ApplyFilter()
        {
            string q = _filter.Text.Trim().ToUpperInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? _allRows
                : _allRows.Where(r => r.Pos.ToString().Contains(q) || r.Rank.Contains(q)).ToList();
            PopulateGrid(filtered);
        }

        private void SelectRandom()
        {
            _filter.Text = Chess960Service.GetRandomPosition().ToString();
        }

        private void Confirm(bool startBot)
        {
            if (_grid.SelectedRows.Count == 0) return;
            if (!int.TryParse(_grid.SelectedRows[0].Cells[0].Value?.ToString(), out int pos)) return;
            SelectedPosition = pos;
            StartBot = startBot;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
