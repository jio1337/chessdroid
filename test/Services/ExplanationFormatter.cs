using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// User Experience enhancements for move explanations
    /// Provides color coding, formatting, complexity levels, and visual presentation
    /// </summary>
    public static class ExplanationFormatter
    {
        // =============================
        // COMPLEXITY LEVELS
        // Different explanation depths for different skill levels
        // =============================

        public enum ComplexityLevel
        {
            Beginner,       // Simple, clear language (e.g., "attacks queen")
            Intermediate,   // More detail (e.g., "creates threat on undefended queen")
            Advanced,       // Full detail (e.g., "knight on strong outpost attacks undefended queen (SEE +6)")
            Master          // Technical (e.g., "Nd5 exploits weak c7 square, creates dual threats on e7 queen and b6 knight")
        }

        public static ComplexityLevel CurrentLevel { get; set; } = ComplexityLevel.Intermediate;

        /// <summary>
        /// Load settings from AppConfig and apply them
        /// </summary>
        public static void LoadFromConfig(AppConfig config)
        {
            // Set complexity level
            CurrentLevel = config.ExplanationComplexity switch
            {
                "Beginner" => ComplexityLevel.Beginner,
                "Intermediate" => ComplexityLevel.Intermediate,
                "Advanced" => ComplexityLevel.Advanced,
                "Master" => ComplexityLevel.Master,
                _ => ComplexityLevel.Intermediate
            };

            // Set feature toggles
            Features.ShowTacticalAnalysis = config.ShowTacticalAnalysis;
            Features.ShowPositionalAnalysis = config.ShowPositionalAnalysis;
            Features.ShowEndgameAnalysis = config.ShowEndgameAnalysis;
            Features.ShowOpeningPrinciples = config.ShowOpeningPrinciples;
            Features.ShowWinPercentage = config.ShowWinPercentage;
            Features.ShowTablebaseInfo = config.ShowTablebaseInfo;
            Features.ShowMoveQualityColor = config.ShowMoveQualityColor;
            Features.ShowSEEValues = config.ShowSEEValues;
        }

        /// <summary>
        /// Simplify explanation based on complexity level
        /// </summary>
        public static string AdjustForComplexity(string fullExplanation, ComplexityLevel level)
        {
            if (string.IsNullOrEmpty(fullExplanation))
                return fullExplanation;

            switch (level)
            {
                case ComplexityLevel.Beginner:
                    return SimplifyForBeginner(fullExplanation);

                case ComplexityLevel.Intermediate:
                    return SimplifyForIntermediate(fullExplanation);

                case ComplexityLevel.Advanced:
                    return fullExplanation; // Keep all details

                case ComplexityLevel.Master:
                    return EnhanceForMaster(fullExplanation);

                default:
                    return fullExplanation;
            }
        }

        private static string SimplifyForBeginner(string explanation)
        {
            // Remove technical terms
            return explanation
                .Replace("SEE +", "wins ")
                .Replace("SEE -", "loses ")
                .Replace("(SEE ", "(")
                .Replace("singular move", "best move")
                .Replace("only good move", "best move")
                .Replace("knight on strong outpost", "knight in good position")
                .Replace("creates passed pawn", "advances pawn")
                .Replace("creates dangerous passed pawn", "dangerous pawn")
                .Replace("opposite-colored bishops (drawish)", "bishops make it hard to win")
                .Replace("zugzwang position", "forced to make bad move")
                .Replace("tablebase", "endgame theory")
                .Replace("sharp tactical position", "complicated position")
                .Replace("technical endgame", "careful endgame");
        }

        private static string SimplifyForIntermediate(string explanation)
        {
            // Keep most terms but simplify very technical ones
            return explanation
                .Replace("opposite-colored bishops (drawish)", "opposite-colored bishops")
                .Replace("zugzwang position (any move worsens position)", "zugzwang");
        }

        private static string EnhanceForMaster(string explanation)
        {
            // Could add more technical details if available
            // For now, just return as-is
            return explanation;
        }

        // =============================
        // COLOR CODING SYSTEM
        // Visual indicators for move quality
        // =============================

        public enum MoveQualityColor
        {
            Excellent,      // Brilliant move
            Good,           // Solid move
            Neutral,        // Acceptable
            Questionable,   // Dubious
            Bad,            // Mistake
            Blunder         // Serious error
        }

        /// <summary>
        /// Get color for move quality
        /// </summary>
        public static Color GetQualityColor(MoveQualityColor quality)
        {
            return quality switch
            {
                MoveQualityColor.Excellent => Color.FromArgb(34, 139, 34),      // Forest Green
                MoveQualityColor.Good => Color.FromArgb(60, 179, 113),          // Medium Sea Green
                MoveQualityColor.Neutral => Color.FromArgb(70, 130, 180),       // Steel Blue
                MoveQualityColor.Questionable => Color.FromArgb(255, 165, 0),   // Orange
                MoveQualityColor.Bad => Color.FromArgb(255, 69, 0),             // Orange Red
                MoveQualityColor.Blunder => Color.FromArgb(178, 34, 34),        // Firebrick Red
                _ => Color.Gray
            };
        }

        /// <summary>
        /// Determine move quality color from explanation text only (legacy method)
        /// </summary>
        public static MoveQualityColor DetermineQualityFromExplanation(string explanation)
        {
            if (string.IsNullOrEmpty(explanation))
                return MoveQualityColor.Neutral;

            string lower = explanation.ToLower();

            // Excellent indicators
            if (lower.Contains("only good move") || lower.Contains("singular move") ||
                lower.Contains("winning position") || lower.Contains("checkmate") ||
                lower.Contains("forced mate") || lower.Contains("brilliant"))
                return MoveQualityColor.Excellent;

            // Good indicators
            if (lower.Contains("good move") || lower.Contains("excellent") ||
                lower.Contains("strong") || lower.Contains("advantage") ||
                lower.Contains("clearly better") || lower.Contains("wins"))
                return MoveQualityColor.Good;

            // Questionable indicators
            if (lower.Contains("marginal") || lower.Contains("questionable") ||
                lower.Contains("loses exchange") || lower.Contains("dubious"))
                return MoveQualityColor.Questionable;

            // Bad indicators
            if (lower.Contains("poor move") || lower.Contains("mistake") ||
                lower.Contains("worsens position") || lower.Contains("bad"))
                return MoveQualityColor.Bad;

            // Blunder indicators
            if (lower.Contains("blunder") || lower.Contains("terrible") ||
                lower.Contains("loses") || lower.Contains("hanging"))
                return MoveQualityColor.Blunder;

            // Neutral (most moves)
            return MoveQualityColor.Neutral;
        }

        /// <summary>
        /// Determine move quality using BOTH evaluation score AND explanation keywords (recommended)
        /// </summary>
        public static MoveQualityColor DetermineQualityFromEvaluation(string explanation, string evaluation)
        {
            // Parse evaluation to get numeric value
            double? eval = MovesExplanation.ParseEvaluation(evaluation);

            if (!eval.HasValue)
            {
                // No evaluation available, fall back to text-only analysis
                return DetermineQualityFromExplanation(explanation);
            }

            double evalValue = eval.Value;
            MoveQualityColor qualityFromEval;

            // Handle mate scores specially
            if (evaluation.Contains("Mate"))
            {
                return MoveQualityColor.Excellent; // Forced mate is always excellent
            }

            // Determine base quality from evaluation score
            // Using absolute value and adjusting thresholds for clarity
            double absEval = Math.Abs(evalValue);

            if (absEval >= 3.0)
                qualityFromEval = MoveQualityColor.Excellent;  // +3.0 or better (clearly winning)
            else if (absEval >= 1.5)
                qualityFromEval = MoveQualityColor.Good;       // +1.5 to +2.9 (significant advantage)
            else if (absEval >= 0.5)
                qualityFromEval = MoveQualityColor.Neutral;    // +0.5 to +1.4 (slight edge)
            else if (absEval >= 0.0)
                qualityFromEval = MoveQualityColor.Neutral;    // 0.0 to +0.4 (balanced)
            else
                qualityFromEval = MoveQualityColor.Neutral;    // Should not reach here

            // Get quality from explanation text
            MoveQualityColor qualityFromText = DetermineQualityFromExplanation(explanation);

            // Combine both signals - if text strongly suggests different quality, adjust
            // Text can upgrade or downgrade by one level for nuance
            if (qualityFromText == MoveQualityColor.Blunder)
            {
                // Text says blunder - downgrade eval-based quality
                return MoveQualityColor.Blunder;
            }
            else if (qualityFromText == MoveQualityColor.Excellent && qualityFromEval == MoveQualityColor.Good)
            {
                // Text says excellent, eval says good - upgrade to excellent
                return MoveQualityColor.Excellent;
            }
            else if (qualityFromText == MoveQualityColor.Bad && qualityFromEval != MoveQualityColor.Excellent)
            {
                // Text says bad move - trust it unless eval is clearly winning
                return MoveQualityColor.Bad;
            }

            // Otherwise, trust the evaluation score
            return qualityFromEval;
        }

        /// <summary>
        /// Get emoji/symbol for move quality (visual indicator)
        /// </summary>
        public static string GetQualitySymbol(MoveQualityColor quality)
        {
            return quality switch
            {
                MoveQualityColor.Excellent => "!!", // Brilliant
                MoveQualityColor.Good => "!",       // Good
                MoveQualityColor.Neutral => "",     // No symbol
                MoveQualityColor.Questionable => "?!", // Dubious
                MoveQualityColor.Bad => "?",        // Mistake
                MoveQualityColor.Blunder => "??",   // Blunder
                _ => ""
            };
        }

        // =============================
        // WIN PERCENTAGE FORMATTING
        // User-friendly evaluation display
        // =============================

        /// <summary>
        /// Format evaluation with win percentage
        /// </summary>
        public static string FormatEvaluationWithWinRate(string evaluation, int materialCount)
        {
            double? eval = MovesExplanation.ParseEvaluation(evaluation);
            if (!eval.HasValue)
                return evaluation;

            // Get win percentage
            double winRate = AdvancedAnalysis.EvalToWinningPercentage(Math.Abs(eval.Value), materialCount);

            // Format based on evaluation type
            if (evaluation.Contains("Mate"))
            {
                return evaluation; // Keep mate scores as-is
            }

            // Show both eval and win%
            string side = eval.Value >= 0 ? "White" : "Black";
            return $"{evaluation} ({side}: {winRate:F0}% win chance)";
        }

        /// <summary>
        /// Get color for win percentage display
        /// </summary>
        public static Color GetWinRateColor(double winPercentage)
        {
            if (winPercentage >= 90)
                return Color.FromArgb(34, 139, 34);   // Dark green (winning)
            else if (winPercentage >= 70)
                return Color.FromArgb(60, 179, 113);  // Green (clearly better)
            else if (winPercentage >= 55)
                return Color.FromArgb(135, 206, 250); // Light blue (slight advantage)
            else if (winPercentage >= 45)
                return Color.Gray;                     // Gray (balanced)
            else if (winPercentage >= 30)
                return Color.FromArgb(255, 165, 0);   // Orange (disadvantage)
            else
                return Color.FromArgb(178, 34, 34);   // Red (losing)
        }

        // =============================
        // RICH TEXT FORMATTING
        // Apply colors and styles to RichTextBox
        // =============================

        /// <summary>
        /// Append formatted explanation to RichTextBox with color coding
        /// </summary>
        public static void AppendFormattedExplanation(RichTextBox richTextBox, string moveNotation,
            string explanation, string evaluation, int materialCount)
        {
            if (richTextBox == null || string.IsNullOrEmpty(explanation))
                return;

            // Determine move quality color
            MoveQualityColor quality = DetermineQualityFromExplanation(explanation);
            Color qualityColor = GetQualityColor(quality);
            string qualitySymbol = GetQualitySymbol(quality);

            // Adjust explanation for complexity level
            string adjustedExplanation = AdjustForComplexity(explanation, CurrentLevel);

            // Start formatting
            richTextBox.SelectionStart = richTextBox.TextLength;
            richTextBox.SelectionLength = 0;

            // Move notation in bold
            richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Bold);
            richTextBox.SelectionColor = Color.Black;
            richTextBox.AppendText($"  {moveNotation} ");

            // Quality symbol
            if (!string.IsNullOrEmpty(qualitySymbol))
            {
                richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Bold);
                richTextBox.SelectionColor = qualityColor;
                richTextBox.AppendText(qualitySymbol + " ");
            }

            // Explanation in color
            richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
            richTextBox.SelectionColor = qualityColor;
            richTextBox.AppendText(adjustedExplanation);

            // Evaluation with win rate (if intermediate or advanced)
            if (CurrentLevel >= ComplexityLevel.Intermediate && !string.IsNullOrEmpty(evaluation))
            {
                double? eval = MovesExplanation.ParseEvaluation(evaluation);
                if (eval.HasValue)
                {
                    double winRate = AdvancedAnalysis.EvalToWinningPercentage(Math.Abs(eval.Value), materialCount);
                    Color winRateColor = GetWinRateColor(winRate);

                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Italic);
                    richTextBox.SelectionColor = winRateColor;

                    string side = eval.Value >= 0 ? "White" : "Black";
                    richTextBox.AppendText($" ({side} {winRate:F0}%)");
                }
            }

            richTextBox.AppendText(Environment.NewLine);

            // Reset formatting
            richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
            richTextBox.SelectionColor = richTextBox.ForeColor;
        }

        // =============================
        // FEATURE TOGGLES
        // Allow users to enable/disable specific analysis features
        // =============================

        public class FeatureToggles
        {
            public bool ShowTacticalAnalysis { get; set; } = true;
            public bool ShowPositionalAnalysis { get; set; } = true;
            public bool ShowEndgameAnalysis { get; set; } = true;
            public bool ShowOpeningPrinciples { get; set; } = true;
            public bool ShowWinPercentage { get; set; } = true;
            public bool ShowTablebaseInfo { get; set; } = true;
            public bool ShowMoveQualityColor { get; set; } = true;
            public bool ShowSEEValues { get; set; } = true;

            /// <summary>
            /// Get description for each feature (for UI tooltips)
            /// </summary>
            public static string GetFeatureDescription(string featureName)
            {
                return featureName switch
                {
                    "ShowTacticalAnalysis" => "Detect pins, forks, skewers, and other tactical patterns",
                    "ShowPositionalAnalysis" => "Analyze pawn structure, piece activity, and king safety",
                    "ShowEndgameAnalysis" => "Identify endgame patterns and zugzwang positions",
                    "ShowOpeningPrinciples" => "Show opening principles for the first 20 moves",
                    "ShowWinPercentage" => "Display winning chances as percentages",
                    "ShowTablebaseInfo" => "Show perfect endgame moves from tablebase knowledge",
                    "ShowMoveQualityColor" => "Color-code moves by quality (green=good, red=bad)",
                    "ShowSEEValues" => "Show Static Exchange Evaluation for captures",
                    _ => ""
                };
            }
        }

        public static FeatureToggles Features { get; set; } = new FeatureToggles();

        // =============================
        // EXPLANATION CATEGORIES
        // Group explanations by type for filtering/display
        // =============================

        public enum ExplanationType
        {
            Tactical,       // Pins, forks, skewers
            Positional,     // Pawn structure, outposts
            Endgame,        // Endgame patterns
            Opening,        // Opening principles
            Strategic,      // Long-term plans
            Forced,         // Forced moves, checks
            Unknown         // Uncategorized
        }

        /// <summary>
        /// Categorize explanation by content
        /// </summary>
        public static ExplanationType CategorizeExplanation(string explanation)
        {
            if (string.IsNullOrEmpty(explanation))
                return ExplanationType.Unknown;

            string lower = explanation.ToLower();

            // Tactical
            if (lower.Contains("fork") || lower.Contains("pin") || lower.Contains("skewer") ||
                lower.Contains("discovered") || lower.Contains("sacrifice") || lower.Contains("threat"))
                return ExplanationType.Tactical;

            // Forced
            if (lower.Contains("check") || lower.Contains("forced") || lower.Contains("only") ||
                lower.Contains("mate"))
                return ExplanationType.Forced;

            // Endgame
            if (lower.Contains("endgame") || lower.Contains("tablebase") || lower.Contains("zugzwang") ||
                lower.Contains("pawn vs king") || lower.Contains("bare king"))
                return ExplanationType.Endgame;

            // Opening
            if (lower.Contains("opening") || lower.Contains("develops") || lower.Contains("controls center"))
                return ExplanationType.Opening;

            // Positional
            if (lower.Contains("pawn") || lower.Contains("outpost") || lower.Contains("mobility") ||
                lower.Contains("king safety") || lower.Contains("structure"))
                return ExplanationType.Positional;

            // Strategic
            if (lower.Contains("advantage") || lower.Contains("position") || lower.Contains("balance"))
                return ExplanationType.Strategic;

            return ExplanationType.Unknown;
        }
    }
}
