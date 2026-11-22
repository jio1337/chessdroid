using ChessDroid.Controls;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §2.4: GetFEN() used to hardcode the halfmove clock and
/// fullmove number as "0 1" always, so exported FENs always claimed move 1 and the engine
/// never saw the 50-move counter (breaking draw detection and Syzygy probing near rule-50).
/// Fix: track both counters on the control and update them on move/reset/load.
/// </summary>
public class ChessBoardControlFenTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public void NewBoard_FenEndsInZeroOne()
    {
        var board = new ChessBoardControl();
        Assert.EndsWith(" 0 1", board.GetFEN());
    }

    [Fact]
    public void PawnMove_ResetsHalfmoveClock_FullmoveUnchangedAfterWhiteMove()
    {
        var board = new ChessBoardControl();
        board.MakeMove("e2e4"); // pawn move: resets halfmove clock to 0; White just moved, fullmove stays 1

        var parts = board.GetFEN().Split(' ');
        Assert.Equal("0", parts[4]);
        Assert.Equal("1", parts[5]);
    }

    [Fact]
    public void KnightMoveThenKnightMove_IncrementsHalfmoveClock_AndFullmoveAfterBlackMoves()
    {
        var board = new ChessBoardControl();
        board.MakeMove("g1f3"); // knight move, not pawn/capture -> halfmove clock increments to 1
        board.MakeMove("g8f6"); // Black's knight move -> halfmove clock increments to 2, fullmove -> 2

        var parts = board.GetFEN().Split(' ');
        Assert.Equal("2", parts[4]);
        Assert.Equal("2", parts[5]);
    }

    [Fact]
    public void CaptureMove_ResetsHalfmoveClock()
    {
        var board = new ChessBoardControl();
        board.MakeMove("e2e4");
        board.MakeMove("d7d5");
        board.MakeMove("e4d5"); // capture -> halfmove clock resets to 0

        var parts = board.GetFEN().Split(' ');
        Assert.Equal("0", parts[4]);
    }

    [Fact]
    public void ResetBoard_RestoresCountersToZeroAndOne()
    {
        var board = new ChessBoardControl();
        board.MakeMove("g1f3");
        board.MakeMove("g8f6");
        board.ResetBoard();

        Assert.EndsWith(" 0 1", board.GetFEN());
    }

    [Fact]
    public void LoadFen_WithNonDefaultCounters_RoundTripsThemBack()
    {
        var board = new ChessBoardControl();
        board.LoadFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 37 42");

        var parts = board.GetFEN().Split(' ');
        Assert.Equal("37", parts[4]);
        Assert.Equal("42", parts[5]);
    }
}
