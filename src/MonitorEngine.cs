using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHubReleaseMonitor
{
    public sealed class CheckTarget
    {
        public string RepoId = "";
        public string Owner = "";
        public string Repo = "";
        public bool IncludePrerelease;
    }

    public sealed class RepoCheckOutcome
    {
        public string RepoId = "";
        public ReleaseLookupResult Result;
        public DateTime CheckedAtUtc;
    }

    public sealed class CheckSummary
    {
        public List<RepoCheckOutcome> Outcomes = new List<RepoCheckOutcome>();
        public int RateLimit = -1;
        public int RateRemaining = -1;
        public long RateResetUnix = 0;
    }

    public static class CheckRunner
    {
        public static CheckSummary Run(IList<CheckTarget> targets, AppSettings settings, string token)
        {
            CheckSummary summary = new CheckSummary();
            if (targets == null || targets.Count == 0) return summary;

            GitHubClient client = new GitHubClient(settings, token);
            for (int i = 0; i < targets.Count; i++)
            {
                CheckTarget target = targets[i];
                RepoCheckOutcome outcome = new RepoCheckOutcome();
                outcome.RepoId = target.RepoId;
                outcome.CheckedAtUtc = DateTime.UtcNow;

                try
                {
                    outcome.Result = client.GetLatestRelease(target.Owner, target.Repo, target.IncludePrerelease);
                }
                catch (Exception ex)
                {
                    Log.Error("检查仓库失败: " + target.Owner + "/" + target.Repo, ex);
                    outcome.Result = ReleaseLookupResult.Failure(L10n.F("err.check", ex.Message));
                }

                summary.Outcomes.Add(outcome);

                if (i < targets.Count - 1)
                {
                    Thread.Sleep(600);
                }
            }

            summary.RateLimit = client.RateLimit;
            summary.RateRemaining = client.RateRemaining;
            summary.RateResetUnix = client.RateResetUnix;
            return summary;
        }
    }
}
