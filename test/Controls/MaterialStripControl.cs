using System.Diagnostics;

namespace ChessDroid.Controls
{
    public class MaterialStripControl : Control
    {
        private Dictionary<char, Image?> _images = new();
        private string _templateSet = "";
        private char[] _pieces = Array.Empty<char>();
        private int _advantage = 0;
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

        private void LoadImages()
        {
            foreach (var img in _images.Values) img?.Dispose();
            _images.Clear();

            if (string.IsNullOrEmpty(_templateSet)) return;

            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", _templateSet);
            var map = new Dictionary<char, string>
            {
                { 'P', "wP.png" }, { 'N', "wN.png" }, { 'B', "wB.png" }, { 'R', "wR.png" }, { 'Q', "wQ.png" },
                { 'p', "bP.png" }, { 'n', "bN.png" }, { 'b', "bB.png" }, { 'r', "bR.png" }, { 'q', "bQ.png" },
            };
            foreach (var kvp in map)
            {
                string path = Path.Combine(basePath, kvp.Value);
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
            int sz = Math.Max(12, Height - 4);
            int x = 2;

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
