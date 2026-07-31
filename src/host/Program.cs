using System.Diagnostics;
using System.Threading;

namespace OpenCodeDesktopWidget;

internal static class Program
{
    private const string MutexName = "OpenCodeDesktopWidget.WebView2.Singleton";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            NativeMethods.BroadcastShowMessage();
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception error)
        {
            MessageBox.Show(
                $"程序启动失败：\n\n{error.Message}\n\n请确认已安装 Microsoft Edge WebView2 Runtime。",
                AppConstants.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
