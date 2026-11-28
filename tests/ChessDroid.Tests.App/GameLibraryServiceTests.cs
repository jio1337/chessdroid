using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// GameLibraryService had zero test coverage before this. LoadAll() also had the same silent
/// data-loss bug AppConfig used to have (see AppConfigTests): a corrupt saved-game file was
/// silently dropped on a bare `catch { }`, with no quarantine and no log entry, so a user could
/// lose a saved game from the library with no trace it ever happened. Fixed to log via
/// CrashLogger and quarantine the bad file instead of deleting/ignoring it, mirroring
/// AppConfig.QuarantineCorruptConfig. These tests cover both the normal save/load/delete flow
/// and that regression.
///
/// Each test gets its own unique temp folder (constructor-per-test under xUnit), so unlike
/// AppConfigTests these don't need to worry about a shared static file path.
/// </summary>
public class GameLibraryServiceTests : IDisposable
{
    private static readonly string[] NewestFirstIds = { "g3", "g2", "g1" };

    private readonly string _appFolder;
    private readonly GameLibraryService _library;

    public GameLibraryServiceTests()
    {
        _appFolder = Path.Combine(Path.GetTempPath(), "chessdroid-tests-" + Guid.NewGuid().ToString("N"));
        _library = new GameLibraryService(_appFolder);
    }

