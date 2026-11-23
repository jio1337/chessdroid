using System.Resources;
using ChessDroid.Services;

[assembly: NeutralResourcesLanguage("en-US")]

namespace ChessDroid
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            Application.ThreadException += OnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var config = AppConfig.Load();
            Application.Run(new AnalysisBoardForm(config));
        }

        // Exceptions on a background thread outside any try/catch (e.g. a non-UI thread crash).
        // The process is going down either way; log, tell the user, then let it terminate.
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new InvalidOperationException($"Non-exception object thrown: {e.ExceptionObject}");
            CrashLogger.LogException(ex, "Fatal (AppDomain.UnhandledException)");
            ShowCrashDialog(ex, fatal: true);
        }

        // Exceptions raised on the WinForms UI thread (e.g. inside an event handler) that
        // weren't caught locally. The message loop survives this, so the app can keep running.
        private static void OnThreadException(object? sender, System.Threading.ThreadExceptionEventArgs e)
        {
            CrashLogger.LogException(e.Exception, "UI thread exception (Application.ThreadException)");
            ShowCrashDialog(e.Exception, fatal: false);
        }

        // A Task faulted and nothing ever observed its exception (common with fire-and-forget
        // `_ = SomeAsync()` calls). Just log it — by the time the GC raises this, there is no
        // user-facing context left to show a dialog for.
        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            CrashLogger.LogException(e.Exception, "Unobserved task exception (TaskScheduler.UnobservedTaskException)");
            e.SetObserved();
        }

        private static void ShowCrashDialog(Exception ex, bool fatal)
        {
            string title = fatal ? "Chessdroid — Fatal Error" : "Chessdroid — Unexpected Error";
            string message = fatal
                ? $"Chessdroid hit an unrecoverable error and needs to close.\n\nDetails have been saved to the logs folder.\n\n{ex.Message}"
                : $"Chessdroid hit an unexpected error but should be able to continue.\n\nDetails have been saved to the logs folder.\n\n{ex.Message}";

            try
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK,
                    fatal ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            catch
            {
                // If even showing the dialog fails, the log entry is the only record left.
            }
        }
    }
}