using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Analyzes chess moves for "sharpness" - how aggressive/risky vs solid/safe they are.
    /// Used to filter multi-PV candidates based on Aggressiveness slider setting.
    ///
    /// Sharpness Score: 0 = very solid/safe, 100 = very aggressive/sharp
    ///
    /// Factors that increase sharpness (aggressive moves):
    /// - Captures (especially piece exchanges)
    /// - Pawn pushes (especially central pawns, passed pawns)
    /// - Checks (forcing moves)
    /// - Piece sacrifices (losing material for initiative)
    /// - King attacks (moves toward enemy king)
    /// - Opening the position (pawn exchanges, breaks)
    /// - Castling opposite sides (usually leads to sharp play)
    ///
    /// Factors that decrease sharpness (solid moves):
    /// - Quiet piece development
    /// - Defensive moves (blocking, retreating)
    /// - Maintaining material balance
    /// - Consolidating king safety
    /// - Prophylactic moves
    /// </summary>
    public class MoveSharpnessAnalyzer
    {
        /// <summary>
        /// Calculates a sharpness score for a move (0-100).
        /// Higher score = more aggressive/risky move.
        /// Lower score = more solid/safe move.
        /// </summary>
        /// <param name="move">UCI move string (e.g., "e2e4", "e7e8q")</param>
        /// <param name="fen">Current position FEN</param>
        /// <param name="evaluation">Engine evaluation string (e.g., "+1.50", "Mate in 3")</param>
        /// <param name="pvLine">Full PV line from engine</param>
        /// <returns>Sharpness score 0-100</returns>
        public int CalculateSharpness(string move, string fen, string evaluation, string pvLine)
        {
            if (string.IsNullOrEmpty(move) || move.Length < 4)
                return 50; // Default to balanced

            int sharpness = 50; // Start at balanced

            // Parse move components
            string from = move.Substring(0, 2);
            string to = move.Substring(2, 2);
            char? promotion = move.Length > 4 ? move[4] : null;

            // Parse FEN for position analysis
            string[] fenParts = fen.Split(' ');
            string boardPart = fenParts.Length > 0 ? fenParts[0] : "";
            bool whiteToMove = fenParts.Length > 1 && fenParts[1] == "w";

            // Get piece being moved
            char? movedPiece = GetPieceAt(boardPart, from);
            char? capturedPiece = GetPieceAt(boardPart, to);

            // Factor 1: Captures (+10 to +25 sharpness)
            if (capturedPiece.HasValue && capturedPiece != '.')
            {
                int capturedValue = GetPieceValue(capturedPiece.Value);
                int movedValue = movedPiece.HasValue ? GetPieceValue(movedPiece.Value) : 1;

                if (capturedValue >= movedValue)
                {
                    // Winning or equal capture - moderately sharp
                    sharpness += 10 + Math.Min(capturedValue / 2, 15);
                }
                else
                {
                    // Sacrifice! Very sharp
                    sharpness += 25;
                }
            }

            // Factor 2: Pawn moves (+5 to +15 sharpness for center/attacking pawns)
            if (movedPiece.HasValue && char.ToLower(movedPiece.Value) == 'p')
            {
                int toFile = to[0] - 'a';
                int toRank = to[1] - '1';

                // Central pawn pushes are sharp (d/e files)
                if (toFile >= 3 && toFile <= 4)
                {
                    sharpness += 8;
                }

                // Passed pawn advances are very sharp
                if ((whiteToMove && toRank >= 5) || (!whiteToMove && toRank <= 2))
                {
                    sharpness += 10;
                }

                // Pawn breaks (c4, d4, e4, f4 or c5, d5, e5, f5) are sharp
                if ((whiteToMove && toRank == 3 && toFile >= 2 && toFile <= 5) ||
                    (!whiteToMove && toRank == 4 && toFile >= 2 && toFile <= 5))
                {
                    sharpness += 7;
                }
            }

            // Factor 3: Promotion (+20 sharpness - always dramatic)
            if (promotion.HasValue)
            {
                sharpness += 20;
                // Queen promotion is slightly more forcing
                if (char.ToLower(promotion.Value) == 'q')
                    sharpness += 5;
            }

            // Factor 4: Check detection from PV line (+15 sharpness)
            // Moves that deliver check are inherently forcing/sharp
            if (pvLine.Contains("+") || pvLine.Contains("#"))
            {
                sharpness += 15;
            }

            // Factor 5: King safety - castling is solid (-10 sharpness)
            if (IsCastling(move))
            {
                sharpness -= 10;
            }

            // Factor 6: Piece development in opening
            if (movedPiece.HasValue)
            {
                char piece = char.ToLower(movedPiece.Value);

                // Count pieces to estimate game phase
                int pieceCount = CountPieces(boardPart);

                if (pieceCount >= 28) // Opening (most pieces on board)
                {
                    if (piece == 'q')
                    {
                        // Early queen development - risky/sharp
                        sharpness += 12;
                    }
                    else if (piece == 'n' || piece == 'b')
                    {
                        // Knight/Bishop development is standard - neutral
                        // Central development (to c3/c6, d2/d7, e2/e7, f3/f6) is slightly sharp
                        int toFile = to[0] - 'a';
                        if (toFile >= 2 && toFile <= 5) // c through f files
                        {
                            sharpness += 3; // Active central development
                        }
                    }
                    else if (piece == 'p')
                    {
                        // Quiet pawn moves in opening (not captures, not center) are solid
                        int toFile = to[0] - 'a';
                        if (toFile < 3 || toFile > 4) // Not d/e files
                        {
                            sharpness -= 5; // Flank pawns are typically more solid
                        }
                    }
                }
            }

            // Factor 7: Evaluation swing analysis
            // If move changes eval significantly, it's sharp
            double? evalValue = ParseEvaluation(evaluation);
            if (evalValue.HasValue)
            {
                double absEval = Math.Abs(evalValue.Value);

                // Large advantage moves tend to be consolidating (solid)
                if (absEval > 3.0)
                {
                    sharpness -= 8;
                }
                // Slight edge requires precision - more tactical/sharp
                else if (absEval < 0.5)
                {
                    sharpness += 5;
                }
            }

            // Factor 8: Mate threats are maximally sharp
            if (evaluation.Contains("Mate"))
            {
                sharpness += 20;
            }

            // Factor 9: Piece placement aggressiveness
            // Pieces moving toward enemy king area are sharp
            if (movedPiece.HasValue)
            {
                int toRank = to[1] - '1';
                int toFile = to[0] - 'a';

                // Moving into enemy territory (ranks 6-7 for white, ranks 0-1 for black)
                if ((whiteToMove && toRank >= 5) || (!whiteToMove && toRank <= 2))
                {
                    sharpness += 7;
                }

                // King attacks (files around enemy king) - would need king position tracking
                // Simplified: moving to edge files near enemy back rank
                if ((whiteToMove && toRank == 7) || (!whiteToMove && toRank == 0))
                {
                    sharpness += 8;
                }
            }

            // Clamp to valid range
            sharpness = Math.Max(0, Math.Min(100, sharpness));

            Debug.WriteLine($"[Sharpness] Move {move}: {sharpness} (capture={capturedPiece.HasValue}, promo={promotion.HasValue})");

            return sharpness;
        }

        /// <summary>
        /// Selects the best move from candidates based on aggressiveness preference.
        /// </summary>
        /// <param name="candidates">List of (move, evaluation, pvLine, sharpness) tuples, sorted by Multi-PV (best first for current player)</param>
        /// <param name="aggressiveness">User's aggressiveness setting 0-100</param>
        /// <param name="evalTolerance">Max eval loss to accept for style preference (in pawns)</param>
        /// <returns>Index of selected move in candidates list</returns>
        public int SelectMoveByAggressiveness(
            List<(string move, string evaluation, string pvLine, int sharpness)> candidates,
            int aggressiveness,
            double evalTolerance = 0.30) // Allow up to 0.30 pawn loss for style
        {
            if (candidates.Count == 0)
                return -1;

            if (candidates.Count == 1)
                return 0;

            // Parse best move's evaluation as baseline
            // NOTE: Candidates are already sorted by Multi-PV (best for current player first)
            // So candidates[0] has the best eval from the current player's perspective
            double? bestEval = ParseEvaluation(candidates[0].evaluation);
            if (!bestEval.HasValue)
                return 0; // Can't compare, return sorted best

            // Filter candidates within evaluation tolerance
            // Since candidates are sorted best-first, we compare each move's eval to the best
            // The tolerance is the maximum eval degradation we'll accept for style
            var acceptable = new List<(int index, string move, double eval, int sharpness)>();

            for (int i = 0; i < candidates.Count; i++)
            {
                double? eval = ParseEvaluation(candidates[i].evaluation);
                if (!eval.HasValue) continue;

                // Calculate absolute eval difference from the best move
                // Since candidates are sorted best-first, later candidates are worse
                // We use absolute difference to measure how much worse a move is
                double evalDiff = Math.Abs(bestEval.Value - eval.Value);

                // Accept if within tolerance
                if (evalDiff <= evalTolerance)
                {
                    acceptable.Add((i, candidates[i].move, eval.Value, candidates[i].sharpness));
                }
            }

            if (acceptable.Count == 0)
                return 0;

            if (acceptable.Count == 1)
                return acceptable[0].index;

            // Select based on aggressiveness preference
            // aggressiveness 0 = prefer lowest sharpness (solid)
            // aggressiveness 100 = prefer highest sharpness (aggressive)
            // aggressiveness 50 = prefer middle ground

            if (aggressiveness <= 30)
            {
                // Solid: prefer lowest sharpness
                return acceptable.OrderBy(c => c.sharpness).First().index;
            }
            else if (aggressiveness >= 70)
            {
                // Aggressive: prefer highest sharpness
                return acceptable.OrderByDescending(c => c.sharpness).First().index;
            }
            else
            {
                // Balanced: prefer the engine's top choice among acceptable moves
                // (trust engine eval, only deviate for extreme preferences)
                return acceptable[0].index;
            }
        }

        /// <summary>
        /// Gets the piece at a given square from FEN board representation.
        /// </summary>
        private char? GetPieceAt(string boardFen, string square)
        {
            if (string.IsNullOrEmpty(boardFen) || string.IsNullOrEmpty(square) || square.Length != 2)
                return null;

            int file = square[0] - 'a';
            int rank = square[1] - '1';

            if (file < 0 || file > 7 || rank < 0 || rank > 7)
                return null;

            // FEN is rank 8 to rank 1 (top to bottom)
            string[] ranks = boardFen.Split('/');
            if (ranks.Length != 8)
                return null;

            string fenRank = ranks[7 - rank]; // Convert to FEN index (reversed)

            int currentFile = 0;
            foreach (char c in fenRank)
            {
                if (char.IsDigit(c))
                {
                    currentFile += c - '0';
                }
                else
                {
                    if (currentFile == file)
                        return c;
                    currentFile++;
                }

                if (currentFile > file)
                    break;
            }

            return '.'; // Empty square
        }

        /// <summary>
        /// Returns material value of a piece (pawn = 1, knight/bishop = 3, rook = 5, queen = 9)
        /// </summary>
        private int GetPieceValue(char piece)
        {
            return char.ToLower(piece) switch
            {
                'p' => 1,
                'n' => 3,
                'b' => 3,
                'r' => 5,
                'q' => 9,
                'k' => 0, // King has no material value
                _ => 0
            };
        }

        /// <summary>
        /// Checks if a move is castling.
        /// </summary>
        private bool IsCastling(string move)
        {
            return move == "e1g1" || move == "e1c1" || // White castling
                   move == "e8g8" || move == "e8c8";   // Black castling
        }

        /// <summary>
        /// Counts total pieces on the board.
        /// </summary>
        private int CountPieces(string boardFen)
        {
            int count = 0;
            foreach (char c in boardFen)
            {
                if (char.IsLetter(c) && c != '/')
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Parses evaluation string to numeric value.
        /// </summary>
        private double? ParseEvaluation(string eval)
        {
            if (string.IsNullOrEmpty(eval))
                return null;

            // Handle mate scores
            if (eval.StartsWith("Mate in"))
            {
                if (int.TryParse(eval.Replace("Mate in ", "").Trim(), out int mateIn))
                {
                    // Positive mate = winning, negative = losing
                    return mateIn > 0 ? 100.0 : -100.0;
                }
                return null;
            }

            // Parse centipawn evaluation - use InvariantCulture to match formatting
            string cleanEval = eval.Replace("+", "").Trim();
            if (double.TryParse(cleanEval, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;

            return null;
        }
    }
}
