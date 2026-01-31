namespace ChessDroid.Models
{
    public class GameState
    {
        public ChessBoard Board { get; set; }
        public bool WhiteToMove { get; set; }
        public string CastlingRights { get; set; }
        public string EnPassantTarget { get; set; }
        public int HalfMoveClock { get; set; }
        public int FullMoveNumber { get; set; }
        public DateTime LastMoveTime { get; set; }

        public GameState()
        {
            Board = new ChessBoard();
            WhiteToMove = true;
            CastlingRights = "KQkq";
            EnPassantTarget = "-";
            HalfMoveClock = 0;
            FullMoveNumber = 1;
            LastMoveTime = DateTime.Now;
        }

        public string ToCompleteFEN()
        {
            string turn = WhiteToMove ? "w" : "b";
            string castling = string.IsNullOrEmpty(CastlingRights) ? "-" : CastlingRights;
            return $"{Board.ToFEN()} {turn} {castling} {EnPassantTarget} {HalfMoveClock} {FullMoveNumber}";
        }

        public static GameState FromFEN(string completeFEN)
        {
            var state = new GameState();
            string[] parts = completeFEN.Split(' ');

            if (parts.Length > 0)
                state.Board = ChessBoard.FromFEN(parts[0]);
            if (parts.Length > 1)
                state.WhiteToMove = parts[1] == "w";
            if (parts.Length > 2)
                state.CastlingRights = parts[2] == "-" ? "" : parts[2];
            if (parts.Length > 3)
                state.EnPassantTarget = parts[3];
            if (parts.Length > 4)
            {
                int halfMove;
                if (int.TryParse(parts[4], out halfMove))
                    state.HalfMoveClock = halfMove;
            }
            if (parts.Length > 5)
            {
                int fullMove;
                if (int.TryParse(parts[5], out fullMove))
                    state.FullMoveNumber = fullMove;
            }

            return state;
        }
    }
}