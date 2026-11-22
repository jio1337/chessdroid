using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §3.4's ECO consolidation: OpeningDatabase and EcoBookService
/// both used to independently parse the same ecoA-E.json files. A first consolidation
/// attempt routed EcoBookService through OpeningDatabase's already-loaded (position-deduped)
/// dictionary, which looked correct by inspection but silently dropped 233 real opening
/// entries and corrupted 21 more with shorter move sequences — the two consumers dedup by
/// incompatible keys (board position vs. (eco,name)), so one cannot be derived from the
/// other's deduped output. The fix shares only the raw, non-deduped parse (EcoFileCache);
/// each consumer still applies its own dedup on top.
///
/// These tests use the real bundled Books/ecoA-E.json data (~4.4MB, copied into this test
/// project's output via the chessdroid.csproj ProjectReference) and independently recompute
/// each consumer's expected dedup from EcoFileCache's raw rows, so a regression that silently
/// drops or corrupts entries — in either direction — fails here.
/// </summary>
public class EcoDataConsolidationTests
{
    private static string BooksFolder => Path.Combine(Application.StartupPath, "Books");

    [Fact]
    public void EcoFileCache_LoadsSubstantialRealData()
    {
        var raw = EcoFileCache.LoadRaw(BooksFolder);
        Assert.True(raw.Count > 1000, $"expected a substantial number of raw ECO entries, got {raw.Count}");
    }

    [Fact]
    public void EcoBookService_DedupesByEcoAndName_KeepingLongestMoveSequence_NoDataLoss()
    {
        var raw = EcoFileCache.LoadRaw(BooksFolder);
        var expected = raw
            .GroupBy(e => (e.Eco, e.Name))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.Moves.Length).First().Moves);

        var loaded = EcoBookService.LoadAll(BooksFolder);

        // Must not have silently dropped (or invented) any (eco,name) pair.
        Assert.Equal(expected.Count, loaded.Count);

        foreach (var entry in loaded)
        {
            bool found = expected.TryGetValue((entry.Eco, entry.Name), out var expectedMoves);
            Assert.True(found, $"unexpected (eco,name) pair in EcoBookService output: {entry.Eco} {entry.Name}");

            // Must keep the longest move sequence for this (eco,name), not a shorter/wrong one.
            Assert.Equal(expectedMoves, entry.Moves);
        }
    }

    [Fact]
    public void OpeningDatabase_DedupesByPiecePlacement_MatchesDistinctPositionCount()
    {
        var raw = EcoFileCache.LoadRaw(BooksFolder);
        int expectedCount = raw
            .Select(e => ExtractPiecePlacement(e.FullFen))
            .Where(pp => !string.IsNullOrEmpty(pp))
            .Distinct()
            .Count();

        Assert.Equal(expectedCount, OpeningDatabase.Instance.Count);
    }

    private static string ExtractPiecePlacement(string fen)
    {
        if (string.IsNullOrEmpty(fen)) return "";
        int sp = fen.IndexOf(' ');
        return sp > 0 ? fen.Substring(0, sp) : fen;
    }
}
