using System;
using System.Text.RegularExpressions;

namespace GitHubReleaseMonitor
{
    internal enum AppUpdateStatus
    {
        Failed,
        UpToDate,
        Available
    }

    internal sealed class AppUpdateResult
    {
        public AppUpdateStatus Status = AppUpdateStatus.Failed;
        public string Version = "";
        public string Url = "";
    }

    internal static class AppUpdateChecker
    {
        public const string Owner = "yutils";
        public const string Repo = "GitHubReleaseMonitor";

        private static readonly Regex VersionPattern =
            new Regex(@"\d+(?:\.\d+)+", RegexOptions.Compiled);

        public static AppUpdateResult Check(AppSettings settings, string token)
        {
            AppUpdateResult result = new AppUpdateResult();

            try
            {
                GitHubClient client = new GitHubClient(settings, token);
                ReleaseLookupResult lookup = client.GetLatestRelease(Owner, Repo, false);
                if (!lookup.Success)
                {
                    return result;
                }

                if (lookup.NoRelease || lookup.Release == null)
                {
                    result.Status = AppUpdateStatus.UpToDate;
                    return result;
                }

                string version = NormalizeVersion(lookup.Release.TagName);
                if (version.Length == 0)
                {
                    version = NormalizeVersion(lookup.Release.Name);
                }
                if (version.Length == 0)
                {
                    return result;
                }

                if (!IsNewer(version, AppInfo.Version))
                {
                    result.Status = AppUpdateStatus.UpToDate;
                    return result;
                }

                result.Status = AppUpdateStatus.Available;
                result.Version = version;
                result.Url = string.IsNullOrEmpty(lookup.Release.HtmlUrl)
                    ? "https://github.com/" + Owner + "/" + Repo + "/releases"
                    : lookup.Release.HtmlUrl;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error("检查软件更新失败", ex);
                return result;
            }
        }

        public static string NormalizeVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            Match match = VersionPattern.Match(value);
            return match.Success ? match.Value : "";
        }

        public static bool IsNewer(string latest, string current)
        {
            Version latestVersion;
            Version currentVersion;
            if (!Version.TryParse(latest, out latestVersion)) return false;
            if (!Version.TryParse(current, out currentVersion)) return false;
            return latestVersion > currentVersion;
        }
    }
}
