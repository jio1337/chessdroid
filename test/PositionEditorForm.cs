using ChessDroid.Models;

namespace ChessDroid
{
    public partial class PositionEditorForm : Form
    {
        // ── State ──────────────────────────────────────────────────────
        private ChessBoard _board = new ChessBoard();
        private bool _whiteToMove = true;
        private bool _castleWK = true, _castleWQ = true, _castleBK = true, _castleBQ = true;

        private char _selectedPiece = '\0'; // '\0' = nothing selected; '.' = erase
        private Panel? _highlightedCell;
        private Dictionary<char, Image?> _images = new();
        private Panel[] _paletteCells = Array.Empty<Panel>();

        public string ResultFen { get; private set; } = string.Empty;

        // Palette piece order — must match order of panels in Designer
        private static readonly char[] PaletteOrder =
            { 'K','Q','R','B','N','P', 'k','q','r','b','n','p', '.' };

        private static readonly Dictionary<char, string> Glyphs = new()
        {
            {'K',"♔"},{'Q',"♕"},{'R',"♖"},{'B',"♗"},{'N',"♘"},{'P',"♙"},
            {'k',"♚"},{'q',"♛"},{'r',"♜"},{'b',"♝"},{'n',"♞"},{'p',"♟"},
            {'.', "✕"}
        };

        public PositionEditorForm(string initialFen, string templateSet, bool isDarkMode, string templatesPath)
        {
            InitializeComponent();
            BuildPalette();
            LoadImages(templateSet, templatesPath);
            ApplyTheme(isDarkMode);
            LoadFen(initialFen);
        }

        private void BuildPalette()
        {
            // col A=504, col B=547, rows step 43 starting at 36
            (char piece, int x, int y)[] layout =
            {
                ('K', 504,  36), ('Q', 547,  36),
                ('R', 504,  79), ('B', 547,  79),
                ('N', 504, 122), ('P', 547, 122),
                ('k', 504, 165), ('q', 547, 165),
                ('r', 504, 208), ('b', 547, 208),
                ('n', 504, 251), ('p', 547, 251),
                ('.', 504, 294),
            };

            var cells = new List<Panel>(layout.Length);
            foreach (var (piece, x, y) in layout)
            {
                var cell = new Panel
                {
                    Location = new Point(x, y),
                    Size     = new Size(40, 40),
                    Tag      = piece,
                    Cursor   = Cursors.Hand
                };
                cell.Paint  += PaletteCell_Paint;
                cell.Click  += PaletteCell_Click;
                Controls.Add(cell);
                cells.Add(cell);
            }
            _paletteCells = cells.ToArray();
        }

        // ── Image loading ──────────────────────────────────────────────

        private void LoadImages(string templateSet, string templatesPath)
        {
            var map = new Dictionary<char, string>
            {
                {'K',"wK.png"},{'Q',"wQ.png"},{'R',"wR.png"},{'B',"wB.png"},{'N',"wN.png"},{'P',"wP.png"},
                {'k',"bK.png"},{'q',"bQ.png"},{'r',"bR.png"},{'b',"bB.png"},{'n',"bN.png"},{'p',"bP.png"}
            };

            string baseDir = Path.Combine(templatesPath, templateSet);
            foreach (var kv in map)
            {
                string path = Path.Combine(baseDir, kv.Value);
                try { _images[kv.Key] = File.Exists(path) ? Image.FromFile(path) : null; }
                catch { _images[kv.Key] = null; }
            }
        }

        // ── Theme ──────────────────────────────────────────────────────

        private void ApplyTheme(bool dark)
        {
            Color bg      = dark ? Color.FromArgb(45, 45, 48)  : Color.WhiteSmoke;
            Color fg      = dark ? Color.White                  : Color.Black;
            Color panelBg = dark ? Color.FromArgb(55, 55, 60)  : Color.Gainsboro;
            Color btnBg   = dark ? Color.FromArgb(60, 60, 65)  : Color.White;

            BackColor = bg;

            lblPalette.ForeColor = fg;
            lblSideToMove.ForeColor = fg;
            lblCastling.ForeColor = fg;
            lblStatus.ForeColor = dark ? Color.LightGray : Color.DarkSlateGray;

            rdoWhite.ForeColor = fg; rdoWhite.BackColor = bg;
            rdoBlack.ForeColor = fg; rdoBlack.BackColor = bg;

            foreach (var chk in new[] { chkCastleWK, chkCastleWQ, chkCastleBK, chkCastleBQ })
            {
                chk.ForeColor = fg; chk.BackColor = bg;
            }

            foreach (var btn in new[] { btnClear, btnStartPos, btnApply, btnCancel })
            {
                btn.BackColor = btnBg; btn.ForeColor = fg;
            }

            btnApply.ForeColor  = dark ? Color.LightGreen  : Color.DarkGreen;
            btnCancel.ForeColor = dark ? Color.LightCoral  : Color.DarkRed;

            foreach (var cell in _paletteCells)
                cell.BackColor = panelBg;
        }

