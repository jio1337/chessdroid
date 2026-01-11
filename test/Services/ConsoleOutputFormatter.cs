using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Handles formatting and display of chess analysis output to RichTextBox
    /// Extracted from MainForm to centralize console output formatting
    /// </summary>
    public class ConsoleOutputFormatter
    {
        private readonly RichTextBox richTextBox;
        private readonly AppConfig config;
        private readonly Func<string, string, List<string>, string, string> generateExplanation;

        public ConsoleOutputFormatter(
            RichTextBox richTextBox,
            AppConfig config,
            Func<string, string, List<string>, string, string> generateExplanation)
        {
            this.richTextBox = richTextBox;
            this.config = config;
            this.generateExplanation = generateExplanation;
        }

        /// <summary>
        /// Displays blunder warning with colored background
        /// </summary>
        public void DisplayBlunderWarning(string blunderType, double evalDrop, bool whiteBlundered)
        {
            AppendTextWithFormat($"⚠ {blunderType}! Eval swing: {evalDrop:F2} pawns{Environment.NewLine}",
                Color.Orange, Color.Black, FontStyle.Bold);

            // Determine who blundered
            if (whiteBlundered)
            {
                richTextBox.AppendText($"White just blundered and gave Black a big opportunity!{Environment.NewLine}");
            }
            else
            {
                richTextBox.AppendText($"Black just blundered and gave White a big opportunity!{Environment.NewLine}");
            }

            ResetFormatting();
            richTextBox.AppendText(Environment.NewLine);
        }

        /// <summary>
        /// Displays a move line with evaluation and optional explanation
        /// </summary>
        public void DisplayMoveLine(
            string label,
            string sanMove,
            string evaluation,
            string completeFen,
            List<string> pvs,
            string firstMove,
            Color headerBackColor,
            Color defaultExplanationColor)
        {
            // Display header with background color
            AppendTextWithFormat($"{label}: {sanMove} {evaluation}{Environment.NewLine}",
                headerBackColor, Color.Black, FontStyle.Regular);

            // Generate and display explanation
            string explanation = generateExplanation(firstMove, completeFen, pvs, evaluation);
            if (!string.IsNullOrEmpty(explanation))
            {
                ResetBackground();

                // Determine quality and color if move quality coloring is enabled
                Color explanationColor = defaultExplanationColor;
                string qualitySymbol = "";

                if (config?.ShowMoveQualityColor == true)
                {
                    var quality = ExplanationFormatter.DetermineQualityFromEvaluation(explanation, evaluation);
                    explanationColor = ExplanationFormatter.GetQualityColor(quality);
                    qualitySymbol = ExplanationFormatter.GetQualitySymbol(quality);

                    // Add space after symbol if present
                    if (!string.IsNullOrEmpty(qualitySymbol))
                        qualitySymbol = qualitySymbol + " ";
                }

                AppendTextWithFormat($"  → {qualitySymbol}{explanation}{Environment.NewLine}",
                    richTextBox.BackColor, explanationColor, FontStyle.Italic);
            }

            ResetFormatting();
        }

        /// <summary>
        /// Formats evaluation with win percentage if enabled
        /// </summary>
        public string FormatEvaluationWithWinPercentage(string evaluation, string completeFen)
        {
            if (config?.ShowWinPercentage == true &&
                ExplanationFormatter.CurrentLevel >= ExplanationFormatter.ComplexityLevel.Intermediate)
            {
                var tempBoard = ChessBoard.FromFEN(completeFen);
                int materialCount = EndgameAnalysis.CountTotalPieces(tempBoard);
                return ExplanationFormatter.FormatEvaluationWithWinRate(evaluation, materialCount, completeFen);
            }
            return evaluation;
        }

        /// <summary>
        /// Appends text with specific formatting
        /// </summary>
        private void AppendTextWithFormat(string text, Color backColor, Color foreColor, FontStyle fontStyle)
        {
            richTextBox.SelectionBackColor = backColor;
            richTextBox.SelectionColor = foreColor;
            richTextBox.SelectionFont = new Font(richTextBox.Font, fontStyle);
            richTextBox.AppendText(text);
            richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
        }

        /// <summary>
        /// Resets background to default
        /// </summary>
        private void ResetBackground()
        {
            richTextBox.SelectionBackColor = richTextBox.BackColor;
        }

        /// <summary>
        /// Resets all formatting to default
        /// </summary>
        public void ResetFormatting()
        {
            richTextBox.SelectionBackColor = richTextBox.BackColor;
            richTextBox.SelectionColor = richTextBox.ForeColor;
            richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Regular);
        }

        /// <summary>
        /// Clears the console
        /// </summary>
        public void Clear()
        {
            richTextBox.Clear();
        }

        /// <summary>
        /// Displays complete analysis results including blunder detection and multiple lines
        /// </summary>
        public void DisplayAnalysisResults(
            string bestMove,
            string evaluation,
            List<string> pvs,
            List<string> evaluations,
            string completeFen,
            double? previousEvaluation,
            bool showSecondLine,
            bool showThirdLine)
        {
            Clear();

            // Check for blunders
            double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
            if (currentEval.HasValue && previousEvaluation.HasValue)
            {
                // Extract whose turn it is from FEN to determine who just moved
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                bool whiteJustMoved = !whiteToMove;

                var (isBlunder, blunderType, evalDrop, whiteBlundered) = MovesExplanation.DetectBlunder(
                    currentEval, previousEvaluation, whiteJustMoved);

                if (isBlunder)
                {
                    DisplayBlunderWarning(blunderType, evalDrop, whiteBlundered);
                }
            }

            // Best line
            string bestSanFull = ConvertPvToSan(pvs, 0, bestMove, completeFen);
            string formattedEval = FormatEvaluationWithWinPercentage(evaluation, completeFen);
            DisplayMoveLine(
                "Best line",
                bestSanFull,
                formattedEval,
                completeFen,
                pvs,
                bestMove,
                Color.MediumSeaGreen,
                Color.PaleGreen);

            // Second best
            if (showSecondLine && pvs.Count >= 2)
            {
                var secondSan = ChessNotationService.ConvertFullPvToSan(pvs[1], completeFen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string secondMove = pvs[1].Split(' ')[0];
                string secondEval = evaluations.Count >= 2 ? evaluations[1] : "";
                string formattedSecondEval = FormatEvaluationWithWinPercentage(secondEval, completeFen);

                DisplayMoveLine(
                    "Second best",
                    secondSan,
                    formattedSecondEval,
                    completeFen,
                    pvs,
                    secondMove,
                    Color.Yellow,
                    Color.DarkGoldenrod);
            }

            // Third best
            if (showThirdLine && pvs.Count >= 3)
            {
                var thirdSan = ChessNotationService.ConvertFullPvToSan(pvs[2], completeFen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string thirdMove = pvs[2].Split(' ')[0];
                string thirdEval = evaluations.Count >= 3 ? evaluations[2] : "";

                DisplayMoveLine(
                    "Third best",
                    thirdSan,
                    thirdEval,
                    completeFen,
                    pvs,
                    thirdMove,
                    Color.Red,
                    Color.DarkRed);
            }

            ResetFormatting();
        }

        /// <summary>
        /// Converts PV to SAN notation with fallback
        /// </summary>
        private static string ConvertPvToSan(List<string> pvs, int index, string fallbackMove, string completeFen)
        {
            if (pvs != null && pvs.Count > index && !string.IsNullOrWhiteSpace(pvs[index]))
            {
                return ChessNotationService.ConvertFullPvToSan(pvs[index], completeFen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
            }
            return ChessNotationService.ConvertFullPvToSan(fallbackMove, completeFen,
                ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
        }
    }
}