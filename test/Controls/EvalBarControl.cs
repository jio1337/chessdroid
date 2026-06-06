using System.Drawing.Drawing2D;

namespace ChessDroid.Controls
{
    /// <summary>
    /// Vertical evaluation bar that visually represents the engine evaluation.
    /// White section at bottom grows upward when White is better;
    /// black section at top grows downward when Black is better.
    /// </summary>
    public class EvalBarControl : Control
    {
        private double _displayPercent = 50.0; // Current animated bar fill % (what's drawn)
        private double _targetPercent  = 50.0; // Where the bar is heading
        private double _targetEvaluation;      // Raw centipawn value — used only for text display
        private bool isMate;
        private int mateIn; // Positive = White delivers mate, Negative = Black delivers mate

        // Lerp in visual-percent space so the animation speed is uniform regardless of eval magnitude.
        // Going from equal to +0.7 looks the same speed as going from +0.7 to mate.
        private readonly System.Windows.Forms.Timer _animTimer;
        private const double LerpSpeed = 0.18; // fraction of remaining gap closed per 16ms tick

        // Colors
        private readonly Color whiteColor = Color.FromArgb(238, 238, 238);
        private readonly Color blackColor = Color.FromArgb(51, 51, 51);
        private readonly Color borderColor = Color.FromArgb(100, 100, 100);

        // Font for eval text
        private readonly Font evalFont = new Font("Segoe UI", 8f, FontStyle.Bold);

        // Cached GDI objects — created once, reused every OnPaint
        private readonly SolidBrush _blackBrush;
        private readonly SolidBrush _whiteBrush;
        private readonly Pen _borderPen;

        public EvalBarControl()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            _blackBrush = new SolidBrush(blackColor);
            _whiteBrush = new SolidBrush(whiteColor);
            _borderPen  = new Pen(borderColor, 1f);

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += AnimTimer_Tick;
        }

        private static double CentipawnsToPercent(double cp)
        {
            double t = Math.Tanh(cp / 500.0);
            return Math.Clamp(50.0 + 50.0 * t, 4.0, 96.0);
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            double delta = _targetPercent - _displayPercent;
            if (Math.Abs(delta) < 0.1)
            {
                _displayPercent = _targetPercent;
                _animTimer.Stop();
            }
            else
            {
                _displayPercent += delta * LerpSpeed;
            }
            Invalidate();
        }

        /// <summary>
        /// Sets the evaluation in centipawns from White's perspective.
        /// Positive = White is better, Negative = Black is better.
        /// </summary>
        public void SetEvaluation(double centipawns)
        {
            isMate = false;
            mateIn = 0;
            _targetEvaluation = centipawns;
            _targetPercent = CentipawnsToPercent(centipawns);
            _animTimer.Start();
        }

        /// <summary>
        /// Sets a mate score. Positive = White delivers mate, Negative = Black delivers mate.
        /// </summary>
        public void SetMate(int mateInMoves)
        {
            isMate = true;
            mateIn = mateInMoves;
            _targetEvaluation = mateInMoves > 0 ? 10000 : -10000;
            _targetPercent = mateInMoves > 0 ? 100.0 : 0.0;
            _animTimer.Start();
        }

        /// <summary>
        /// Sets the bar to the terminal checkmate state (game already over). Displays "M0",
        /// bar fills white if White won, black if Black won.
        /// </summary>
        public void SetTerminalMate(bool whiteWon)
        {
            isMate = true;
            mateIn = 0;
            _targetEvaluation = whiteWon ? 10000 : -10000;
            _targetPercent = whiteWon ? 100.0 : 0.0;
            _animTimer.Start();
        }

        /// <summary>
        /// Resets the bar to equal (0.0) instantly (no animation).
        /// </summary>
        public void Reset()
        {
            isMate = false;
            mateIn = 0;
            _animTimer.Stop();
            _displayPercent = 50.0;
            _targetPercent  = 50.0;
            _targetEvaluation = 0;
            Invalidate();
        }

        private double GetWhitePercent() => _displayPercent;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            if (w <= 0 || h <= 0) return;

            double whitePct = GetWhitePercent() / 100.0;
            int whiteHeight = (int)(h * whitePct);
            int blackHeight = h - whiteHeight;

            // Draw black section (top)
            g.FillRectangle(_blackBrush, 0, 0, w, blackHeight);

            // Draw white section (bottom)
            g.FillRectangle(_whiteBrush, 0, blackHeight, w, whiteHeight);

            // Draw eval text
            string evalText = GetEvalText();
            if (!string.IsNullOrEmpty(evalText))
            {
                SizeF textSize = g.MeasureString(evalText, evalFont);
                float textX = (w - textSize.Width) / 2f;

                // Place text on the dominant side
                bool whiteIsBetter = _targetEvaluation >= 0;
                float textY;
                SolidBrush textBrush;

                if (whiteIsBetter)
                {
                    // Text in white section (bottom), near the boundary
                    textY = blackHeight + 4f;
                    textY = Math.Min(textY, h - textSize.Height - 2f);
                    textBrush = _blackBrush;
                }
                else
                {
                    // Text in black section (top), near the boundary
                    textY = blackHeight - textSize.Height - 4f;
                    textY = Math.Max(textY, 2f);
                    textBrush = _whiteBrush;
                }

                g.DrawString(evalText, evalFont, textBrush, textX, textY);
            }

            // Draw border
            g.DrawRectangle(_borderPen, 0, 0, w - 1, h - 1);
        }

        private string GetEvalText()
        {
            if (isMate)
                return $"M{Math.Abs(mateIn)}";

            // Show the target value so the label doesn't float during animation
            double pawns = _targetEvaluation / 100.0;
            if (Math.Abs(pawns) < 0.05)
                return "0.0";

            string sign = pawns > 0 ? "+" : "";
            return sign + pawns.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer.Stop();
                _animTimer.Dispose();
                evalFont.Dispose();
                _blackBrush.Dispose();
                _whiteBrush.Dispose();
                _borderPen.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
