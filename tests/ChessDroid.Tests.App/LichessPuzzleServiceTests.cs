using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for:
/// - §2.8: GetDailyPuzzle/GetRandomBatch must sort Directory.GetFiles results (Array.Sort
///   with StringComparer.Ordinal) before indexing into them, so puzzle selection is
///   deterministic regardless of filesystem enumeration order.
/// - §2.3's actual precondition: a batch request that matches nothing (narrow filters, or
///   an empty folder) must come back as an empty list, not throw — PuzzleLoadNext's retry
///   loop and empty-queue guard depend on GetRandomBatch never throwing here.
/// </summary>
public class LichessPuzzleServiceTests : IDisposable
{
    private readonly string _tempDir;

    public LichessPuzzleServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "chessdroid-puzzle-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private static void WritePuzzleFile(string path, string puzzleId, string themes = "opening")
    {
        // PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags
        File.WriteAllLines(path, new[]
        {
            "PuzzleId,FEN,Moves,Rating,RatingDeviation,Popularity,NbPlays,Themes,GameUrl,OpeningTags",
            $"{puzzleId},rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1,e2e4 e7e5,1500,80,90,100,{themes},https://example.com,A00"
        });
    }

    [Fact]
    public void GetDailyPuzzle_SelectsFromOrdinalSortedFiles_NotCreationOrder()
    {
        // Write "b" before "a" so creation order and Ordinal-sorted order disagree.
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles_b.csv"), "FROM_B");
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles_a.csv"), "FROM_A");

        // Replicate GetDailyPuzzle's own file-selection math to predict which file it must
        // land on *if* it sorts (Ordinal) before indexing — this is exactly what the §2.8
        // fix (Array.Sort(files, StringComparer.Ordinal)) guarantees.
        var files = Directory.GetFiles(_tempDir, "*.csv");
        Array.Sort(files, StringComparer.Ordinal);
        int seed = DateTime.Today.Year * 10000 + DateTime.Today.Month * 100 + DateTime.Today.Day;
        int expectedFileIndex = new Random(seed).Next(files.Length);
        string expectedId = Path.GetFileNameWithoutExtension(files[expectedFileIndex]).EndsWith("_a") ? "FROM_A" : "FROM_B";

        var puzzle = LichessPuzzleService.GetDailyPuzzle(_tempDir);

        Assert.NotNull(puzzle);
        Assert.Equal(expectedId, puzzle!.Id);
    }

    [Fact]
    public void GetDailyPuzzle_SameDay_IsDeterministicAcrossCalls()
    {
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles_x.csv"), "X");
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles_y.csv"), "Y");

        var first = LichessPuzzleService.GetDailyPuzzle(_tempDir);
        var second = LichessPuzzleService.GetDailyPuzzle(_tempDir);

        Assert.NotNull(first);
        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public void GetDailyPuzzle_EmptyFolder_ReturnsNullWithoutThrowing()
    {
        Assert.Null(LichessPuzzleService.GetDailyPuzzle(_tempDir));
    }

    [Fact]
    public void GetRandomBatch_EmptyFolder_ReturnsEmptyListWithoutThrowing()
    {
        var result = LichessPuzzleService.GetRandomBatch(_tempDir, 200);
        Assert.Empty(result);
    }

    [Fact]
    public void GetRandomBatch_FilterMatchesNothing_ReturnsEmptyListWithoutThrowing()
    {
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles.csv"), "ONLY", themes: "endgame");

        // This is the exact precondition that used to crash PuzzleLoadNext's unguarded
        // _puzzleQueue[0] — a narrow filter combination legitimately matching nothing.
        var result = LichessPuzzleService.GetRandomBatch(_tempDir, 200, themeFilter: "nonexistentTheme12345");

        Assert.Empty(result);
    }

    [Fact]
    public void GetRandomBatch_MatchingTheme_ReturnsPuzzle()
    {
        WritePuzzleFile(Path.Combine(_tempDir, "puzzles.csv"), "ONLY", themes: "endgame fork");

        var result = LichessPuzzleService.GetRandomBatch(_tempDir, 200, themeFilter: "fork");

        Assert.NotEmpty(result);
        Assert.All(result, p => Assert.Equal("ONLY", p.Id));
    }
}