    public void Dispose()
    {
        try { Directory.Delete(_appFolder, recursive: true); } catch { /* best effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    private static SavedGame MakeGame(string id, DateTime savedAt) => new()
    {
        Id = id,
        White = "Alice",
        Black = "Bob",
        Pgn = "1. e4 e5",
        SavedAt = savedAt,
    };

    [Fact]
    public void Constructor_CreatesGamesSubfolder()
        => Assert.True(Directory.Exists(_library.GamesFolder));

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var game = MakeGame("abc123", DateTime.Now);
        _library.Save(game);

        var loaded = _library.Load("abc123");

        Assert.NotNull(loaded);
        Assert.Equal("Alice", loaded!.White);
        Assert.Equal("Bob", loaded.Black);
        Assert.Equal("1. e4 e5", loaded.Pgn);
    }

    [Fact]
    public void Load_NonexistentId_ReturnsNull()
        => Assert.Null(_library.Load("does-not-exist"));

    [Fact]
    public void Delete_RemovesGameFile()
    {
        _library.Save(MakeGame("todelete", DateTime.Now));
        Assert.NotNull(_library.Load("todelete"));

        _library.Delete("todelete");

        Assert.Null(_library.Load("todelete"));
    }

    [Fact]
    public void Delete_NonexistentId_DoesNotThrow()
        => _library.Delete("never-existed");

    [Fact]
    public void LoadAll_EmptyFolder_ReturnsEmptyList()
        => Assert.Empty(_library.LoadAll());

    [Fact]
    public void LoadAll_ReturnsGamesSortedNewestFirst()
    {
        var oldest = MakeGame("g1", new DateTime(2026, 1, 1));
        var middle = MakeGame("g2", new DateTime(2026, 6, 1));
        var newest = MakeGame("g3", new DateTime(2026, 11, 1));
        _library.Save(oldest);
        _library.Save(newest);
        _library.Save(middle);

        var all = _library.LoadAll();

        Assert.Equal(NewestFirstIds, all.Select(g => g.Id));
    }

    [Fact]
    public void LoadAll_CorruptFile_IsQuarantinedInsteadOfSilentlyDropped()
    {
        _library.Save(MakeGame("valid", DateTime.Now));
        string corruptPath = Path.Combine(_library.GamesFolder, "corrupt.json");
        File.WriteAllText(corruptPath, "{ this is not valid json");

        var result = _library.LoadAll();

        Assert.Single(result);
        Assert.Equal("valid", result[0].Id);
        Assert.False(File.Exists(corruptPath), "the corrupt file should be moved aside, not left in place");
        Assert.Contains(
            Directory.GetFiles(_library.GamesFolder, "corrupt.corrupt-*.json"),
            _ => true);
    }

    [Fact]
    public void LoadAll_CorruptFile_DoesNotThrow()
    {
        File.WriteAllText(Path.Combine(_library.GamesFolder, "bad.json"), "not json at all");

        var result = _library.LoadAll();

        Assert.Empty(result);
    }

    [Fact]
    public void Load_CorruptFile_IsQuarantinedInsteadOfSilentlyReturningNull()
    {
        // Load(id) used to have its own separate bare catch-returns-null — same silent
        // data-loss shape LoadAll() was already fixed away from, just not yet applied here.
        string path = Path.Combine(_library.GamesFolder, "corrupt.json");
        File.WriteAllText(path, "{ not valid json");

        var result = _library.Load("corrupt");

        Assert.Null(result);
        Assert.False(File.Exists(path), "the corrupt file should be moved aside, not left in place");
        Assert.Contains(
            Directory.GetFiles(_library.GamesFolder, "corrupt.corrupt-*.json"),
            _ => true);
    }

    [Fact]
    public void Save_Twice_OverwritesRatherThanLeavingATempFileBehind()
    {
        var game = MakeGame("same-id", DateTime.Now);
        _library.Save(game);
        game.White = "Changed";
        _library.Save(game);

        var loaded = _library.Load("same-id");
        Assert.NotNull(loaded);
        Assert.Equal("Changed", loaded!.White);

        // The write-then-replace pattern must never leave its .tmp scratch file behind.
        Assert.Empty(Directory.GetFiles(_library.GamesFolder, "*.tmp"));
    }

    // ---- ImportPgns: "Save to Library" from the multi-game PGN picker ----

    private const string Ruy =
        "[Event \"London\"]\n[White \"Mackenzie, George Henry\"]\n[Black \"Steinitz, William\"]\n[Result \"0-1\"]\n\n" +
        "1.e4 e5 2.Nf3 Nc6 3.Bb5 a6 4.Ba4 Nf6 0-1";
    private const string Evans =
        "[Event \"London m5\"]\n[White \"Mackenzie, George Henry\"]\n[Black \"Paulsen, Louis\"]\n[Result \"1-0\"]\n\n" +
        "1.e4 e5 2.Nf3 Nc6 3.Bc4 Bc5 4.b4 Bxb4 1-0";

    [Fact]
    public void ImportPgns_SavesEachGameWithHeaders()
    {
        var (saved, skipped) = _library.ImportPgns(new[] { Ruy, Evans });

        Assert.Equal((2, 0), (saved, skipped));
        var all = _library.LoadAll();
        Assert.Equal(new[] { "Steinitz, William", "Paulsen, Louis" }, all.Select(g => g.Black)); // input order kept
        var ruy = all[0];
        Assert.Equal("Mackenzie, George Henry", ruy.White);
        Assert.Equal("London", ruy.Event);
        Assert.Equal("0-1", ruy.Result);
        Assert.Contains("4.Ba4 Nf6", ruy.Pgn);
        Assert.False(string.IsNullOrEmpty(ruy.Opening), "the opening should be detected from the moves");
    }

    [Fact]
    public void ImportPgns_SkipsGamesAlreadyInLibrary()
    {
        _library.ImportPgns(new[] { Ruy });
        var (saved, skipped) = _library.ImportPgns(new[] { Ruy, Evans });

        Assert.Equal((1, 1), (saved, skipped));
        Assert.Equal(2, _library.LoadAll().Count);
    }

    [Fact]
    public void ImportPgns_SkipsDuplicatesWithinTheBatch_IgnoringFormattingAndComments()
    {
        string reformatted = Ruy.Replace("1.e4 e5", "1. e4 {King's pawn} e5");
        var (saved, skipped) = _library.ImportPgns(new[] { Ruy, reformatted });

        Assert.Equal((1, 1), (saved, skipped));
    }

    [Fact]
    public void ImportPgns_SameMovesDifferentPlayers_AreDifferentGames()
    {
        string otherPlayers = Ruy.Replace("Steinitz, William", "Zukertort, Johannes Hermann");
        var (saved, _) = _library.ImportPgns(new[] { Ruy, otherPlayers });

        Assert.Equal(2, saved);
    }

    [Fact]
    public void SanMoves_StripsHeadersCommentsVariationsNagsAndResult()
    {
        string pgn = "[White \"A\"]\n\n1. e4 $1 {best} e5 (1... c5 2. Nf3 (2. c3)) 2. Nf3 ; note\nNc6 1-0";
        Assert.Equal(new[] { "e4", "e5", "Nf3", "Nc6" }, GameLibraryService.SanMoves(pgn));
    }

    [Fact]
    public void ImportPgns_CheckMarksAndAnnotations_DoNotMakeADifferentGame()
    {
        // The board writes mate as "#"; the 1874 record wrote "+". Same game either way.
        string record = Ruy.Replace("4.Ba4 Nf6", "4.Ba4+ Nf6!");
        var (saved, skipped) = _library.ImportPgns(new[] { Ruy, record });

        Assert.Equal((1, 1), (saved, skipped));
    }

    [Fact]
    public void Save_NewGame_DoesNotLeaveATempFileBehind()
    {
        _library.Save(MakeGame("fresh", DateTime.Now));
        Assert.Empty(Directory.GetFiles(_library.GamesFolder, "*.tmp"));
    }
}
