using ChessDroid.Controls;

namespace ChessDroid.Tests.App;

/// <summary>
/// MoveAttempted is a new vetoable event fired from ExecuteMove before a move commits, added
/// so puzzle/opening-training validation can reject a legal-but-wrong move outright instead of
/// committing it and undoing it afterward by reloading the position (the old approach — see
/// AnalysisBoardForm.Training.cs's RejectPuzzleMove/RejectOpeningTrainingMove). MakeMove(uci)
/// is the public API that routes through ExecuteMove, same as a real click/drag move, so it's
/// usable here directly without needing a live mouse event or an AnalysisBoardForm.
/// </summary>
public class ChessBoardControlMoveAttemptedTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public void MoveAttempted_FiresBeforeMoveCommits_WithCorrectSquaresAndUci()
    {
        var board = new ChessBoardControl();
        MoveAttemptEventArgs? captured = null;
        string? fenAtAttemptTime = null;
        board.MoveAttempted += (_, e) =>
        {
            captured = e;
            fenAtAttemptTime = board.GetFEN();
        };

        board.MakeMove("e2e4");

        Assert.NotNull(captured);
        Assert.Equal("e2e4", captured!.UciMove);
        // e2 -> internal (row 6, col 4); e4 -> internal (row 4, col 4)
        Assert.Equal(6, captured.FromRow);
        Assert.Equal(4, captured.FromCol);
        Assert.Equal(4, captured.ToRow);
        Assert.Equal(4, captured.ToCol);
        Assert.Equal(StartFen, fenAtAttemptTime); // board unchanged at the moment the event fires
    }

    [Fact]
    public void CancellingMoveAttempted_PreventsTheMoveFromCommitting()
    {
        var board = new ChessBoardControl();
        board.MoveAttempted += (_, e) => e.Cancel = true;

        bool result = board.MakeMove("e2e4");

        Assert.False(result);
        Assert.Equal(StartFen, board.GetFEN());
    }

    [Fact]
    public void CancellingMoveAttempted_SuppressesMoveMade()
    {
        var board = new ChessBoardControl();
        board.MoveAttempted += (_, e) => e.Cancel = true;
        bool moveMadeFired = false;
        board.MoveMade += (_, _) => moveMadeFired = true;

        board.MakeMove("e2e4");

        Assert.False(moveMadeFired);
    }

    [Fact]
    public void NotCancellingMoveAttempted_LetsTheMoveProceedNormally()
    {
        var board = new ChessBoardControl();
        bool attemptFired = false;
        bool moveMadeFired = false;
        board.MoveAttempted += (_, e) => { attemptFired = true; Assert.False(e.Cancel); };
        board.MoveMade += (_, _) => moveMadeFired = true;

        bool result = board.MakeMove("e2e4");

        Assert.True(attemptFired);
        Assert.True(moveMadeFired);
        Assert.True(result);
        Assert.NotEqual(StartFen, board.GetFEN());
    }

    [Fact]
    public void MoveAttempted_CarriesPromotionPieceWhenSpecified()
    {
        // e7e8q as a full UCI string with promotion suffix — MakeMove parses it directly,
        // bypassing the interactive promotion-piece dialog entirely.
        var board = new ChessBoardControl();
        board.LoadFEN("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");
        MoveAttemptEventArgs? captured = null;
        board.MoveAttempted += (_, e) => captured = e;

        board.MakeMove("a7a8q");

        Assert.NotNull(captured);
        Assert.Equal('q', captured!.Promotion);
    }
}
