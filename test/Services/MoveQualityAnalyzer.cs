using System.Drawing;

namespace ChessDroid.Services
{
    /// <summary>
    /// Analyzes move quality similar to chess.com's system.
    /// Classifies moves as Brilliant, Best, Excellent, Good, Inaccuracy, Mistake, or Blunder.
    /// </summary>
    public static class MoveQualityAnalyzer
    {
        /// <summary>
        /// Move quality classifications
        /// </summary>
        public enum MoveQuality
        {
            Brilliant,    // !! - Exceptional move, often a sacrifice that's hard to find
            Best,         // Best engine move
            Excellent,    // Very close to best (within 10cp)
            Good,         // Reasonable move (within 30cp)
            Book,         // Opening book move
            Inaccuracy,   // ?! - Slight mistake (30-100cp loss)
            Mistake,      // ? - Significant mistake (100-300cp loss)
            Blunder,      // ?? - Severe mistake (300+cp loss or missing forced mate)
            Forced        // Only legal move
        }

        /// <summary>
        /// Move quality result with display info
        /// </summary>
        public class MoveQualityResult
        {
            public MoveQuality Quality { get; set; }
            public string Symbol { get; set; } = "";
            public string Description { get; set; } = "";
            public Color Color { get; set; }
            public double CentipawnLoss { get; set; }
        }

        /// <summary>
        /// Analyze the quality of a move based on centipawn loss and other factors.
        /// </summary>
        /// <param name="evalBefore">Evaluation before the move (from our perspective, positive = good)</param>
        /// <param name="evalAfter">Evaluation after the move</param>
        /// <param name="isBestMove">Whether this was the engine's best move</param>
        /// <param name="isOnlyLegalMove">Whether this was the only legal move</param>
        /// <param name="isBookMove">Whether this matches opening theory</param>
        /// <param name="isSacrifice">Whether the move sacrifices material</param>
        /// <param name="winsSignificantMaterial">Whether the move wins significant material unexpectedly</param>
        /// <param name="aggressiveness">User's aggressiveness setting (0-100)</param>
        /// <returns>Move quality classification</returns>
        public static MoveQualityResult AnalyzeMoveQuality(
            double evalBefore,
            double evalAfter,
            bool isBestMove,
            bool isOnlyLegalMove = false,
            bool isBookMove = false,
            bool isSacrifice = false,
            bool winsSignificantMaterial = false,
            int aggressiveness = 50)
        {
            // Calculate centipawn loss (negative means improvement, but shouldn't happen normally)
            double cpLoss = evalBefore - evalAfter;

            // Handle mate scores specially
            bool wasMateForUs = evalBefore > 9000;
            bool wasMateAgainstUs = evalBefore < -9000;
            bool isMateForUs = evalAfter > 9000;
            bool isMateAgainstUs = evalAfter < -9000;

            // Adjust thresholds based on aggressiveness
            // More aggressive = more lenient on sharp play, harsher on passive play
            double aggressivenessMultiplier = 1.0 + (aggressiveness - 50) / 200.0; // 0.75 to 1.25

            // Special cases first
            if (isOnlyLegalMove)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Forced,
                    Symbol = "",
                    Description = "Forced",
                    Color = Color.Gray,
                    CentipawnLoss = cpLoss
                };
            }

