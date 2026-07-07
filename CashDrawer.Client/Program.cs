using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CashDrawer.Client
{
    static class Program
    {
        // Machine-wide single-instance guard. "Global\" makes it span all user
        // sessions so only ONE client can run on the computer at a time.
        private const string MutexName = "Global\\CashDrawerClient_SingleInstance";
        private static Mutex? _instanceMutex;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            _instanceMutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Another instance already owns the mutex. Silently surface the
                // existing window and exit without starting a second client
                // (prevents duplicate logging / tally corruption) — no popup.
                TryFocusExistingInstance();
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.Run(new MainForm());
            }
            finally
            {
                try { _instanceMutex?.ReleaseMutex(); } catch { }
                _instanceMutex?.Dispose();
            }
        }

        private static void TryFocusExistingInstance()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                var other = Process.GetProcessesByName(current.ProcessName)
                    .FirstOrDefault(p => p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero);

                if (other != null)
                {
                    ShowWindow(other.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(other.MainWindowHandle);
                }
            }
            catch
            {
                // Best-effort only (e.g. instance is minimized to tray with no
                // main window handle) - the message box below still informs the user.
            }
        }
    }
}
