using ChessDroid.Controls;

namespace ChessDroid.Tests.App;

/// <summary>
/// Perft ("performance test", standard chess-programming technique): count every leaf node
/// reachable by playing out all legal moves to a fixed depth, and compare against published
/// reference values. Exhaustively exercises move generation — captures, castling (both
/// sides, rights revocation), en passant, promotion (all 4 pieces), and check/pin legality —
/// far beyond what the targeted §2.x regression tests cover.
///
/// No "enumerate all legal moves" method existed anywhere in the codebase before this; the
/// only complete, correct legal-move logic (castling/en passant/check filtering) lived as
/// private per-square methods on ChessBoardControl, used only to highlight UI click targets.
/// Added ChessBoardControl.GetAllLegalMovesUci() as a thin public wrapper around that existing
/// logic (GetLegalMovesForPiece looped over every own piece), reusing it rather than writing a
/// second, independent generator — Core's ChessRulesService.HasAnyLegalMove is boolean-only,
/// doesn't generate castling moves, and its en-passant handling is broken (verified during
/// this investigation; tracked as a known gap, not fixed here since nothing currently depends
/// on it for move generation).
///
/// Reference values are the standard chess programming perft positions (chessprogramming.org).
/// Depths are chosen to keep each test in the sub-few-seconds range — this drives real legality
/// checks + a full LoadFEN/MakeMove round trip at every node, not a cheap pseudo-move count.
/// </summary>
public class PerftTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    // "Kiwipete" — the standard perft stress position: both sides can castle either way,
    // multiple pieces en prise, promotion threats.
    private const string KiwipeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    // Position 3 — isolated kings, no castling rights; stresses en passant and pawn endgame play.
    private const string Position3Fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    // Position 4 — white pawns one step from promoting; stresses underpromotion branching.
    private const string Position4Fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";

    private static long Perft(ChessBoardControl board, int depth)
    {
        if (depth == 0) return 1;

        string fenBeforeAnyMove = board.GetFEN();
        long nodes = 0;
        foreach (var move in board.GetAllLegalMovesUci())
        {
            board.LoadFEN(fenBeforeAnyMove); // undo the previous sibling move, if any
            bool applied = board.MakeMove(move);
            if (!applied)
                throw new InvalidOperationException($"GetAllLegalMovesUci() produced a move MakeMove rejected: {move}");
            nodes += Perft(board, depth - 1);
        }
        board.LoadFEN(fenBeforeAnyMove); // leave the board as found, for the caller
        return nodes;
    }

    private static ChessBoardControl BoardAt(string fen)
    {
        var board = new ChessBoardControl();
        board.LoadFEN(fen);
        return board;
    }

    private static ChessBoardControl Chess960BoardAt(string fen)
    {
        var board = new ChessBoardControl();
        board.Chess960Mode = true;
        board.LoadFEN(fen);
        return board;
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 400)]
    [InlineData(3, 8902)]
    [InlineData(4, 197281)]
    public void StartingPosition_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = BoardAt(StartFen);
        Assert.Equal(expectedNodes, Perft(board, depth));
    }

    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    [InlineData(3, 97862)]
    public void Kiwipete_CastlingAndCapturesHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = BoardAt(KiwipeteFen);
        Assert.Equal(expectedNodes, Perft(board, depth));
    }

    [Theory]
    [InlineData(1, 14)]
    [InlineData(2, 191)]
    [InlineData(3, 2812)]
    [InlineData(4, 43238)]
    public void Position3_EnPassantHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = BoardAt(Position3Fen);
        Assert.Equal(expectedNodes, Perft(board, depth));
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 264)]
    [InlineData(3, 9467)]
    public void Position4_PromotionHeavy_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = BoardAt(Position4Fen);
        Assert.Equal(expectedNodes, Perft(board, depth));
    }

    // Chess960 coverage: rooks on their standard a/h files under Chess960 rules is operationally
    // identical to standard castling, so these must match the exact same published counts as the
    // non-960 positions above — but driven through the Chess960 code path end to end (king-
    // captures-own-rook UCI notation from GetAllLegalMovesUci, through ExecuteMove's consolidated
    // ApplyUciMove call). Any divergence here means the Chess960 castling consolidation broke
    // something that the non-960 perft suite can't see.
    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 400)]
    [InlineData(3, 8902)]
    public void Chess960_StartingPositionWithStandardRookFiles_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = Chess960BoardAt("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w HAha - 0 1");
        Assert.Equal(expectedNodes, Perft(board, depth));
    }

    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    public void Chess960_KiwipeteWithStandardRookFiles_MatchesPublishedPerftCounts(int depth, long expectedNodes)
    {
        var board = Chess960BoardAt("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w HAha - 0 1");
        Assert.Equal(expectedNodes, Perft(board, depth));
    }
}
