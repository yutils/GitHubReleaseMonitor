using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GitHubReleaseMonitor
{
    public sealed class ReleaseInfo
    {
        public long Id;
        public string TagName = "";
        public string Name = "";
        public string HtmlUrl = "";
        public string PublishedAt = "";
        public bool Prerelease;
    }

    public sealed class ReleaseLookupResult
    {
        public bool Success;
        public bool NoRelease;
        public ReleaseInfo Release;
        public string Error = "";
        public int HttpStatus;
        public bool Unauthorized;
        public bool NotFound;
        public bool RateLimited;

        public static ReleaseLookupResult Failure(string message)
        {
            ReleaseLookupResult result = new ReleaseLookupResult();
            result.Success = false;
            result.Error = message;
            return result;
        }
    }

    public sealed class ConnectionTestResult
    {
        public bool Success;
        public string Message = "";
    }

    // Fields are populated by DataContractJsonSerializer.
#pragma warning disable 0649
    [DataContract]
    internal sealed class ReleaseDto
    {
        [DataMember(Name = "id")] public long Id;
        [DataMember(Name = "tag_name")] public string TagName;
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "html_url")] public string HtmlUrl;
        [DataMember(Name = "published_at")] public string PublishedAt;
        [DataMember(Name = "draft")] public bool Draft;
        [DataMember(Name = "prerelease")] public bool Prerelease;
    }

    [DataContract]
    internal sealed class ApiErrorDto
    {
        [DataMember(Name = "message")] public string Message;
    }
