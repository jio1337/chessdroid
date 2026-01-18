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
    }
}