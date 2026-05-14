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

        public EvalGraphControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public void SetData(MoveTree? tree, MoveNode? currentNode, bool isDarkMode)
        {
            _moveTree = tree;
            _currentNode = currentNode;
            _isDarkMode = isDarkMode;
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

            var mainLine = _moveTree?.GetMainLine() ?? new List<MoveNode>();
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

            // Build polygon points: baseline-start → eval points → baseline-end
            var points = new List<PointF> { new PointF(0, midY) };
            for (int i = 0; i < mainLine.Count; i++)
            {
                float x = (i + 0.5f) * w / mainLine.Count;
                float eval = mainLine[i].Evaluation is double ev
                    ? (float)Math.Max(-maxEval, Math.Min(maxEval, ev))
                    : 0f;
                float y = Math.Max(1, Math.Min(h - 1, midY - eval / maxEval * midY));
                points.Add(new PointF(x, y));
                _nodePositions.Add((mainLine[i], x));
            }
            points.Add(new PointF(w, midY));
            var pts = points.ToArray();

            // White advantage (above midY)
            Color whiteAdv = _isDarkMode ? Color.FromArgb(195, 195, 195) : Color.FromArgb(225, 225, 225);
            using (var clip = new Region(new RectangleF(0, 0, w, midY)))
            {
                g.SetClip(clip, CombineMode.Replace);
                using var brush = new SolidBrush(whiteAdv);
                g.FillPolygon(brush, pts);
                g.ResetClip();
            }

            // Black advantage (below midY)
            Color blackAdv = _isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(90, 90, 90);
            using (var clip = new Region(new RectangleF(0, midY, w, h - midY)))
            {
                g.SetClip(clip, CombineMode.Replace);
                using var brush = new SolidBrush(blackAdv);
                g.FillPolygon(brush, pts);
                g.ResetClip();
            }

            // Eval curve (skip baseline endpoints) — DrawLines needs at least 2 points
            if (points.Count > 3)
            {
                Color lineColor = _isDarkMode ? Color.FromArgb(140, 140, 140) : Color.FromArgb(100, 100, 100);
                using var pen = new Pen(lineColor, 1.5f);
                g.DrawLines(pen, points.Skip(1).Take(points.Count - 2).ToArray());
            }

            DrawCenterLine(g);

            // Current move indicator
            if (_currentNode != null)
            {
                var pos = _nodePositions.FirstOrDefault(np => ReferenceEquals(np.node, _currentNode));
                if (pos.node != null)
                {
                    using var pen = new Pen(Color.FromArgb(180, 80, 200, 255), 1.5f);
                    g.DrawLine(pen, pos.x, 0, pos.x, h);
                }
            }

            DrawBorder(g);
        }

        private void DrawCenterLine(Graphics g)
        {
            Color c = _isDarkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(155, 155, 155);
            using var pen = new Pen(c, 1f);
            float midY = Height / 2f;
            g.DrawLine(pen, 0, midY, Width, midY);
        }

        private void DrawBorder(Graphics g)
        {
            Color c = _isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(180, 180, 180);
            using var pen = new Pen(c);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
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
