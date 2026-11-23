using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests;

/// <summary>
/// Regression coverage for the Phase 5 IsCheckmate consolidation (CHESSDROID_AUDIT.md §6.3).
/// ConsoleOutputFormatter.IsCheckmate and DefenseDetection.IsCheckmate were independent,
/// buggy copies — their blocking-check scan used ChessUtilities.CanAttackSquare for every
/// piece type, which only models pawn CAPTURE geometry, not pawn PUSHES. A check that could
/// only be blocked by a pawn push (not a capture) was invisible to them, so they could
/// misreport checkmate. Both were redirected onto ChessUtilities.IsKingMated, an existing,
/// more complete implementation (already used for SAN "#" suffixes) whose CanPseudoMove
/// correctly includes pawn pushes.
/// </summary>
public class IsKingMatedTests
{
    private static readonly string[] ExpectedPawnPushBlockMoves = { "c2c3" };

    [Fact]
    public void PawnPushBlock_IsNotCheckmate()
    {
        // White king e1 in check from the bishop on a5 along the a5-e1 diagonal. Every
        // flight square is occupied by white's own pieces or still attacked by the bishop
        // (d2), and nothing can capture the bishop — the only defense is pushing the c2
        // pawn to c3, interposing on the diagonal. Verified independently against
        // MoveGenerator.GetAllLegalMovesUci (perft-verified): the only legal move in this
        // position is c2c3.
        string fen = "4k3/8/8/b7/8/8/2P1PP2/3BKB2 w - - 0 1";
        var board = ChessBoard.FromFEN(fen);

        Assert.True(ChessUtilities.IsKingInCheck(board, true));
        Assert.False(ChessUtilities.IsKingMated(board, true));

        var castling = new CastlingState(false, false, false, false);
        var legalMoves = MoveGenerator.GetAllLegalMovesUci(board, true, castling, null);
        Assert.Equal(ExpectedPawnPushBlockMoves, legalMoves);
    }

    [Fact]
    public void FoolsMate_IsCheckmate()
    {
        var board = ChessBoard.FromFEN("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3");
        Assert.True(ChessUtilities.IsKingMated(board, true));
    }

    [Fact]
    public void StartingPosition_IsNotCheckmate()
    {
        var board = ChessBoard.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        Assert.False(ChessUtilities.IsKingMated(board, true));
    }
}
