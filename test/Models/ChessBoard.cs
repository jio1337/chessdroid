using System.Text;

namespace ChessDroid.Models
{
    public class ChessBoard
    {
        private const int BOARD_SIZE = 8;
        private char[,] board;

        // Zobrist hashing for fast position comparison
        private static readonly ulong[,] ZobristTable;
        private static readonly Dictionary<char, int> PieceIndex;
        private ulong? cachedHash = null;

        // Cached king positions for O(1) lookup (eliminates 20+ O(n²) scans)
        private int whiteKingRow = -1, whiteKingCol = -1;
        private int blackKingRow = -1, blackKingCol = -1;

        static ChessBoard()
        {
            // Initialize Zobrist random numbers (12 piece types * 64 squares)
            // Piece indices: P=0, N=1, B=2, R=3, Q=4, K=5, p=6, n=7, b=8, r=9, q=10, k=11
            ZobristTable = new ulong[12, 64];
            PieceIndex = new Dictionary<char, int>
            {
                {'P', 0}, {'N', 1}, {'B', 2}, {'R', 3}, {'Q', 4}, {'K', 5},
                {'p', 6}, {'n', 7}, {'b', 8}, {'r', 9}, {'q', 10}, {'k', 11}
            };

            // Use fixed seed for reproducible hashes across runs
            var random = new Random(0x12345678);
            for (int piece = 0; piece < 12; piece++)
            {
                for (int square = 0; square < 64; square++)
                {
                    // Generate 64-bit random number
                    byte[] buffer = new byte[8];
                    random.NextBytes(buffer);
                    ZobristTable[piece, square] = BitConverter.ToUInt64(buffer, 0);
                }
            }
        }

        public ChessBoard()
        {
            board = new char[BOARD_SIZE, BOARD_SIZE];
            InitializeEmpty();
        }

        public ChessBoard(char[,] existingBoard)
        {
            board = new char[BOARD_SIZE, BOARD_SIZE];
            Array.Copy(existingBoard, board, existingBoard.Length);
            // Scan for kings since we're copying raw array without position info
            ScanForKings();
        }

        public char this[int row, int col]
        {
            get => board[row, col];
            set
            {
                if (row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE)
                {
                    char oldPiece = board[row, col];
                    if (oldPiece == value) return; // No change

                    board[row, col] = value;

                    // Update cached king positions
                    UpdateKingCache(row, col, oldPiece, value);

                    // Incremental Zobrist hash update (O(1) instead of O(64))
                    if (cachedHash.HasValue)
                    {
                        ulong hash = cachedHash.Value;
                        int square = row * 8 + col;

                        // XOR out old piece
                        if (oldPiece != '.' && PieceIndex.TryGetValue(oldPiece, out int oldIdx))
                            hash ^= ZobristTable[oldIdx, square];

                        // XOR in new piece
                        if (value != '.' && PieceIndex.TryGetValue(value, out int newIdx))
                            hash ^= ZobristTable[newIdx, square];

                        cachedHash = hash;
                    }
                    else
                    {
                        cachedHash = null; // Invalidate hash cache when board changes
                    }
                }
            }
        }

        public char[,] GetArray() => (char[,])board.Clone();

        private void InitializeEmpty()
        {
            for (int i = 0; i < BOARD_SIZE; i++)
                for (int j = 0; j < BOARD_SIZE; j++)
                    board[i, j] = '.';
        }

        public bool Equals(ChessBoard other)
        {
            if (other == null) return false;
            for (int i = 0; i < BOARD_SIZE; i++)
                for (int j = 0; j < BOARD_SIZE; j++)
                    if (board[i, j] != other.board[i, j])
                        return false;
            return true;
        }

