using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Shared chess utility functions used across multiple services.
    /// Consolidates duplicate code for piece movement, attack detection, and board analysis.
    /// </summary>
    public static class ChessUtilities
    {
        // =============================
        // PIECE VALUE AND NAMING
        // =============================

        /// <summary>
        /// Get material value of piece type
        /// </summary>
        public static int GetPieceValue(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 3,
                PieceType.Bishop => 3,
                PieceType.Rook => 5,
                PieceType.Queen => 9,
                PieceType.King => 100,
                _ => 0
            };
        }

        /// <summary>
        /// Get human-readable name of piece type
        /// </summary>
        public static string GetPieceName(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => "pawn",
                PieceType.Knight => "knight",
                PieceType.Bishop => "bishop",
                PieceType.Rook => "rook",
                PieceType.Queen => "queen",
                PieceType.King => "king",
                _ => "piece"
            };
        }

        // =============================
        // ATTACK AND DEFENSE DETECTION
        // =============================

        /// <summary>
        /// Check if a piece at (fromRow, fromCol) can attack a square at (toRow, toCol)
        /// </summary>
        public static bool CanAttackSquare(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            if (fromRow == toRow && fromCol == toCol)
                return false;

            PieceType pieceType = PieceHelper.GetPieceType(piece);

            switch (pieceType)
            {
                case PieceType.Pawn:
                    bool isWhite = char.IsUpper(piece);
                    int direction = isWhite ? -1 : 1;
                    // Pawns attack diagonally
                    return (toRow == fromRow + direction) && Math.Abs(toCol - fromCol) == 1;

                case PieceType.Knight:
                    int rowDiff = Math.Abs(toRow - fromRow);
                    int colDiff = Math.Abs(toCol - fromCol);
                    return (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);

                case PieceType.Bishop:
                    return IsDiagonalClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Rook:
                    return IsLineClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Queen:
                    return IsDiagonalClear(board, fromRow, fromCol, toRow, toCol) ||
                           IsLineClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.King:
                    return Math.Abs(toRow - fromRow) <= 1 && Math.Abs(toCol - fromCol) <= 1;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if a square is attacked/defended by a specific side
        /// </summary>
        public static bool IsSquareAttackedBy(ChessBoard board, int row, int col, bool byWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (CanAttackSquare(board, r, c, piece, row, col))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Alias for IsSquareAttackedBy - check if a square is defended by a specific side
        /// </summary>
        public static bool IsSquareDefended(ChessBoard board, int row, int col, bool byWhite)
            => IsSquareAttackedBy(board, row, col, byWhite);

        // =============================
        // PATH VALIDATION
        // =============================

        /// <summary>
        /// Check if diagonal path is clear between two squares
        /// </summary>
        public static bool IsDiagonalClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowDiff = Math.Abs(toRow - fromRow);
            int colDiff = Math.Abs(toCol - fromCol);

            // Not a diagonal
            if (rowDiff != colDiff || rowDiff == 0)
                return false;

            int rowStep = toRow > fromRow ? 1 : -1;
            int colStep = toCol > fromCol ? 1 : -1;

            int r = fromRow + rowStep;
            int c = fromCol + colStep;

            while (r != toRow && c != toCol)
            {
                if (board.GetPiece(r, c) != '.')
                    return false;

                r += rowStep;
                c += colStep;
            }

            return true;
        }

        /// <summary>
        /// Check if straight line path is clear between two squares
        /// </summary>
        public static bool IsLineClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            // Must be same row or same column
            if (fromRow != toRow && fromCol != toCol)
                return false;

            if (fromRow == toRow && fromCol == toCol)
                return false;

            int rowStep = 0;
            int colStep = 0;

            if (fromRow == toRow)
            {
                // Horizontal movement
                colStep = toCol > fromCol ? 1 : -1;
            }
            else
            {
                // Vertical movement
                rowStep = toRow > fromRow ? 1 : -1;
            }

            int r = fromRow + rowStep;
            int c = fromCol + colStep;

            while (r != toRow || c != toCol)
            {
                if (board.GetPiece(r, c) != '.')
                    return false;

                r += rowStep;
                c += colStep;
            }

            return true;
        }

        // =============================
        // BOARD ANALYSIS HELPERS
        // =============================

        /// <summary>
        /// Convert row/col indices to algebraic notation (e.g., 0,4 -> "e8")
        /// </summary>
        public static string GetSquareName(int row, int col)
        {
            char file = (char)('a' + col);
            int rank = 8 - row;
            return $"{file}{rank}";
        }

        /// <summary>
        /// Count number of attackers on a square
        /// </summary>
        public static int CountAttackers(ChessBoard board, int row, int col, bool byWhite)
        {
            int count = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (CanAttackSquare(board, r, c, piece, row, col))
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Count number of defenders of a square (excludes the piece on the square itself)
        /// </summary>
        public static int CountDefenders(ChessBoard board, int row, int col, bool byWhite)
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
                    if (pieceIsWhite != byWhite) continue;

                    if (CanAttackSquare(board, r, c, piece, row, col))
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Get the value of the lowest-value attacker of a square
        /// Returns 0 if no attackers
        /// </summary>
        public static int GetLowestAttackerValue(ChessBoard board, int row, int col, bool attackerIsWhite)
        {
            int lowestValue = int.MaxValue;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != attackerIsWhite) continue;

                    if (CanAttackSquare(board, r, c, piece, row, col))
                    {
                        int value = GetPieceValue(PieceHelper.GetPieceType(piece));
                        if (value < lowestValue)
                            lowestValue = value;
                    }
                }
            }

            return lowestValue == int.MaxValue ? 0 : lowestValue;
        }

        // =============================
        // BLOCKING DETECTION
        // =============================

        /// <summary>
        /// Check if a sliding piece attack can be blocked by interposing a piece.
        /// For bishop, rook, and queen attacks, a defender can place a piece between
        /// the attacker and target to block the threat.
        /// </summary>
        /// <param name="board">Current board position</param>
        /// <param name="targetRow">Row of the piece being attacked</param>
        /// <param name="targetCol">Column of the piece being attacked</param>
        /// <param name="defenderIsWhite">Color of the defending side (same as the target piece)</param>
        /// <returns>True if the attack can be blocked</returns>
        public static bool CanBlockSlidingAttack(ChessBoard board, int targetRow, int targetCol, bool defenderIsWhite)
        {
            // Find all attackers of the target square
            var attackers = FindAttackers(board, targetRow, targetCol, !defenderIsWhite);

            foreach (var (attackerRow, attackerCol, attackerPiece) in attackers)
            {
                PieceType attackerType = PieceHelper.GetPieceType(attackerPiece);

                // Only sliding pieces (bishop, rook, queen) can be blocked
                // Knight attacks cannot be blocked
                if (attackerType != PieceType.Bishop && attackerType != PieceType.Rook && attackerType != PieceType.Queen)
                    continue;

                // Get squares between attacker and target
                var blockingSquares = GetSquaresBetween(attackerRow, attackerCol, targetRow, targetCol);

                if (blockingSquares.Count == 0)
                    continue; // Adjacent attack, can't be blocked

                // Check if any defender piece can move to a blocking square
                foreach (var (blockRow, blockCol) in blockingSquares)
                {
                    if (CanDefenderReachSquare(board, blockRow, blockCol, defenderIsWhite))
                    {
                        return true; // Found a way to block this attacker
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Find all pieces of the specified color that attack a given square.
        /// </summary>
        private static List<(int row, int col, char piece)> FindAttackers(ChessBoard board, int targetRow, int targetCol, bool attackerIsWhite)
        {
            var attackers = new List<(int row, int col, char piece)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != attackerIsWhite) continue;

                    if (CanAttackSquare(board, r, c, piece, targetRow, targetCol))
                    {
                        attackers.Add((r, c, piece));
                    }
                }
            }

            return attackers;
        }

        /// <summary>
        /// Get all squares between two points (exclusive of both endpoints).
        /// Only works for straight lines (horizontal, vertical, diagonal).
        /// </summary>
        private static List<(int row, int col)> GetSquaresBetween(int fromRow, int fromCol, int toRow, int toCol)
        {
            var squares = new List<(int, int)>();

            int rowDir = Math.Sign(toRow - fromRow);
            int colDir = Math.Sign(toCol - fromCol);

            // Must be a straight line (rook) or diagonal (bishop)
            int rowDiff = Math.Abs(toRow - fromRow);
            int colDiff = Math.Abs(toCol - fromCol);

            if (rowDiff != colDiff && rowDiff != 0 && colDiff != 0)
                return squares; // Not a valid line

            int r = fromRow + rowDir;
            int c = fromCol + colDir;

            while (r != toRow || c != toCol)
            {
                squares.Add((r, c));
                r += rowDir;
                c += colDir;

                // Safety check to prevent infinite loop
                if (squares.Count > 7) break;
            }

            return squares;
        }

        /// <summary>
        /// Check if any piece of the specified color can move to the given square.
        /// </summary>
        private static bool CanDefenderReachSquare(ChessBoard board, int targetRow, int targetCol, bool defenderIsWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != defenderIsWhite) continue;

                    PieceType pieceType = PieceHelper.GetPieceType(piece);

                    // Don't count the king as a blocker (too valuable/risky)
                    if (pieceType == PieceType.King) continue;

                    // Check if this piece can legally move to the blocking square
                    if (CanPieceMoveToSquare(board, r, c, piece, targetRow, targetCol, defenderIsWhite))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a specific piece can move to a given square (for blocking).
        /// </summary>
        private static bool CanPieceMoveToSquare(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol, bool pieceIsWhite)
        {
            // Target square must be empty (we're blocking, not capturing)
            if (board.GetPiece(toRow, toCol) != '.')
                return false;

            PieceType pieceType = PieceHelper.GetPieceType(piece);

            switch (pieceType)
            {
                case PieceType.Pawn:
                    int direction = pieceIsWhite ? -1 : 1;
                    int startRank = pieceIsWhite ? 6 : 1;

                    // Single push
                    if (fromCol == toCol && fromRow + direction == toRow)
                        return true;

                    // Double push from starting position
                    if (fromCol == toCol && fromRow == startRank && fromRow + 2 * direction == toRow)
                    {
                        // Check that the intermediate square is empty
                        if (board.GetPiece(fromRow + direction, fromCol) == '.')
                            return true;
                    }
                    return false;

                case PieceType.Knight:
                    int rowDiff = Math.Abs(toRow - fromRow);
                    int colDiff = Math.Abs(toCol - fromCol);
                    return (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);

                case PieceType.Bishop:
                    if (Math.Abs(toRow - fromRow) != Math.Abs(toCol - fromCol))
                        return false;
                    return IsPathClearForBlock(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Rook:
                    if (toRow != fromRow && toCol != fromCol)
                        return false;
                    return IsPathClearForBlock(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Queen:
                    bool isDiagonal = Math.Abs(toRow - fromRow) == Math.Abs(toCol - fromCol);
                    bool isStraight = toRow == fromRow || toCol == fromCol;
                    if (!isDiagonal && !isStraight)
                        return false;
                    return IsPathClearForBlock(board, fromRow, fromCol, toRow, toCol);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if the path between two squares is clear (no pieces in the way).
        /// </summary>
        private static bool IsPathClearForBlock(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowDir = Math.Sign(toRow - fromRow);
            int colDir = Math.Sign(toCol - fromCol);

            int r = fromRow + rowDir;
            int c = fromCol + colDir;

            while (r != toRow || c != toCol)
            {
                if (board.GetPiece(r, c) != '.')
                    return false;
                r += rowDir;
                c += colDir;
            }

            return true;
        }

        // =============================
        // CHECK DETECTION
        // =============================

        /// <summary>
        /// Checks if a piece on the given square is giving check to the enemy king.
        /// </summary>
        public static bool IsGivingCheck(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            // Find enemy king
            char enemyKing = isWhite ? 'k' : 'K';

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == enemyKing)
                    {
                        return CanAttackSquare(board, pieceRow, pieceCol, piece, r, c);
                    }
                }
            }

            return false;
        }

        // =============================
        // PHANTOM THREAT DETECTION
        // =============================

        /// <summary>
        /// Checks if the moving piece gives check AND will be immediately recaptured.
        /// In such cases, tactical threats (skewers, forks, mate threats) are "phantom" - they don't survive.
        /// Example: Qxd8+ where the queen captures with check but will be recaptured.
        /// Example: Bxc6+ where a pawn can simply recapture - fork on king+knight is phantom.
        /// </summary>
        public static bool IsPieceImmediatelyRecapturable(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            // Check if we're giving check
            bool givesCheck = IsGivingCheck(board, pieceRow, pieceCol, piece, isWhite);
            if (!givesCheck) return false;

            // Check if our piece can be captured by the opponent
            bool canBeRecaptured = IsSquareDefended(board, pieceRow, pieceCol, !isWhite);
            if (!canBeRecaptured) return false;

            // IMPORTANT: If a PAWN can recapture, it's almost always going to happen.
            // Pawn recapture is "free" - opponent wins material AND escapes check.
            // This covers cases like Bxc6+ bxc6 where the "fork" is phantom.
            char enemyPawn = isWhite ? 'p' : 'P';
            // Black pawns (capturing white pieces) are ABOVE our piece (lower row index) and capture downward
            // White pawns (capturing black pieces) are BELOW our piece (higher row index) and capture upward
            int pawnCaptureDir = isWhite ? -1 : 1; // Where enemy pawn would be relative to our piece

            // Check if an enemy pawn can capture our piece
            int pawnRow = pieceRow + pawnCaptureDir;
            if (pawnRow >= 0 && pawnRow < 8)
            {
                // Check both diagonal squares where a pawn could be
                for (int dc = -1; dc <= 1; dc += 2)
                {
                    int pawnCol = pieceCol + dc;
                    if (pawnCol >= 0 && pawnCol < 8)
                    {
                        if (board.GetPiece(pawnRow, pawnCol) == enemyPawn)
                        {
                            // A pawn can recapture - this is a phantom threat
                            return true;
                        }
                    }
                }
            }

            // Find enemy king to check escape squares
            char enemyKing = isWhite ? 'k' : 'K';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == enemyKing)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow >= 0) break;
            }

            if (kingRow < 0) return false;

            // Check if king can escape to a safe square (not capturing our piece)
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int newRow = kingRow + dr;
                    int newCol = kingCol + dc;
                    if (newRow < 0 || newRow >= 8 || newCol < 0 || newCol >= 8) continue;

                    // Skip if this is our piece's square (that would be capturing, not escaping)
                    if (newRow == pieceRow && newCol == pieceCol) continue;

                    char targetSquare = board.GetPiece(newRow, newCol);
                    // Can't move to square with own piece
                    if (targetSquare != '.' && char.IsUpper(targetSquare) == !isWhite) continue;

                    // Check if square is safe (simulate king move)
                    ChessBoard tempBoard = new ChessBoard(board.GetArray());
                    tempBoard.SetPiece(kingRow, kingCol, '.');
                    tempBoard.SetPiece(newRow, newCol, enemyKing);

                    if (!IsSquareAttackedBy(tempBoard, newRow, newCol, isWhite))
                    {
                        // King has an escape square - our piece might survive
                        return false;
                    }
                }
            }

            // Check if check can be blocked (only matters for sliding pieces)
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            if (pieceType == PieceType.Bishop || pieceType == PieceType.Rook || pieceType == PieceType.Queen)
            {
                // Can a piece block between our piece and their king?
                if (CanBlockSlidingAttack(board, kingRow, kingCol, !isWhite))
                {
                    // Check can be blocked - our piece might survive
                    return false;
                }
            }

            // King can't escape and can't block - must capture our piece
            return true;
        }
    }
}