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

    /// <summary>
    /// Regression test for a real bug found via live testing of Tournament mode (4 concurrent
    /// matches): StartMatchAsync opened a match-debug log file at a FIXED path
    /// ("match_debug.txt" in the exe directory), outside its own try/catch. StreamWriter opens
    /// with exclusive write access by default, so when multiple EngineMatchService instances
    /// started within the same instant (as Tournament mode's 4 panels do), only the first to
    /// win the race could actually open the file — the other 3 threw IOException before even
    /// reaching engine initialization, and since the exception was outside the try block, it
    /// propagated out of this (fire-and-forget, in production) async method as a silent
    /// unobserved task exception. Symptom: only 1 of 4 tournament boards ever played a move;
    /// Task Manager showed only 2 engine processes (1 match's worth) instead of 8.
    /// Fixed by giving each match's log file a unique name and moving the file-open inside the
    /// try block. This test starts 4 EngineMatchService instances concurrently (mirroring
    /// Tournament mode exactly) and asserts all 4 reach OnMatchEnded normally — before the fix,
    /// this reliably failed with an unhandled IOException on at least one of the 4 tasks.
    /// </summary>
    [Fact]
    public async Task FourConcurrentMatches_AllStartWithoutLogFileCollision()
    {
        var config = new AppConfig();
        var timeControl = new EngineMatchTimeControl { Type = TimeControlType.FixedTimePerMove, MoveTimeMs = 10 };

        var services = Enumerable.Range(0, 4).Select(_ => new EngineMatchService(config)).ToArray();
        var endedTcs = services.Select(_ => new TaskCompletionSource<EngineMatchResult>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        for (int i = 0; i < services.Length; i++)
        {
            var tcs = endedTcs[i];
            services[i].OnMatchEnded += r => tcs.TrySetResult(r);
        }

        // NullMovePath reports no legal move immediately, so each match terminates fast — this
        // test only cares that all 4 survive engine startup without the log-file race, not that
        // a full game plays out.
        var startTasks = services.Select(s => s.StartMatchAsync(
            FakeEngineFixture.NullMovePath, FakeEngineFixture.NullMovePath,
            "White", "Black", timeControl)).ToArray();

        var allEnded = Task.WhenAll(endedTcs.Select(t => t.Task));
        await Task.WhenAny(allEnded, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.True(endedTcs.All(t => t.Task.IsCompletedSuccessfully),
            "Not all 4 concurrent matches reached OnMatchEnded within the timeout — at least one likely died silently during startup.");

        // StartMatchAsync's own task must not have faulted either — that's exactly the shape
        // the original bug took (IOException thrown before the try block).
        await Task.WhenAll(startTasks);

        foreach (var s in services) s.Dispose();
    }
}
