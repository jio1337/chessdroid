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

                // Store destination square to exclude from "defends piece" detection
                // A piece can't defend itself by moving to a square!
                string destSquare = GetSquareName(destRank, destFile);

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
                // Pass destSquare to exclude the piece that just moved (can't defend itself)
                DetectNewlyDefendedPieces(board, afterMove, movingPlayerIsWhite, destSquare, defenses);

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
            bool weAreWhite, string destSquare, List<Defense> defenses)
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

                    // Skip the destination square - a piece can't defend itself by moving there!
                    if (square == destSquare) continue;

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
        /// A piece is IN DANGER if attacked by a lower-value piece (e.g., bishop attacked by pawn).
        /// </summary>
        private static void DetectEscape(ChessBoard before, ChessBoard after,
            int srcRow, int srcCol, int destRow, int destCol, char piece,
            bool weAreWhite, List<Defense> defenses)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            if (pieceType == PieceType.King) return; // King escapes handled differently

            int pieceValue = ChessUtilities.GetPieceValue(pieceType);
            if (pieceValue < 3) return; // Don't report for pawns

            // Was the piece attacked on its original square?
            bool wasAttacked = IsSquareAttackedBy(before, srcRow, srcCol, !weAreWhite);
            if (!wasAttacked) return;

            // Get the value of their lowest attacker
            int lowestAttackerValue = GetLowestAttackerValue(before, srcRow, srcCol, !weAreWhite);

            // Key insight: If attacked by a lower-value piece, the piece IS in danger
            // regardless of whether it's defended. Example: Bishop (3) attacked by Pawn (1)
            // - If they take, we recapture: they lose 1, we lose 3, net -2 for us
            bool attackedByLowerValue = lowestAttackerValue > 0 && lowestAttackerValue < pieceValue;

            if (!attackedByLowerValue)
            {
                // Attacked by same-value or higher-value piece
                // Check if we have adequate defense
                bool wasDefended = IsSquareAttackedBy(before, srcRow, srcCol, weAreWhite);

                if (wasDefended)
                {
                    int lowestDefenderValue = GetLowestDefenderValue(before, srcRow, srcCol, weAreWhite);

                    // If our defender is worth <= their attacker, we're safe
                    if (lowestDefenderValue <= lowestAttackerValue)
                    {
                        return;
                    }

                    // Also check defender count
                    int defenders = CountDefenders(before, srcRow, srcCol, weAreWhite);
                    int attackers = CountAttackers(before, srcRow, srcCol, !weAreWhite);
                    if (defenders >= attackers)
                    {
                        return;
                    }
                }
            }

            // Piece WAS in danger - check if it's safe now
            bool isNowAttacked = IsSquareAttackedBy(after, destRow, destCol, !weAreWhite);

            if (!isNowAttacked)
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

        /// <summary>
        /// Gets the value of the lowest-value defender of a square
        /// </summary>
        private static int GetLowestDefenderValue(ChessBoard board, int row, int col, bool defendingIsWhite)
        {
            int lowestValue = int.MaxValue;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (r == row && c == col) continue;

                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != defendingIsWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                    {
                        PieceType pieceType = PieceHelper.GetPieceType(piece);
                        int value = ChessUtilities.GetPieceValue(pieceType);
                        lowestValue = Math.Min(lowestValue, value);
                    }
                }
            }

            return lowestValue == int.MaxValue ? 0 : lowestValue;
        }

        /// <summary>
        /// Gets the value of the lowest-value attacker of a square
        /// </summary>
        private static int GetLowestAttackerValue(ChessBoard board, int row, int col, bool attackingIsWhite)
            => ChessUtilities.GetLowestAttackerValue(board, row, col, attackingIsWhite);

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
            else
            {
                // Check if opponent was threatening checkmate before but not anymore
                bool mateThreatenedBefore = IsCheckmateThreatened(before, kingRow, kingCol, weAreWhite);
                bool mateThreatenedAfter = IsCheckmateThreatened(after, kingRow, kingCol, weAreWhite);

                if (mateThreatenedBefore && !mateThreatenedAfter)
                {
                    defenses.Add(new Defense
                    {
                        Description = "stops checkmate threat",
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
        }

        /// <summary>
        /// Checks if opponent has a checkmate threat (can deliver mate on their next move)
        /// Actually simulates opponent moves to find if any delivers checkmate
        /// </summary>
        private static bool IsCheckmateThreatened(ChessBoard board, int kingRow, int kingCol, bool weAreWhite)
        {
            // Check if any opponent piece can move to give checkmate
            // We look for squares adjacent to king (or the king's square itself for discovered checks)
            // where an opponent piece could deliver check AND king has no escape

            // First, find squares where opponent could give check
            var checkSquares = new List<(int row, int col)>();

            // Check squares that would give check to our king
            // This includes the king's adjacent squares and squares on same rank/file/diagonal
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Would a piece on (r,c) attack the king?
                    // Check for opponent pieces that could move here and give check
                    char currentPiece = board.GetPiece(r, c);

                    // Look for opponent pieces
                    if (currentPiece != '.' && char.IsUpper(currentPiece) != weAreWhite)
                    {
                        // This is an opponent piece - can it move somewhere to give check?
                        if (CanPieceDeliverMateInOne(board, r, c, currentPiece, kingRow, kingCol, weAreWhite))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a specific opponent piece can deliver checkmate in one move
        /// </summary>
        private static bool CanPieceDeliverMateInOne(ChessBoard board, int pieceRow, int pieceCol, char piece,
            int kingRow, int kingCol, bool weAreWhite)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);

            // Get all squares this piece could move to
            var possibleMoves = GetPieceMoves(board, pieceRow, pieceCol, piece);

            foreach (var (destRow, destCol) in possibleMoves)
            {
                // Simulate the move
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                tempBoard.SetPiece(destRow, destCol, piece);
                tempBoard.SetPiece(pieceRow, pieceCol, '.');

                // Check if this gives check
                if (!IsSquareAttackedBy(tempBoard, kingRow, kingCol, !weAreWhite))
                {
                    continue; // Doesn't give check
                }

                // It gives check - now check if it's checkmate (king has no escape)
                if (IsCheckmate(tempBoard, kingRow, kingCol, weAreWhite))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets possible move destinations for a piece (simplified, doesn't check all legality)
        /// </summary>
        private static List<(int row, int col)> GetPieceMoves(ChessBoard board, int row, int col, char piece)
        {
            var moves = new List<(int row, int col)>();
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            bool isWhite = char.IsUpper(piece);

            switch (pieceType)
            {
                case PieceType.Queen:
                case PieceType.Rook:
                    // Straight lines
                    AddSlidingMoves(board, row, col, isWhite, new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }, moves);
                    if (pieceType == PieceType.Rook) break;
                    // Fall through for queen to add diagonal
                    goto case PieceType.Bishop;
                case PieceType.Bishop:
                    AddSlidingMoves(board, row, col, isWhite, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }, moves);
                    break;
                case PieceType.Knight:
                    var knightOffsets = new[] { (2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2) };
                    foreach (var (dr, dc) in knightOffsets)
                    {
                        int r = row + dr, c = col + dc;
                        if (r >= 0 && r < 8 && c >= 0 && c < 8)
                        {
                            char target = board.GetPiece(r, c);
                            if (target == '.' || char.IsUpper(target) != isWhite)
                                moves.Add((r, c));
                        }
                    }
                    break;
            }

            return moves;
        }

        /// <summary>
        /// Adds sliding piece moves along given directions
        /// </summary>
        private static void AddSlidingMoves(ChessBoard board, int row, int col, bool isWhite,
            (int dr, int dc)[] directions, List<(int row, int col)> moves)
        {
            foreach (var (dr, dc) in directions)
            {
                int r = row + dr, c = col + dc;
                while (r >= 0 && r < 8 && c >= 0 && c < 8)
                {
                    char target = board.GetPiece(r, c);
                    if (target == '.')
                    {
                        moves.Add((r, c));
                    }
                    else
                    {
                        if (char.IsUpper(target) != isWhite)
                            moves.Add((r, c)); // Can capture
                        break; // Blocked
                    }
                    r += dr;
                    c += dc;
                }
            }
        }

        /// <summary>
        /// Checks if the position is checkmate for the given king
        /// </summary>
        private static bool IsCheckmate(ChessBoard board, int kingRow, int kingCol, bool weAreWhite)
        {
            // King must be in check
            if (!IsSquareAttackedBy(board, kingRow, kingCol, !weAreWhite))
                return false;

            // Check if king can escape
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int r = kingRow + dr;
                    int c = kingCol + dc;
                    if (r < 0 || r >= 8 || c < 0 || c >= 8) continue;

                    char sq = board.GetPiece(r, c);
                    // Can't move to square with our own piece
                    if (sq != '.' && char.IsUpper(sq) == weAreWhite) continue;

                    // Simulate king move
                    ChessBoard tempBoard = new ChessBoard(board.GetArray());
                    char king = weAreWhite ? 'K' : 'k';
                    tempBoard.SetPiece(r, c, king);
                    tempBoard.SetPiece(kingRow, kingCol, '.');

                    // Check if king is safe there
                    if (!IsSquareAttackedBy(tempBoard, r, c, !weAreWhite))
                    {
                        return false; // King can escape
                    }
                }
            }

            // King can't move - check if we can capture the attacker
            // Find the attacking piece(s)
            var attackers = FindAttackers(board, kingRow, kingCol, !weAreWhite);

            // If there's only one attacker, check if we can capture it
            if (attackers.Count == 1)
            {
                var (attackerRow, attackerCol) = attackers[0];

                // Can any of our pieces (other than king) capture the attacker?
                if (IsSquareAttackedBy(board, attackerRow, attackerCol, weAreWhite))
                {
                    // We can capture the attacker - not checkmate
                    return false;
                }

                // Can we block the check? (only relevant for sliding pieces)
                char attackerPiece = board.GetPiece(attackerRow, attackerCol);
                PieceType attackerType = PieceHelper.GetPieceType(attackerPiece);

                if (attackerType == PieceType.Bishop || attackerType == PieceType.Rook || attackerType == PieceType.Queen)
                {
                    // Check if any of our pieces can block
                    if (CanBlockCheck(board, kingRow, kingCol, attackerRow, attackerCol, weAreWhite))
                    {
                        return false; // We can block - not checkmate
                    }
                }
            }
            // If there are multiple attackers (double check), only king moves work, which we already checked

            return true;
        }

        /// <summary>
        /// Find all pieces attacking a square
        /// </summary>
        private static List<(int row, int col)> FindAttackers(ChessBoard board, int targetRow, int targetCol, bool attackerIsWhite)
        {
            var attackers = new List<(int row, int col)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;
                    if (char.IsUpper(piece) != attackerIsWhite) continue;

                    if (ChessUtilities.CanAttackSquare(board, r, c, piece, targetRow, targetCol))
                    {
                        attackers.Add((r, c));
                    }
                }
            }

            return attackers;
        }

        /// <summary>
        /// Check if any of our pieces can block a check from a sliding piece
        /// </summary>
        private static bool CanBlockCheck(ChessBoard board, int kingRow, int kingCol, int attackerRow, int attackerCol, bool weAreWhite)
        {
            // Get squares between attacker and king
            var squaresBetween = GetSquaresBetween(attackerRow, attackerCol, kingRow, kingCol);

            foreach (var (blockRow, blockCol) in squaresBetween)
            {
                // Can any of our pieces move to this square to block?
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;
                        if (char.IsUpper(piece) != weAreWhite) continue;
                        if (PieceHelper.GetPieceType(piece) == PieceType.King) continue; // King can't block

                        // Can this piece move to the blocking square?
                        if (CanPieceMoveToSquare(board, r, c, piece, blockRow, blockCol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get squares between two points (exclusive of endpoints)
        /// </summary>
        private static List<(int row, int col)> GetSquaresBetween(int r1, int c1, int r2, int c2)
        {
            var squares = new List<(int row, int col)>();

            int dr = Math.Sign(r2 - r1);
            int dc = Math.Sign(c2 - c1);

            int r = r1 + dr;
            int c = c1 + dc;

            while (r != r2 || c != c2)
            {
                squares.Add((r, c));
                r += dr;
                c += dc;
            }

            return squares;
        }

        /// <summary>
        /// Check if a piece can legally move to a target square (simplified)
        /// </summary>
        private static bool CanPieceMoveToSquare(ChessBoard board, int pieceRow, int pieceCol, char piece, int targetRow, int targetCol)
        {
            // Target must be empty for blocking
            if (board.GetPiece(targetRow, targetCol) != '.') return false;

            PieceType pieceType = PieceHelper.GetPieceType(piece);
            bool isWhite = char.IsUpper(piece);

            switch (pieceType)
            {
                case PieceType.Pawn:
                    int direction = isWhite ? -1 : 1;
                    // Single push
                    if (pieceCol == targetCol && pieceRow + direction == targetRow)
                        return true;
                    // Double push from starting rank
                    int startRank = isWhite ? 6 : 1;
                    if (pieceCol == targetCol && pieceRow == startRank && pieceRow + 2 * direction == targetRow)
                    {
                        // Check if path is clear
                        if (board.GetPiece(pieceRow + direction, pieceCol) == '.')
                            return true;
                    }
                    return false;

                case PieceType.Knight:
                    int dr = Math.Abs(targetRow - pieceRow);
                    int dc = Math.Abs(targetCol - pieceCol);
                    return (dr == 2 && dc == 1) || (dr == 1 && dc == 2);

                case PieceType.Bishop:
                case PieceType.Rook:
                case PieceType.Queen:
                    return ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, targetRow, targetCol);

                default:
                    return false;
            }
        }

        #region Helper Methods

        private static string GetSquareName(int row, int col) => ChessUtilities.GetSquareName(row, col);

        private static bool IsSquareAttackedBy(ChessBoard board, int row, int col, bool byWhite)
            => ChessUtilities.IsSquareAttackedBy(board, row, col, byWhite);

        private static int CountDefenders(ChessBoard board, int row, int col, bool defendingIsWhite)
            => ChessUtilities.CountDefenders(board, row, col, defendingIsWhite);

        private static int CountAttackers(ChessBoard board, int row, int col, bool attackingIsWhite)
            => ChessUtilities.CountAttackers(board, row, col, attackingIsWhite);

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