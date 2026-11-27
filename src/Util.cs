using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GitHubReleaseMonitor
{
    internal static class AppInfo
    {
        public const string Name = "GitHub Release Monitor";
        public static readonly string Version = ReadVersion();
        public const string UserAgent = "GitHubReleaseMonitor/1.0";
        public const string SingleInstanceMutex = @"Local\GitHubReleaseMonitor.SingleInstance";
        public const string ShowWindowMessage = "GitHubReleaseMonitor.ShowWindow";
        public const string AutoStartValueName = "GitHubReleaseMonitor";

        private static string ReadVersion()
        {
            System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null) return "0.0.1";
            int build = version.Build < 0 ? 0 : version.Build;
            return version.Major + "." + version.Minor + "." + build;
        }
    }

    internal static class GitHubSlug
    {
        public static bool TryParse(string input, out string owner, out string repo)
        {
            owner = "";
            repo = "";
            if (string.IsNullOrEmpty(input)) return false;

            string value = input.Trim();
            int cut = value.IndexOfAny(new char[] { '?', '#' });
            if (cut >= 0) value = value.Substring(0, cut);
            value = value.TrimEnd('/');

            if (value.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring("git@github.com:".Length);
            }
            else
            {
                int index = value.IndexOf("github.com/", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    value = value.Substring(index + "github.com/".Length);
                }
            }

            value = value.Trim('/');
            if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 4);
            }

            string[] parts = value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            owner = parts[0].Trim();
            repo = parts[1].Trim();
            return IsValidPart(owner) && IsValidPart(repo);
        }

        private static bool IsValidPart(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 100) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.';
                if (!ok) return false;
            }
            return value != "." && value != "..";
        }
    }

    internal static class TimeText
    {
        public static bool TryParseUtc(string value, out DateTime utc)
        {
            utc = DateTime.MinValue;
            if (string.IsNullOrEmpty(value)) return false;
            DateTime parsed;
            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
            {
                return false;
            }
            utc = parsed;
            return true;
        }

        public static string Relative(DateTime utc)
        {
            TimeSpan span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return L10n.T("time.justNow");
            if (span.TotalMinutes < 60)
            {
                int minutes = (int)Math.Floor(span.TotalMinutes);
                return L10n.F(minutes == 1 ? "time.minuteAgo" : "time.minutesAgo", minutes);
            }
            if (span.TotalHours < 24)
            {
                int hours = (int)Math.Floor(span.TotalHours);
                return L10n.F(hours == 1 ? "time.hourAgo" : "time.hoursAgo", hours);
            }
            if (span.TotalDays < 30)
            {
                int days = (int)Math.Floor(span.TotalDays);
                return L10n.F(days == 1 ? "time.dayAgo" : "time.daysAgo", days);
            }
            return utc.ToLocalTime().ToString("yyyy-MM-dd");
        }

        public static string RelativeFromLocal(DateTime localTime)
        {
            if (localTime == DateTime.MinValue) return L10n.T("time.never");
            return Relative(localTime.ToUniversalTime());
        }

        public static string Countdown(TimeSpan span)
        {
            if (span.TotalSeconds <= 0) return L10n.T("time.dueNow");
            if (span.TotalSeconds < 60) return L10n.F("time.inSeconds", (int)Math.Ceiling(span.TotalSeconds));
            if (span.TotalMinutes < 60) return L10n.F("time.inMinutes", (int)Math.Ceiling(span.TotalMinutes));
            int hours = (int)Math.Floor(span.TotalHours);
            int minutes = (int)Math.Ceiling(span.TotalMinutes - hours * 60);
            if (minutes >= 60)
            {
                hours++;
                minutes = 0;
            }
            if (minutes == 0) return L10n.F("time.inHours", hours);
            return L10n.F("time.inHoursMinutes", hours, minutes);
        }

        public static string Interval(int minutes)
        {
            if (minutes < 60) return L10n.F("time.intervalMinutes", minutes);
            int hours = minutes / 60;
            int remainder = minutes % 60;
            if (remainder == 0) return L10n.F("time.intervalHours", hours);
            return L10n.F("time.intervalHoursMinutes", hours, remainder);
        }
    }

    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void Apply(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        Log.Error("无法打开开机启动注册表项", null);
                        return;
                    }

                    string expected = "\"" + Application.ExecutablePath + "\" --minimized";
                    object current = key.GetValue(AppInfo.AutoStartValueName);

                    if (enabled)
                    {
                        string currentText = current as string;
                        if (currentText != expected)
                        {
                            key.SetValue(AppInfo.AutoStartValueName, expected, RegistryValueKind.String);
                        }
                    }
                    else if (current != null)
                    {
                        key.DeleteValue(AppInfo.AutoStartValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("更新开机启动设置失败", ex);
            }
        }
    }

    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void StartWindowDrag(IntPtr handle)
        {
            ReleaseCapture();
            SendMessage(handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        public static void TryEnableRoundedCorners(IntPtr handle)
        {
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
            }
        }
    }

    internal static class AppMessages
    {
        public static readonly int ShowWindow = NativeMethods.RegisterWindowMessage(AppInfo.ShowWindowMessage);
    }

    internal static class AppIcon
    {
        private static Icon _large;
        private static Icon _small;
        private static Icon _unread;
        private static Bitmap _bitmap32;
        private static Bitmap _bitmap64;

        public static Icon Large
        {
            get
            {
                if (_large == null) _large = LoadIcon(0);
                return _large;
            }
        }

        public static Icon Small
        {
            get
            {
                if (_small == null) _small = LoadIcon(Math.Max(16, SystemInformation.SmallIconSize.Width));
                return _small;
            }
        }

        public static Icon Unread
        {
            get
            {
                if (_unread != null) return _unread;
                try
                {
                    int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
                    using (Bitmap bitmap = new Bitmap(size, size))
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.Clear(Color.Transparent);
                            graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            using (Icon baseIcon = LoadIcon(size))
                            {
                                graphics.DrawIcon(baseIcon, new Rectangle(0, 0, size, size));
                            }

                            int dot = Math.Max(6, size * 3 / 8);
                            int x = size - dot;
                            int y = 0;
                            using (SolidBrush white = new SolidBrush(Color.White))
                            {
                                graphics.FillEllipse(white, x - 1, y - 1, dot + 2, dot + 2);
                            }
                            using (SolidBrush red = new SolidBrush(Theme.Red))
                            {
                                graphics.FillEllipse(red, x, y, dot, dot);
                            }
                        }
                        _unread = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("生成未读托盘图标失败", ex);
                    _unread = LoadIcon(Math.Max(16, SystemInformation.SmallIconSize.Width));
                }
                return _unread;
            }
        }

        public static Bitmap Bitmap32
        {
            get
            {
                if (_bitmap32 == null) _bitmap32 = LoadIcon(32).ToBitmap();
                return _bitmap32;
            }
        }

        public static Bitmap Bitmap64
        {
            get
            {
                if (_bitmap64 == null) _bitmap64 = LoadIcon(64).ToBitmap();
                return _bitmap64;
            }
        }

        private static Icon LoadIcon(int size)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("app.ico"))
                {
                    if (stream == null) return SystemIcons.Application;
                    if (size > 0) return new Icon(stream, size, size);
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Error("加载应用图标失败", ex);
                return SystemIcons.Application;
            }
        }
    }
}
