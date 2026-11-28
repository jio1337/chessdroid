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
            Precise,      // !  - Only winning or only saving move in a critical position
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

            // (Mate scores need no special case: EvaluationParser maps a mate to ±10..15 pawns,
            // i.e. a win probability of ~0 or ~1, so missing or allowing a mate is judged by the
            // win-probability drop below like everything else. The old "> 9000" mate branches
            // here could never fire for that reason.)

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

            // If it's the engine's best move it cannot be an inaccuracy/mistake/blunder.
            // Two independent engine searches (before and after the move) may disagree by
            // 30-100cp due to different search paths — that eval delta is noise, not a real
            // mistake. Always trust isBestMove over the raw centipawn loss.
            if (isBestMove)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Best,
                    Symbol = "",
                    Description = "Best",
                    Color = Color.FromArgb(150, 194, 90),
                    CentipawnLoss = cpLoss
                };
            }

            // Errors are judged by the drop in win probability, not raw centipawns — the same rule
            // the analysis panel uses for its alternative-move labels and the accuracy score uses.
            // Raw centipawns called +10.12 → +6.23 (still completely winning) a "Blunder" and
            // -3.5 → -4.6 (already lost) a "Mistake"; 4 pawns matter far more at +1 than at +10.
            double winDrop = WinProbability(evalBefore / 100.0) - WinProbability(evalAfter / 100.0);
            double blunderThreshold    = BlunderDrop    * aggressivenessMultiplier;
            double mistakeThreshold    = MistakeDrop    * aggressivenessMultiplier;
            double inaccuracyThreshold = InaccuracyDrop * aggressivenessMultiplier;
            double excellentThreshold  =  10;

            if (winDrop >= blunderThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Blunder,
                    Symbol = "??",
                    Description = "Blunder",
                    Color = Color.FromArgb(202, 52, 49),
                    CentipawnLoss = cpLoss
                };
            }

            if (winDrop >= mistakeThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Mistake,
                    Symbol = "?",
                    Description = "Mistake",
                    Color = Color.FromArgb(232, 106, 51),
                    CentipawnLoss = cpLoss
                };
            }

            if (winDrop >= inaccuracyThreshold)
            {
                return new MoveQualityResult
                {
                    Quality = MoveQuality.Inaccuracy,
                    Symbol = "?!",
                    Description = "Inaccuracy",
                    Color = Color.FromArgb(247, 199, 72),
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
                    Color = Color.FromArgb(150, 194, 90),
                    CentipawnLoss = cpLoss
                };
            }

            return new MoveQualityResult
            {
                Quality = MoveQuality.Good,
                Symbol = "",
                Description = "Good",
                Color = Color.FromArgb(119, 171, 89),
                CentipawnLoss = cpLoss
            };
        }

        // Win-probability drops (expected points lost) that make a move an error. Shared with
        // the analysis panel's labels on alternative moves (ConsoleOutputFormatter), so a move
        // gets the same "?!/?/??" there as in Game Review.
        public const double InaccuracyDrop = 0.05;
        public const double MistakeDrop = 0.10;
        public const double BlunderDrop = 0.20;

        /// <summary>
        /// Win probability (0..1) for the side an eval (in pawns) favours — Lichess's curve,
        /// 1 / (1 + e^(-0.00368208·cp)). Game Review's accuracy score (Lichess's per-move formula,
        /// 103.1668·e^(-0.04354·loss) − 3.1669) was fitted to this curve, and with the 5/10/20%
        /// thresholds it puts inaccuracy/mistake/blunder at ≈0.5/1.1/2.3 pawns near equality
        /// while shrinking them once one side is winning big. The steeper 1/(1+10^(-eval/4)) used
        /// before made a 0.35-pawn slip (about the noise between two depth-15 searches) an
        /// inaccuracy and 1.5 pawns a blunder.
        /// </summary>
        public static double WinProbability(double evalInPawns)
            => 1.0 / (1.0 + Math.Exp(-0.368208 * evalInPawns));
    }
}
