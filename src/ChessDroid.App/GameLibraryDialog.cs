using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid
{
    public class GameLibraryDialog : Form
    {
        private readonly ListView _listView;
        private readonly Button _btnOpen;
        private readonly Button _btnDelete;
        private readonly Button _btnClose;
        private readonly Label _lblStatus;
        private readonly GameLibraryService _library;
        private readonly bool _isDark;
        private List<SavedGame> _games;

        // Inline name editor state
        private TextBox? _inlineEditor;
        private ListViewItem? _editingItem;
        private int _editingCol;
        private bool _editCancelled;

        public SavedGame? SelectedGame { get; private set; }

        public GameLibraryDialog(GameLibraryService library, bool isDark)
        {
            _library = library;
            _isDark = isDark;
            _games = library.LoadAll();

            Text = "Game Library";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            Size = new Size(860, 420);
            MinimumSize = new Size(640, 340);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;

            // ─── ListView ───
            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _listView.Columns.Add("White", 120);
            _listView.Columns.Add("Black", 120);
            _listView.Columns.Add("Saved", 125);
            _listView.Columns.Add("W%", 44);
            _listView.Columns.Add("B%", 44);
            _listView.Columns.Add("Opening", 185);
            _listView.Columns.Add("Annotator", 105);
            _listView.MouseDoubleClick += ListView_MouseDoubleClick;
            _listView.SelectedIndexChanged += (s, e) => UpdateButtons();

            // ─── Bottom button bar ───
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(8, 7, 8, 7)
            };

            _lblStatus = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 320,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Consolas", 8.5F)
            };

            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F)
            };
            _btnClose.Click += (s, e) => { CommitEdit(); DialogResult = DialogResult.Cancel; Close(); };

            _btnDelete = new Button
            {
                Text = "Delete",
                Size = new Size(80, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F),
                Enabled = false
            };
            _btnDelete.Click += (s, e) => DeleteSelected();

            _btnOpen = new Button
            {
                Text = "Open",
                Size = new Size(80, 30),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Courier New", 9F, FontStyle.Bold),
                Enabled = false
            };
            _btnOpen.Click += (s, e) => OpenSelected();

            // DockStyle.Right renders right-to-left — add Close first for far-right position
            pnlButtons.Controls.Add(_lblStatus);
            pnlButtons.Controls.Add(_btnClose);
            pnlButtons.Controls.Add(_btnDelete);
            pnlButtons.Controls.Add(_btnOpen);

            Controls.Add(_listView);
            Controls.Add(pnlButtons);
            CancelButton = _btnClose;

            ApplyTheme();
            PopulateList();
        }

        // ─── List management ───────────────────────────────────────────────

        private void PopulateList()
        {
            _listView.Items.Clear();
            foreach (var game in _games)
            {
                var item = new ListViewItem(Truncate(game.White, 18));
                item.SubItems.Add(Truncate(game.Black, 18));
                item.SubItems.Add(game.SavedAt.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(game.WhiteAccuracy.HasValue ? $"{game.WhiteAccuracy:F0}" : "—");
                item.SubItems.Add(game.BlackAccuracy.HasValue ? $"{game.BlackAccuracy:F0}" : "—");
                item.SubItems.Add(string.IsNullOrEmpty(game.Opening) ? "—" : Truncate(game.Opening, 28));
                item.SubItems.Add(Truncate(game.EngineName, 16));
                item.Tag = game;
                _listView.Items.Add(item);
            }

            UpdateStatus();
            UpdateButtons();
        }

        private void UpdateStatus()
        {
            if (_games.Count == 0)
                _lblStatus.Text = "No saved games yet.";
            else
                _lblStatus.Text = $"{_games.Count} game{(_games.Count == 1 ? "" : "s")} · double-click White/Black to rename";
        }

        private void UpdateButtons()
        {
            bool sel = _listView.SelectedItems.Count > 0;
            _btnOpen.Enabled = sel;
            _btnDelete.Enabled = sel;
        }

        private void OpenSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            CommitEdit();
            SelectedGame = (SavedGame)_listView.SelectedItems[0].Tag!;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DeleteSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            CommitEdit();
            var game = (SavedGame)_listView.SelectedItems[0].Tag!;

            string label = game.White == "?" && game.Black == "?"
                ? game.SavedAt.ToString("yyyy-MM-dd HH:mm")
                : $"{game.White} vs {game.Black}";

            if (MessageBox.Show($"Delete \"{label}\" from the library?\nThis cannot be undone.",
                    "Delete Game", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            _library.Delete(game.Id);
            _games.Remove(game);
            PopulateList();
        }

        // ─── Inline name editor ────────────────────────────────────────────

        private void ListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var hit = _listView.HitTest(e.X, e.Y);
            if (hit.Item == null) return;

            int col = GetSubItemIndex(hit);
            if (col == 0 || col == 1)
                StartInlineEdit(hit.Item, col);
            else
                OpenSelected();
        }

        private static int GetSubItemIndex(ListViewHitTestInfo hit)
        {
            if (hit.SubItem == null || hit.Item == null) return 0;
            for (int i = 0; i < hit.Item.SubItems.Count; i++)
                if (hit.Item.SubItems[i] == hit.SubItem) return i;
            return 0;
        }

        private void StartInlineEdit(ListViewItem item, int col)
        {
            CommitEdit(); // finish any previous edit first

            var game = (SavedGame)item.Tag!;
            var cellBounds = item.SubItems[col].Bounds;

            // Map cell bounds to form coordinates for reliable overlay positioning
            var screenRect = _listView.RectangleToScreen(cellBounds);
            var formRect = RectangleToClient(screenRect);

            _editCancelled = false;
            _editingItem = item;
            _editingCol = col;

            _inlineEditor = new TextBox
            {
                Location = new Point(formRect.X + 1, formRect.Y + 1),
                Size = new Size(formRect.Width - 2, formRect.Height),
                Text = col == 0 ? game.White : game.Black,
                Font = _listView.Font,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _isDark ? Color.FromArgb(55, 80, 130) : Color.FromArgb(210, 225, 255),
                ForeColor = _isDark ? Color.White : Color.Black,
                MaxLength = 60
            };

            _inlineEditor.KeyDown += InlineEditor_KeyDown;
            _inlineEditor.LostFocus += (s, e) => CommitEdit();

            Controls.Add(_inlineEditor);
            _inlineEditor.BringToFront();
            _inlineEditor.Focus();
            _inlineEditor.SelectAll();
        }

        private void InlineEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _listView.Focus(); // triggers LostFocus → CommitEdit
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _editCancelled = true;
                _listView.Focus(); // triggers LostFocus, but cancelled flag skips save
            }
        }

        private void CommitEdit()
        {
            if (_inlineEditor == null) return;

            // Grab and clear state before doing anything — prevents re-entry from LostFocus
            var editor = _inlineEditor;
            var item = _editingItem;
            int col = _editingCol;
            bool cancelled = _editCancelled;

            _inlineEditor = null;
            _editingItem = null;
            _editCancelled = false;

            string text = editor.Text; // read before Dispose — disposed controls return ""
            Controls.Remove(editor);
            editor.Dispose();

            if (cancelled || item == null) return;

            string newValue = text.Trim();
            if (string.IsNullOrEmpty(newValue)) newValue = "?";

            var game = (SavedGame)item.Tag!;
            if (col == 0) game.White = newValue;
            else game.Black = newValue;

            _library.Save(game);
            item.SubItems[col].Text = Truncate(newValue, 18);
        }

        // ─── Theme ────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            if (!_isDark) return;

            var bg = Color.FromArgb(45, 45, 48);
            var panelBg = Color.FromArgb(30, 30, 35);
            var btnBg = Color.FromArgb(60, 60, 65);
            var border = Color.FromArgb(100, 100, 105);

            BackColor = bg;
            ForeColor = Color.White;

            _listView.BackColor = panelBg;
            _listView.ForeColor = Color.White;

            foreach (Control c in Controls)
            {
                if (c is not Panel pnl) continue;
                pnl.BackColor = bg;
                foreach (Control pc in pnl.Controls)
                {
                    pc.ForeColor = Color.White;
                    if (pc is Button btn)
                    {
                        btn.BackColor = btnBg;
                        btn.FlatAppearance.BorderColor = border;
                    }
                    else
                    {
                        pc.BackColor = bg;
                    }
                }
            }
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "…";
    }
}
