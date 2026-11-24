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
}