            if (isBookMove && cpLoss < 30)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Book,
                    Symbol = "",
                    Description = "Book",
                    Color = Color.FromArgb(168, 168, 168),
                    CentipawnLoss = cpLoss
                };
            }

            // Blunder detection - missing mate or huge material loss
            if (wasMateForUs && !isMateForUs)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Blunder,
                    Symbol = "??",
                    Description = "Blunder - missed checkmate",
                    Color = Color.FromArgb(202, 52, 49), // Red
                    CentipawnLoss = 9999
                };
            }

            if (!wasMateAgainstUs && isMateAgainstUs)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Blunder,
                    Symbol = "??",
                    Description = "Blunder - allows checkmate",
                    Color = Color.FromArgb(202, 52, 49),
                    CentipawnLoss = 9999
                };
            }

            // Brilliant move detection
            // A move is brilliant if:
            // 1. It's the best move AND
            // 2. It involves a sacrifice OR wins significant material unexpectedly
            // 3. AND it maintains or improves the position significantly
            if (isBestMove && (isSacrifice || winsSignificantMaterial) && cpLoss <= 0)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Brilliant,
                    Symbol = "!!",
                    Description = "Brilliant",
                    Color = Color.FromArgb(26, 179, 148), // Cyan/teal
                    CentipawnLoss = cpLoss
                };
            }

            // Standard quality classification based on centipawn loss
            double blunderThreshold = 300 * aggressivenessMultiplier;
            double mistakeThreshold = 100 * aggressivenessMultiplier;
            double inaccuracyThreshold = 30 * aggressivenessMultiplier;
            double excellentThreshold = 10;

            if (cpLoss >= blunderThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Blunder,
                    Symbol = "??",
                    Description = "Blunder",
                    Color = Color.FromArgb(202, 52, 49), // Red
                    CentipawnLoss = cpLoss
                };
            }

            if (cpLoss >= mistakeThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Mistake,
                    Symbol = "?",
                    Description = "Mistake",
                    Color = Color.FromArgb(232, 106, 51), // Orange
                    CentipawnLoss = cpLoss
                };
            }

            if (cpLoss >= inaccuracyThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Inaccuracy,
                    Symbol = "?!",
                    Description = "Inaccuracy",
                    Color = Color.FromArgb(247, 199, 72), // Yellow
                    CentipawnLoss = cpLoss
                };
            }

            if (isBestMove)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Best,
                    Symbol = "",
                    Description = "Best",
                    Color = Color.FromArgb(150, 194, 90), // Green
                    CentipawnLoss = cpLoss
                };
            }

            if (cpLoss <= excellentThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Excellent,
                    Symbol = "",
                    Description = "Excellent",
                    Color = Color.FromArgb(150, 194, 90), // Light green
                    CentipawnLoss = cpLoss
                };
            }

            return new MoveQualityResult
            {
                Quality = MoveQuality.Good,
                Symbol = "",
                Description = "Good",
                Color = Color.FromArgb(119, 171, 89), // Darker green
                CentipawnLoss = cpLoss
            };
        }

        /// <summary>
        /// Quick analysis based just on centipawn loss (for display purposes)
        /// </summary>
        public static MoveQualityResult QuickAnalyze(double cpLoss, int aggressiveness = 50)
        {
            return AnalyzeMoveQuality(
                evalBefore: cpLoss,
                evalAfter: 0,
                isBestMove: cpLoss <= 0,
                aggressiveness: aggressiveness
            );
        }

        /// <summary>
        /// Get a color for a given centipawn loss value
        /// </summary>
        public static Color GetColorForCpLoss(double cpLoss)
        {
            if (cpLoss <= 0) return Color.FromArgb(150, 194, 90);    // Green - best/improvement
            if (cpLoss < 30) return Color.FromArgb(119, 171, 89);    // Dark green - good
            if (cpLoss < 100) return Color.FromArgb(247, 199, 72);   // Yellow - inaccuracy
            if (cpLoss < 300) return Color.FromArgb(232, 106, 51);   // Orange - mistake
            return Color.FromArgb(202, 52, 49);                       // Red - blunder
        }

        /// <summary>
        /// Get a description based on evaluation difference from best move
        /// </summary>
        public static string GetMoveQualitySymbol(double cpLoss)
        {
            if (cpLoss <= 0) return "";      // Best move
            if (cpLoss < 30) return "";      // Good
            if (cpLoss < 100) return "?!";   // Inaccuracy
            if (cpLoss < 300) return "?";    // Mistake
            return "??";                      // Blunder
        }
    }
}
