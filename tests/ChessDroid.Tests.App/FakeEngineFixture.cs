namespace ChessDroid.Tests.App;

/// <summary>
/// Locates the FakeUciEngine build next to this test assembly and produces the
/// differently-named copies (hang/crash/nullmove) that Program.cs sniffs its own
/// process name for — see FakeUciEngine/Program.cs for why name-based, not args-based
/// (ChessEngineService.InitializeAsync starts the process with no arguments).
/// </summary>
public static class FakeEngineFixture
{
    private static readonly object _lock = new();
    private static bool _variantsReady;

    public static string NormalPath => GetPath("fakeuciengine");
    public static string HangPath => GetPath("fakeuciengine-hang");
    public static string CrashPath => GetPath("fakeuciengine-crash");
    public static string NullMovePath => GetPath("fakeuciengine-nullmove");

    private static string GetPath(string baseName)
    {
        EnsureVariants();
        return Path.Combine(AppContext.BaseDirectory, baseName + ".exe");
    }

    private static void EnsureVariants()
    {
        if (_variantsReady) return;
        lock (_lock)
        {
            if (_variantsReady) return;

            string dir = AppContext.BaseDirectory;
            string sourceExe = Path.Combine(dir, "fakeuciengine.exe");
            if (!File.Exists(sourceExe))
                throw new FileNotFoundException(
                    "fakeuciengine.exe not found next to test assembly — the ProjectReference to " +
                    "FakeUciEngine.csproj should copy it here automatically.", sourceExe);

            string[] extensions = { ".exe", ".dll", ".runtimeconfig.json", ".deps.json" };
            foreach (var variant in new[] { "fakeuciengine-hang", "fakeuciengine-crash", "fakeuciengine-nullmove" })
            {
                foreach (var ext in extensions)
                {
                    string src = Path.Combine(dir, "fakeuciengine" + ext);
                    string dst = Path.Combine(dir, variant + ext);
                    if (File.Exists(src))
                        File.Copy(src, dst, overwrite: true);
                }
            }

            _variantsReady = true;
        }
    }
}
