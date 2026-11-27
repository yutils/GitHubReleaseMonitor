using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember(Name = "intervalMinutes")] public int IntervalMinutes = 10;
        [DataMember(Name = "proxyMode")] public string ProxyMode = "system";
        [DataMember(Name = "proxyAddress")] public string ProxyAddress = "";
        [DataMember(Name = "proxyUsername")] public string ProxyUsername = "";
        [DataMember(Name = "proxyPasswordProtected")] public string ProxyPasswordProtected = "";
        [DataMember(Name = "autoStart")] public bool AutoStart = true;
        [DataMember(Name = "closeToTray")] public bool CloseToTray = true;
        [DataMember(Name = "closeToTrayTipShown")] public bool CloseToTrayTipShown = false;
        [DataMember(Name = "checkOnStartup")] public bool CheckOnStartup = true;
        [DataMember(Name = "notifyOnError")] public bool NotifyOnError = false;
        [DataMember(Name = "language")] public string Language = "zh-CN";

        public string ProxyPassword = "";

        public AppSettings Clone()
        {
            AppSettings copy = new AppSettings();
            copy.IntervalMinutes = IntervalMinutes;
            copy.ProxyMode = ProxyMode;
            copy.ProxyAddress = ProxyAddress;
            copy.ProxyUsername = ProxyUsername;
            copy.ProxyPassword = ProxyPassword;
            copy.ProxyPasswordProtected = ProxyPasswordProtected;
            copy.AutoStart = AutoStart;
            copy.CloseToTray = CloseToTray;
            copy.CloseToTrayTipShown = CloseToTrayTipShown;
            copy.CheckOnStartup = CheckOnStartup;
            copy.NotifyOnError = NotifyOnError;
            copy.Language = Language;
            return copy;
        }
    }

    [DataContract]
    public sealed class RepoConfig
    {
        [DataMember(Name = "id")] public string Id = "";
        [DataMember(Name = "name")] public string Name = "";
        [DataMember(Name = "owner")] public string Owner = "";
        [DataMember(Name = "repo")] public string Repo = "";
        [DataMember(Name = "enabled")] public bool Enabled = true;
        [DataMember(Name = "includePrerelease")] public bool IncludePrerelease = false;
        [DataMember(Name = "notify")] public bool Notify = true;
        [DataMember(Name = "baselineInitialized")] public bool BaselineInitialized = false;
        [DataMember(Name = "seenReleaseId")] public long SeenReleaseId = 0;
        [DataMember(Name = "latestReleaseId")] public long LatestReleaseId = 0;
        [DataMember(Name = "latestTag")] public string LatestTag = "";
        [DataMember(Name = "latestName")] public string LatestName = "";
        [DataMember(Name = "latestUrl")] public string LatestUrl = "";
        [DataMember(Name = "latestPublishedAt")] public string LatestPublishedAt = "";
        [DataMember(Name = "unreadReleaseId")] public long UnreadReleaseId = 0;
        [DataMember(Name = "unreadTag")] public string UnreadTag = "";
        [DataMember(Name = "unreadName")] public string UnreadName = "";
        [DataMember(Name = "unreadUrl")] public string UnreadUrl = "";
        [DataMember(Name = "lastCheckedAt")] public string LastCheckedAt = "";
        [DataMember(Name = "lastStatus")] public string LastStatus = "";
        [DataMember(Name = "lastError")] public string LastError = "";

        public string Slug
        {
            get { return Owner + "/" + Repo; }
        }

        public string RepoUrl
        {
            get { return "https://github.com/" + Slug; }
        }

        public bool HasUnread
        {
            get { return UnreadReleaseId != 0; }
        }

        public void ResetTracking()
        {
            BaselineInitialized = false;
            SeenReleaseId = 0;
            LatestReleaseId = 0;
            LatestTag = "";
            LatestName = "";
            LatestUrl = "";
            LatestPublishedAt = "";
            UnreadReleaseId = 0;
            UnreadTag = "";
            UnreadName = "";
            UnreadUrl = "";
            LastCheckedAt = "";
            LastStatus = "";
            LastError = "";
        }
    }

    [DataContract]
    public sealed class AppConfig
    {
        [DataMember(Name = "version")] public int Version = 1;
        [DataMember(Name = "githubTokenProtected")] public string GithubTokenProtected = "";
        [DataMember(Name = "settings")] public AppSettings Settings = new AppSettings();
        [DataMember(Name = "repos")] public List<RepoConfig> Repos = new List<RepoConfig>();

        public string GithubToken = "";

        public void Normalize()
        {
            if (Settings == null) Settings = new AppSettings();
            if (Repos == null) Repos = new List<RepoConfig>();

            if (Settings.IntervalMinutes < 1 || Settings.IntervalMinutes > 1440)
            {
                Settings.IntervalMinutes = 10;
            }
            if (Settings.ProxyMode != "system" && Settings.ProxyMode != "none" && Settings.ProxyMode != "custom")
            {
                Settings.ProxyMode = "system";
            }
            if (Settings.ProxyMode == null) Settings.ProxyMode = "system";
            if (Settings.ProxyAddress == null) Settings.ProxyAddress = "";
            if (Settings.ProxyUsername == null) Settings.ProxyUsername = "";
            if (Settings.ProxyPasswordProtected == null) Settings.ProxyPasswordProtected = "";
            if (Settings.Language != "en-US") Settings.Language = "zh-CN";

            List<RepoConfig> valid = new List<RepoConfig>();
            foreach (RepoConfig repo in Repos)
            {
                if (repo == null) continue;
                if (string.IsNullOrEmpty(repo.Id)) repo.Id = Guid.NewGuid().ToString("N");
                if (repo.Name == null) repo.Name = "";
                if (repo.Owner == null) repo.Owner = "";
                if (repo.Repo == null) repo.Repo = "";
                if (repo.LatestTag == null) repo.LatestTag = "";
                if (repo.LatestName == null) repo.LatestName = "";
                if (repo.LatestUrl == null) repo.LatestUrl = "";
                if (repo.LatestPublishedAt == null) repo.LatestPublishedAt = "";
                if (repo.UnreadTag == null) repo.UnreadTag = "";
                if (repo.UnreadName == null) repo.UnreadName = "";
                if (repo.UnreadUrl == null) repo.UnreadUrl = "";
                if (repo.LastCheckedAt == null) repo.LastCheckedAt = "";
                if (repo.LastStatus == null) repo.LastStatus = "";
                if (repo.LastError == null) repo.LastError = "";
                repo.Owner = repo.Owner.Trim();
                repo.Repo = repo.Repo.Trim();
                if (string.IsNullOrEmpty(repo.Owner) || string.IsNullOrEmpty(repo.Repo)) continue;
                if (string.IsNullOrEmpty(repo.Name)) repo.Name = repo.Repo;
                valid.Add(repo);
            }
            Repos = valid;
        }
    }

    internal static class TokenProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GitHubReleaseMonitor.v1");
        private const string Prefix = "dpapi:";

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), Entropy,
                    DataProtectionScope.CurrentUser);
                return Prefix + Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                Log.Error("加密敏感配置失败，将使用明文保存", ex);
                return plainText;
            }
        }

        public static string Unprotect(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return "";
            if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal)) return storedValue;
            try
            {
                byte[] data = Convert.FromBase64String(storedValue.Substring(Prefix.Length));
                byte[] plain = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex)
            {
                Log.Error("解密敏感配置失败", ex);
                return "";
            }
        }
    }

    internal static class ConfigStore
    {
        public static string BaseDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GitHubReleaseMonitor");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(BaseDir, "config.json"); }
        }

        public static string LogPath
        {
            get { return Path.Combine(BaseDir, "app.log"); }
        }

        public static AppConfig Load()
        {
            AppConfig config = null;
            try
            {
                Directory.CreateDirectory(BaseDir);
                if (File.Exists(ConfigPath))
                {
                    using (FileStream stream = File.OpenRead(ConfigPath))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppConfig));
                        config = serializer.ReadObject(stream) as AppConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("读取配置失败，将使用默认配置", ex);
                try
                {
                    if (File.Exists(ConfigPath))
                    {
                        File.Copy(ConfigPath, ConfigPath + ".broken", true);
                    }
                }
                catch
                {
                }
            }

            if (config == null) config = new AppConfig();
            config.Normalize();
            config.GithubToken = TokenProtector.Unprotect(config.GithubTokenProtected);
            config.Settings.ProxyPassword = TokenProtector.Unprotect(config.Settings.ProxyPasswordProtected);
            return config;
        }

        public static void Save(AppConfig config)
        {
            if (config == null) return;
            try
            {
                Directory.CreateDirectory(BaseDir);
                config.Normalize();
                config.GithubTokenProtected = TokenProtector.Protect(config.GithubToken);
                config.Settings.ProxyPasswordProtected = TokenProtector.Protect(config.Settings.ProxyPassword);

                string tempPath = ConfigPath + ".tmp";
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppConfig));
                using (FileStream stream = File.Create(tempPath))
                {
                    serializer.WriteObject(stream, config);
                    stream.Flush();
                }

                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        File.Replace(tempPath, ConfigPath, null);
                    }
                    catch
                    {
                        File.Copy(tempPath, ConfigPath, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, ConfigPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error("保存配置失败", ex);
            }
        }
    }

    internal static class Log
    {
        private static readonly object SyncRoot = new object();

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(ConfigStore.BaseDir);
                    FileInfo info = new FileInfo(ConfigStore.LogPath);
                    if (info.Exists && info.Length > 1024 * 1024)
                    {
                        File.Delete(ConfigStore.LogPath);
                    }
                    StringBuilder line = new StringBuilder();
                    line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    line.Append(" [");
                    line.Append(level);
                    line.Append("] ");
                    line.Append(message);
                    if (exception != null)
                    {
                        line.Append(" :: ");
                        line.Append(exception);
                    }
                    line.Append(Environment.NewLine);
                    File.AppendAllText(ConfigStore.LogPath, line.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }
    }
}
