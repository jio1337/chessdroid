using System.Drawing;
using System.Drawing.Drawing2D;
using ChessDroid.Models;

namespace ChessDroid.Controls
{
    public class EvalGraphControl : Panel
    {
        public event Action<MoveNode>? MoveNodeSelected;

        private MoveTree? _moveTree;
        private MoveNode? _currentNode;
        private bool _isDarkMode = true;
        private readonly List<(MoveNode node, float x)> _nodePositions = new();

        // Cached polygon points list — reused every paint, avoiding per-frame List<PointF> allocation
        private readonly List<PointF> _points = new();
        private static readonly List<MoveNode> _emptyMainLine = new();

        // Cached GDI objects — rebuilt only when theme changes
        private SolidBrush? _whiteAdvBrush;
        private SolidBrush? _blackAdvBrush;
        private Pen? _linePen;
        private Pen? _centerLinePen;
        private Pen? _borderPen;
        private readonly Pen _currentNodePen = new Pen(Color.FromArgb(180, 80, 200, 255), 1.5f);

        public EvalGraphControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            RebuildThemeResources();
        }

        private void RebuildThemeResources()
        {
            _whiteAdvBrush?.Dispose();
            _blackAdvBrush?.Dispose();
            _linePen?.Dispose();
            _centerLinePen?.Dispose();
            _borderPen?.Dispose();

            _whiteAdvBrush = new SolidBrush(_isDarkMode ? Color.FromArgb(195, 195, 195) : Color.FromArgb(225, 225, 225));
            _blackAdvBrush = new SolidBrush(_isDarkMode ? Color.FromArgb(55, 55, 55)  : Color.FromArgb(90, 90, 90));
            _linePen       = new Pen(_isDarkMode ? Color.FromArgb(140, 140, 140) : Color.FromArgb(100, 100, 100), 1.5f);
            _centerLinePen = new Pen(_isDarkMode ? Color.FromArgb(70, 70, 70)    : Color.FromArgb(155, 155, 155), 1f);
            _borderPen     = new Pen(_isDarkMode ? Color.FromArgb(60, 60, 60)    : Color.FromArgb(180, 180, 180));
        }

        public void SetData(MoveTree? tree, MoveNode? currentNode, bool isDarkMode)
        {
            _moveTree = tree;
            _currentNode = currentNode;
            if (_isDarkMode != isDarkMode)
            {
                _isDarkMode = isDarkMode;
                RebuildThemeResources();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = _isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(245, 245, 245);
            g.Clear(bg);

            _nodePositions.Clear();

            var mainLine = _moveTree?.GetMainLine() ?? _emptyMainLine;
            if (mainLine.Count == 0)
            {
                DrawCenterLine(g);
                DrawBorder(g);
                return;
            }

            float w = Width;
            float h = Height;
            float midY = h / 2f;
            const float maxEval = 5f;

            // Build polygon points into cached list — no per-frame List<PointF> allocation
            _points.Clear();
            _points.Add(new PointF(0, midY));
            for (int i = 0; i < mainLine.Count; i++)
            {
                float x = (i + 0.5f) * w / mainLine.Count;
                float eval = mainLine[i].Evaluation is double ev
                    ? (float)Math.Max(-maxEval, Math.Min(maxEval, ev))
                    : 0f;
                float y = Math.Max(1, Math.Min(h - 1, midY - eval / maxEval * midY));
                _points.Add(new PointF(x, y));
                _nodePositions.Add((mainLine[i], x));
            }
            _points.Add(new PointF(w, midY));
            var pts = _points.ToArray();

            // White advantage (above midY) — SetClip(RectangleF) avoids Region allocation
            g.SetClip(new RectangleF(0, 0, w, midY));
            g.FillPolygon(_whiteAdvBrush!, pts);
            g.ResetClip();

            // Black advantage (below midY)
            g.SetClip(new RectangleF(0, midY, w, h - midY));
            g.FillPolygon(_blackAdvBrush!, pts);
            g.ResetClip();

            // Eval curve (skip baseline endpoints) — DrawLines needs at least 2 points
            if (_points.Count > 3)
                g.DrawLines(_linePen!, pts[1..^1]);

            DrawCenterLine(g);

            // Current move indicator
            if (_currentNode != null)
            {
                var pos = _nodePositions.FirstOrDefault(np => ReferenceEquals(np.node, _currentNode));
                if (pos.node != null)
                    g.DrawLine(_currentNodePen, pos.x, 0, pos.x, h);
            }

            DrawBorder(g);
        }

        private void DrawCenterLine(Graphics g)
        {
            float midY = Height / 2f;
            g.DrawLine(_centerLinePen!, 0, midY, Width, midY);
        }

        private void DrawBorder(Graphics g)
        {
            g.DrawRectangle(_borderPen!, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _whiteAdvBrush?.Dispose();
                _blackAdvBrush?.Dispose();
                _linePen?.Dispose();
                _centerLinePen?.Dispose();
                _borderPen?.Dispose();
                _currentNodePen.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_nodePositions.Count == 0) return;

            MoveNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var (node, x) in _nodePositions)
            {
                float dist = Math.Abs(e.X - x);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }

            if (closest != null)
                MoveNodeSelected?.Invoke(closest);
        }
    }
}
