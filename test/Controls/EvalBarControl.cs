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
        private double evaluation; // Centipawns from White's perspective
        private bool isMate;
        private int mateIn; // Positive = White delivers mate, Negative = Black delivers mate

        // Colors
        private readonly Color whiteColor = Color.FromArgb(238, 238, 238);
        private readonly Color blackColor = Color.FromArgb(51, 51, 51);
        private readonly Color borderColor = Color.FromArgb(100, 100, 100);

        // Font for eval text
        private readonly Font evalFont = new Font("Segoe UI", 8f, FontStyle.Bold);

        public EvalBarControl()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        /// <summary>
        /// Sets the evaluation in centipawns from White's perspective.
        /// Positive = White is better, Negative = Black is better.
        /// </summary>
        public void SetEvaluation(double centipawns)
        {
            isMate = false;
            mateIn = 0;
            evaluation = centipawns;
            Invalidate();
        }

        /// <summary>
        /// Sets a mate score. Positive = White delivers mate, Negative = Black delivers mate.
        /// </summary>
        public void SetMate(int mateInMoves)
        {
            isMate = true;
            mateIn = mateInMoves;
            evaluation = mateInMoves > 0 ? 10000 : -10000;
            Invalidate();
        }

        /// <summary>
        /// Resets the bar to equal (0.0).
        /// </summary>
        public void Reset()
        {
            isMate = false;
            mateIn = 0;
            evaluation = 0;
            Invalidate();
        }

        /// <summary>
        /// Converts centipawn evaluation to white's percentage of the bar.
        /// Uses a sigmoid curve so the bar responds smoothly.
        /// </summary>
        private double GetWhitePercent()
        {
            if (isMate)
            {
                return mateIn > 0 ? 96.0 : 4.0;
            }

            // Sigmoid: 50 + 50 * tanh(cp / 500)
            // ±200cp ≈ 69/31%, ±400cp ≈ 84/16%, ±700cp ≈ 94/6%
            double t = Math.Tanh(evaluation / 500.0);
            double pct = 50.0 + 50.0 * t;

            // Clamp so both colors are always slightly visible
            return Math.Clamp(pct, 4.0, 96.0);
        }

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
            using (var blackBrush = new SolidBrush(blackColor))
            {
                g.FillRectangle(blackBrush, 0, 0, w, blackHeight);
            }

            // Draw white section (bottom)
            using (var whiteBrush = new SolidBrush(whiteColor))
            {
                g.FillRectangle(whiteBrush, 0, blackHeight, w, whiteHeight);
            }

            // Draw eval text
            string evalText = GetEvalText();
            if (!string.IsNullOrEmpty(evalText))
            {
                SizeF textSize = g.MeasureString(evalText, evalFont);
                float textX = (w - textSize.Width) / 2f;

                // Place text on the dominant side
                bool whiteIsBetter = evaluation >= 0 && (!isMate || mateIn > 0);
                float textY;
                Color textColor;

                if (whiteIsBetter)
                {
                    // Text in white section (bottom), near the boundary
                    textY = blackHeight + 4f;
                    textY = Math.Min(textY, h - textSize.Height - 2f);
                    textColor = blackColor;
                }
                else
                {
                    // Text in black section (top), near the boundary
                    textY = blackHeight - textSize.Height - 4f;
                    textY = Math.Max(textY, 2f);
                    textColor = whiteColor;
                }

                using (var textBrush = new SolidBrush(textColor))
                {
                    g.DrawString(evalText, evalFont, textBrush, textX, textY);
                }
            }

            // Draw border
            using (var borderPen = new Pen(borderColor, 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }
        }

        private string GetEvalText()
        {
            if (isMate)
            {
                return $"M{Math.Abs(mateIn)}";
            }

            double pawns = evaluation / 100.0;
            if (Math.Abs(pawns) < 0.05)
                return "0.0";

            string sign = pawns > 0 ? "+" : "";
            return $"{sign}{pawns:F1}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                evalFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