        // ── FEN loading ────────────────────────────────────────────────

        private void LoadFen(string fen)
        {
            try
            {
                string[] parts = fen.Trim().Split(' ');
                _board = ChessBoard.FromFEN(fen);
                _whiteToMove = parts.Length < 2 || parts[1] != "b";

                string castling = parts.Length >= 3 ? parts[2] : "KQkq";
                _castleWK = castling.Contains('K');
                _castleWQ = castling.Contains('Q');
                _castleBK = castling.Contains('k');
                _castleBQ = castling.Contains('q');

                rdoWhite.Checked = _whiteToMove;
                rdoBlack.Checked = !_whiteToMove;
                chkCastleWK.Checked = _castleWK;
                chkCastleWQ.Checked = _castleWQ;
                chkCastleBK.Checked = _castleBK;
                chkCastleBQ.Checked = _castleBQ;

                pnlBoard.Invalidate();
            }
            catch
            {
                _board = new ChessBoard();
                pnlBoard.Invalidate();
            }
        }

        // ── Board paint ────────────────────────────────────────────────

        private void PnlBoard_Paint(object? sender, PaintEventArgs e)
        {
            const int SQ = 60;
            var g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var light = Color.FromArgb(240, 217, 181);
            var dark  = Color.FromArgb(181, 136, 99);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    using var b = new SolidBrush((row + col) % 2 == 0 ? light : dark);
                    g.FillRectangle(b, col * SQ, row * SQ, SQ, SQ);

                    char piece = _board.GetPiece(row, col);
                    if (piece != '.') DrawPiece(g, piece, col * SQ, row * SQ, SQ);
                }
            }

