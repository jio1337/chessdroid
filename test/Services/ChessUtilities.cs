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
        /// Check if a square is defended by a specific side
        /// </summary>
        public static bool IsSquareDefended(ChessBoard board, int row, int col, bool byWhite)
        {
            try
            {
                // Check all possible piece types that could defend this square
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        bool pieceIsWhite = char.IsUpper(piece);
                        if (pieceIsWhite != byWhite) continue;

                        // Check if this piece can attack the target square
                        if (CanAttackSquare(board, r, c, piece, row, col))
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

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

        /// <summary>
        /// Check if path is clear (diagonal OR straight line)
        /// </summary>
        public static bool IsPathClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            return IsDiagonalClear(board, fromRow, fromCol, toRow, toCol) ||
                   IsLineClear(board, fromRow, fromCol, toRow, toCol);
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
        /// Check if a piece is pinned
        /// </summary>
        public static bool IsPiecePinned(ChessBoard board, int row, int col)
        {
            char piece = board.GetPiece(row, col);
            if (piece == '.') return false;

            bool isWhite = char.IsUpper(piece);

            // Find king position
            char king = isWhite ? 'K' : 'k';
            int kingRow = -1, kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(r, c) == king)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
                if (kingRow != -1) break;
            }

            if (kingRow == -1) return false;

            // Check if piece is on a line with the king
            bool onSameLine = (row == kingRow || col == kingCol);
            bool onSameDiagonal = (Math.Abs(row - kingRow) == Math.Abs(col - kingCol));

            if (!onSameLine && !onSameDiagonal)
                return false;

            // Check if enemy long-range piece is attacking through this piece
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char enemyPiece = board.GetPiece(r, c);
                    if (enemyPiece == '.') continue;

                    bool enemyIsWhite = char.IsUpper(enemyPiece);
                    if (enemyIsWhite == isWhite) continue;

                    PieceType enemyType = PieceHelper.GetPieceType(enemyPiece);

                    // Check if enemy piece is a long-range piece that could pin
                    if (enemyType == PieceType.Bishop || enemyType == PieceType.Rook || enemyType == PieceType.Queen)
                    {
                        // Check if enemy piece can attack king
                        if (CanAttackSquare(board, r, c, enemyPiece, kingRow, kingCol))
                        {
                            // Check if our piece is between enemy and king
                            if (IsPieceBetween(board, r, c, kingRow, kingCol, row, col))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a piece at (pieceRow, pieceCol) is between (fromRow, fromCol) and (toRow, toCol)
        /// </summary>
        private static bool IsPieceBetween(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol, int pieceRow, int pieceCol)
        {
            // Check if on same line/diagonal
            bool onLine = (fromRow == toRow && toRow == pieceRow) || (fromCol == toCol && toCol == pieceCol);
            bool onDiagonal = Math.Abs(fromRow - toRow) == Math.Abs(fromCol - toCol) &&
                             Math.Abs(fromRow - pieceRow) == Math.Abs(fromCol - pieceCol);

            if (!onLine && !onDiagonal)
                return false;

            // Check if piece is between the two points
            bool rowBetween = (pieceRow > Math.Min(fromRow, toRow) && pieceRow < Math.Max(fromRow, toRow)) ||
                             (fromRow == toRow && pieceRow == fromRow);
            bool colBetween = (pieceCol > Math.Min(fromCol, toCol) && pieceCol < Math.Max(fromCol, toCol)) ||
                             (fromCol == toCol && pieceCol == fromCol);

            return rowBetween && colBetween;
        }
    }
}