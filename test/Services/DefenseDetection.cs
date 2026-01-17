using ChessDroid.Models;
using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// Detects defensive aspects of chess moves.
    ///
    /// Types of defense detected:
    /// 1. Direct Defense: Moving a piece to defend an attacked piece
    /// 2. Blocking: Interposing a piece between attacker and target
    /// 3. Escape: Moving an attacked piece to safety
    /// 4. Counter-attack: Creating a threat that forces opponent to respond
    /// 5. Protecting weaknesses: Defending pawns or squares under pressure
    /// </summary>
    public static class DefenseDetection
    {
        /// <summary>
        /// Represents a detected defensive action
        /// </summary>
        public class Defense
        {
            public string Description { get; set; } = "";
            public DefenseType Type { get; set; }
            public int Importance { get; set; } // 1-5, higher = more important
            public string Square { get; set; } = ""; // e.g., "e4"
        }

        public enum DefenseType
        {
            ProtectPiece,       // Adds a defender to an attacked piece
            ProtectPawn,        // Adds a defender to an attacked pawn
            BlockAttack,        // Interposes between attacker and target
            Escape,             // Moves attacked piece to safety
            CounterAttack,      // Creates threat that deflects opponent's attack
            CoverWeakness,      // Defends a weak square or pawn
            ProtectKing,        // Improves king safety
            PreventThreat       // Prophylactic defense against potential threats
        }

        /// <summary>
        /// Analyzes what our move DEFENDS.
        /// Compares position before and after the move to find pieces that were
        /// attacked before but are now defended or safe.
        /// </summary>
        public static List<Defense> AnalyzeDefensesAfterMove(ChessBoard board, string move, bool movingPlayerIsWhite)
        {
            var defenses = new List<Defense>();

            try
            {
                // Parse the move
                if (move.Length < 4) return defenses;

                int srcFile = move[0] - 'a';
                int srcRank = 8 - (move[1] - '0');
                int destFile = move[2] - 'a';
                int destRank = 8 - (move[3] - '0');

                char movingPiece = board.GetPiece(srcRank, srcFile);

                // Create board after the move
                ChessBoard afterMove = new ChessBoard(board.GetArray());
                afterMove.SetPiece(destRank, destFile, movingPiece);
                afterMove.SetPiece(srcRank, srcFile, '.');

                // Handle promotion
                if (move.Length > 4)
                {
                    char promotionPiece = movingPlayerIsWhite ? char.ToUpper(move[4]) : char.ToLower(move[4]);
                    afterMove.SetPiece(destRank, destFile, promotionPiece);
                }

                // 1. Check if any of our pieces were attacked before and are now defended
                DetectNewlyDefendedPieces(board, afterMove, movingPlayerIsWhite, defenses);

                // 2. Check if the moving piece was escaping an attack
                DetectEscape(board, afterMove, srcRank, srcFile, destRank, destFile, movingPiece, movingPlayerIsWhite, defenses);

                // 3. Check for blocking attacks (interposing)
                DetectBlocking(board, afterMove, destRank, destFile, movingPiece, movingPlayerIsWhite, defenses);

                // 4. Check for king safety improvements
                DetectKingSafetyImprovement(board, afterMove, movingPlayerIsWhite, defenses);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DefenseDetection error: {ex.Message}");
            }

            // Sort by importance (highest first) and deduplicate
            return defenses
                .GroupBy(d => d.Description)
                .Select(g => g.First())
                .OrderByDescending(d => d.Importance)
                .Take(2) // Limit to 2 most important defenses
                .ToList();
        }

        /// <summary>
        /// Detects pieces that were attacked (and undefended or under-defended) before
        /// the move but are now defended after the move.
        /// </summary>
        private static void DetectNewlyDefendedPieces(ChessBoard before, ChessBoard after,
            bool weAreWhite, List<Defense> defenses)
        {
            // Check each of our pieces
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = after.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != weAreWhite) continue; // Only check our pieces

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    if (pieceType == PieceType.King) continue; // King handled separately

                    string square = GetSquareName(r, c);

                    // Was this piece attacked before?
                    bool wasAttackedBefore = IsSquareAttackedBy(before, r, c, !weAreWhite);
                    if (!wasAttackedBefore) continue;

                    // Was it undefended or under-defended before?
                    bool wasDefendedBefore = IsSquareAttackedBy(before, r, c, weAreWhite);
                    int defendersBefore = CountDefenders(before, r, c, weAreWhite);
                    int attackersBefore = CountAttackers(before, r, c, !weAreWhite);

                    // Is it defended now?
                    int defendersAfter = CountDefenders(after, r, c, weAreWhite);
                    int attackersAfter = CountAttackers(after, r, c, !weAreWhite);

                    // Check if defense improved
                    bool wasVulnerable = !wasDefendedBefore || attackersBefore > defendersBefore;
                    bool isNowSafe = defendersAfter > 0 && defendersAfter >= attackersAfter;

                    if (wasVulnerable && isNowSafe && defendersAfter > defendersBefore)
                    {
                        string pieceName = ChessUtilities.GetPieceName(pieceType);
                        int pieceValue = ChessUtilities.GetPieceValue(pieceType);

                        DefenseType defType = pieceType == PieceType.Pawn
                            ? DefenseType.ProtectPawn
                            : DefenseType.ProtectPiece;

                        defenses.Add(new Defense
                        {
                            Description = $"defends {pieceName} on {square}",
                            Type = defType,
                            Importance = Math.Min(pieceValue, 5),
                            Square = square
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detects if the moving piece was escaping an attack.
        /// </summary>
        private static void DetectEscape(ChessBoard before, ChessBoard after,
            int srcRow, int srcCol, int destRow, int destCol, char piece,
            bool weAreWhite, List<Defense> defenses)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            if (pieceType == PieceType.King) return; // King escapes handled differently

            // Was the piece attacked on its original square?
            bool wasAttacked = IsSquareAttackedBy(before, srcRow, srcCol, !weAreWhite);
            if (!wasAttacked) return;

            // Is the piece safe on its new square?
            bool isNowAttacked = IsSquareAttackedBy(after, destRow, destCol, !weAreWhite);
            bool isNowDefended = IsSquareAttackedBy(after, destRow, destCol, weAreWhite);

            if (!isNowAttacked || (isNowDefended && !wasAttacked))
            {
                // Piece escaped to safety
                // Note: We don't add this as it would be redundant with the move itself
                // Only add if it was a valuable piece that escaped
                int pieceValue = ChessUtilities.GetPieceValue(pieceType);
                if (pieceValue >= 3 && !isNowAttacked)
                {
                    string pieceName = ChessUtilities.GetPieceName(pieceType);
                    defenses.Add(new Defense
                    {
                        Description = $"saves {pieceName}",
                        Type = DefenseType.Escape,
                        Importance = Math.Min(pieceValue - 1, 4),
                        Square = GetSquareName(destRow, destCol)
                    });
                }
            }
        }

        /// <summary>
        /// Detects if the move blocks an attack on a valuable piece.
        /// </summary>
        private static void DetectBlocking(ChessBoard before, ChessBoard after,
            int destRow, int destCol, char movingPiece, bool weAreWhite, List<Defense> defenses)
        {
            // Check if placing our piece on this square blocks an attack on another piece
            // Look for our valuable pieces that were under attack by sliding pieces

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = before.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != weAreWhite) continue;

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    int pieceValue = pieceType == PieceType.King ? 100 : ChessUtilities.GetPieceValue(pieceType);
                    if (pieceValue < 3 && pieceType != PieceType.King) continue;

                    // Was this piece attacked before by a sliding piece through our destination square?
                    bool wasAttackedThrough = WasAttackedThroughSquare(before, r, c, destRow, destCol, !weAreWhite);
                    if (!wasAttackedThrough) continue;

                    // Is the attack now blocked?
                    bool stillAttacked = IsSquareAttackedBy(after, r, c, !weAreWhite);
                    bool wasAttacked = IsSquareAttackedBy(before, r, c, !weAreWhite);

                    if (wasAttacked && !stillAttacked)
                    {
                        string pieceName = pieceType == PieceType.King ? "king" : ChessUtilities.GetPieceName(pieceType);
                        string square = GetSquareName(r, c);

                        defenses.Add(new Defense
                        {
                            Description = $"blocks attack on {pieceName}",
                            Type = DefenseType.BlockAttack,
                            Importance = Math.Min(pieceValue / 2 + 1, 4),
                            Square = square
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Detects if the move improves king safety.
        /// </summary>
        private static void DetectKingSafetyImprovement(ChessBoard before, ChessBoard after,
            bool weAreWhite, List<Defense> defenses)
        {
            // Find our king
            char ourKing = weAreWhite ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (after.GetPiece(r, c) == ourKing)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow >= 0) break;
            }

            if (kingRow < 0) return;

            // Count attackers on king or adjacent squares before and after
            int attackersBefore = CountAttackersAroundKing(before, kingRow, kingCol, !weAreWhite);
            int attackersAfter = CountAttackersAroundKing(after, kingRow, kingCol, !weAreWhite);

            // Check if king was in check before
            bool wasInCheck = IsSquareAttackedBy(before, kingRow, kingCol, !weAreWhite);
            bool isInCheck = IsSquareAttackedBy(after, kingRow, kingCol, !weAreWhite);

            if (wasInCheck && !isInCheck)
            {
                defenses.Add(new Defense
                {
                    Description = "gets out of check",
                    Type = DefenseType.ProtectKing,
                    Importance = 5,
                    Square = GetSquareName(kingRow, kingCol)
                });
            }
            else if (attackersAfter < attackersBefore && attackersBefore >= 2)
            {
                defenses.Add(new Defense
                {
                    Description = "improves king safety",
                    Type = DefenseType.ProtectKing,
                    Importance = 3,
                    Square = GetSquareName(kingRow, kingCol)
                });
            }
        }

        #region Helper Methods

        private static string GetSquareName(int row, int col)
        {
            char file = (char)('a' + col);
            int rank = 8 - row;
            return $"{file}{rank}";
        }

        private static bool IsSquareAttackedBy(ChessBoard board, int row, int col, bool byWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
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

        private static int CountDefenders(ChessBoard board, int row, int col, bool defendingIsWhite)
        {
            int count = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (r == row && c == col) continue; // Don't count the piece itself

                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != defendingIsWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        count++;
                }
            }
            return count;
        }

        private static int CountAttackers(ChessBoard board, int row, int col, bool attackingIsWhite)
        {
            int count = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != attackingIsWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        count++;
                }
            }
            return count;
        }

        private static int CountAttackersAroundKing(ChessBoard board, int kingRow, int kingCol, bool attackingIsWhite)
        {
            int count = 0;

            // Check king square and all adjacent squares
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int r = kingRow + dr;
                    int c = kingCol + dc;
                    if (r < 0 || r >= 8 || c < 0 || c >= 8) continue;

                    if (IsSquareAttackedBy(board, r, c, attackingIsWhite))
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Checks if a piece at (targetRow, targetCol) was being attacked by a sliding piece
        /// through the square (blockRow, blockCol).
        /// </summary>
        private static bool WasAttackedThroughSquare(ChessBoard board, int targetRow, int targetCol,
            int blockRow, int blockCol, bool byWhite)
        {
            // Check if blockSquare is on a line between targetSquare and any enemy sliding piece
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    PieceType pieceType = PieceHelper.GetPieceType(piece);
                    if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                        continue;

                    // Check if this piece attacks the target through the block square
                    if (IsOnLineBetween(r, c, blockRow, blockCol, targetRow, targetCol, pieceType))
                    {
                        // Verify the piece could actually attack along this line
                        if (CanSlidingPieceAttackThrough(board, r, c, blockRow, blockCol, targetRow, targetCol, piece))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsOnLineBetween(int attackerRow, int attackerCol, int blockRow, int blockCol,
            int targetRow, int targetCol, PieceType pieceType)
        {
            // Check if block square is on the line between attacker and target
            int dRowAT = targetRow - attackerRow;
            int dColAT = targetCol - attackerCol;
            int dRowAB = blockRow - attackerRow;
            int dColAB = blockCol - attackerCol;

            // Normalize directions
            int dirRowAT = dRowAT == 0 ? 0 : dRowAT / Math.Abs(dRowAT);
            int dirColAT = dColAT == 0 ? 0 : dColAT / Math.Abs(dColAT);
            int dirRowAB = dRowAB == 0 ? 0 : dRowAB / Math.Abs(dRowAB);
            int dirColAB = dColAB == 0 ? 0 : dColAB / Math.Abs(dColAB);

            // Must be same direction
            if (dirRowAT != dirRowAB || dirColAT != dirColAB) return false;

            // Check piece type can move in this direction
            bool isDiagonal = dirRowAT != 0 && dirColAT != 0;
            bool isStraight = dirRowAT == 0 || dirColAT == 0;

            if (pieceType == PieceType.Bishop && !isDiagonal) return false;
            if (pieceType == PieceType.Rook && !isStraight) return false;

            // Block must be between attacker and target
            int distToBlock = Math.Max(Math.Abs(dRowAB), Math.Abs(dColAB));
            int distToTarget = Math.Max(Math.Abs(dRowAT), Math.Abs(dColAT));

            return distToBlock < distToTarget && distToBlock > 0;
        }

        private static bool CanSlidingPieceAttackThrough(ChessBoard board, int attackerRow, int attackerCol,
            int blockRow, int blockCol, int targetRow, int targetCol, char piece)
        {
            int dRow = targetRow - attackerRow;
            int dCol = targetCol - attackerCol;
            int dirRow = dRow == 0 ? 0 : dRow / Math.Abs(dRow);
            int dirCol = dCol == 0 ? 0 : dCol / Math.Abs(dCol);

            // Walk from attacker toward target, should reach block square with no pieces in between
            int r = attackerRow + dirRow;
            int c = attackerCol + dirCol;

            while (r != blockRow || c != blockCol)
            {
                if (r < 0 || r >= 8 || c < 0 || c >= 8) return false;

                char sq = board.GetPiece(r, c);
                if (sq != '.') return false; // Piece in the way before block square

                r += dirRow;
                c += dirCol;
            }

            // Continue from block to target (block square should be empty for this to be a blocking move)
            // Since we're checking BEFORE the move, the block square should be empty
            r = blockRow + dirRow;
            c = blockCol + dirCol;

            while (r != targetRow || c != targetCol)
            {
                if (r < 0 || r >= 8 || c < 0 || c >= 8) return false;

                char sq = board.GetPiece(r, c);
                if (sq != '.') return false; // Piece in the way after block square

                r += dirRow;
                c += dirCol;
            }

            return true;
        }

        #endregion Helper Methods
    }
}