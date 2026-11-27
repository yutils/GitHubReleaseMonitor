using System.Collections.Generic;
using System.Globalization;

namespace GitHubReleaseMonitor
{
    internal static class L10n
    {
        private static readonly Dictionary<string, string> Chinese =
            new Dictionary<string, string>();
        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>();

        private static string _language = "zh-CN";

        public static bool IsEnglish
        {
            get { return _language == "en-US"; }
        }

        public static string Current
        {
            get { return _language; }
        }

        public static void SetLanguage(string language)
        {
            _language = string.Equals(language, "en-US", System.StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : "zh-CN";
        }

        public static string T(string key)
        {
            string value;
            if (IsEnglish && English.TryGetValue(key, out value)) return value;
            if (Chinese.TryGetValue(key, out value)) return value;
            return key;
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }

        private static void A(string key, string chinese, string english)
        {
            Chinese[key] = chinese;
            English[key] = english;
        }

        static L10n()
        {
            A("common.addRepo", "+  添加仓库", "+  Add repo");
            A("common.addRepoShort", "添加仓库", "Add repository");
            A("common.cancel", "取消", "Cancel");
            A("common.save", "保存", "Save");
            A("common.open", "打开", "Open");
            A("common.minimize", "最小化", "Minimize");
            A("common.closeToTray", "关闭到托盘", "Close to tray");
            A("common.settings", "设置", "Settings");
            A("common.settingsTooltip", "设置 (Ctrl+,)", "Settings (Ctrl+,)");
            A("common.unknownError", "未知错误", "Unknown error");
            A("common.checkFailed", "检查失败", "Check failed");
            A("common.noRelease", "暂无 Release", "No releases");
            A("common.waitingFirstCheck", "等待首次检查", "Waiting for first check");
            A("common.synced", "已同步，等待更新", "Synced, waiting for updates");
            A("common.upToDate", "已是最新", "Up to date");
            A("common.updateAvailable", "发现新版本", "Update available");
            A("common.paused", "已暂停监听", "Paused");
            A("common.versionUnknown", "尚未获取到版本信息", "No version information yet");

            A("time.never", "尚未检查", "Never checked");
            A("time.justNow", "刚刚", "just now");
            A("time.minutesAgo", "{0} 分钟前", "{0} min ago");
            A("time.hoursAgo", "{0} 小时前", "{0} hr ago");
            A("time.daysAgo", "{0} 天前", "{0} days ago");
            A("time.minuteAgo", "{0} 分钟前", "{0} minute ago");
            A("time.hourAgo", "{0} 小时前", "{0} hour ago");
            A("time.dayAgo", "{0} 天前", "{0} day ago");
            A("time.dueNow", "即将检查", "due now");
            A("time.inSeconds", "{0} 秒后", "in {0} sec");
            A("time.inMinutes", "{0} 分钟后", "in {0} min");
            A("time.inHours", "{0} 小时后", "in {0} hr");
            A("time.inHoursMinutes", "{0} 小时 {1} 分钟后", "in {0} hr {1} min");
            A("time.intervalMinutes", "{0} 分钟", "{0} min");
            A("time.intervalHours", "{0} 小时", "{0} hr");
            A("time.intervalHoursMinutes", "{0} 小时 {1} 分钟", "{0} hr {1} min");

            A("header.noRepos", "添加仓库后开始监听 Release 更新", "Add a repository to start monitoring releases");
            A("header.checking", "正在检查 {0} 个仓库…", "Checking {0} repositories…");
            A("header.checkingOne", "正在检查 {0} 个仓库…", "Checking {0} repository…");
            A("header.paused", "自动检查已暂停 · 共 {0} 个仓库", "Auto check paused · {0} repositories");
            A("header.pausedOne", "自动检查已暂停 · 共 {0} 个仓库", "Auto check paused · {0} repository");
            A("header.unread", "{0} 个仓库有新版本 · 共 {1} 个仓库", "{0} repositories with updates · {1} total");
            A("header.unreadOne", "{0} 个仓库有新版本 · 共 {1} 个仓库", "{0} repository with updates · {1} total");
            A("header.interval", "每 {0}检查一次 · 共 {1} 个仓库", "Every {0} · {1} repositories");
            A("header.intervalOne", "每 {0}检查一次 · 共 {1} 个仓库", "Every {0} · {1} repository");
            A("header.languageToEnglish", "切换到英文", "Switch to English");
            A("header.languageToChinese", "切换到中文", "Switch to Chinese");

            A("main.title", "监听的仓库", "Repositories");
            A("main.count", "共 {0} 个仓库", "{0} repositories");
            A("main.countOne", "共 {0} 个仓库", "{0} repository");
            A("main.countUnread", "共 {0} 个仓库 · {1} 个未读", "{0} repositories · {1} unread");
            A("main.countUnreadOne", "共 {0} 个仓库 · {1} 个未读", "{0} repository · {1} unread");
            A("main.checkNow", "立即检查", "Check now");
            A("main.checkingButton", "检查中…", "Checking…");
            A("main.checkingStatus", "正在检查更新…", "Checking for updates…");
            A("main.pausedStatus", "自动检查已暂停", "Auto check paused");
            A("main.noReposStatus", "暂无监听的仓库，点击「添加仓库」开始", "No repositories yet. Click “Add repo” to start.");
            A("main.allPausedStatus", "所有仓库均已暂停监听", "All repositories are paused");
            A("main.nextStatus", "下次检查 {0}（{1}）", "Next check {0} ({1})");
            A("main.dueNow", "即将检查", "Check due now");
            A("main.lastStatus", "上次检查 {0}", "Last checked {0}");
            A("main.neverStatus", "尚未检查", "Never checked");
            A("main.apiTooltip", "GitHub API 配额：剩余 {0} / {1}", "GitHub API quota: {0} / {1} remaining");
            A("main.apiReset", "配额重置时间：{0}", "Quota resets at {0}");
            A("main.apiHint", "GitHub API 每小时有请求配额限制，未认证为 60 次/小时。", "GitHub API has an hourly request quota (60/hour without a token).");
            A("main.trayRepos", "{0} 个仓库", "{0} repos");
            A("main.trayReposOne", "{0} 个仓库", "{0} repo");
            A("main.trayUpdates", "{0} 个新版本", "{0} new");
            A("main.trayAppUpdate", "新版本 v{0}", "v{0} available");
            A("main.checkUpdate", "检查更新", "Check for updates");
            A("main.checkingUpdateShort", "检查中…", "Checking…");
            A("main.upToDateShort", "已是最新", "Up to date");
            A("main.checkUpdateTooltip", "当前版本 v{0}，点击检查更新", "Current version v{0}. Click to check for updates.");
            A("main.checkingUpdateTooltip", "正在检查软件更新…", "Checking for updates…");
            A("main.updateReadyTooltip", "发现新版本 v{0}，点击打开 Release 页面", "Version v{0} is available. Click to open the release.");
            A("main.upToDateTooltip", "当前已是最新版本", "You're up to date.");
            A("main.trayUpdate", "检查更新", "Check for updates");
            A("main.trayUpdateAvailable", "有新版本 v{0}", "v{0} available");
            A("main.trayOpen", "打开主界面", "Open main window");
            A("main.trayCheck", "立即检查", "Check now");
            A("main.trayPause", "暂停自动检查", "Pause auto check");
            A("main.trayResume", "恢复自动检查", "Resume auto check");
            A("main.trayExit", "退出", "Exit");
            A("main.minimizedTitle", "已最小化到托盘", "Minimized to tray");
            A("main.minimizedText", "GitHub Release Monitor 会继续在后台检查更新。双击托盘图标可重新打开。", "GitHub Release Monitor keeps checking in the background. Double-click the tray icon to reopen.");
            A("main.updateTitle", "{0} 有新版本", "New release from {0}");
            A("main.updateText", "{0} 已发布，点击查看 Release。", "{0} is now available. Click to view the release.");
            A("main.newVersion", "新版本", "New release");
            A("main.multiTitle", "{0} 个仓库有新版本", "{0} repositories have new releases");
            A("main.multiText", "{0}，点击查看。", "{0}. Click to view.");
            A("main.multiEllipsis", "等 {0} 个", "and {0} more");
            A("main.listSeparator", "、", ", ");
            A("main.errorTitle", "检查更新失败", "Update check failed");
            A("main.errorOne", "{0}：{1}", "{0}: {1}");
            A("main.errorMany", "{0} 个仓库检查失败，请打开主界面查看详情。", "{0} repositories failed. Open the main window for details.");
            A("main.deleteTitle", "删除仓库", "Delete repository");
            A("main.deleteMessage", "确定要删除「{0}」吗？\r\n\r\n删除后不再检查该仓库的更新。", "Delete “{0}”?\r\n\r\nThis repository will no longer be monitored.");

            A("card.monitor", "监听", "Monitor");
            A("card.open", "打开", "Open");
            A("card.openTooltip", "在浏览器中打开最新 Release", "Open the latest release in your browser");
            A("card.moreActions", "更多操作", "More actions");
            A("card.openAccessible", "打开最新 Release", "Open the latest release");
            A("card.monitorAccessible", "启用监听", "Enable monitoring");
            A("card.moreTooltip", "编辑、标记已读或删除仓库", "Edit, mark as read, or delete");
            A("card.monitorTooltip", "暂停或恢复监听此仓库", "Pause or resume monitoring");
            A("card.published", "发布于 {0}", "Published {0}");
            A("card.error", "检查失败：{0}", "Check failed: {0}");
            A("card.menuOpenRelease", "在浏览器中打开 Release", "Open release in browser");
            A("card.menuOpenRepo", "在 GitHub 打开仓库", "Open repository on GitHub");
            A("card.menuMarkRead", "标记为已读", "Mark as read");
            A("card.menuEdit", "编辑仓库…", "Edit repository…");
            A("card.menuDelete", "删除仓库", "Delete repository");

            A("empty.title", "还没有监听的仓库", "No repositories yet");
            A("empty.subtitle", "添加 GitHub 仓库后，这里会显示最新 Release 状态。", "Add a GitHub repository to see its latest release status.");

            A("settings.window", "设置", "Settings");
            A("settings.title", "设置", "Settings");
            A("settings.subtitle", "调整检查频率、代理与通知方式。", "Configure check interval, proxy, and notifications.");
            A("settings.tabGeneral", "常规", "General");
            A("settings.tabNetwork", "网络", "Network");
            A("settings.tabNotify", "通知", "Notifications");
            A("settings.tabAbout", "关于", "About");
            A("settings.frequency", "检查频率", "Check frequency");
            A("settings.interval", "自动检查间隔", "Check interval");
            A("settings.custom", "自定义", "Custom");
            A("settings.minutes", "分钟", "minutes");
            A("settings.freqHint", "默认每 10 分钟检查一次。频率越高越及时，但会消耗更多 GitHub API 配额。", "Checks every 10 minutes by default. More frequent checks are more responsive but use more GitHub API quota.");
            A("settings.startup", "启动与关闭", "Startup and closing");
            A("settings.autoStart", "开机自动启动", "Start with Windows");
            A("settings.checkOnStartup", "启动时立即检查一次", "Check once at startup");
            A("settings.closeToTray", "关闭主窗口时最小化到系统托盘", "Minimize to tray when closing the main window");
            A("settings.tokenTitle", "GitHub Token（可选）", "GitHub Token (optional)");
            A("settings.show", "显示", "Show");
            A("settings.tokenHint", "未认证时每小时仅 60 次请求，配置 Token 后可提升至 5000 次/小时，并可访问私有仓库。Token 使用 Windows DPAPI 加密保存在本机。", "Unauthenticated requests are limited to 60/hour. A token raises this to 5,000/hour and enables private repositories. Tokens are encrypted with Windows DPAPI.");
            A("settings.createToken", "前往 GitHub 创建 Token", "Create a token on GitHub");
            A("settings.proxyTitle", "代理设置", "Proxy");
            A("settings.proxySystem", "使用系统代理（推荐）", "Use system proxy (recommended)");
            A("settings.proxyNone", "不使用代理（直连）", "No proxy (direct)");
            A("settings.proxyCustom", "自定义 HTTP 代理", "Custom HTTP proxy");
            A("settings.proxyAddress", "地址", "Address");
            A("settings.proxyUser", "用户名", "Username");
            A("settings.proxyPass", "密码", "Password");
            A("settings.proxyHint", "仅支持 HTTP/HTTPS 代理。地址示例：http://127.0.0.1:7890。用户名和密码可留空。", "HTTP/HTTPS proxy only. Example: http://127.0.0.1:7890. Username and password are optional.");
            A("settings.test", "测试连接", "Test connection");
            A("settings.testing", "测试中…", "Testing…");
            A("settings.connecting", "正在连接 GitHub…", "Connecting to GitHub…");
            A("settings.testFailed", "测试失败：{0}", "Test failed: {0}");
            A("settings.proxyNote", "系统代理会读取 Windows「设置 → 网络和 Internet → 代理」中的配置。测试连接会实际访问 GitHub API。", "The system proxy uses Windows Settings → Network and Internet → Proxy. The connection test accesses the GitHub API.");
            A("settings.notifyTitle", "通知", "Notifications");
            A("settings.notifyOnError", "检查失败时也发送通知", "Notify me when a check fails");
            A("settings.notifyNote", "发现新版本时，程序会在屏幕右下角弹出气泡提醒，并在托盘图标上显示红点。点击气泡可直接打开 Release 页面。每个版本只提醒一次，可在仓库菜单中标记已读。", "New releases appear as a notification in the bottom-right corner, with a red dot on the tray icon. Click the notification to open the release. Each version is notified once; use the repository menu to mark it as read.");
            A("settings.notifyNote2", "首次添加仓库时以当前最新版本为基线，不会立即提醒。若需要测试通知，可在仓库菜单中清除已读状态后等待下一次检查。", "New repositories use the current latest release as the baseline, so no immediate alert is sent. To test notifications, clear the read state from the repository menu and wait for the next check.");
            A("settings.aboutVersion", "版本 {0} · .NET Framework 4.8", "Version {0} · .NET Framework 4.8");
            A("settings.aboutDescription", "一个轻量的 GitHub Release 更新提醒工具。常驻系统托盘，按设定频率检查仓库，发现新版本时在右下角通知。", "A lightweight GitHub release notifier for Windows. It lives in the tray, checks repositories on a schedule, and notifies you when a new release appears.");
            A("settings.configTitle", "配置与日志", "Config and logs");
            A("settings.openConfig", "打开配置目录", "Open config folder");
            A("settings.openLog", "打开日志", "Open log");
            A("settings.aboutFooter", "单文件 exe，无第三方 DLL。数据仅保存在本机，不上传任何信息。", "Single-file exe with no third-party DLLs. All data stays on this machine.");
            A("settings.openSourceTitle", "开源与授权", "Open source and license");
            A("settings.openSourceFree", "本软件完全免费，欢迎 Star 和反馈问题。", "Completely free. Stars and feedback are welcome.");
            A("settings.proxyRequired", "请填写代理服务器地址。", "Enter a proxy server address.");
            A("settings.proxyInvalid", "代理地址格式无效，示例：http://127.0.0.1:7890", "Invalid proxy address. Example: http://127.0.0.1:7890");
            A("settings.proxyScheme", "仅支持 HTTP/HTTPS 代理。", "Only HTTP/HTTPS proxies are supported.");

            A("add.windowTitleAdd", "添加仓库", "Add repository");
            A("add.windowTitleEdit", "编辑仓库", "Edit repository");
            A("add.titleAdd", "添加监听的仓库", "Add a repository");
            A("add.titleEdit", "编辑监听的仓库", "Edit repository");
            A("add.subtitle", "监听 GitHub 仓库的 Release，有新版本时在托盘提醒你。", "Monitor GitHub releases and get a tray notification when a new version is published.");
            A("add.name", "名称（可选）", "Name (optional)");
            A("add.nameHint", "留空则使用仓库名。", "Leave blank to use the repository name.");
            A("add.url", "仓库地址（必填）", "Repository URL (required)");
            A("add.urlHint", "支持粘贴 https://github.com/owner/repo，或直接输入 owner/repo。", "Paste https://github.com/owner/repo, or simply enter owner/repo.");
            A("add.prerelease", "包含预发布版本（Pre-release）", "Include pre-releases");
            A("add.notify", "有新版本时发送托盘通知", "Show a tray notification for new releases");
            A("add.baselineAdd", "首次添加会记录当前最新版本作为基线，不会立即提醒。", "The current latest release is recorded as the baseline, so no alert is sent immediately.");
            A("add.baselineEdit", "修改仓库地址会重置已读基线；仅修改名称不会影响监听记录。", "Changing the repository URL resets the read baseline. Renaming only does not affect monitoring.");
            A("add.verifying", "验证中…", "Verifying…");
            A("add.verifyStatus", "正在验证仓库…", "Verifying repository…");
            A("add.invalidUrl", "请输入有效的 GitHub 仓库地址，例如 https://github.com/owner/repo", "Enter a valid GitHub repository URL, e.g. https://github.com/owner/repo");
            A("add.duplicate", "这个仓库已在监听列表中。", "This repository is already monitored.");
            A("add.verifyFailed", "验证失败：{0}", "Verification failed: {0}");
            A("add.verifyFailedTitle", "无法验证仓库", "Unable to verify repository");
            A("add.verifyFailedMessage", "无法验证仓库：\r\n{0}\r\n\r\n仍要添加这个仓库吗？", "Unable to verify this repository:\r\n{0}\r\n\r\nAdd it anyway?");

            A("err.proxyEmpty", "未填写代理地址", "Proxy address is empty");
            A("err.proxyInvalid", "代理地址格式无效", "Invalid proxy address");
            A("err.proxyScheme", "仅支持 HTTP/HTTPS 代理", "Only HTTP/HTTPS proxies are supported");
            A("err.token", "Token 无效或已过期", "Invalid or expired token");
            A("err.notFound", "仓库不存在或无权访问", "Repository not found or access denied");
            A("err.rate", "GitHub API 已达请求上限，请稍后重试或在设置中配置 Token", "GitHub API rate limit reached. Try again later or configure a token in Settings.");
            A("err.forbidden", "GitHub 拒绝访问（HTTP {0}）", "GitHub denied access (HTTP {0})");
            A("err.http", "GitHub 返回 HTTP {0}", "GitHub returned HTTP {0}");
            A("err.parse", "GitHub 返回的数据无法解析", "Unable to parse the GitHub response");
            A("err.timeout", "请求超时，请检查网络或代理设置", "Request timed out. Check your network or proxy.");
            A("err.dns", "无法解析 api.github.com，请检查 DNS 或代理设置", "Cannot resolve api.github.com. Check DNS or proxy settings.");
            A("err.proxyDns", "无法解析代理服务器地址", "Cannot resolve the proxy server.");
            A("err.connect", "无法连接 GitHub，请检查网络或代理设置", "Cannot connect to GitHub. Check your network or proxy.");
            A("err.tls", "TLS 连接失败，请检查系统时间或代理设置", "TLS connection failed. Check the system time or proxy.");
            A("err.network", "网络请求失败：{0}", "Network request failed: {0}");
            A("err.check", "检查失败：{0}", "Check failed: {0}");
            A("err.request", "请求失败：{0}", "Request failed: {0}");
            A("conn.ok", "连接成功", "Connected");
            A("conn.okToken", "连接成功，Token 有效", "Connected. The token is valid.");
            A("conn.api", " · API 剩余 {0}/{1}", " · API remaining {0}/{1}");

            A("program.errorTitle", "程序遇到错误", "Application error");
            A("program.errorMessage", "程序遇到错误：\r\n{0}", "An error occurred:\r\n{0}");
        }
    }
}
