using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
    }
}
