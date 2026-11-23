namespace ChessDroid.Models
{
    /// <summary>
    /// Castling rights for both sides, plus the Chess960 rook-file tracking needed to
    /// disambiguate "which rook" when standard KQkq letters aren't enough (Shredder FEN).
    /// Defaults match the standard-chess rook starting files.
    /// </summary>
    public readonly record struct CastlingState(
        bool WhiteKingside,
        bool WhiteQueenside,
        bool BlackKingside,
        bool BlackQueenside,
        bool Chess960Mode = false,
        int WhiteKingsideRookFile = 7,
        int WhiteQueensideRookFile = 0,
        int BlackKingsideRookFile = 7,
        int BlackQueensideRookFile = 0);
}
