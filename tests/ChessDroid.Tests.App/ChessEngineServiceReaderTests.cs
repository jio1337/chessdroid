using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §3.1: ChessEngineService used to have ~6 methods each running
/// their own inline read loop directly against the shared StreamReader, correct only because
/// callers never actually overlapped reads. Fixed with a single dedicated background task
/// that owns the StreamReader and publishes lines into a Channel&lt;string&gt;; every consumer
/// reads through that channel instead. These tests exercise the behaviors that mattered for
/// that refactor to be safe: continuous analysis streaming through mid-search cancellation,
/// process/channel reuse afterward, and RestartAsync producing a fresh working channel.
/// </summary>
public class ChessEngineServiceReaderTests
{
    private const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public async Task RunContinuousAnalysisAsync_StreamsMultipleDepths_ThenCancelsCleanly()
    {
        var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
        var svc = new ChessEngineService(config);
        try
        {
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);

            var seenDepths = new List<int>();
            using var cts = new CancellationTokenSource();

            var task = svc.RunContinuousAnalysisAsync(Fen, multiPV: 2, (bm, ev, pvs, evals, wdl, depth) =>
            {
                seenDepths.Add(depth);
                if (seenDepths.Count >= 5) cts.Cancel();
            }, cts.Token);

            try { await task; } catch (OperationCanceledException) { }

            Assert.True(seenDepths.Count >= 5, $"expected at least 5 depth updates, got {seenDepths.Count}");
            Assert.Equal(EngineState.Ready, svc.State);
        }
        finally
        {
            svc.Dispose();
        }
    }

    [Fact]
    public async Task GetBestMoveAsync_AfterCancelledContinuousAnalysis_StillWorks()
    {
        var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
        var svc = new ChessEngineService(config);
        try
        {
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);

            using var cts = new CancellationTokenSource();
            int updates = 0;
            var continuous = svc.RunContinuousAnalysisAsync(Fen, 1, (bm, ev, pvs, evals, wdl, depth) =>
            {
                if (++updates >= 3) cts.Cancel();
            }, cts.Token);
            try { await continuous; } catch (OperationCanceledException) { }

            var (bestMove, _, _, _, _) = await svc.GetBestMoveAsync(Fen, depth: 4, multiPV: 1);

            Assert.Equal("e2e4", bestMove);
            Assert.Equal(EngineState.Ready, svc.State);
        }
        finally
        {
            svc.Dispose();
        }
    }

    [Fact]
    public async Task RestartAsync_ProducesFreshWorkingChannel()
    {
        var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
        var svc = new ChessEngineService(config);
        try
        {
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);
            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1);

            await svc.RestartAsync();

            Assert.Equal(EngineState.Ready, svc.State);
            Assert.True(svc.IsEngineAlive());

            var (bestMove, _, _, _, _) = await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1);
            Assert.Equal("e2e4", bestMove);
        }
        finally
        {
            svc.Dispose();
        }
    }

    [Fact]
    public async Task GetBestMoveAsync_AgainstHungEngine_TimesOutToErrorWithoutDeadlocking()
    {
        var config = new AppConfig { EngineResponseTimeoutMs = 800, MaxEngineRetries = 1 };
        var svc = new ChessEngineService(config);
        try
        {
            await svc.InitializeAsync(FakeEngineFixture.HangPath);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var (bestMove, _, _, _, _) = await svc.GetBestMoveAsync(Fen, depth: 5, multiPV: 1);
            sw.Stop();

            Assert.True(string.IsNullOrEmpty(bestMove));
            Assert.True(sw.ElapsedMilliseconds < 15000, $"took {sw.ElapsedMilliseconds}ms — should time out, not hang");
        }
        finally
        {
            svc.Dispose();
        }
    }
}
