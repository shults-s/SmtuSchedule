using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core.Utilities
{
    public static class ApplicationUtilities
    {
        public const String LatestReleaseDownloadPageUrl = "https://github.com/shults-s/SmtuSchedule/releases/latest/";

        private const String RepositoryRawUrl = "https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/";

        public static IHttpClient HttpClient { get; internal set; }

        static ApplicationUtilities() => HttpClient = new HttpClientProxy();

        public static Boolean IsUniversitySiteConnectionAvailable(out String failReason)
        {
            // Эмулятор Android, созданный на основе QEMU, не поддерживает ICMP-запросы, поэтому ping в нем не работает,
            // что делает невозможным тестирование некоторых возможностей приложения в режиме отладки.
#if DEBUG
            failReason = null;
            return true;
#endif

            const String UniversitySiteHostName = "www.smtu.ru";

            PingReply reply = null;
            try
            {
                reply = new Ping().Send(UniversitySiteHostName);

                failReason = (reply.Status != IPStatus.Success)
                    ? $"Ping failed with the status {reply?.Status} without throwing an exception."
                    : null;

                return (reply.Status == IPStatus.Success);
            }
            catch (PingException exception)
            {
                failReason = $"Ping failed with the {reply?.Status} status and threw an exception: {exception.Format()}";
                return false;
            }
        }

        public static Task<ReleaseDescription> GetLatestReleaseDescription()
        {
            return Task.Run(async () =>
            {
                const String Url = RepositoryRawUrl + "SmtuSchedule.Android/Release.json";

                try
                {
                    String json = await HttpClient.GetAsync(Url).ConfigureAwait(false);
                    return ReleaseDescription.FromJson(json).Validate();
                }
                catch (Exception exception) when (
                    exception is HttpRequestException
                    || exception is JsonException
                    || exception is ValidationException
                )
                {
                    return null;
                }
            });
        }

        public static Task<String> ParseLatestReleaseVersionFromRepositoryChangeLogAsync()
        {
            return Task.Run(async () =>
            {
                const String Url = RepositoryRawUrl + "CHANGELOG.md";

                try
                {
                    String changeLog = await HttpClient.GetAsync(Url).ConfigureAwait(false);

                    // ## [Версия X.X.X]
                    Match match = Regex.Match(changeLog, @"\#\# \[[\p{L}\s]*(?<version>[\d.]+)\]");
                    if (!match.Success)
                    {
                        return null;
                    }

                    return match.Groups["version"].Value;
                }
                catch (HttpRequestException)
                {
                    return null;
                }
            });
        }

        // Если v1 > v2, вернется 1; если v1 = v2, вернется 0; если v1 < v2, вернется -1.
        public static Int32 CompareVersions(String version1, String version2)
        {
            if (String.IsNullOrWhiteSpace(version1))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(version1));
            }

            if (String.IsNullOrWhiteSpace(version2))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(version2));
            }

            Int32[] v1 = version1.Split('.').Select(s => Int32.TryParse(s, out Int32 value) ? value : Int32.MaxValue)
                .ToArray();

            if (v1.Length < 2)
            {
                throw new ArgumentException("String must match the format: 'Major.Minor[.Patch]'.", nameof(version1));
            }

            Int32[] v2 = version2.Split('.').Select(s => Int32.TryParse(s, out Int32 value) ? value : Int32.MaxValue)
                .ToArray();

            if (v2.Length < 2)
            {
                throw new ArgumentException("String must match the format: 'Major.Minor[.Patch]'.", nameof(version2));
            }

            Int32 majorComparsion = (v1[0] > v2[0]) ? 1 : (v1[0] < v2[0] ? -1 : 0);
            Int32 minorComparsion = (v1[1] > v2[1]) ? 1 : (v1[1] < v2[1] ? -1 : 0);

            Int32 patchComparsion = (v1.Length == 3 && v2.Length == 3)
                ? (v1[2] > v2[2] ? 1 : v1[2] < v2[2] ? -1 : 0)
                : (v1.Length == v2.Length ? 0 : v1.Length > v2.Length ? 1 : -1);

            return (majorComparsion == 0) ? (minorComparsion == 0 ? patchComparsion : minorComparsion) : majorComparsion;
        }
    }
}