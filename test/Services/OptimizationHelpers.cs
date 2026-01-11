using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Performance optimization helpers for chess analysis
    /// Provides caching and common utility functions to reduce redundant calculations
    /// </summary>
    public static class OptimizationHelpers
    {
        // =============================
        // CACHING LAYER
        // Cache frequently computed values to avoid redundant calculations
        // =============================

        private static Dictionary<string, (int white, int black, int total)> pieceCounts =
            new Dictionary<string, (int, int, int)>();

        private static Dictionary<string, int> materialBalance =
            new Dictionary<string, int>();

        /// <summary>
        /// Generate simple hash for board position (for caching)
        /// </summary>
        private static string GetBoardHash(ChessBoard board)
        {
            int hash = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    hash = hash * 31 + piece.GetHashCode();
                }
            }
            return hash.ToString();
        }

        /// <summary>
        /// Clear all caches (call at start of new position analysis)
        /// </summary>
        public static void ClearCache()
        {
            pieceCounts.Clear();
            materialBalance.Clear();
        }

        /// <summary>
        /// Get or compute piece counts for a position (with caching)
        /// Returns (white pieces, black pieces, total)
        /// </summary>
        public static (int white, int black, int total) GetPieceCounts(ChessBoard board)
        {
            string key = GetBoardHash(board);

            if (pieceCounts.TryGetValue(key, out var cached))
                return cached;

            int whitePieces = 0, blackPieces = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.' || piece == 'K' || piece == 'k') continue;

                    if (char.IsUpper(piece))
                        whitePieces++;
                    else
                        blackPieces++;
                }
            }

            var result = (whitePieces, blackPieces, whitePieces + blackPieces);
            pieceCounts[key] = result;
            return result;
        }

        /// <summary>
        /// Get or compute material balance (with caching)
        /// Positive = White ahead, Negative = Black ahead
        /// </summary>
        public static int GetMaterialBalance(ChessBoard board)
        {
            string key = GetBoardHash(board);

            if (materialBalance.TryGetValue(key, out int cached))
                return cached;

            int balance = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    int value = GetPieceValueFast(piece);

                    if (char.IsUpper(piece))
                        balance += value;
                    else
                        balance -= value;
                }
            }

            materialBalance[key] = balance;
            return balance;
        }

        // =============================
        // FAST UTILITY FUNCTIONS
        // Optimized versions of common operations
        // =============================

        /// <summary>
        /// Fast piece value lookup using switch expression (faster than dictionary)
        /// </summary>
        public static int GetPieceValueFast(char piece)
        {
            return char.ToUpper(piece) switch
            {
                'P' => 1,
                'N' => 3,
                'B' => 3,
                'R' => 5,
                'Q' => 9,
                'K' => 0,  // Don't count king in material
                _ => 0
            };
        }

        /// <summary>
        /// Get piece type from char without creating PieceType enum
        /// Faster for quick checks
        /// </summary>
        public static char GetPieceTypeFast(char piece)
        {
            return char.ToUpper(piece);
        }

        /// <summary>
        /// Check if square is within board bounds (inline for speed)
        /// </summary>
        public static bool IsValidSquare(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }

        /// <summary>
        /// Check if path is clear between two squares (optimized)
        /// </summary>
        public static bool IsPathClearFast(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            int dR = Math.Sign(toRow - fromRow);
            int dF = Math.Sign(toCol - fromCol);

            // Must be straight line or diagonal
            if (dR == 0 && dF == 0) return false;
            if (dR != 0 && dF != 0 && Math.Abs(toRow - fromRow) != Math.Abs(toCol - fromCol)) return false;

            int r = fromRow + dR;
            int c = fromCol + dF;

            while (r != toRow || c != toCol)
            {
                if (!IsValidSquare(r, c)) return false;
                if (board.GetPiece(r, c) != '.') return false;

                r += dR;
                c += dF;
            }

            return true;
        }

        /// <summary>
        /// Quick check if piece can attack square (optimized, no safety checks)
        /// Use only when you're sure parameters are valid
        /// </summary>
        public static bool CanAttackFast(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            char pieceType = char.ToUpper(piece);
            int dR = toRow - fromRow;
            int dF = toCol - fromCol;

            switch (pieceType)
            {
                case 'N': // Knight
                    return (Math.Abs(dR) == 2 && Math.Abs(dF) == 1) ||
                           (Math.Abs(dR) == 1 && Math.Abs(dF) == 2);

                case 'K': // King
                    return Math.Abs(dR) <= 1 && Math.Abs(dF) <= 1 && (dR != 0 || dF != 0);

                case 'P': // Pawn (attacks only)
                    int direction = char.IsUpper(piece) ? -1 : 1;
                    return dR == direction && Math.Abs(dF) == 1;

                case 'B': // Bishop
                    return Math.Abs(dR) == Math.Abs(dF) && dR != 0 &&
                           IsPathClearFast(board, fromRow, fromCol, toRow, toCol);

                case 'R': // Rook
                    return (dR == 0 || dF == 0) && (dR != 0 || dF != 0) &&
                           IsPathClearFast(board, fromRow, fromCol, toRow, toCol);

                case 'Q': // Queen
                    return ((dR == 0 || dF == 0) || (Math.Abs(dR) == Math.Abs(dF))) &&
                           (dR != 0 || dF != 0) &&
                           IsPathClearFast(board, fromRow, fromCol, toRow, toCol);

                default:
                    return false;
            }
        }

        // =============================
        // BATCH OPERATIONS
        // Process multiple items efficiently
        // =============================

        /// <summary>
        /// Find all pieces of a specific type (optimized)
        /// Returns list of (row, col) positions
        /// </summary>
        public static List<(int row, int col)> FindPieces(ChessBoard board, char pieceType)
        {
            var positions = new List<(int, int)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == pieceType)
                        positions.Add((r, c));
                }
            }

            return positions;
        }

        /// <summary>
        /// Find all attackers to a square (optimized with early exit)
        /// </summary>
        public static List<(int row, int col, char piece)> FindAttackers(
            ChessBoard board, int targetRow, int targetCol, bool byWhite)
        {
            var attackers = new List<(int, int, char)>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (CanAttackFast(board, r, c, piece, targetRow, targetCol))
                        attackers.Add((r, c, piece));
                }
            }

            return attackers;
        }

        /// <summary>
        /// Check if square is attacked (fast, with early exit)
        /// </summary>
        public static bool IsSquareAttackedFast(ChessBoard board, int row, int col, bool byWhite)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char piece = board.GetPiece(r, c);
                    if (piece == '.') continue;

                    bool pieceIsWhite = char.IsUpper(piece);
                    if (pieceIsWhite != byWhite) continue;

                    if (CanAttackFast(board, r, c, piece, row, col))
                        return true; // Early exit on first attacker found
                }
            }

            return false;
        }

        // =============================
        // CONSTANTS
        // Pre-computed values for common calculations
        // =============================

        /// <summary>
        /// File letters for square notation
        /// </summary>
        public static readonly char[] FILES = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

        /// <summary>
        /// Piece value lookup table (materialized for O(1) access)
        /// </summary>
        public static readonly Dictionary<char, int> PIECE_VALUES = new Dictionary<char, int>
        {
            { 'P', 1 }, { 'p', 1 },
            { 'N', 3 }, { 'n', 3 },
            { 'B', 3 }, { 'b', 3 },
            { 'R', 5 }, { 'r', 5 },
            { 'Q', 9 }, { 'q', 9 },
            { 'K', 0 }, { 'k', 0 }
        };

        /// <summary>
        /// Central squares (d4, e4, d5, e5) for quick checks
        /// </summary>
        public static readonly (int row, int col)[] CENTRAL_SQUARES =
        {
            (4, 3), // d4
            (4, 4), // e4
            (3, 3), // d5
            (3, 4)  // e5
        };

        /// <summary>
        /// Extended center (c3-f3, c4-f4, c5-f5, c6-f6)
        /// </summary>
        public static readonly (int row, int col)[] EXTENDED_CENTER =
        {
            // Rank 3
            (5, 2), (5, 3), (5, 4), (5, 5),
            // Rank 4
            (4, 2), (4, 3), (4, 4), (4, 5),
            // Rank 5
            (3, 2), (3, 3), (3, 4), (3, 5),
            // Rank 6
            (2, 2), (2, 3), (2, 4), (2, 5)
        };
    }
}