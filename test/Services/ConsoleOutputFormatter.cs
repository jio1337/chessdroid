using ChessDroid.Models;
using System.Linq;

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
            Color headerForeColor,
            Color defaultExplanationColor,
            bool showThreats = false,
            bool isOnlyWinningMove = false,
            (string label, string symbol, Color color)? classification = null,
            string? overrideExplanation = null)
        {
            // Display header with foreground color only (no background highlight)
            // Include classification symbol if provided (e.g., "??" for Blunder)
            string classificationSuffix = "";
            if (classification.HasValue && !string.IsNullOrEmpty(classification.Value.symbol))
            {
                classificationSuffix = $" {classification.Value.symbol}";
            }
            AppendTextWithFormat($"{label}: {sanMove} {evaluation}{classificationSuffix}{Environment.NewLine}",
                richTextBox.BackColor, headerForeColor, FontStyle.Bold);

            // Generate and display explanation
            // Use override explanation (e.g., for Brilliant moves) if provided, otherwise generate normally
            string explanation = !string.IsNullOrEmpty(overrideExplanation)
                ? overrideExplanation
                : generateExplanation(firstMove, completeFen, pvs, evaluation);

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

                // Use default explanation color (new v2.5 classification handles quality indicators)
                Color explanationColor = defaultExplanationColor;

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
                    // Add "only winning move" indicator if applicable
                    string criticalPrefix = "";
                    if (isOnlyWinningMove)
                    {
                        criticalPrefix = "⚡ only winning move, ";
                    }

                    // Add classification label if provided (e.g., "BLUNDER" for second/third best when only winning move)
                    if (classification.HasValue && !string.IsNullOrEmpty(classification.Value.label))
                    {
                        AppendTextWithFormat($"  → ", richTextBox.BackColor, explanationColor, FontStyle.Italic);
                        AppendTextWithFormat($"{classification.Value.label.ToUpper()} ", richTextBox.BackColor, classification.Value.color, FontStyle.Bold);
                        AppendTextWithFormat($"{cleanedExplanation}", richTextBox.BackColor, explanationColor, FontStyle.Italic);
                    }
                    else
                    {
                        AppendTextWithFormat($"  → {criticalPrefix}{cleanedExplanation}",
                            richTextBox.BackColor, explanationColor, FontStyle.Italic);
                    }

                    // Add defenses on same line if present (before threats)
                    if (!string.IsNullOrEmpty(defensesText))
                    {
                        AppendTextWithFormat(" | ", richTextBox.BackColor, GetThemeColor(Color.Gray, Color.DarkSlateGray), FontStyle.Regular);
                        AppendTextWithFormat($"🛡 {defensesText}", richTextBox.BackColor, GetThemeColor(Color.CornflowerBlue, Color.MidnightBlue), FontStyle.Regular);
                    }

                    // Add threats on same line if present
                    if (!string.IsNullOrEmpty(threatsText))
                    {
                        AppendTextWithFormat(" | ", richTextBox.BackColor, GetThemeColor(Color.Gray, Color.DarkSlateGray), FontStyle.Regular);
                        AppendTextWithFormat($"⚔ {threatsText}", richTextBox.BackColor, GetThemeColor(Color.LimeGreen, Color.DarkGreen), FontStyle.Regular);
                    }

                    AppendTextWithFormat(Environment.NewLine, richTextBox.BackColor, explanationColor, FontStyle.Regular);
                }
                else if (!string.IsNullOrEmpty(defensesText) || !string.IsNullOrEmpty(threatsText) || isOnlyWinningMove || classification.HasValue)
                {
                    // No explanation but we have defenses/threats, only winning move, or classification
                    AppendTextWithFormat($"  → ", richTextBox.BackColor, explanationColor, FontStyle.Italic);

                    // Show classification label if provided (e.g., "BLUNDER" for second/third best)
                    if (classification.HasValue && !string.IsNullOrEmpty(classification.Value.label))
                    {
                        AppendTextWithFormat($"{classification.Value.label.ToUpper()}", richTextBox.BackColor, classification.Value.color, FontStyle.Bold);
                        if (!string.IsNullOrEmpty(defensesText) || !string.IsNullOrEmpty(threatsText))
                        {
                            AppendTextWithFormat(" | ", richTextBox.BackColor, GetThemeColor(Color.Gray, Color.DarkSlateGray), FontStyle.Regular);
                        }
                    }
                    // Show "only winning move" first if applicable
                    else if (isOnlyWinningMove)
                    {
                        AppendTextWithFormat($"⚡ only winning move", richTextBox.BackColor, explanationColor, FontStyle.Italic);
                        if (!string.IsNullOrEmpty(defensesText) || !string.IsNullOrEmpty(threatsText))
                        {
                            AppendTextWithFormat(" | ", richTextBox.BackColor, GetThemeColor(Color.Gray, Color.DarkSlateGray), FontStyle.Regular);
                        }
                    }

                    if (!string.IsNullOrEmpty(defensesText))
                    {
                        AppendTextWithFormat($"🛡 {defensesText}", richTextBox.BackColor, GetThemeColor(Color.CornflowerBlue, Color.MidnightBlue), FontStyle.Regular);
                        if (!string.IsNullOrEmpty(threatsText))
                        {
                            AppendTextWithFormat(" | ", richTextBox.BackColor, GetThemeColor(Color.Gray, Color.DarkSlateGray), FontStyle.Regular);
                        }
                    }

                    if (!string.IsNullOrEmpty(threatsText))
                    {
                        AppendTextWithFormat($"⚔ {threatsText}", richTextBox.BackColor, GetThemeColor(Color.LimeGreen, Color.DarkGreen), FontStyle.Regular);
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
        /// Gets theme-aware color (darker colors for light mode, lighter for dark mode)
        /// </summary>
        private Color GetThemeColor(Color darkModeColor, Color lightModeColor)
        {
            bool isDarkMode = config?.Theme == "Dark";
            return isDarkMode ? darkModeColor : lightModeColor;
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
        /// Calculate win probability from centipawn evaluation using logistic function
        /// Standard formula used by Chess.com, Lichess, and most analysis tools
        /// </summary>
        /// <param name="evalInPawns">Evaluation in pawns (e.g., +0.50 for half pawn advantage)</param>
        /// <returns>Win probability from 0.0 to 1.0</returns>
        private static double CalculateWinProbability(double evalInPawns)
        {
            // Logistic function: Win% = 1 / (1 + 10^(-eval/4))
            // This maps eval to a 0-1 probability scale
            return 1.0 / (1.0 + Math.Pow(10, -evalInPawns / 4.0));
        }

        /// <summary>
        /// Check if an enemy pawn can capture on the given square
        /// </summary>
        private static bool CanEnemyPawnCapture(ChessBoard board, int row, int col, bool weAreWhite)
        {
            // Enemy pawns capture diagonally
            // If we are White, enemy is Black - black pawns move DOWN (row increases) and capture diagonally
            // If we are Black, enemy is White - white pawns move UP (row decreases) and capture diagonally

            if (weAreWhite)
            {
                // Black pawns capture from row-1 (above) to row
                // Check squares at (row-1, col-1) and (row-1, col+1) for black pawns
                if (row > 0)
                {
                    if (col > 0 && board.GetPiece(row - 1, col - 1) == 'p') return true;
                    if (col < 7 && board.GetPiece(row - 1, col + 1) == 'p') return true;
                }
            }
            else
            {
                // White pawns capture from row+1 (below) to row
                // Check squares at (row+1, col-1) and (row+1, col+1) for white pawns
                if (row < 7)
                {
                    if (col > 0 && board.GetPiece(row + 1, col - 1) == 'P') return true;
                    if (col < 7 && board.GetPiece(row + 1, col + 1) == 'P') return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a move is a Brilliant move (Chess.com criteria)
        /// Brilliant = piece sacrifice where you're not in a bad position after and weren't already winning
        /// Supports both CAPTURE sacrifices (Nxf7 where knight is lost) and IMPLICIT sacrifices
        /// (Qh5 leaving a bishop hanging, Ng6+ leaving knight en prise)
        /// </summary>
        /// <param name="fen">Position before the move</param>
        /// <param name="uciMove">The move in UCI notation (e.g., "e2e4")</param>
        /// <param name="evalAfter">Evaluation after the move (in pawns)</param>
        /// <param name="evalBefore">Evaluation before the move (in pawns), can be null</param>
        /// <returns>Tuple of (isBrilliant, explanation) - explanation describes the sacrifice</returns>
        internal static (bool isBrilliant, string? explanation) IsBrilliantMove(string fen, string uciMove, double evalAfter, double? evalBefore)
        {
            try
            {
                // Parse FEN to get board and whose turn
                string[] fenParts = fen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                // Condition: Wasn't already completely winning (decisive advantage = > 1.50)
                // If we were already +2.0 or better, it's not brilliant - we were winning anyway
                if (evalBefore.HasValue)
                {
                    if (whiteToMove && evalBefore.Value >= 2.0) return (false, null);
                    if (!whiteToMove && evalBefore.Value <= -2.0) return (false, null);
                }

                // Condition: Not in a bad position after the move
                // "Bad position" = clearly losing (< -0.70 for White, > +0.70 for Black)
                if (whiteToMove && evalAfter < -0.70) return (false, null);
                if (!whiteToMove && evalAfter > 0.70) return (false, null);

                // Parse the move
                if (uciMove.Length < 4) return (false, null);

                int srcCol = uciMove[0] - 'a';
                int srcRow = 8 - (uciMove[1] - '0');
                int destCol = uciMove[2] - 'a';
                int destRow = 8 - (uciMove[3] - '0');

                ChessBoard board = ChessBoard.FromFEN(fen);
                char movingPiece = board.GetPiece(srcRow, srcCol);
                char targetPiece = board.GetPiece(destRow, destCol);

                // Check for CAPTURE sacrifice (piece takes lesser piece and gets recaptured)
                if (targetPiece != '.')
                {
                    return CheckCaptureSacrifice(board, movingPiece, targetPiece,
                        srcRow, srcCol, destRow, destCol, whiteToMove, evalAfter);
                }

                // Check for IMPLICIT sacrifice (non-capture that leaves pieces hanging)
                // Examples: Qh5 leaving bishop hanging, Ng6+ leaving knight en prise
                return CheckImplicitSacrifice(board, movingPiece,
                    srcRow, srcCol, destRow, destCol, whiteToMove, evalAfter);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Check for capture sacrifice: piece captures lesser piece and gets recaptured
        /// Example: Nxf7 (knight takes pawn, knight gets captured)
        /// </summary>
        private static (bool isBrilliant, string? explanation) CheckCaptureSacrifice(
            ChessBoard board, char movingPiece, char targetPiece,
            int srcRow, int srcCol, int destRow, int destCol,
            bool whiteToMove, double evalAfter)
        {
            // The SACRIFICED piece must be a minor piece or better (value >= 3)
            PieceType movingType = PieceHelper.GetPieceType(movingPiece);
            int movingValue = ChessUtilities.GetPieceValue(movingType);

            // Pawn sacrifices don't count as "Brilliant"
            if (movingValue < 3) return (false, null);

            // CRITICAL: If we're capturing a piece worth MORE OR EQUAL to our piece,
            // it's NOT a sacrifice - it's a winning or even trade!
            PieceType capturedType = PieceHelper.GetPieceType(targetPiece);
            int capturedValue = ChessUtilities.GetPieceValue(capturedType);
            if (capturedValue >= movingValue) return (false, null);

            // Simulate the capture
            using var pooled = BoardPool.Rent(board);
            ChessBoard afterCapture = pooled.Board;
            afterCapture.SetPiece(srcRow, srcCol, '.');
            afterCapture.SetPiece(destRow, destCol, movingPiece);

            // Check if enemy has a PAWN that can recapture
            // Pawns are always worth less than minor pieces, so pawn recapture = not a sacrifice
            bool enemyPawnCanRecapture = CanEnemyPawnCapture(afterCapture, destRow, destCol, whiteToMove);
            if (enemyPawnCanRecapture) return (false, null);

            // Check if our piece is defended - if so, any recapture loses material for enemy
            bool ourPieceDefended = ChessUtilities.IsSquareDefended(afterCapture, destRow, destCol, whiteToMove);
            if (ourPieceDefended) return (false, null);

            // CRITICAL: Check for x-ray defense (battery/discovered recapture)
            // Example: Rxf4 with queen behind - if Qxf4, then Qxf4 wins the queen
            // If any enemy recapture would allow us to recapture profitably, it's not a sacrifice
            if (HasProfitableRecapture(afterCapture, destRow, destCol, movingValue, whiteToMove))
                return (false, null);

            // Finally check SEE - must lose material for it to be a sacrifice
            int seeValue = MoveEvaluation.StaticExchangeEvaluation(
                board, destRow, destCol, movingPiece, whiteToMove, srcRow, srcCol);

            // Must be a sacrifice (SEE < 0 means we lose material in the exchange)
            if (seeValue >= 0) return (false, null);

            // All conditions met - it's a Brilliant capture sacrifice!
            return GenerateBrilliantExplanation(movingType, whiteToMove, evalAfter);
        }

        /// <summary>
        /// Check if any enemy recapture would allow us to win material
        /// Covers multiple scenarios:
        /// 1. X-ray/battery: We recapture on same square (Rxf4, Qxf4, Qxf4)
        /// 2. Pin: Enemy piece is pinned, taking exposes bigger piece (Nxe4?? Bxd8)
        /// 3. Discovered attack: Enemy moving reveals our attack on their piece
        /// </summary>
        private static bool HasProfitableRecapture(ChessBoard afterCapture, int pieceRow, int pieceCol, int ourPieceValue, bool weAreWhite)
        {
            char ourPiece = afterCapture.GetPiece(pieceRow, pieceCol);

            // Find all enemy pieces that can capture our piece
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char enemyPiece = afterCapture.GetPiece(r, c);
                    if (enemyPiece == '.') continue;

                    bool enemyIsWhite = char.IsUpper(enemyPiece);
                    if (enemyIsWhite == weAreWhite) continue; // Not an enemy

                    // Can this enemy piece capture our piece?
                    if (!ChessUtilities.CanAttackSquare(afterCapture, r, c, enemyPiece, pieceRow, pieceCol))
                        continue;

                    PieceType enemyType = PieceHelper.GetPieceType(enemyPiece);
                    int enemyValue = ChessUtilities.GetPieceValue(enemyType);

                    // Simulate enemy capturing our piece
                    using var pooled2 = BoardPool.Rent(afterCapture);
                    ChessBoard afterEnemyCapture = pooled2.Board;
                    afterEnemyCapture.SetPiece(r, c, '.'); // Enemy leaves their square
                    afterEnemyCapture.SetPiece(pieceRow, pieceCol, enemyPiece); // Enemy takes our piece

                    // Check 1: Can we recapture on the same square profitably?
                    if (CanRecaptureOnSquare(afterEnemyCapture, pieceRow, pieceCol, enemyValue, ourPieceValue, weAreWhite))
                        return true;

                    // Check 2: Did enemy moving expose any of their pieces to capture?
                    // (This handles pins - e.g., knight was pinned to queen, now queen is exposed)
                    int exposedValue = GetMaxExposedPieceValue(afterCapture, afterEnemyCapture, r, c, weAreWhite);
                    if (exposedValue > 0)
                    {
                        // If we can capture something worth more than what we lost, it's not a sacrifice
                        // We lost ourPieceValue, we can capture exposedValue
                        if (exposedValue >= ourPieceValue)
                            return true;
                    }
                }
            }

            return false; // No profitable response found
        }

        /// <summary>
        /// Check if we can recapture on a square profitably
        /// </summary>
        private static bool CanRecaptureOnSquare(ChessBoard board, int row, int col, int enemyValueOnSquare, int ourLostValue, bool weAreWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char ourPiece = board.GetPiece(r, c);
                    if (ourPiece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(ourPiece);
                    if (pieceIsWhite != weAreWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, ourPiece, row, col))
                    {
                        // We can recapture! Is it profitable?
                        // We lost ourLostValue, we gain enemyValueOnSquare
                        if (enemyValueOnSquare >= ourLostValue)
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get the max value of enemy pieces that became exposed after enemy moved from (fromRow, fromCol)
        /// This detects pins: if enemy knight was pinned to queen, moving the knight exposes the queen
        /// </summary>
        private static int GetMaxExposedPieceValue(ChessBoard beforeMove, ChessBoard afterMove, int fromRow, int fromCol, bool weAreWhite)
        {
            int maxExposed = 0;

            // Check all enemy pieces - did any become attackable after the enemy piece moved?
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char enemyPiece = afterMove.GetPiece(r, c);
                    if (enemyPiece == '.') continue;

                    bool enemyIsWhite = char.IsUpper(enemyPiece);
                    if (enemyIsWhite == weAreWhite) continue; // Not an enemy piece

                    // Was this piece NOT attackable before, but IS attackable now?
                    bool wasAttackable = ChessUtilities.IsSquareAttackedBy(beforeMove, r, c, weAreWhite);
                    bool isAttackable = ChessUtilities.IsSquareAttackedBy(afterMove, r, c, weAreWhite);

                    if (!wasAttackable && isAttackable)
                    {
                        // This piece became exposed!
                        PieceType exposedType = PieceHelper.GetPieceType(enemyPiece);
                        int exposedValue = ChessUtilities.GetPieceValue(exposedType);
                        if (exposedValue > maxExposed)
                            maxExposed = exposedValue;
                    }
                }
            }

            return maxExposed;
        }

        /// <summary>
        /// Check for implicit sacrifice: non-capture that leaves pieces hanging
        /// Examples:
        /// - Qh5 threatening mate while leaving bishop on f4 undefended
        /// - Ng6+ giving check while the knight is en prise
        ///
        /// NOT implicit sacrifices (tactical threats):
        /// - Rd6 attacking queen while knight hangs (opponent can't take knight without losing queen)
        /// - Discovered attack where piece "hangs" but reveals attack on bigger piece
        /// </summary>
        private static (bool isBrilliant, string? explanation) CheckImplicitSacrifice(
            ChessBoard board, char movingPiece,
            int srcRow, int srcCol, int destRow, int destCol,
            bool whiteToMove, double evalAfter)
        {
            // Apply the move to see the resulting position
            using var pooled = BoardPool.Rent(board);
            ChessBoard afterMove = pooled.Board;
            afterMove.SetPiece(srcRow, srcCol, '.');
            afterMove.SetPiece(destRow, destCol, movingPiece);

            // Handle pawn promotion
            if (movingPiece == 'P' && destRow == 0)
                afterMove.SetPiece(destRow, destCol, 'Q');
            else if (movingPiece == 'p' && destRow == 7)
                afterMove.SetPiece(destRow, destCol, 'q');

            // Find all our pieces that are now hanging (undefended and can be captured)
            // A piece is "hanging" if:
            // 1. It's our piece (value >= 3 for brilliant consideration)
            // 2. It's not defended by any of our pieces
            // 3. It CAN be captured by an enemy piece

            (int row, int col, char piece, int value)? hangingPiece = null;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = afterMove.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != whiteToMove) continue; // Not our piece

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    int pieceValue = ChessUtilities.GetPieceValue(pieceType);

                    // Only consider minor pieces or better (value >= 3)
                    if (pieceValue < 3) continue;

                    // Check if this piece is defended by us
                    bool isDefended = IsSquareDefendedExcluding(afterMove, r, c, whiteToMove, r, c);
                    if (isDefended) continue; // Piece is defended, not hanging

                    // Check if enemy can capture this piece
                    bool canBeCaptured = ChessUtilities.IsSquareAttackedBy(afterMove, r, c, !whiteToMove);
                    if (!canBeCaptured) continue; // Enemy can't take it

                    // This piece is hanging! Track the highest value one
                    if (hangingPiece == null || pieceValue > hangingPiece.Value.value)
                    {
                        hangingPiece = (r, c, piece, pieceValue);
                    }
                }
            }

            // No hanging pieces = not an implicit sacrifice
            if (hangingPiece == null) return (false, null);

            // CRITICAL: Check if this is just a tactical threat, not a true sacrifice
            // If the move attacks enemy pieces worth >= the hanging piece, opponent can't profitably take it
            // Examples: Rd6 attacks queen (9) while knight (3) hangs - not a sacrifice, it's a threat
            int maxThreatValue = GetMaxThreatValue(afterMove, srcRow, srcCol, destRow, destCol, whiteToMove);

            if (maxThreatValue >= hangingPiece.Value.value)
            {
                // The hanging piece is "protected" by a bigger threat - not a real sacrifice
                return (false, null);
            }

            // We found a hanging piece with no compensating threat - this is an implicit sacrifice!
            PieceType sacrificedType = PieceHelper.GetPieceType(hangingPiece.Value.piece);
            return GenerateBrilliantExplanation(sacrificedType, whiteToMove, evalAfter, isImplicit: true);
        }

        /// <summary>
        /// Get the maximum value of enemy pieces threatened by our move.
        /// Includes both direct attacks from the moved piece and discovered attacks.
        /// </summary>
        private static int GetMaxThreatValue(ChessBoard afterMove, int srcRow, int srcCol, int destRow, int destCol, bool whiteToMove)
        {
            int maxThreat = 0;
            char movedPiece = afterMove.GetPiece(destRow, destCol);

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char enemyPiece = afterMove.GetPiece(r, c);
                    if (enemyPiece == '.') continue;

                    bool enemyIsWhite = char.IsUpper(enemyPiece);
                    if (enemyIsWhite == whiteToMove) continue; // Not an enemy piece

                    PieceType enemyType = PieceHelper.GetPieceType(enemyPiece);
                    int enemyValue = ChessUtilities.GetPieceValue(enemyType);

                    // Check if moved piece attacks this enemy
                    if (ChessUtilities.CanAttackSquare(afterMove, destRow, destCol, movedPiece, r, c))
                    {
                        if (enemyValue > maxThreat)
                            maxThreat = enemyValue;
                    }

                    // Check for discovered attacks - pieces that were blocked by the moved piece
                    // and now have a clear line to enemy pieces
                    // Check along the line from dest through src (opposite direction of the move)
                    if (IsDiscoveredAttack(afterMove, srcRow, srcCol, r, c, whiteToMove))
                    {
                        if (enemyValue > maxThreat)
                            maxThreat = enemyValue;
                    }
                }
            }

            return maxThreat;
        }

        /// <summary>
        /// Check if there's a discovered attack through the source square to the target square
        /// </summary>
        private static bool IsDiscoveredAttack(ChessBoard board, int srcRow, int srcCol, int targetRow, int targetCol, bool byWhite)
        {
            // Look for our pieces that can attack through the now-empty source square
            // Check along lines and diagonals that pass through srcRow, srcCol

            // For each of our sliding pieces (bishop, rook, queen), check if they attack the target
            // through the source square (which is now empty after the move)
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    PieceType pieceType = PieceHelper.GetPieceType(piece);

                    // Only sliding pieces can create discovered attacks
                    if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                        continue;

                    // Check if this piece attacks the target
                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, targetRow, targetCol))
                    {
                        // Verify the attack goes through or near the source square
                        // (i.e., the source square was blocking this attack before)
                        if (IsOnLineBetween(r, c, srcRow, srcCol, targetRow, targetCol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if point (midRow, midCol) is on the line between (r1, c1) and (r2, c2)
        /// </summary>
        private static bool IsOnLineBetween(int r1, int c1, int midRow, int midCol, int r2, int c2)
        {
            // Check if mid is between r1,c1 and r2,c2 on a straight line or diagonal

            // Same row (horizontal line)
            if (r1 == midRow && midRow == r2)
            {
                int minCol = Math.Min(c1, c2);
                int maxCol = Math.Max(c1, c2);
                return midCol > minCol && midCol < maxCol;
            }

            // Same column (vertical line)
            if (c1 == midCol && midCol == c2)
            {
                int minRow = Math.Min(r1, r2);
                int maxRow = Math.Max(r1, r2);
                return midRow > minRow && midRow < maxRow;
            }

            // Diagonal
            int dr1 = midRow - r1;
            int dc1 = midCol - c1;
            int dr2 = r2 - midRow;
            int dc2 = c2 - midCol;

            // Check if same diagonal direction and mid is between
            if (Math.Abs(dr1) == Math.Abs(dc1) && Math.Abs(dr2) == Math.Abs(dc2))
            {
                // Same diagonal direction
                if (dr1 != 0 && dc1 != 0 && dr2 != 0 && dc2 != 0)
                {
                    int signR1 = Math.Sign(dr1);
                    int signC1 = Math.Sign(dc1);
                    int signR2 = Math.Sign(dr2);
                    int signC2 = Math.Sign(dc2);

                    // Must be same direction from r1,c1 to mid and from mid to r2,c2
                    return signR1 == signR2 && signC1 == signC2;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a square is defended by a side, excluding a specific piece from consideration
        /// Used to check if a piece defends itself (it doesn't)
        /// </summary>
        private static bool IsSquareDefendedExcluding(ChessBoard board, int row, int col, bool byWhite, int excludeRow, int excludeCol)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Skip the excluded square (the piece itself)
                    if (r == excludeRow && c == excludeCol) continue;

                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generate the brilliant move explanation
        /// </summary>
        private static (bool isBrilliant, string? explanation) GenerateBrilliantExplanation(
            PieceType sacrificedType, bool whiteToMove, double evalAfter, bool isImplicit = false)
        {
            string pieceName = sacrificedType switch
            {
                PieceType.Queen => "queen",
                PieceType.Rook => "rook",
                PieceType.Bishop => "bishop",
                PieceType.Knight => "knight",
                _ => "piece"
            };

            // Describe the compensation based on position evaluation
            string compensation;
            double evalAdvantage = whiteToMove ? evalAfter : -evalAfter;

            if (evalAdvantage >= 1.5)
                compensation = "decisive advantage";
            else if (evalAdvantage >= 0.5)
                compensation = "strong initiative";
            else if (evalAdvantage >= 0.0)
                compensation = "lasting compensation";
            else
                compensation = "dynamic counterplay";

            // Different phrasing for implicit vs explicit sacrifice
            string verb = isImplicit ? "leaves" : "sacrifices";
            string suffix = isImplicit ? " en prise" : "";
            string explanation = $"{verb} {pieceName}{suffix} for {compensation}";
            return (true, explanation);
        }

        /// <summary>
        /// Get move classification based on win probability drop (Chess.com criteria)
        /// </summary>
        /// <param name="bestEval">Evaluation of the best move (in pawns)</param>
        /// <param name="moveEval">Evaluation of the move being classified (in pawns)</param>
        /// <param name="whiteToMove">Whether it's White's turn</param>
        /// <returns>Tuple of (label, symbol, color) for the classification</returns>
        private static (string label, string symbol, Color color) GetMoveClassification(
            double bestEval, double moveEval, bool whiteToMove)
        {
            // Calculate win probabilities
            // Evaluations are from White's perspective:
            // - When White to move: higher eval = better for White
            // - When Black to move: lower eval = better for Black
            double bestWinProb, moveWinProb;

            if (whiteToMove)
            {
                // White's perspective: higher is better
                bestWinProb = CalculateWinProbability(bestEval);
                moveWinProb = CalculateWinProbability(moveEval);
            }
            else
            {
                // Black's perspective: flip the sign (lower is better for Black)
                bestWinProb = CalculateWinProbability(-bestEval);
                moveWinProb = CalculateWinProbability(-moveEval);
            }

            // Win probability dropped (expected points lost)
            double winProbDrop = bestWinProb - moveWinProb;

            // Chess.com classification thresholds (expected points lost):
            // Best: 0.00, Excellent: 0.00-0.02, Good: 0.02-0.05
            // Inaccuracy: 0.05-0.10, Mistake: 0.10-0.20, Blunder: 0.20+
            //
            // NOTE: When showing alternatives to the "only winning move", we only
            // display NEGATIVE classifications (Blunder, Mistake, Inaccuracy).
            // If the drop is small, we return null to show no classification.
            if (winProbDrop >= 0.20)
                return ("Blunder", "??", Color.Crimson);
            else if (winProbDrop >= 0.10)
                return ("Mistake", "?", Color.OrangeRed);
            else if (winProbDrop >= 0.05)
                return ("Inaccuracy", "?!", Color.Orange);
            else
                return ("", "", Color.Transparent); // No label for small drops
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
            richTextBox.SelectionColor = GetThemeColor(Color.Gray, Color.DarkSlateGray);
            richTextBox.AppendText("Position: ");

            // Win percentage - green shade based on value (darker for light mode)
            Color winColor = winPercent > 60
                ? GetThemeColor(Color.LimeGreen, Color.DarkGreen)
                : winPercent > 40
                    ? GetThemeColor(Color.MediumSeaGreen, Color.SeaGreen)
                    : GetThemeColor(Color.DarkSeaGreen, Color.ForestGreen);
            richTextBox.SelectionColor = winColor;
            richTextBox.AppendText($"W:{winPercent:F0}% ");

            // Draw percentage - neutral color (darker for light mode)
            richTextBox.SelectionColor = GetThemeColor(Color.Gold, Color.Sienna);
            richTextBox.AppendText($"D:{drawPercent:F0}% ");

            // Loss percentage - red shade based on value
            Color lossColor = lossPercent > 60 ? Color.Crimson :
                             lossPercent > 40 ? Color.IndianRed : GetThemeColor(Color.RosyBrown, Color.Maroon);
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
        /// Displays opening book moves (Polyglot format) in SAN notation
        /// </summary>
        public void DisplayBookMoves(List<BookMove> bookMoves, string fen)
        {
            if (bookMoves == null || bookMoves.Count == 0)
                return;

            // Calculate total weight for percentage display
            int totalWeight = bookMoves.Sum(m => m.Priority);

            richTextBox.SelectionColor = GetThemeColor(Color.Khaki, Color.DarkOliveGreen);
            richTextBox.AppendText($"Book: ");

            for (int i = 0; i < Math.Min(bookMoves.Count, 5); i++)
            {
                var move = bookMoves[i];
                if (i > 0)
                {
                    richTextBox.SelectionColor = GetThemeColor(Color.Gray, Color.DarkSlateGray);
                    richTextBox.AppendText(", ");
                }

                // Convert UCI to SAN notation
                string sanMove = ChessNotationService.ConvertFullPvToSan(move.UciMove, fen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);

                richTextBox.SelectionColor = GetThemeColor(Color.PaleGreen, Color.DarkGreen);
                richTextBox.AppendText($"{sanMove}");

                richTextBox.SelectionColor = GetThemeColor(Color.Gray, Color.DarkSlateGray);
                int pct = totalWeight > 0 ? (int)Math.Round(100.0 * move.Priority / totalWeight) : 0;
                richTextBox.AppendText($" ({pct}%)");
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
            bool showBestLine,
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
            if (config?.ShowWDL == true && wdl != null)
            {
                DisplayWDLInfo(wdl, whiteToMoveForWdl);
            }

            // Display opening name if in known theory and enabled
            if (config?.ShowOpeningName == true)
            {
                string openingDisplay = OpeningBook.GetOpeningDisplay(completeFen);

                if (!string.IsNullOrEmpty(openingDisplay) && openingDisplay != "Starting Position")
                {
                    richTextBox.SelectionColor = GetThemeColor(Color.CornflowerBlue, Color.MidnightBlue);
                    richTextBox.AppendText($"Opening: {openingDisplay}{Environment.NewLine}");
                    ResetFormatting();
                }
                else
                {
                    // Show "Out of book" when we're past known theory
                    richTextBox.SelectionColor = GetThemeColor(Color.Gray, Color.DarkSlateGray);
                    richTextBox.AppendText($"Opening: Out of book{Environment.NewLine}");
                    ResetFormatting();
                }
            }

            // Display book moves if available and enabled
            if (config?.ShowBookMoves == true && bookMoves != null && bookMoves.Count > 0)
            {
                DisplayBookMoves(bookMoves, completeFen);
            }

            // Determine if dark mode is enabled for color selection
            bool isDarkMode = config?.Theme == "Dark";

            // Detect "only winning move" scenario:
            // Best move is winning but second-best loses the advantage
            // Must account for whose turn it is (evals are from White's perspective)
            bool isOnlyWinningMove = false;
            if (showSecondLine && evaluations.Count >= 2)
            {
                double? bestEval = MovesExplanation.ParseEvaluation(evaluation);
                double? secondEval = MovesExplanation.ParseEvaluation(evaluations[1]);

                if (bestEval.HasValue && secondEval.HasValue)
                {
                    // Determine whose turn it is from FEN
                    string[] fenParts = fen.Split(' ');
                    bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                    // Calculate the evaluation swing between best and second-best
                    double evalSwing = Math.Abs(bestEval.Value - secondEval.Value);

                    // Standard evaluation thresholds (chessify.me/Stockfish):
                    // Decisive: > 1.50, Clear: 0.70-1.50, Slight: 0.27-0.70, Equal: < 0.27

                    if (whiteToMove)
                    {
                        // White to move: positive eval = White winning
                        // Basic: best has clear+ advantage (>= 0.70) AND second-best is equal or worse (<= 0.27)
                        // Swing: huge swing (>= 2.0) where best has slight+ advantage (>= 0.27) and second doesn't
                        // Disaster: best keeps any edge (>= 0) but second is losing badly (<= -1.50)
                        bool basicTrigger = bestEval.Value >= 0.70 && secondEval.Value <= 0.27;
                        bool swingTrigger = evalSwing >= 2.0 && bestEval.Value >= 0.27 && secondEval.Value <= 0.0;
                        bool disasterTrigger = bestEval.Value >= 0.0 && secondEval.Value <= -1.50;
                        isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger;
                    }
                    else
                    {
                        // Black to move: negative eval = Black winning
                        // Basic: best has clear+ advantage (<= -0.70) AND second-best is equal or worse (>= -0.27)
                        // Swing: huge swing (>= 2.0) where best has slight+ advantage (<= -0.27) and second doesn't
                        // Disaster: best keeps any edge (<= 0) but second is losing badly (>= +1.50)
                        bool basicTrigger = bestEval.Value <= -0.70 && secondEval.Value >= -0.27;
                        bool swingTrigger = evalSwing >= 2.0 && bestEval.Value <= -0.27 && secondEval.Value >= 0.0;
                        bool disasterTrigger = bestEval.Value <= 0.0 && secondEval.Value >= 1.50;
                        isOnlyWinningMove = basicTrigger || swingTrigger || disasterTrigger;
                    }
                }
            }

            // Best line - always show threats
            string bestSanFull = ConvertPvToSan(pvs, 0, bestMove, fen);
            string formattedEval = FormatEvaluation(evaluation);
            // Use pvs[0] not bestMove - after Multi-PV sorting, bestMove may not match pvs[0]
            // bestMove is the "recommended" move based on aggressiveness setting
            string firstMove = pvs.Count > 0 ? pvs[0].Split(' ')[0] : bestMove;

            // Check if recommended move (bestMove) differs from eval-best move (firstMove)
            // If so, we'll mark the recommended line with an indicator
            bool recommendedDiffersFromBest = !string.IsNullOrEmpty(bestMove) &&
                                               !string.IsNullOrEmpty(firstMove) &&
                                               bestMove != firstMove;

            // Check for Brilliant move (piece sacrifice that works)
            (string label, string symbol, Color color)? bestMoveClassification = null;
            string? brilliantExplanation = null;
            double? evalForBrilliant = MovesExplanation.ParseEvaluation(evaluation);
            if (evalForBrilliant.HasValue)
            {
                var (isBrilliant, explanation) = IsBrilliantMove(fen, firstMove, evalForBrilliant.Value, previousEvaluation);
                if (isBrilliant)
                {
                    bestMoveClassification = ("Brilliant", "!!", GetThemeColor(Color.Cyan, Color.Teal));
                    brilliantExplanation = explanation;
                }
            }

            // Best line - show if enabled
            if (showBestLine)
            {
                DisplayMoveLine(
                    "Best line",
                    bestSanFull,
                    formattedEval,
                    fen,
                    pvs,
                    firstMove,
                    isDarkMode ? Color.PaleGreen : Color.DarkGreen,
                    isDarkMode ? Color.LightGreen : Color.ForestGreen,
                    showThreats: true,
                    isOnlyWinningMove: isOnlyWinningMove,
                    classification: bestMoveClassification,
                    overrideExplanation: brilliantExplanation);
            }

            // Second best - show threats if enabled
            if (showSecondLine && pvs.Count >= 2)
            {
                var secondSan = ChessNotationService.ConvertFullPvToSan(pvs[1], fen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string secondMove = pvs[1].Split(' ')[0];
                string secondEval = evaluations.Count >= 2 ? evaluations[1] : "";
                string formattedSecondEval = FormatEvaluation(secondEval);

                // Calculate classification if best move is the only winning move
                (string label, string symbol, Color color)? secondClassification = null;
                if (isOnlyWinningMove)
                {
                    double? bestEvalParsed = MovesExplanation.ParseEvaluation(evaluation);
                    double? secondEvalParsed = MovesExplanation.ParseEvaluation(secondEval);
                    if (bestEvalParsed.HasValue && secondEvalParsed.HasValue)
                    {
                        string[] fenParts = fen.Split(' ');
                        bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                        secondClassification = GetMoveClassification(bestEvalParsed.Value, secondEvalParsed.Value, whiteToMove);
                    }
                }

                DisplayMoveLine(
                    "Second best",
                    secondSan,
                    formattedSecondEval,
                    fen,
                    pvs,
                    secondMove,
                    isDarkMode ? Color.Khaki : Color.SaddleBrown,
                    isDarkMode ? Color.LightGoldenrodYellow : Color.Sienna,
                    showThreats: true,
                    isOnlyWinningMove: false,
                    classification: secondClassification);
            }

            // Third best - show threats if enabled
            if (showThirdLine && pvs.Count >= 3)
            {
                var thirdSan = ChessNotationService.ConvertFullPvToSan(pvs[2], fen,
                    ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                string thirdMove = pvs[2].Split(' ')[0];
                string thirdEval = evaluations.Count >= 3 ? evaluations[2] : "";
                string formattedThirdEval = FormatEvaluation(thirdEval);

                // Calculate classification if best move is the only winning move
                (string label, string symbol, Color color)? thirdClassification = null;
                if (isOnlyWinningMove)
                {
                    double? bestEvalParsed = MovesExplanation.ParseEvaluation(evaluation);
                    double? thirdEvalParsed = MovesExplanation.ParseEvaluation(thirdEval);
                    if (bestEvalParsed.HasValue && thirdEvalParsed.HasValue)
                    {
                        string[] fenParts = fen.Split(' ');
                        bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";
                        thirdClassification = GetMoveClassification(bestEvalParsed.Value, thirdEvalParsed.Value, whiteToMove);
                    }
                }

                DisplayMoveLine(
                    "Third best",
                    thirdSan,
                    formattedThirdEval,
                    fen,
                    pvs,
                    thirdMove,
                    isDarkMode ? Color.LightCoral : Color.Maroon,
                    isDarkMode ? Color.Salmon : Color.DarkRed,
                    showThreats: true,
                    isOnlyWinningMove: false,
                    classification: thirdClassification);
            }

            // Display recommended move (based on aggressiveness) if:
            // 1. It differs from best line, OR
            // 2. All three lines are hidden (user only wants style recommendation), OR
            // 3. A non-balanced style is active (shows style confirmation even when recommendation = best)
            bool allLinesHidden = !showBestLine && !showSecondLine && !showThirdLine;
            bool hasNonBalancedStyle = config != null && (config.Aggressiveness <= 40 || config.Aggressiveness >= 61);
            bool showRecommendedSection = recommendedDiffersFromBest || allLinesHidden || hasNonBalancedStyle;

            if (showRecommendedSection)
            {
                // Find the recommended move's PV line and eval
                // If recommended doesn't differ, use pvs[0] (the best eval line)
                int recommendedIndex = -1;
                for (int i = 0; i < pvs.Count; i++)
                {
                    string moveFromPv = pvs[i].Split(' ')[0];
                    if (moveFromPv == bestMove)
                    {
                        recommendedIndex = i;
                        break;
                    }
                }

                if (recommendedIndex >= 0 && config != null)
                {
                    var recommendedSan = ChessNotationService.ConvertFullPvToSan(pvs[recommendedIndex], fen,
                        ChessRulesService.ApplyUciMove, ChessRulesService.CanReachSquare, ChessRulesService.FindAllPiecesOfSameType);
                    string recommendedEval = recommendedIndex < evaluations.Count ? evaluations[recommendedIndex] : "";
                    string formattedRecommendedEval = FormatEvaluation(recommendedEval);

                    // Add a blank line separator before style/recommended section (only if there are lines above)
                    if (!allLinesHidden)
                    {
                        richTextBox.AppendText(Environment.NewLine);
                    }

                    // Display play style indicator
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
                        <= 20 => isDarkMode ? Color.SteelBlue : Color.MidnightBlue,
                        <= 40 => isDarkMode ? Color.CadetBlue : Color.DarkCyan,
                        <= 60 => isDarkMode ? Color.Gold : Color.Sienna,
                        <= 80 => isDarkMode ? Color.OrangeRed : Color.Firebrick,
                        _ => isDarkMode ? Color.Crimson : Color.DarkRed
                    };
                    richTextBox.SelectionColor = styleColor;
                    richTextBox.AppendText($"Style: {playStyle} ({config.Aggressiveness}){Environment.NewLine}");
                    ResetFormatting();

                    // Use best line colors when it's the only line shown, orange when shown alongside other lines
                    Color headerColor = allLinesHidden
                        ? (isDarkMode ? Color.PaleGreen : Color.DarkGreen)
                        : (isDarkMode ? Color.Orange : Color.DarkOrange);
                    Color explanationColor = allLinesHidden
                        ? (isDarkMode ? Color.LightGreen : Color.ForestGreen)
                        : (isDarkMode ? Color.Gold : Color.Chocolate);

                    DisplayMoveLine(
                        "Recommended",
                        recommendedSan,
                        formattedRecommendedEval,
                        fen,
                        pvs,
                        bestMove,
                        headerColor,
                        explanationColor,
                        showThreats: allLinesHidden, // Show threats when it's the only line
                        isOnlyWinningMove: false,
                        classification: null);
                }
            }

            // Display opponent threats at the bottom (independent of which move we choose)
            if (config?.ShowThreats == true)
            {
                DisplayOpponentThreats(fen);
            }

            // Display endgame insights if in endgame and enabled
            if (config?.ShowEndgameAnalysis == true)
            {
                DisplayEndgameInsights(fen);
            }

            ResetFormatting();
        }

        /// <summary>
        /// Displays endgame-specific insights (rule of the square, opposition, etc.)
        /// Inspired by Stockfish, Ethereal, and Lc0 endgame evaluation
        /// </summary>
        private void DisplayEndgameInsights(string completeFen)
        {
            try
            {
                ChessBoard board = ChessBoard.FromFEN(completeFen);

                // Only show insights in endgame positions
                if (!EndgameAnalysis.IsEndgame(board))
                    return;

                string[] fenParts = completeFen.Split(' ');
                bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

                var insights = EndgameAnalysis.GetEndgameInsights(board, whiteToMove);

                if (insights.Count > 0)
                {
                    richTextBox.AppendText(Environment.NewLine);

                    // Game phase header
                    string phase = EndgameAnalysis.GetGamePhase(board);
                    AppendTextWithFormat($"♟ Endgame Analysis ({phase}):{Environment.NewLine}",
                        richTextBox.BackColor, GetThemeColor(Color.Cyan, Color.Teal), FontStyle.Bold);

                    foreach (var insight in insights)
                    {
                        // Color-code based on insight type
                        Color insightColor = GetInsightColor(insight);
                        AppendTextWithFormat($"  • {insight}{Environment.NewLine}",
                            richTextBox.BackColor, insightColor, FontStyle.Regular);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying endgame insights: {ex.Message}");
            }
        }

        /// <summary>
        /// Get color for endgame insight based on content
        /// </summary>
        private Color GetInsightColor(string insight)
        {
            string lower = insight.ToLower();

            // Critical advantages
            if (lower.Contains("unstoppable") || lower.Contains("forced checkmate"))
                return GetThemeColor(Color.LimeGreen, Color.DarkGreen);

            // Drawing indicators
            if (lower.Contains("draw") || lower.Contains("insufficient") ||
                lower.Contains("fortress") || lower.Contains("wrong color"))
                return GetThemeColor(Color.Gold, Color.Sienna);

            // Opposition and key squares
            if (lower.Contains("opposition"))
                return GetThemeColor(Color.MediumOrchid, Color.Purple);

            // King activity
            if (lower.Contains("active king") || lower.Contains("centralization"))
                return GetThemeColor(Color.PaleGreen, Color.DarkGreen);

            // Passed pawn insights
            if (lower.Contains("passed pawn"))
                return GetThemeColor(Color.Orange, Color.Firebrick);

            // Zugzwang
            if (lower.Contains("zugzwang"))
                return GetThemeColor(Color.Coral, Color.Maroon);

            // Default
            return GetThemeColor(Color.LightGray, Color.DimGray);
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
                    AppendTextWithFormat("⚠ Opponent threats: ", richTextBox.BackColor, GetThemeColor(Color.Orange, Color.Firebrick), FontStyle.Bold);
                    string oppThreatText = string.Join(", ", opponentThreats.Select(t => t.Description));
                    AppendTextWithFormat($"{oppThreatText}{Environment.NewLine}", richTextBox.BackColor, GetThemeColor(Color.Coral, Color.Maroon), FontStyle.Regular);
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