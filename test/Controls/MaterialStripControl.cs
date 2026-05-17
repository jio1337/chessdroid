using System.Diagnostics;

namespace ChessDroid.Controls
{
    public class MaterialStripControl : Control
    {
        private Dictionary<char, Image?> _images = new();
        private string _templateSet = "";
        private char[] _pieces = Array.Empty<char>();
        private int _advantage = 0;
        private bool _isDarkMode = true;
        private readonly Font _advFont;
        private readonly SolidBrush _brush;

        public MaterialStripControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            _advFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _brush = new SolidBrush(Color.FromArgb(200, 200, 200));
        }

        public void SetTemplateSet(string templateSet)
        {
            if (templateSet == _templateSet) return;
            _templateSet = templateSet;
            LoadImages();
            Invalidate();
        }

        public void SetTextColor(Color color)
        {
            _brush.Color = color;
            Invalidate();
        }

        public void SetDarkMode(bool isDark)
        {
            _isDarkMode = isDark;
            Invalidate();
        }

        private void LoadImages()
        {
            foreach (var img in _images.Values) img?.Dispose();
            _images.Clear();

            if (string.IsNullOrEmpty(_templateSet)) return;

            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", _templateSet);
            string lichessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Lichess");

            var map = new Dictionary<char, string>
            {
                { 'P', "wP.png" }, { 'N', "wN.png" }, { 'B', "wB.png" }, { 'R', "wR.png" }, { 'Q', "wQ.png" },
                { 'p', "bP.png" }, { 'n', "bN.png" }, { 'b', "bB.png" }, { 'r', "bR.png" }, { 'q', "bQ.png" },
            };
            foreach (var kvp in map)
            {
                // SVG sets have no PNGs — fall back to Chess.com (outlined pieces, better contrast on dark backgrounds)
                string path = Path.Combine(basePath, kvp.Value);
                if (!File.Exists(path))
                    path = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Chess.com"), kvp.Value);
                if (!File.Exists(path))
                    path = Path.Combine(lichessPath, kvp.Value);
                try { _images[kvp.Key] = File.Exists(path) ? Image.FromFile(path) : null; }
                catch (Exception ex) { Debug.WriteLine($"MaterialStrip load failed: {ex.Message}"); _images[kvp.Key] = null; }
            }
        }

        public void UpdateMaterial(char[] capturedPieces, int advantage)
        {
            _pieces = capturedPieces;
            _advantage = advantage;
            if (IsHandleCreated) BeginInvoke(new Action(Invalidate));
            else Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_pieces.Length == 0 && _advantage == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int sz = Math.Max(12, Height - 4);
            int x = 2;

            // Draw subtle background only behind the piece area so it doesn't bleed to strip edge
            if (_pieces.Length > 0)
            {
                int pieceAreaW = _pieces.Length * (sz - 3) + 3 + 4;
                int py = (Height - sz) / 2;
                using var bg = new SolidBrush(_isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(50, 0, 0, 0));
                using var path = RoundedRect(new Rectangle(x - 2, py, pieceAreaW, sz), 4);
                g.FillPath(bg, path);
            }

            foreach (char piece in _pieces)
            {
                if (_images.TryGetValue(piece, out Image? img) && img != null)
                    g.DrawImage(img, x, (Height - sz) / 2, sz, sz);
                x += sz - 3; // slight overlap, chess.com style
            }

            if (_advantage > 0)
            {
                float ty = (Height - _advFont.Height) / 2f;
                g.DrawString($"+{_advantage}", _advFont, _brush, x + 3, ty);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var img in _images.Values) img?.Dispose();
                _advFont.Dispose();
                _brush.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
