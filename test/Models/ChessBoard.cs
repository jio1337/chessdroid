using System;
using System.Linq;
using System.Text;

namespace ChessDroid.Models
{
    public class ChessBoard
    {
        private const int BOARD_SIZE = 8;
        private char[,] board;

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
                board[row, col] = piece;
        }

        public bool IsEmpty(int row, int col) => GetPiece(row, col) == '.';

        public override string ToString() => ToFEN();

        public string GetHashKey()
        {
            // Create a simple hash key for caching
            return ToFEN();
        }
    }
}
