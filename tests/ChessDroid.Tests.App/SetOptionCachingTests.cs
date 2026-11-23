using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression test for audit §3.3: ChessEngineService used to send "setoption name Threads"
/// (and MultiPV) on every single GetBestMoveAsync call, even when the value hadn't changed.
/// In recent Stockfish versions, changing Threads re-creates the thread pool and clears the
/// hash table, so resending the same value on every request silently defeated
/// preserveHashTables and added latency to every navigation. Fix: cache the last value sent
/// per option and only resend on an actual change.
///
/// Not run in parallel with other tests in this assembly (see AssemblyInfo.cs) — this test
/// points the fake engine at a process-wide environment variable to capture exactly what
/// was sent over stdin.
/// </summary>
public class SetOptionCachingTests
{
    private const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Fact]
    public async Task RepeatedIdenticalCalls_OnlySendSetOptionOnce()
    {
        string logPath = Path.Combine(Path.GetTempPath(), $"chessdroid-setoption-{Guid.NewGuid()}.log");
        Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", logPath);
        try
        {
            var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
            var svc = new ChessEngineService(config);
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);

            // Same depth/multiPV three times in a row — Threads/MultiPV never change.
            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1);
            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1);
            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1);

            svc.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", null);
        }

        var sent = File.Exists(logPath) ? File.ReadAllLines(logPath) : Array.Empty<string>();

        int threadsSent = sent.Count(l => l.StartsWith("setoption name Threads value", StringComparison.Ordinal));
        int multiPvSent = sent.Count(l => l.StartsWith("setoption name MultiPV value", StringComparison.Ordinal));

        Assert.Equal(1, threadsSent);
        Assert.Equal(1, multiPvSent);

        try { File.Delete(logPath); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public async Task MultiPVChange_ResendsOnlyTheChangedOption()
    {
        string logPath = Path.Combine(Path.GetTempPath(), $"chessdroid-setoption-{Guid.NewGuid()}.log");
        Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", logPath);
        try
        {
            var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
            var svc = new ChessEngineService(config);
            await svc.InitializeAsync(FakeEngineFixture.NormalPath);

            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 1); // Threads=1, MultiPV=1
            await svc.GetBestMoveAsync(Fen, depth: 3, multiPV: 2); // Threads=1 (unchanged), MultiPV=2 (changed)

            svc.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", null);
        }

        var sent = File.Exists(logPath) ? File.ReadAllLines(logPath) : Array.Empty<string>();

        // Threads value never changed across the two calls -> sent once.
        Assert.Equal(1, sent.Count(l => l.StartsWith("setoption name Threads value", StringComparison.Ordinal)));
        // MultiPV changed 1 -> 2 -> sent twice (once per distinct value).
        Assert.Equal(2, sent.Count(l => l.StartsWith("setoption name MultiPV value", StringComparison.Ordinal)));

        try { File.Delete(logPath); } catch { /* best effort cleanup */ }
    }
}
