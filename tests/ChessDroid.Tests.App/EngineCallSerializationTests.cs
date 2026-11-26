namespace ChessDroid.Tests.App;

/// <summary>
/// Regression test for this session's locking-consistency fix (CHESSDROID_AUDIT.md §5.1):
/// GetMoveForMatchAsync/GetPositionEvalAsync/GetPositionEvalTimedAsync used to only check
/// `State != EngineState.Ready` (check-then-act) instead of taking `_analysisSemaphore` like
/// GetBestMoveAsync/RunContinuousAnalysisAsync/StreamPositionEvalAsync already did.
/// `SafeWriteLineAsync` itself has zero internal synchronization (confirmed by reading it —
/// just WriteLineAsync + FlushAsync on the shared StreamWriter), so two of the previously
/// "protected only by a State check" methods, started back-to-back with no await between
/// them, both read State == Ready before either flips it to Analyzing (SyncWithEngineAsync's
/// first real I/O await happens before that point) — meaning in the old code both proceed to
/// send "position"/"go" commands concurrently with no mutual exclusion at all.
///
/// Proven here by firing GetPositionEvalAsync and GetMoveForMatchAsync at the same
/// ChessEngineService instance with no await between the two calls, then checking, via the
/// fake engine's received-command log, that their "position fen"/"go " command pairs never
/// interleave. With the semaphore fix in place this is guaranteed by construction (zero race
/// window), so every iteration below always passes. Without the fix, a single pair of calls
/// only hits the race window often enough to fail about 1 run in 5 (verified manually against
/// the pre-fix code: both calls read State==Ready before either flips it to Analyzing, since
/// SyncWithEngineAsync's first real I/O await happens first — sometimes one loses the State
/// check instead and silently returns null, sometimes both proceed and interleave) — so this
/// repeats the race 15 times to make the regression test itself reliable in both directions.
///
/// Not run in parallel with other tests in this assembly (see AssemblyInfo.cs) — like
/// SetOptionCachingTests, this depends on CHESSDROID_FAKEENGINE_LOG being process-wide.
/// </summary>
public class EngineCallSerializationTests
{
    private const string FenA = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string FenB = "r1bqkbnr/pppppppp/2n5/8/8/2N5/PPPPPPPP/R1BQKBNR w KQkq - 0 1";

    [Fact]
    public async Task ConcurrentCalls_OnSameEngine_NeverInterleaveCommands()
    {
        string logPath = Path.Combine(Path.GetTempPath(), $"chessdroid-serialize-{Guid.NewGuid()}.log");
        Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", logPath);
        try
        {
            // The race only reliably manifests on a freshly-initialized engine (a warmed-up
            // thread pool/JIT seems to consistently resolve the ordering one particular way
            // within a single process — repeating on one long-lived instance did not
            // reproduce it) — so each iteration gets its own fresh ChessEngineService.
            const int iterations = 8;
            for (int i = 0; i < iterations; i++)
            {
                var config = new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 };
                var svc = new ChessDroid.Services.ChessEngineService(config);
                await svc.InitializeAsync(FakeEngineFixture.NormalPath);

                // Both of these only checked `State == Ready` before this session's fix —
                // fired with no await between them so both read Ready before either sets
                // Analyzing.
                var t1 = svc.GetPositionEvalAsync(FenA, depth: 5, CancellationToken.None);
                var t2 = svc.GetMoveForMatchAsync(FenB, "go depth 5", timeoutMs: 5000, CancellationToken.None);
                await Task.WhenAll(t1, t2);

                svc.Dispose();
            }

            var sent = File.Exists(logPath) ? File.ReadAllLines(logPath) : Array.Empty<string>();
            var relevant = sent.Where(l =>
                l.StartsWith("position fen", StringComparison.Ordinal) ||
                l.StartsWith("go depth", StringComparison.Ordinal)).ToList();

            // Each of the 15 iterations should contribute exactly one "position"+"go" pair
            // from each of the 2 calls = 4 lines/iteration, strictly alternating. A missing
            // line means one call silently bailed out on a stale State check (the other
            // symptom of the same race); two "position" (or two "go") lines back-to-back
            // means the commands actually interleaved. Either way is the bug this fix closed.
            Assert.Equal(iterations * 4, relevant.Count);

            for (int i = 0; i < relevant.Count; i++)
            {
                bool isPosition = relevant[i].StartsWith("position fen", StringComparison.Ordinal);
                bool expectedPosition = i % 2 == 0;
                Assert.True(isPosition == expectedPosition,
                    $"commands interleaved at index {i}: {string.Join(" | ", relevant)}");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHESSDROID_FAKEENGINE_LOG", null);
            try { File.Delete(logPath); } catch { /* best effort cleanup */ }
        }
    }
}
