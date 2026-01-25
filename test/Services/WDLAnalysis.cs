namespace ChessDroid.Services
{
    /// <summary>
    /// Win/Draw/Loss information from Stockfish evaluation.
    /// Values are in per mille (out of 1000).
    /// Inspired by Leela Chess Zero's WDL evaluation model.
    /// </summary>
    public class WDLInfo
    {
        /// <summary>Win probability (per mille, 0-1000)</summary>
        public int Win { get; }

        /// <summary>Draw probability (per mille, 0-1000)</summary>
        public int Draw { get; }

        /// <summary>Loss probability (per mille, 0-1000)</summary>
        public int Loss { get; }

        public WDLInfo(int win, int draw, int loss)
        {
            Win = win;
            Draw = draw;
            Loss = loss;
        }

        /// <summary>Win probability as percentage (0-100)</summary>
        public double WinPercent => Win / 10.0;

        /// <summary>Draw probability as percentage (0-100)</summary>
        public double DrawPercent => Draw / 10.0;

        /// <summary>Loss probability as percentage (0-100)</summary>
        public double LossPercent => Loss / 10.0;

        /// <summary>
        /// Calculate position sharpness based on WDL spread.
        /// Higher values = sharper/more volatile position.
        /// Lower values = more drawish/stable position.
        ///
        /// Formula inspired by Lc0's approach:
        /// - High draw probability = low sharpness
        /// - Balanced W/L with low D = high sharpness
        /// </summary>
        public double Sharpness
        {
            get
            {
                // Sharpness = 1 - (Draw / 1000)
                // A position with 100% draw has 0 sharpness
                // A position with 0% draw has 1.0 sharpness
                // We also factor in the W-L imbalance
                double drawFactor = 1.0 - (Draw / 1000.0);

                // If W and L are very imbalanced, the position is "decided" not "sharp"
                // Sharp positions have uncertainty about the outcome (could go either way)
                double total = Win + Loss;
                double balance = total > 0 ? 1.0 - Math.Abs(Win - Loss) / (double)total : 0;

                // Combine: low draw + balanced W/L = sharp
                return drawFactor * balance;
            }
        }

        /// <summary>
        /// Get a human-readable description of position character.
        /// Combines sharpness (draw probability) with advantage assessment (W/L imbalance).
        /// </summary>
        public string GetPositionCharacter()
        {
            double drawPct = DrawPercent;
            double winPct = WinPercent;
            double lossPct = LossPercent;

            // First check for drawish positions (high draw probability)
            if (drawPct >= 70)
                return "very drawish";
            if (drawPct >= 50)
                return "drawish";

            // Then check for decisive/winning positions based on W/L advantage
            // This is the key fix: 69% win is NOT balanced, it's clearly winning!
            if (winPct >= 80 || lossPct >= 80)
                return "decisive";
            if (winPct >= 65 || lossPct >= 65)
                return "winning";
            if (winPct >= 55 || lossPct >= 55)
                return "clear advantage";
            if (winPct >= 45 && winPct > lossPct + 10)
                return "slight edge";
            if (lossPct >= 45 && lossPct > winPct + 10)
                return "slight edge";

            // Check sharpness for balanced positions
            double sharpness = Sharpness;
            if (sharpness >= 0.7)
                return "very sharp";
            if (sharpness >= 0.4)
                return "sharp";

            return "balanced";
        }

        /// <summary>
        /// Format WDL as a compact display string.
        /// Example: "W:45% D:30% L:25%"
        /// </summary>
        public string ToDisplayString()
        {
            return $"W:{WinPercent:F0}% D:{DrawPercent:F0}% L:{LossPercent:F0}%";
        }

        /// <summary>
        /// Format WDL with sharpness indicator.
        /// Example: "W:45% D:30% L:25% (sharp)"
        /// </summary>
        public string ToDisplayStringWithCharacter()
        {
            return $"{ToDisplayString()} ({GetPositionCharacter()})";
        }

        public override string ToString() => ToDisplayString();
    }

    /// <summary>
    /// Static utilities for WDL-based analysis.
    /// </summary>
    public static class WDLUtilities
    {
        /// <summary>
        /// Convert centipawn evaluation to win probability using Lc0-style formula.
        /// Formula: cp = 111.714640912 × tan(1.5620688421 × Q)
        /// Inverted: Q = atan(cp / 111.714640912) / 1.5620688421
        /// Win% = (Q + 1) / 2 * 100
        /// </summary>
        public static double CentipawnsToWinProbability(double centipawns)
        {
            // Lc0 formula constants
            const double A = 111.714640912;
            const double B = 1.5620688421;

            // Clamp extreme values to avoid overflow
            centipawns = Math.Max(-1500, Math.Min(1500, centipawns));

            // Q = atan(cp / A) / B
            double q = Math.Atan(centipawns / A) / B;

            // Clamp Q to [-1, 1]
            q = Math.Max(-1, Math.Min(1, q));

            // Convert Q to win percentage: (Q + 1) / 2 * 100
            return (q + 1) / 2 * 100;
        }

        /// <summary>
        /// Estimate WDL from centipawn evaluation when engine doesn't provide it.
        /// Uses a simplified model based on evaluation magnitude.
        /// </summary>
        public static WDLInfo EstimateWDLFromCentipawns(double centipawns)
        {
            double winProb = CentipawnsToWinProbability(centipawns);
            double lossProb = 100 - winProb;

            // Estimate draw probability based on evaluation magnitude
            // Near equal positions have higher draw probability
            double absCp = Math.Abs(centipawns);
            double drawProb;

            if (absCp < 20)
                drawProb = 50; // Very equal = high draw chance
            else if (absCp < 50)
                drawProb = 40;
            else if (absCp < 100)
                drawProb = 25;
            else if (absCp < 200)
                drawProb = 15;
            else if (absCp < 400)
                drawProb = 8;
            else
                drawProb = 3; // Large advantage = low draw chance

            // Redistribute the draw probability from win/loss
            double totalWL = winProb + lossProb - drawProb;
            double winRatio = winProb / (winProb + lossProb);
            winProb = totalWL * winRatio;
            lossProb = totalWL * (1 - winRatio);

            // Convert to per mille
            return new WDLInfo(
                (int)(winProb * 10),
                (int)(drawProb * 10),
                (int)(lossProb * 10)
            );
        }

        /// <summary>
        /// Get a color suggestion for sharpness display.
        /// </summary>
        public static System.Drawing.Color GetSharpnessColor(double sharpness)
        {
            if (sharpness >= 0.7)
                return System.Drawing.Color.OrangeRed;      // Very sharp - danger/excitement
            if (sharpness >= 0.4)
                return System.Drawing.Color.Orange;          // Sharp
            if (sharpness >= 0.2)
                return System.Drawing.Color.Gold;            // Balanced
            return System.Drawing.Color.LightBlue;           // Drawish - calm
        }
    }
}
