namespace ChessDroid.Models
{
    /// <summary>
    /// Represents the type of chess piece (case-insensitive)
    /// </summary>
    public enum PieceType
    {
        None = '.',
        Pawn = 'p',
        Knight = 'n',
        Bishop = 'b',
        Rook = 'r',
        Queen = 'q',
        King = 'k'
    }

    /// <summary>
    /// Represents a chess piece with color
    /// </summary>
    public enum Piece
    {
        Empty = '.',
        WhitePawn = 'P',
        WhiteKnight = 'N',
        WhiteBishop = 'B',
        WhiteRook = 'R',
        WhiteQueen = 'Q',
        WhiteKing = 'K',
        BlackPawn = 'p',
        BlackKnight = 'n',
        BlackBishop = 'b',
        BlackRook = 'r',
        BlackQueen = 'q',
        BlackKing = 'k'
    }

    /// <summary>
    /// Helper methods for working with chess pieces
    /// </summary>
    public static class PieceHelper
    {
        public static PieceType GetPieceType(char piece)
        {
            return char.ToLower(piece) switch
            {
                'p' => PieceType.Pawn,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'r' => PieceType.Rook,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => PieceType.None
            };
        }

        public static bool IsWhite(char piece)
        {
            return char.IsUpper(piece) && piece != '.';
        }

        public static bool IsBlack(char piece)
        {
            return char.IsLower(piece) && piece != '.';
        }

        public static Piece FromChar(char c)
        {
            return c switch
            {
                '.' => Piece.Empty,
                'P' => Piece.WhitePawn,
                'N' => Piece.WhiteKnight,
                'B' => Piece.WhiteBishop,
                'R' => Piece.WhiteRook,
                'Q' => Piece.WhiteQueen,
                'K' => Piece.WhiteKing,
                'p' => Piece.BlackPawn,
                'n' => Piece.BlackKnight,
                'b' => Piece.BlackBishop,
                'r' => Piece.BlackRook,
                'q' => Piece.BlackQueen,
                'k' => Piece.BlackKing,
                _ => Piece.Empty
            };
        }
    }
}