        public string ToFEN()
        {
            var rows = new string[BOARD_SIZE];
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                int emptyCount = 0;
                var rowFen = new StringBuilder();
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    char piece = board[row, col];
                    if (piece == '.')
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            rowFen.Append(emptyCount);
                            emptyCount = 0;
                        }
                        rowFen.Append(piece);
                    }
                }
                if (emptyCount > 0)
                    rowFen.Append(emptyCount);
                rows[row] = rowFen.ToString();
            }
            return string.Join("/", rows);
        }

        public static ChessBoard FromFEN(string fen)
        {
            var chessBoard = new ChessBoard();
            string[] fenParts = fen.Split(' ');
            string placement = fenParts.Length > 0 ? fenParts[0] : "";
            string[] rows = placement.Split('/');

            for (int i = 0; i < Math.Min(BOARD_SIZE, rows.Length); i++)
            {
                int col = 0;
                foreach (char c in rows[i])
                {
                    if (col >= BOARD_SIZE) break;

                    if (char.IsDigit(c))
                    {
                        int empty = c - '0';
                        for (int k = 0; k < empty && col < BOARD_SIZE; k++)
                            chessBoard[i, col++] = '.';
                    }
                    else
                    {
                        chessBoard[i, col++] = c;
                    }
                }
            }
            return chessBoard;
        }

        public char GetPiece(int row, int col)
        {
            if (row < 0 || row >= BOARD_SIZE || col < 0 || col >= BOARD_SIZE)
                return '.';
            return board[row, col];
        }

        public void SetPiece(int row, int col, char piece)
        {
            if (row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE)
            {
                char oldPiece = board[row, col];
                if (oldPiece == piece) return; // No change

                board[row, col] = piece;

                // Update cached king positions
                UpdateKingCache(row, col, oldPiece, piece);

                // Incremental Zobrist hash update (O(1) instead of O(64))
                if (cachedHash.HasValue)
                {
                    ulong hash = cachedHash.Value;
                    int square = row * 8 + col;

                    // XOR out old piece
                    if (oldPiece != '.' && PieceIndex.TryGetValue(oldPiece, out int oldIdx))
                        hash ^= ZobristTable[oldIdx, square];

                    // XOR in new piece
                    if (piece != '.' && PieceIndex.TryGetValue(piece, out int newIdx))
                        hash ^= ZobristTable[newIdx, square];

                    cachedHash = hash;
                }
                else
                {
                    cachedHash = null; // Invalidate hash cache when board changes
                }
            }
        }

        public bool IsEmpty(int row, int col) => GetPiece(row, col) == '.';

        /// <summary>
        /// Updates the cached king position when a piece is placed or removed.
        /// Called by SetPiece and indexer setter.
        /// </summary>
        private void UpdateKingCache(int row, int col, char oldPiece, char newPiece)
        {
            // If a king was removed from this square
            if (oldPiece == 'K')
            {
                whiteKingRow = -1;
                whiteKingCol = -1;
            }
            else if (oldPiece == 'k')
            {
                blackKingRow = -1;
                blackKingCol = -1;
            }

            // If a king was placed on this square
            if (newPiece == 'K')
            {
                whiteKingRow = row;
                whiteKingCol = col;
            }
            else if (newPiece == 'k')
            {
                blackKingRow = row;
                blackKingCol = col;
            }
        }

        /// <summary>
        /// Gets the king position for the specified color in O(1) time.
        /// Returns (-1, -1) if king is not found (shouldn't happen in valid positions).
        /// </summary>
        /// <param name="isWhite">True for white king, false for black king</param>
        /// <returns>Tuple of (row, col) or (-1, -1) if not found</returns>
        public (int row, int col) GetKingPosition(bool isWhite)
        {
            if (isWhite)
            {
                // If not cached, scan to find it (fallback for boards created without tracking)
                if (whiteKingRow < 0)
                    ScanForKings();
                return (whiteKingRow, whiteKingCol);
            }
            else
            {
                if (blackKingRow < 0)
                    ScanForKings();
                return (blackKingRow, blackKingCol);
            }
        }

        /// <summary>
        /// Scans the board to find and cache king positions.
        /// Called lazily when GetKingPosition is called and cache is empty.
        /// </summary>
        private void ScanForKings()
        {
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    char piece = board[r, c];
                    if (piece == 'K')
                    {
                        whiteKingRow = r;
                        whiteKingCol = c;
                    }
                    else if (piece == 'k')
                    {
                        blackKingRow = r;
                        blackKingCol = c;
                    }
                }
            }
        }

        public override string ToString() => ToFEN();

        /// <summary>
        /// Returns a Zobrist hash for fast position comparison.
        /// Much faster than FEN string comparison for detecting board changes.
        /// </summary>
        public ulong GetZobristHash()
        {
            if (cachedHash.HasValue)
                return cachedHash.Value;

            ulong hash = 0;
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    char piece = board[row, col];
                    if (piece != '.' && PieceIndex.TryGetValue(piece, out int pieceIdx))
                    {
                        int square = row * 8 + col;
                        hash ^= ZobristTable[pieceIdx, square];
                    }
                }
            }

            cachedHash = hash;
            return hash;
        }

        /// <summary>
        /// Fast comparison using Zobrist hash - O(1) after first computation.
        /// </summary>
        public bool HashEquals(ChessBoard other)
        {
            if (other == null) return false;
            return GetZobristHash() == other.GetZobristHash();
        }

        public string GetHashKey()
        {
            // Return hex string of Zobrist hash for caching
            return GetZobristHash().ToString("X16");
        }
    }
}