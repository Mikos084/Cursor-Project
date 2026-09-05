using System.Threading;

namespace MultiplePointers;

internal static class Program
{
    private const string MutexName = @"Local\MultiplePointers.SingleInstance.v1";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(
            initiallyOwned: true,
            name: MutexName,
            createdNew: out bool createdNew);

        if (!createdNew)
        {
            string displayName = AppBranding.Format(SettingsStore.Load().DisplayName);
            MessageBox.Show(
                $"{displayName} jest już uruchomiony. Sprawdź ikonę programu przy zegarze Windows.",
                displayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, _) =>
        {
            // Never leave a global ClipCursor restriction behind after a UI crash.
            NativeMethods.ClipCursor(IntPtr.Zero);
            Application.Exit();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            NativeMethods.ClipCursor(IntPtr.Zero);
        };

        try
        {
            Application.Run(new TrayApplicationContext());
        }
        finally
        {
            // Final safety net for normal exit and exceptions escaping the loop.
            NativeMethods.ClipCursor(IntPtr.Zero);
        }
    }
}