            // Rank/file labels
            using var lf = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var lb = new SolidBrush(Color.FromArgb(160, 60, 40, 20));
            for (int i = 0; i < 8; i++)
            {
                g.DrawString(((char)('a' + i)).ToString(), lf, lb, i * SQ + SQ - 11, 480 - 14);
                g.DrawString((8 - i).ToString(), lf, lb, 2, i * SQ + 2);
            }
        }

        private void DrawPiece(Graphics g, char piece, int x, int y, int sq)
        {
            if (_images.TryGetValue(piece, out var img) && img != null)
            {
                g.DrawImage(img, x + 2, y + 2, sq - 4, sq - 4);
                return;
            }
            if (Glyphs.TryGetValue(piece, out string? glyph))
            {
                using var font = new Font("Segoe UI Symbol", sq * 0.55f, FontStyle.Regular, GraphicsUnit.Pixel);
                bool isWhite = char.IsUpper(piece);
                using var tb = new SolidBrush(isWhite ? Color.WhiteSmoke : Color.FromArgb(30, 30, 30));
                using var sb = new SolidBrush(isWhite ? Color.FromArgb(80, 0, 0, 0) : Color.FromArgb(80, 255, 255, 255));
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(glyph, font, sb, new RectangleF(x + 1, y + 1, sq - 2, sq - 2), sf);
                g.DrawString(glyph, font, tb, new RectangleF(x, y, sq - 2, sq - 2), sf);
            }
        }

        // ── Palette paint ──────────────────────────────────────────────

        private void PaletteCell_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel cell) return;
            char piece = (char)cell.Tag!;
            var g = e.Graphics;

            bool selected = cell == _highlightedCell;
            if (selected)
            {
                using var hlb = new SolidBrush(Color.FromArgb(80, 100, 200, 100));
                g.FillRectangle(hlb, 0, 0, cell.Width, cell.Height);
                using var hlp = new Pen(Color.LimeGreen, 2);
                g.DrawRectangle(hlp, 1, 1, cell.Width - 3, cell.Height - 3);
            }

            if (piece == '.')
            {
                using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
                using var brush = new SolidBrush(Color.OrangeRed);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("✕", font, brush, new RectangleF(0, 0, cell.Width, cell.Height), sf);
            }
            else
            {
                DrawPiece(g, piece, 2, 2, cell.Width - 4);
            }
        }

        // ── Palette click ──────────────────────────────────────────────

        private void PaletteCell_Click(object? sender, EventArgs e)
        {
            if (sender is not Panel cell) return;
            char piece = (char)cell.Tag!;

            if (_highlightedCell == cell)
            {
                _highlightedCell = null;
                _selectedPiece = '\0';
                lblStatus.Text = "Deselected — pick a piece from the palette";
            }
            else
            {
                _highlightedCell = cell;
                _selectedPiece = piece;
                string name = piece == '.' ? "Eraser" : PieceName(piece);
                lblStatus.Text = $"{name} selected — click a square to place it";
            }

            foreach (var c in _paletteCells) c.Invalidate();
        }

        // ── Board click ────────────────────────────────────────────────

        private void PnlBoard_MouseClick(object? sender, MouseEventArgs e)
        {
            const int SQ = 60;
            int col = e.X / SQ;
            int row = e.Y / SQ;
            if (col < 0 || col > 7 || row < 0 || row > 7) return;

            if (e.Button == MouseButtons.Right)
            {
                _board.SetPiece(row, col, '.');
                pnlBoard.Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (_selectedPiece == '\0')
                {
                    lblStatus.Text = "Select a piece from the palette first";
                    return;
                }
                _board.SetPiece(row, col, _selectedPiece);
                pnlBoard.Invalidate();
            }
        }

        // ── Controls ───────────────────────────────────────────────────

        private void RdoWhite_CheckedChanged(object? sender, EventArgs e)
        {
            if (rdoWhite.Checked) _whiteToMove = true;
        }

        private void RdoBlack_CheckedChanged(object? sender, EventArgs e)
        {
            if (rdoBlack.Checked) _whiteToMove = false;
        }

        private void ChkCastleWK_CheckedChanged(object? sender, EventArgs e) => _castleWK = chkCastleWK.Checked;
        private void ChkCastleWQ_CheckedChanged(object? sender, EventArgs e) => _castleWQ = chkCastleWQ.Checked;
        private void ChkCastleBK_CheckedChanged(object? sender, EventArgs e) => _castleBK = chkCastleBK.Checked;
        private void ChkCastleBQ_CheckedChanged(object? sender, EventArgs e) => _castleBQ = chkCastleBQ.Checked;

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            _board = new ChessBoard();
            pnlBoard.Invalidate();
            lblStatus.Text = "Board cleared";
        }

        private void BtnStartPos_Click(object? sender, EventArgs e)
        {
            LoadFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            lblStatus.Text = "Starting position loaded";
        }

        // ── Apply ──────────────────────────────────────────────────────

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            bool hasWK = false, hasBK = false;
            int wTotal = 0, bTotal = 0, wPawns = 0, bPawns = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    char p = _board.GetPiece(r, c);
                    if (p == '.') continue;
                    if (p == 'K') hasWK = true;
                    if (p == 'k') hasBK = true;
                    if (char.IsUpper(p)) { wTotal++; if (p == 'P') wPawns++; }
                    else                 { bTotal++; if (p == 'p') bPawns++; }
                }

            if (!hasWK || !hasBK)
            {
                MessageBox.Show("Both kings (K and k) must be on the board.",
                    "Invalid Position", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (wTotal > 16 || bTotal > 16 || wPawns > 8 || bPawns > 8)
            {
                MessageBox.Show(
                    $"Too many pieces — White: {wTotal}/16, Black: {bTotal}/16, " +
                    $"White pawns: {wPawns}/8, Black pawns: {bPawns}/8.\n" +
                    "The engine cannot analyze positions that exceed legal piece counts.",
                    "Invalid Position", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string side = _whiteToMove ? "w" : "b";
            var sb = new System.Text.StringBuilder();
            if (_castleWK) sb.Append('K');
            if (_castleWQ) sb.Append('Q');
            if (_castleBK) sb.Append('k');
            if (_castleBQ) sb.Append('q');
            string castling = sb.Length > 0 ? sb.ToString() : "-";

            ResultFen = $"{_board.ToFEN()} {side} {castling} - 0 1";
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            Close();
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static string PieceName(char piece)
        {
            bool white = char.IsUpper(piece);
            return char.ToUpper(piece) switch
            {
                'K' => white ? "White King"   : "Black King",
                'Q' => white ? "White Queen"  : "Black Queen",
                'R' => white ? "White Rook"   : "Black Rook",
                'B' => white ? "White Bishop" : "Black Bishop",
                'N' => white ? "White Knight" : "Black Knight",
                'P' => white ? "White Pawn"   : "Black Pawn",
                _   => piece.ToString()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                foreach (var img in _images.Values) img?.Dispose();
                _images.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
