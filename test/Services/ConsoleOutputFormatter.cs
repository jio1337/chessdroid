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
        /// Displays a move line with evaluation, explanation, and threats
        /// </summary>
        public void DisplayMoveLine(
            string label,
            string sanMove,
            string evaluation,
            string completeFen,
            List<string> pvs,
            string firstMove,
            Color headerBackColor,
            Color defaultExplanationColor,
            bool showThreats = false)
        {
            // Display header with background color
            AppendTextWithFormat($"{label}: {sanMove} {evaluation}{Environment.NewLine}",
                headerBackColor, Color.Black, FontStyle.Regular);

            // Generate and display explanation
            string explanation = generateExplanation(firstMove, completeFen, pvs, evaluation);

            // Get threats for this specific move if enabled
            // Skip threats/defenses when WE have forced mate - they don't matter
            // "Mate in X" = we have mate, "Mate in -X" = opponent has mate (still show analysis)
            string threatsText = "";
            string defensesText = "";
            bool weHaveForcedMate = !string.IsNullOrEmpty(evaluation) &&
                                     evaluation.StartsWith("Mate in ") &&
                                     !evaluation.Contains("-");

            if (showThreats && config?.ShowThreats == true && !weHaveForcedMate)
            {
                threatsText = GetThreatsForMove(completeFen, firstMove);
                defensesText = GetDefensesForMove(completeFen, firstMove);
            }

            if (!string.IsNullOrEmpty(explanation) || !string.IsNullOrEmpty(threatsText) || !string.IsNullOrEmpty(defensesText))
            {
                ResetBackground();

                // Determine quality and color if move quality coloring is enabled
                Color explanationColor = defaultExplanationColor;
                string qualitySymbol = "";

                if (config?.ShowMoveQualityColor == true && !string.IsNullOrEmpty(explanation))
                {
                    var quality = ExplanationFormatter.DetermineQualityFromEvaluation(explanation, evaluation);
                    explanationColor = ExplanationFormatter.GetQualityColor(quality);
                    qualitySymbol = ExplanationFormatter.GetQualitySymbol(quality);

                    // Add space after symbol if present
                    if (!string.IsNullOrEmpty(qualitySymbol))
                        qualitySymbol = qualitySymbol + " ";
                }

                // Remove redundant phrases from explanation
                string cleanedExplanation = explanation;

                // Remove redundant check-related phrases if they're in threats
                if (!string.IsNullOrEmpty(threatsText))
                {
                    bool threatsHasCheck = threatsText.Contains("check", StringComparison.OrdinalIgnoreCase) ||
                                           threatsText.Contains("checkmate", StringComparison.OrdinalIgnoreCase);
                    if (threatsHasCheck)
                    {
                        cleanedExplanation = RemoveRedundantCheckPhrases(cleanedExplanation);
                    }
                }

                // Remove redundant passed pawn phrases (always check, internal redundancy)
                cleanedExplanation = RemoveRedundantPassedPawnPhrases(cleanedExplanation);

                // Build the explanation line
                if (!string.IsNullOrEmpty(cleanedExplanation))
                {
                    AppendTextWithFormat($"  → {qualitySymbol}{cleanedExplanation}",
                        richTextBox.BackColor, explanationColor, FontStyle.Italic);

                    // Add defenses on same line if present (before threats)
                    if (!string.IsNullOrEmpty(defensesText))
                    {
                        AppendTextWithFormat(" | ", richTextBox.BackColor, Color.Gray, FontStyle.Regular);
                        AppendTextWithFormat($"🛡 {defensesText}", richTextBox.BackColor, Color.CornflowerBlue, FontStyle.Regular);
                    }

                    // Add threats on same line if present
                    if (!string.IsNullOrEmpty(threatsText))
                    {
                        AppendTextWithFormat(" | ", richTextBox.BackColor, Color.Gray, FontStyle.Regular);
                        AppendTextWithFormat($"⚔ {threatsText}", richTextBox.BackColor, Color.LimeGreen, FontStyle.Regular);
                    }

                    AppendTextWithFormat(Environment.NewLine, richTextBox.BackColor, explanationColor, FontStyle.Regular);
                }
                else if (!string.IsNullOrEmpty(defensesText) || !string.IsNullOrEmpty(threatsText))
                {
                    // No explanation but we have defenses/threats
                    AppendTextWithFormat($"  → ", richTextBox.BackColor, explanationColor, FontStyle.Italic);

                    if (!string.IsNullOrEmpty(defensesText))
                    {
                        AppendTextWithFormat($"🛡 {defensesText}", richTextBox.BackColor, Color.CornflowerBlue, FontStyle.Regular);
                        if (!string.IsNullOrEmpty(threatsText))
                        {
                            AppendTextWithFormat(" | ", richTextBox.BackColor, Color.Gray, FontStyle.Regular);
                        }
                    }

                    if (!string.IsNullOrEmpty(threatsText))
                    {
                        AppendTextWithFormat($"⚔ {threatsText}", richTextBox.BackColor, Color.LimeGreen, FontStyle.Regular);
                    }

                    AppendTextWithFormat(Environment.NewLine, richTextBox.BackColor, explanationColor, FontStyle.Regular);
                }
            }

            ResetFormatting();
        }

        /// <summary>
        /// Gets threat descriptions for a specific move
        /// </summary>
        private string GetThreatsForMove(string completeFen, string move)
        {
            try
            {
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                string enPassantSquare = fenParts.Length > 3 ? fenParts[3] : "-";

                ChessBoard board = ChessBoard.FromFEN(completeFen);
                var threats = ThreatDetection.AnalyzeThreatsAfterMove(board, move, whiteToMove, enPassantSquare);

                if (threats.Count > 0)
                {
                    return string.Join(", ", threats.Select(t => t.Description));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting threats for move: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Gets defense descriptions for a specific move
        /// </summary>
        private string GetDefensesForMove(string completeFen, string move)
        {
            try
            {
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                ChessBoard board = ChessBoard.FromFEN(completeFen);
                var defenses = DefenseDetection.AnalyzeDefensesAfterMove(board, move, whiteToMove);

                if (defenses.Count > 0)
                {
                    return string.Join(", ", defenses.Select(d => d.Description));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting defenses for move: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Formats evaluation string (win percentage removed - WDL replaces it)
        /// </summary>
        public string FormatEvaluation(string evaluation)
        {
            // Format mate scores to be more readable
            if (!string.IsNullOrEmpty(evaluation) && evaluation.StartsWith("Mate in "))
            {
                string mateStr = evaluation.Replace("Mate in ", "").Trim();
                if (int.TryParse(mateStr, out int mateIn))
                {
                    if (mateIn > 0)
                        return $"Mate in {mateIn}"; // White mates
                    else if (mateIn < 0)
                        return $"Mate in {mateIn}"; // Black mates (keep negative)
                }
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
        /// Removes redundant check-related phrases from explanation when they're already in threats
        /// </summary>
        private static string RemoveRedundantCheckPhrases(string explanation)
        {
            if (string.IsNullOrEmpty(explanation))
                return explanation;

            // Phrases to remove (they'll be covered by the threats section)
            string[] redundantPhrases = new[]
            {
                "check with attack",
                "gives check",
                "creates threat on king",
                ", check",
                "check, "
            };

            string result = explanation;
            foreach (var phrase in redundantPhrases)
            {
                result = result.Replace(phrase, "", StringComparison.OrdinalIgnoreCase);
            }

            // Clean up any resulting artifacts (double commas, trailing commas, etc.)
            result = CleanupExplanationArtifacts(result);

            return result;
        }

        /// <summary>
        /// Removes redundant passed pawn phrases (keeps the more specific one)
        /// </summary>
        private static string RemoveRedundantPassedPawnPhrases(string explanation)
        {
            if (string.IsNullOrEmpty(explanation))
                return explanation;

            // If we have both "advances passed pawn" and "creates (dangerous) passed pawn", keep only one
            bool hasAdvances = explanation.Contains("advances passed pawn", StringComparison.OrdinalIgnoreCase);
            bool hasCreates = explanation.Contains("creates dangerous passed pawn", StringComparison.OrdinalIgnoreCase) ||
                              explanation.Contains("creates passed pawn", StringComparison.OrdinalIgnoreCase);

            if (hasAdvances && hasCreates)
            {
                // Keep "creates dangerous passed pawn" as it's more specific, remove "advances passed pawn"
                string result = explanation.Replace("advances passed pawn", "", StringComparison.OrdinalIgnoreCase);
                return CleanupExplanationArtifacts(result);
            }

            return explanation;
        }

        /// <summary>
        /// Cleans up artifacts from phrase removal (double commas, etc.)
        /// </summary>
        private static string CleanupExplanationArtifacts(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = text;
            result = System.Text.RegularExpressions.Regex.Replace(result, @",\s*,", ",");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"^\s*,\s*", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @",\s*$", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\|\s*\|", "|");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"^\s*\|\s*", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s*\|\s*$", "");
            result = result.Trim();

            return result;
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
        /// Displays WDL (Win/Draw/Loss) information with sharpness indicator
        /// Inspired by Leela Chess Zero's evaluation model
        /// </summary>
        private void DisplayWDLInfo(WDLInfo wdl, bool whiteToMove)
        {
            // WDL values are always from White's perspective
            // When Black is to move, swap W and L to show from Black's perspective
            double winPercent = whiteToMove ? wdl.WinPercent : wdl.LossPercent;
            double lossPercent = whiteToMove ? wdl.LossPercent : wdl.WinPercent;
            double drawPercent = wdl.DrawPercent;

            // WDL header
            richTextBox.SelectionColor = Color.Gray;
            richTextBox.AppendText("Position: ");

            // Win percentage - green shade based on value
            Color winColor = winPercent > 60 ? Color.LimeGreen :
                            winPercent > 40 ? Color.MediumSeaGreen : Color.DarkSeaGreen;
            richTextBox.SelectionColor = winColor;
            richTextBox.AppendText($"W:{winPercent:F0}% ");

            // Draw percentage - neutral color
            richTextBox.SelectionColor = Color.Gold;
            richTextBox.AppendText($"D:{drawPercent:F0}% ");

            // Loss percentage - red shade based on value
            Color lossColor = lossPercent > 60 ? Color.Crimson :
                             lossPercent > 40 ? Color.IndianRed : Color.RosyBrown;
            richTextBox.SelectionColor = lossColor;
            richTextBox.AppendText($"L:{lossPercent:F0}% ");

            // Sharpness indicator (same from both perspectives)
            string character = wdl.GetPositionCharacter();
            Color sharpnessColor = WDLUtilities.GetSharpnessColor(wdl.Sharpness);
            richTextBox.SelectionColor = sharpnessColor;
            richTextBox.AppendText($"({character})");

            richTextBox.AppendText(Environment.NewLine);
            ResetFormatting();
        }

        /// <summary>
        /// Displays ABK opening book moves
        /// </summary>
        public void DisplayBookMoves(List<BookMove> bookMoves)
        {
            if (bookMoves == null || bookMoves.Count == 0)
                return;

            richTextBox.SelectionColor = Color.Khaki;
            richTextBox.AppendText($"📖 Book moves: ");

            for (int i = 0; i < Math.Min(bookMoves.Count, 5); i++)
            {
                var move = bookMoves[i];
                if (i > 0)
                {
                    richTextBox.SelectionColor = Color.Gray;
                    richTextBox.AppendText(", ");
                }

                // Move with win rate
                richTextBox.SelectionColor = Color.PaleGreen;
                richTextBox.AppendText($"{move.UciMove}");

                richTextBox.SelectionColor = Color.Gray;
                richTextBox.AppendText($"({move.WinRate:F0}%/{move.Games})");
            }

            richTextBox.AppendText(Environment.NewLine);
            ResetFormatting();
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
            bool showThirdLine,
            WDLInfo? wdl = null,
            List<BookMove>? bookMoves = null)
        {
            // Validate required parameter - store in local var for null-state tracking
            string fen = completeFen ?? throw new ArgumentNullException(nameof(completeFen));

            Clear();

            // Check for blunders
            double? currentEval = MovesExplanation.ParseEvaluation(evaluation);
            if (currentEval.HasValue && previousEvaluation.HasValue)
            {
                // Extract whose turn it is from FEN to determine who just moved
                string[] fenParts = fen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                bool whiteJustMoved = !whiteToMove;

                var (isBlunder, blunderType, evalDrop, whiteBlundered) = MovesExplanation.DetectBlunder(
                    currentEval, previousEvaluation, whiteJustMoved);

                if (isBlunder)
                {
                    DisplayBlunderWarning(blunderType, evalDrop, whiteBlundered);
                }
            }

            // Extract whose turn it is from FEN for WDL display
            string[] fenPartsForWdl = fen.Split(' ');
            bool whiteToMoveForWdl = fenPartsForWdl.Length > 1 && fenPartsForWdl[1] == "w";

            // Display WDL information if available and enabled
            if (config?.ShowWDL == true)
            {
                if (wdl != null)
                {
                    System.Diagnostics.Debug.WriteLine($"WDL data received: W={wdl.Win} D={wdl.Draw} L={wdl.Loss}");
                    DisplayWDLInfo(wdl, whiteToMoveForWdl);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("WDL: No data available from engine");
                }
            }

            // Display opening name if in known theory and enabled
            if (config?.ShowOpeningName == true)
            {
                string openingDisplay = OpeningBook.GetOpeningDisplay(completeFen);
                System.Diagnostics.Debug.WriteLine($"OpeningBook lookup for FEN: {completeFen?.Split(' ')[0]} => {openingDisplay}");

                if (!string.IsNullOrEmpty(openingDisplay) && openingDisplay != "Starting Position")
                {
                    richTextBox.SelectionColor = Color.CornflowerBlue;
                    richTextBox.AppendText($"Opening: {openingDisplay}{Environment.NewLine}");
                    ResetFormatting();
                }
                else
                {
                    // Show "Out of book" when we're past known theory
                    richTextBox.SelectionColor = Color.Gray;
                    richTextBox.AppendText($"Opening: Out of book{Environment.NewLine}");
                    ResetFormatting();
                }
            }

            // Display ABK book moves if available and enabled
            if (config?.ShowBookMoves == true && bookMoves != null && bookMoves.Count > 0)
            {
                DisplayBookMoves(bookMoves);
            }

            // Display play style indicator based on aggressiveness setting
            if (config != null)
            {
                string playStyle = config.Aggressiveness switch
                {
                    <= 20 => "Very Solid",
                    <= 40 => "Solid",
                    <= 60 => "Balanced",
                    <= 80 => "Aggressive",
                    _ => "Very Aggressive"
                };

                Color styleColor = config.Aggressiveness switch
                {
                    <= 20 => Color.SteelBlue,
                    <= 40 => Color.CadetBlue,
                    <= 60 => Color.Gold,
                    <= 80 => Color.OrangeRed,
                    _ => Color.Crimson
                };

                richTextBox.SelectionColor = styleColor;
                richTextBox.AppendText($"Style: {playStyle} ({config.Aggressiveness}){Environment.NewLine}");
                ResetFormatting();
            }

            // Best line - always show threats
            string bestSanFull = ConvertPvToSan(pvs, 0, bestMove, fen);
            string formattedEval = FormatEvaluation(evaluation);
            // Use pvs[0] not bestMove - after Multi-PV sorting, bestMove may not match pvs[0]
            string firstMove = pvs.Count > 0 ? pvs[0].Split(' ')[0] : bestMove;
            DisplayMoveLine(
                "Best line",
                bestSanFull,
                formattedEval,
                fen,
                pvs,
                firstMove,
                Color.MediumSeaGreen,
                Color.PaleGreen,
                showThreats: true);

            // Second best - show threats if enabled
            if (showSecondLine && pvs.Count >= 2)
            {
                var secondSan = ChessNotationService.ConvertFullPvToSan(pvs[1], fen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string secondMove = pvs[1].Split(' ')[0];
                string secondEval = evaluations.Count >= 2 ? evaluations[1] : "";
                string formattedSecondEval = FormatEvaluation(secondEval);

                DisplayMoveLine(
                    "Second best",
                    secondSan,
                    formattedSecondEval,
                    fen,
                    pvs,
                    secondMove,
                    Color.Yellow,
                    Color.DarkGoldenrod,
                    showThreats: true);
            }

            // Third best - show threats if enabled
            if (showThirdLine && pvs.Count >= 3)
            {
                var thirdSan = ChessNotationService.ConvertFullPvToSan(pvs[2], fen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string thirdMove = pvs[2].Split(' ')[0];
                string thirdEval = evaluations.Count >= 3 ? evaluations[2] : "";

                DisplayMoveLine(
                    "Third best",
                    thirdSan,
                    thirdEval,
                    fen,
                    pvs,
                    thirdMove,
                    Color.Red,
                    Color.DarkRed,
                    showThreats: true);
            }

            // Display opponent threats at the bottom (independent of which move we choose)
            if (config?.ShowThreats == true)
            {
                DisplayOpponentThreats(fen);
            }

            ResetFormatting();
        }

        /// <summary>
        /// Displays opponent threats (shown at the bottom, independent of our move choice)
        /// </summary>
        private void DisplayOpponentThreats(string completeFen)
        {
            try
            {
                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                string enPassantSquare = fenParts.Length > 3 ? fenParts[3] : "-";

                ChessBoard board = ChessBoard.FromFEN(completeFen);
                var opponentThreats = ThreatDetection.AnalyzeOpponentThreats(board, whiteToMove, enPassantSquare);

                if (opponentThreats.Count > 0)
                {
                    richTextBox.AppendText(Environment.NewLine);
                    AppendTextWithFormat("⚠ Opponent threats: ", richTextBox.BackColor, Color.Orange, FontStyle.Bold);
                    string oppThreatText = string.Join(", ", opponentThreats.Select(t => t.Description));
                    AppendTextWithFormat($"{oppThreatText}{Environment.NewLine}", richTextBox.BackColor, Color.Coral, FontStyle.Regular);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying opponent threats: {ex.Message}");
            }
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