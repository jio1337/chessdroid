using ChessDroid.Models;

namespace ChessDroid.Services
{
    /// <summary>
    /// Caches piece locations and attack information for performance optimization.
    /// Eliminates repeated O(n²) board scans by maintaining indexed lookups.
    /// Expected performance improvement: 4-10x faster for tactical analysis.
    /// </summary>
    public class BoardCache
    {
        private readonly ChessBoard board;

        // Cache of piece locations by color
        private readonly List<(int row, int col, char piece)> whitePieces;

        private readonly List<(int row, int col, char piece)> blackPieces;

        // Cache of attacked squares by color
        private readonly HashSet<(int row, int col)> whiteAttacks;

        private readonly HashSet<(int row, int col)> blackAttacks;

        // King positions (frequently needed)
        private (int row, int col) whiteKingPos;

        private (int row, int col) blackKingPos;

        public BoardCache(ChessBoard board)
        {
            this.board = board;
            this.whitePieces = new List<(int, int, char)>();
            this.blackPieces = new List<(int, int, char)>();
            this.whiteAttacks = new HashSet<(int, int)>();
            this.blackAttacks = new HashSet<(int, int)>();
            this.whiteKingPos = (-1, -1);
            this.blackKingPos = (-1, -1);

            BuildCache();
        }

        /// <summary>
        /// Build all caches in a single pass through the board
        /// </summary>
        private void BuildCache()
        {
            // Single O(n²) pass to catalog all pieces
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    char piece = board.GetPiece(row, col);
                    if (piece == '.') continue;

                    bool isWhite = char.IsUpper(piece);

                    // Add to appropriate piece list
                    if (isWhite)
                    {
                        whitePieces.Add((row, col, piece));

                        // Track king position
                        if (piece == 'K')
                            whiteKingPos = (row, col);
                    }
                    else
                    {
                        blackPieces.Add((row, col, piece));

                        // Track king position
                        if (piece == 'k')
                            blackKingPos = (row, col);
                    }
                }
            }

            // Build attack maps (O(n²) instead of O(n³))
            BuildAttackMap(whitePieces, whiteAttacks);
            BuildAttackMap(blackPieces, blackAttacks);
        }

        /// <summary>
        /// Build attack map for a set of pieces
        /// </summary>
        private void BuildAttackMap(List<(int row, int col, char piece)> pieces, HashSet<(int, int)> attackMap)
        {
            foreach (var (row, col, piece) in pieces)
            {
                // For each piece, find all squares it attacks
                for (int targetRow = 0; targetRow < 8; targetRow++)
                {
                    for (int targetCol = 0; targetCol < 8; targetCol++)
                    {
                        if (ChessUtilities.CanAttackSquare(board, row, col, piece, targetRow, targetCol))
                        {
                            attackMap.Add((targetRow, targetCol));
                        }
                    }
                }
            }
        }

        // =============================
        // PUBLIC QUERY METHODS
        // =============================

        /// <summary>
        /// Get all pieces for a specific color
        /// </summary>
        public IEnumerable<(int row, int col, char piece)> GetPieces(bool isWhite)
        {
            return isWhite ? whitePieces : blackPieces;
        }

        /// <summary>
        /// Get pieces of a specific type and color
        /// </summary>
        public IEnumerable<(int row, int col, char piece)> GetPiecesByType(PieceType type, bool isWhite)
        {
            var pieces = isWhite ? whitePieces : blackPieces;

            foreach (var (row, col, piece) in pieces)
            {
                if (PieceHelper.GetPieceType(piece) == type)
                    yield return (row, col, piece);
            }
        }

        /// <summary>
        /// Check if a square is attacked by a specific color (O(1) lookup)
        /// </summary>
        public bool IsSquareAttacked(int row, int col, bool byWhite)
        {
            var attackMap = byWhite ? whiteAttacks : blackAttacks;
            return attackMap.Contains((row, col));
        }

        /// <summary>
        /// Count attackers on a square (optimized with piece list)
        /// </summary>
        public int CountAttackers(int row, int col, bool byWhite)
        {
            var pieces = byWhite ? whitePieces : blackPieces;
            int count = 0;

            foreach (var (pieceRow, pieceCol, piece) in pieces)
            {
                if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, row, col))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get king position for a color (O(1) lookup)
        /// </summary>
        public (int row, int col) GetKingPosition(bool isWhite)
        {
            return isWhite ? whiteKingPos : blackKingPos;
        }

        /// <summary>
        /// Find all attackers of a square
        /// </summary>
        public List<(int row, int col, char piece)> GetAttackers(int row, int col, bool byWhite)
        {
            var pieces = byWhite ? whitePieces : blackPieces;
            var attackers = new List<(int, int, char)>();

            foreach (var (pieceRow, pieceCol, piece) in pieces)
            {
                if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, row, col))
                    attackers.Add((pieceRow, pieceCol, piece));
            }

            return attackers;
        }

        /// <summary>
        /// Get count of pieces for a color
        /// </summary>
        public int GetPieceCount(bool isWhite)
        {
            return isWhite ? whitePieces.Count : blackPieces.Count;
        }

        /// <summary>
        /// Check if a specific piece exists on the board
        /// </summary>
        public bool HasPiece(PieceType type, bool isWhite)
        {
            var pieces = isWhite ? whitePieces : blackPieces;

            foreach (var (_, _, piece) in pieces)
            {
                if (PieceHelper.GetPieceType(piece) == type)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get total material count (number of pieces excluding pawns)
        /// </summary>
        public int GetMaterialCount()
        {
            int count = 0;

            foreach (var (_, _, piece) in whitePieces)
            {
                PieceType type = PieceHelper.GetPieceType(piece);
                if (type != PieceType.Pawn && type != PieceType.King)
                    count++;
            }

            foreach (var (_, _, piece) in blackPieces)
            {
                PieceType type = PieceHelper.GetPieceType(piece);
                if (type != PieceType.Pawn && type != PieceType.King)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Find pieces on a specific rank (row)
        /// </summary>
        public IEnumerable<(int row, int col, char piece)> GetPiecesOnRank(int rank, bool isWhite)
        {
            var pieces = isWhite ? whitePieces : blackPieces;

            foreach (var (row, col, piece) in pieces)
            {
                if (row == rank)
                    yield return (row, col, piece);
            }
        }

        /// <summary>
        /// Find pieces on a specific file (column)
        /// </summary>
        public IEnumerable<(int row, int col, char piece)> GetPiecesOnFile(int file, bool isWhite)
        {
            var pieces = isWhite ? whitePieces : blackPieces;

            foreach (var (row, col, piece) in pieces)
            {
                if (col == file)
                    yield return (row, col, piece);
            }
        }
    }
}