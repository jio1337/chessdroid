namespace ChessDroid.Services
{
    /// <summary>
    /// Minimal file logger for unhandled exceptions. Writes to logs/chessdroid-&lt;date&gt;.log
    /// next to the executable. Intentionally has no dependencies on AppConfig or WinForms
    /// so it can run even if those have failed to initialize.
    /// </summary>
    public static class CrashLogger
    {
        private static readonly object _lock = new object();

        private static string LogFilePath => Path.Combine(
            AppContext.BaseDirectory, "logs", $"chessdroid-{DateTime.Now:yyyy-MM-dd}.log");

        public static void LogException(Exception ex, string context)
        {
            try
            {
                lock (_lock)
                {
                    string dir = Path.Combine(AppContext.BaseDirectory, "logs");
                    Directory.CreateDirectory(dir);
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, entry);
                }
            }
            catch
            {
                // Logging must never throw during exception handling; nothing further we can do.
            }
        }
    }
}
