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
        }

        public char this[int row, int col]
        {
            get => board[row, col];
            set => board[row, col] = value;
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
                board[row, col] = piece;
                cachedHash = null; // Invalidate hash cache when board changes
            }
        }

        public bool IsEmpty(int row, int col) => GetPiece(row, col) == '.';

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