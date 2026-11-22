using ChessDroid;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression tests for audit §2.1: a corrupt config.json used to be silently overwritten
/// with defaults on the next load, wiping all user data (streaks, personal bests, engine
/// profiles, Elo ladder, opening stats). The fix: atomic save (temp file + File.Replace),
/// quarantine-don't-overwrite on a bad load, and a .bak of the last known-good load.
///
/// Not parallelized with other classes that touch config.json/.bak/.tmp in the same
/// directory — AppConfig's file paths are static, shared across the whole process.
/// </summary>
public class AppConfigTests : IDisposable
{
    private readonly string _configPath;
    private readonly string _backupPath;
    private readonly string _tempPath;
    private readonly string _dir;

    public AppConfigTests()
    {
        _dir = Application.StartupPath;
        _configPath = Path.Combine(_dir, "config.json");
        _backupPath = _configPath + ".bak";
        _tempPath = _configPath + ".tmp";
        CleanupFiles();
    }

    public void Dispose() => CleanupFiles();

    private void CleanupFiles()
    {
        TryDelete(_configPath);
        TryDelete(_backupPath);
        TryDelete(_tempPath);
        foreach (var f in Directory.GetFiles(_dir, "config.corrupt-*.json"))
            TryDelete(f);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var original = new AppConfig { EngineDepth = 17, Theme = "Light", Aggressiveness = 73 };
        original.Save();

        var loaded = AppConfig.Load();

        Assert.Equal(17, loaded.EngineDepth);
        Assert.Equal("Light", loaded.Theme);
        Assert.Equal(73, loaded.Aggressiveness);
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        new AppConfig { EngineDepth = 9 }.Save();

        Assert.True(File.Exists(_configPath));
        Assert.False(File.Exists(_tempPath), "SaveInternal must consume the .tmp file via File.Replace/Move, never leave it behind");
    }

    [Fact]
    public void Load_WithTruncatedConfig_QuarantinesInsteadOfOverwriting()
    {
        const string garbage = "{ \"EngineDepth\": 12, \"Theme\": \"Da"; // truncated JSON
        File.WriteAllText(_configPath, garbage);

        var loaded = AppConfig.Load();

        // Must not have overwritten/destroyed the bad file — it should be quarantined instead.
        Assert.False(File.Exists(_configPath) && File.ReadAllText(_configPath) == garbage);
        var quarantined = Directory.GetFiles(_dir, "config.corrupt-*.json");
        Assert.Single(quarantined);
        Assert.Equal(garbage, File.ReadAllText(quarantined[0]));

        // Load() must still return a usable config (defaults, since no .bak exists here).
        Assert.NotNull(loaded);
    }

    [Fact]
    public void Load_WithCorruptConfigButGoodBackup_RecoversFromBackup()
    {
        // 1) Save a good config, then Load() once so Load's own success path refreshes .bak.
        new AppConfig { EngineDepth = 20, Theme = "Light" }.Save();
        var afterFirstLoad = AppConfig.Load();
        Assert.Equal(20, afterFirstLoad.EngineDepth);
        Assert.True(File.Exists(_backupPath), "a successful Load() should refresh config.json.bak");

        // 2) Corrupt config.json.
        File.WriteAllText(_configPath, "not valid json at all {{{");

        // 3) Load() should quarantine the corrupt file and recover EngineDepth=20 from .bak.
        var recovered = AppConfig.Load();

        Assert.Equal(20, recovered.EngineDepth);
        Assert.Equal("Light", recovered.Theme);
        Assert.Single(Directory.GetFiles(_dir, "config.corrupt-*.json"));

        // Recovery re-saves config.json from the backup, so a plain Load() right after
        // should see the recovered data again without needing the backup a second time.
        var reloaded = AppConfig.Load();
        Assert.Equal(20, reloaded.EngineDepth);
    }

    [Fact]
    public void Load_WithNoConfigAtAll_FallsBackToDefaultsWithoutThrowing()
    {
        var loaded = AppConfig.Load();

        Assert.NotNull(loaded);
        Assert.True(File.Exists(_configPath), "Load() with nothing on disk should persist the new defaults");
    }
}
