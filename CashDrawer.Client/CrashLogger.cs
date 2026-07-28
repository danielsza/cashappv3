using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    /// <summary>
    /// Last line of defence for client-side faults.
    ///
    /// Before 3.11.7 the client had no global exception handling at all: an
    /// unhandled exception on the UI thread killed it instantly and wrote nothing
    /// anywhere, so a cashier reporting "it crashed" left nothing to diagnose.
    /// Everything here is best-effort and must never throw - a logger that throws
    /// during a crash just hides the original fault.
    /// </summary>
    public static class CrashLogger
    {
        private static readonly object _lock = new();

        /// <summary>Folder the crash log is written to (same place as client_settings.json).</summary>
        public static string LogDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CashDrawer", "Logs");

        public static string CurrentLogFile =>
            Path.Combine(LogDirectory, $"client-errors-{DateTime.Now:yyyy-MM-dd}.log");

        /// <summary>
        /// Install handlers for every way a fault can escape on this process.
        /// Call once, before Application.Run.
        /// </summary>
        public static void Install()
        {
            // Route UI-thread exceptions to ThreadException instead of letting the
            // runtime tear the process down, so a fault in one click doesn't take
            // the till offline mid-shift.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
                Handle(e.Exception, "UI thread", terminating: false);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Handle(e.ExceptionObject as Exception, "background thread",
                       terminating: e.IsTerminating);

            // Faults inside an un-awaited Task surface here when it is collected.
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Handle(e.Exception, "unobserved task", terminating: false);
                e.SetObserved();
            };
        }

        /// <summary>Record a handled problem that the user shouldn't be interrupted for.</summary>
        public static void LogWarning(string context, Exception ex) =>
            Write(Format(ex, context, terminating: false));

        private static void Handle(Exception? ex, string source, bool terminating)
        {
            var text = Format(ex, source, terminating);
            Write(text);

            // A terminating fault gives us no usable UI - just get it on disk.
            if (terminating) return;

            try
            {
                MessageBox.Show(
                    "Something went wrong and the action may not have completed.\n\n" +
                    $"{ex?.GetType().Name}: {ex?.Message}\n\n" +
                    "The details were saved for support:\n" +
                    CurrentLogFile + "\n\n" +
                    "Check the drawer and the transaction log before repeating the action, " +
                    "so it isn't recorded twice.",
                    "Cash Drawer - Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch { /* No UI available - the file is what matters. */ }
        }

        private static string Format(Exception? ex, string source, bool terminating)
        {
            var sb = new StringBuilder();
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                sb.AppendLine(new string('=', 78));
                sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  client {version}  " +
                              $"{Environment.MachineName}\\{Environment.UserName}");
                sb.AppendLine($"source: {source}{(terminating ? "  (PROCESS TERMINATING)" : "")}");
                sb.AppendLine(ex?.ToString() ?? "(no exception object)");
            }
            catch
            {
                // Even formatting must not throw during a crash.
                sb.AppendLine("(failed to format exception)");
            }
            return sb.ToString();
        }

        private static void Write(string text)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(CurrentLogFile, text);
                }
            }
            catch { /* Nothing useful left to do. */ }
        }
    }
}
