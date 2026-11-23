using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §2.7: the classification cache used to be keyed only by
/// FEN, so a cached result from a shallower depth than the current review (e.g. left over
/// from live analysis at depth 10, now reviewing at depth 20) was silently reused instead
/// of re-querying the engine. Fix: cache reads now require cached.Depth >= engineDepth.
/// </summary>
public class MoveClassificationServiceTests : IAsyncLifetime, IDisposable
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string AfterE4Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

    private ChessEngineService _engine = null!;

    public async Task InitializeAsync()
    {
        _engine = new ChessEngineService(new AppConfig { EngineResponseTimeoutMs = 5000, MaxEngineRetries = 2 });
        await _engine.InitializeAsync(FakeEngineFixture.NormalPath);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static List<MoveNode> OneMoveMainLine() => new()
    {
        new MoveNode { ParentFEN = StartFen, FEN = AfterE4Fen, UciMove = "e2e4", SanMove = "e4", HalfMove = 0 }
    };

    // GetPositionKey is private, but it's documented as "first 4 space-separated FEN fields" —
    // replicate that here to construct cache keys the same way the service would.
    private static string PositionKey(string fen)
    {
        var parts = fen.Split(' ');
        return parts.Length >= 4 ? $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}" : fen;
    }

    [Fact]
    public async Task ClassifyAsync_CacheEntryAtOrAboveRequestedDepth_IsUsedWithoutCallingEngine()
    {
        // Deliberately implausible eval the fake engine would never produce (it only ever
        // returns small centipawn-range evals), so if this value shows up in the result we
        // know it came from the cache, not a fresh engine call.
        var cache = new Dictionary<string, CachedAnalysis>
        {
            [PositionKey(StartFen)] = new CachedAnalysis { BestMove = "a2a3", Evaluation = "+9.99", Depth = 99 },
            [PositionKey(AfterE4Fen)] = new CachedAnalysis { BestMove = "a2a3", Evaluation = "+9.99", Depth = 99 },
        };

        var service = new MoveClassificationService(_engine);
        var result = await service.ClassifyAsync(OneMoveMainLine(), engineDepth: 5, cache, null, null, default);

        Assert.NotNull(result);
        var move = Assert.Single(result!.MoveResults);
        Assert.Equal(9.99, move.EvalBefore, precision: 2);
        Assert.Equal(9.99, move.EvalAfter, precision: 2);

        // Cache reads must not have been overwritten by a (non-existent) engine call.
        Assert.Equal(99, cache[PositionKey(StartFen)].Depth);
        Assert.Equal(99, cache[PositionKey(AfterE4Fen)].Depth);
    }

    [Fact]
    public async Task ClassifyAsync_CacheEntryBelowRequestedDepth_IsIgnoredAndRefreshed()
    {
        var cache = new Dictionary<string, CachedAnalysis>
        {
            [PositionKey(StartFen)] = new CachedAnalysis { BestMove = "a2a3", Evaluation = "+9.99", Depth = 1 },
            [PositionKey(AfterE4Fen)] = new CachedAnalysis { BestMove = "a2a3", Evaluation = "+9.99", Depth = 1 },
        };

        var service = new MoveClassificationService(_engine);
        var result = await service.ClassifyAsync(OneMoveMainLine(), engineDepth: 5, cache, null, null, default);

        Assert.NotNull(result);
        var move = Assert.Single(result!.MoveResults);

        // A stale shallow-depth cache entry must not win — the real (small) fake-engine
        // eval should appear instead of the implausible cached 9.99.
        Assert.NotEqual(9.99, move.EvalBefore, precision: 2);
        Assert.NotEqual(9.99, move.EvalAfter, precision: 2);

        // And the cache should now hold a fresh entry at the requested depth.
        Assert.Equal(5, cache[PositionKey(StartFen)].Depth);
        Assert.Equal(5, cache[PositionKey(AfterE4Fen)].Depth);
    }
}
