using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §2.2: GetMoveForMatchAsync returning a null move used to be
/// indistinguishable from "no legal move," so a hung or crashed engine got scored as a
/// stalemate/checkmate and fed into the Elo ladder and PGN. Fix: only treat a null move as
/// stalemate/checkmate when the engine is still healthy; otherwise end the match as
/// MatchTermination.EngineError.
/// </summary>
public class EngineMatchServiceTests
{
    [Fact]
    public async Task HungOpponentEngine_EndsMatchAsEngineError_NotStalemateOrCheckmate()
    {
        var config = new AppConfig();
        var service = new EngineMatchService(config);
        var resultTcs = new TaskCompletionSource<EngineMatchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.OnMatchEnded += r => resultTcs.TrySetResult(r);

        var timeControl = new EngineMatchTimeControl
        {
            Type = TimeControlType.FixedTimePerMove,
            MoveTimeMs = 10 // GetTimeoutMs adds a fixed +5000ms floor regardless, but keep this small
        };

        // White responds normally; Black hangs on every "go" — the match must stall on
        // Black's first move and come back as EngineError, not a drawn/lost game on the board.
        var matchTask = service.StartMatchAsync(
            FakeEngineFixture.NormalPath, FakeEngineFixture.HangPath,
            "White", "Black", timeControl);

        var completed = await Task.WhenAny(resultTcs.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(resultTcs.Task, completed);

        var result = await resultTcs.Task;
        Assert.Equal(MatchTermination.EngineError, result.Termination);
        Assert.NotEqual(MatchTermination.Stalemate, result.Termination);
        Assert.NotEqual(MatchTermination.Checkmate, result.Termination);

        await matchTask;
        service.Dispose();
    }

    [Fact]
    public async Task CrashedOpponentEngine_EndsMatchAsEngineError()
    {
        var config = new AppConfig();
        var service = new EngineMatchService(config);
        var resultTcs = new TaskCompletionSource<EngineMatchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.OnMatchEnded += r => resultTcs.TrySetResult(r);

        var timeControl = new EngineMatchTimeControl { Type = TimeControlType.FixedTimePerMove, MoveTimeMs = 10 };

        var matchTask = service.StartMatchAsync(
            FakeEngineFixture.NormalPath, FakeEngineFixture.CrashPath,
            "White", "Black", timeControl);

        var completed = await Task.WhenAny(resultTcs.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Same(resultTcs.Task, completed);

        var result = await resultTcs.Task;
        Assert.Equal(MatchTermination.EngineError, result.Termination);

        await matchTask;
        service.Dispose();
    }
}
