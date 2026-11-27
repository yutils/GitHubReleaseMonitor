using System;
using System.Threading;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool noAutoStart = HasArgument(args, "--no-autostart");
            bool startMinimized = HasArgument(args, "--minimized");

            bool createdNew;
            using (Mutex mutex = new Mutex(true, AppInfo.SingleInstanceMutex, out createdNew))
            {
                if (!createdNew)
                {
                    ActivateExistingWindow();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                Log.Info("程序启动，版本 " + AppInfo.Version);
                AppConfig config = ConfigStore.Load();
                L10n.SetLanguage(config.Settings.Language);

                Application.Run(new MainForm(config, noAutoStart, startMinimized));
                Log.Info("程序退出");
                GC.KeepAlive(mutex);
            }
        }

        private static bool HasArgument(string[] args, string value)
        {
            if (args == null) return false;
            foreach (string argument in args)
            {
                if (string.Equals(argument, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static void ActivateExistingWindow()
        {
            IntPtr handle = IntPtr.Zero;
            for (int i = 0; i < 25; i++)
            {
                handle = NativeMethods.FindWindow(null, MainForm.WindowTitle);
                if (handle != IntPtr.Zero) break;
                Thread.Sleep(120);
            }
            if (handle != IntPtr.Zero)
            {
                NativeMethods.PostMessage(handle, AppMessages.ShowWindow, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("未处理的界面异常", e.Exception);
            try
            {
                MessageBox.Show(L10n.F("program.errorMessage", e.Exception.Message), AppInfo.Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            Log.Error("未处理的异常", exception);
        }
    }
}