#pragma warning restore 0649

    internal static class ProxyFactory
    {
        public static IWebProxy Create(AppSettings settings)
        {
            if (settings == null || settings.ProxyMode == "system")
            {
                IWebProxy systemProxy = WebRequest.GetSystemWebProxy();
                if (systemProxy != null)
                {
                    systemProxy.Credentials = CredentialCache.DefaultCredentials;
                }
                return systemProxy;
            }

            if (settings.ProxyMode == "none")
            {
                return null;
            }

            string address = (settings.ProxyAddress ?? "").Trim();
            if (address.Length == 0)
            {
                throw new ArgumentException(L10n.T("err.proxyEmpty"));
            }
            if (address.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                address = "http://" + address;
            }

            Uri uri;
            if (!Uri.TryCreate(address, UriKind.Absolute, out uri))
            {
                throw new ArgumentException(L10n.T("err.proxyInvalid"));
            }
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(L10n.T("err.proxyScheme"));
            }

            WebProxy proxy = new WebProxy(uri);
            if (!string.IsNullOrEmpty(settings.ProxyUsername))
            {
                proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword ?? "");
            }
            return proxy;
        }
    }

    public sealed class GitHubClient
    {
        private readonly AppSettings _settings;
        private readonly string _token;

        public int RateLimit = -1;
        public int RateRemaining = -1;
        public long RateResetUnix = 0;

        static GitHubClient()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch
            {
            }
        }

        public GitHubClient(AppSettings settings, string token)
        {
            _settings = settings ?? new AppSettings();
            _token = (token ?? "").Trim();
        }

        public ReleaseLookupResult GetLatestRelease(string owner, string repo, bool includePrerelease)
        {
            string url = "https://api.github.com/repos/" + Uri.EscapeDataString(owner) + "/" +
                         Uri.EscapeDataString(repo) + "/releases?per_page=10";

            string body;
            int status;
            string networkError;
            if (!TryRequest(url, out body, out status, out networkError))
            {
                return ReleaseLookupResult.Failure(networkError);
            }

            ReleaseLookupResult result = new ReleaseLookupResult();
            result.HttpStatus = status;
            result.Unauthorized = status == 401;
            result.NotFound = status == 404;
            result.RateLimited = status == 429 || (status == 403 && RateRemaining == 0);

            if (status != 200)
            {
                result.Success = false;
                result.Error = DescribeHttpError(status, body);
                return result;
            }

            List<ReleaseDto> releases;
            try
            {
                releases = Deserialize<List<ReleaseDto>>(body);
            }
            catch (Exception ex)
            {
                Log.Error("解析 GitHub Release 数据失败", ex);
                result.Success = false;
                result.Error = L10n.T("err.parse");
                return result;
            }

            result.Success = true;
            if (releases == null)
            {
                result.NoRelease = true;
                return result;
            }

            foreach (ReleaseDto dto in releases)
            {
                if (dto == null || dto.Draft) continue;
                if (dto.Prerelease && !includePrerelease) continue;

                ReleaseInfo info = new ReleaseInfo();
                info.Id = dto.Id;
                info.TagName = dto.TagName ?? "";
                info.Name = string.IsNullOrEmpty(dto.Name) ? info.TagName : dto.Name;
                info.HtmlUrl = dto.HtmlUrl ?? "";
                info.PublishedAt = dto.PublishedAt ?? "";
                info.Prerelease = dto.Prerelease;
                result.Release = info;
                return result;
            }

            result.NoRelease = true;
            return result;
        }

        public ConnectionTestResult TestConnection()
        {
            string url = _token.Length > 0
                ? "https://api.github.com/user"
                : "https://api.github.com/rate_limit";

            string body;
            int status;
            string networkError;
            if (!TryRequest(url, out body, out status, out networkError))
            {
                ConnectionTestResult failed = new ConnectionTestResult();
                failed.Success = false;
                failed.Message = networkError;
                return failed;
            }

            ConnectionTestResult result = new ConnectionTestResult();
            if (status >= 200 && status < 300)
            {
                result.Success = true;
                result.Message = _token.Length > 0 ? L10n.T("conn.okToken") : L10n.T("conn.ok");
                if (RateRemaining >= 0)
                {
                    if (RateLimit > 0)
                    {
                        result.Message += L10n.F("conn.api", RateRemaining, RateLimit);
                    }
                    else
                    {
                        result.Message += " · API " + RateRemaining;
                    }
                }
            }
            else
            {
                result.Success = false;
                result.Message = DescribeHttpError(status, body);
            }
            return result;
        }

        private bool TryRequest(string url, out string body, out int statusCode, out string error)
        {
            body = "";
            statusCode = 0;
            error = "";

            HttpWebRequest request;
            try
            {
                request = CreateRequest(url);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                statusCode = (int)response.StatusCode;
                ReadRateLimit(response);
                body = ReadBody(response);
                return true;
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    statusCode = (int)response.StatusCode;
                    ReadRateLimit(response);
                    body = ReadBody(response);
                    return true;
                }
                error = DescribeNetworkError(ex);
                Log.Error("请求 GitHub 失败: " + url, ex);
                return false;
            }
            catch (Exception ex)
            {
                error = L10n.F("err.request", ex.Message);
                Log.Error("请求 GitHub 失败: " + url, ex);
                return false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        private HttpWebRequest CreateRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = AppInfo.UserAgent;
            request.Accept = "application/vnd.github+json";
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.AllowAutoRedirect = true;
            request.KeepAlive = false;
            request.Proxy = ProxyFactory.Create(_settings);
            if (_token.Length > 0)
            {
                request.Headers.Add("Authorization", "Bearer " + _token);
            }
            return request;
        }

        private void ReadRateLimit(HttpWebResponse response)
        {
            try
            {
                string limit = response.Headers["X-RateLimit-Limit"];
                string remaining = response.Headers["X-RateLimit-Remaining"];
                string reset = response.Headers["X-RateLimit-Reset"];

                int parsed;
                if (int.TryParse(limit, out parsed)) RateLimit = parsed;
                if (int.TryParse(remaining, out parsed)) RateRemaining = parsed;

                long parsedReset;
                if (long.TryParse(reset, out parsedReset)) RateResetUnix = parsedReset;
            }
            catch
            {
            }
        }

        private static string ReadBody(HttpWebResponse response)
        {
            try
            {
                using (Stream stream = response.GetResponseStream())
                {
                    if (stream == null) return "";
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private string DescribeHttpError(int status, string body)
        {
            string apiMessage = TrimMessage(GetApiMessage(body));

            if (status == 401)
            {
                return L10n.T("err.token");
            }
            if (status == 404)
            {
                return L10n.T("err.notFound");
            }
            if (status == 403 || status == 429)
            {
                if (RateRemaining == 0)
                {
                    return L10n.T("err.rate");
                }
                return string.IsNullOrEmpty(apiMessage) ? L10n.F("err.forbidden", status) : apiMessage;
            }
            if (!string.IsNullOrEmpty(apiMessage))
            {
                return apiMessage;
            }
            return L10n.F("err.http", status);
        }

        private static string GetApiMessage(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            try
            {
                ApiErrorDto error = Deserialize<ApiErrorDto>(body);
                return error == null ? "" : (error.Message ?? "");
            }
            catch
            {
                return "";
            }
        }

        private static string TrimMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            message = message.Replace("\r", " ").Replace("\n", " ").Trim();
            if (message.Length > 160) message = message.Substring(0, 157) + "...";
            return message;
        }

        private static string DescribeNetworkError(WebException ex)
        {
            if (ex.Status == WebExceptionStatus.Timeout)
            {
                return L10n.T("err.timeout");
            }
            if (ex.Status == WebExceptionStatus.NameResolutionFailure)
            {
                return L10n.T("err.dns");
            }
            if (ex.Status == WebExceptionStatus.ProxyNameResolutionFailure)
            {
                return L10n.T("err.proxyDns");
            }
            if (ex.Status == WebExceptionStatus.ConnectFailure)
            {
                return L10n.T("err.connect");
            }
            if (ex.Status == WebExceptionStatus.SecureChannelFailure)
            {
                return L10n.T("err.tls");
            }
            return L10n.F("err.network", ex.Message);
        }

        private static T Deserialize<T>(string json)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}
