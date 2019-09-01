using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace SmtuSchedule.Core.Utilities
{
    public static class ApplicationHelper
    {
        public const String LatestReleaseUrl = "https://github.com/shults-s/SmtuSchedule/releases/latest";

        public static Boolean IsUniversitySiteConnectionAvailable(out String failReason)
        {
            IPStatus status = IPStatus.Success;
            try
            {
                // Эмулятор Android, построенный на основе QEMU, не поддерживает ICMP-запросы и потому ping может не работать.
                status = new Ping().Send("www.smtu.ru").Status;

                failReason = (status != IPStatus.Success)
                    ? "Ping failed with status " + Enum.GetName(typeof(IPStatus), status) + " but didn't throw an exception."
                    : null;

                return status == IPStatus.Success;
            }
            catch (Exception exception)
            {
                failReason = "Ping failed with status " + Enum.GetName(typeof(IPStatus), status) + ": " + exception.Format();
                return false;
            }
        }

        public static async Task<String> GetCurrentVersionAsync()
        {
            const String Url = "https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/CHANGELOG.md";

            try
            {
                String changeLog = await HttpHelper.GetAsync(Url).ConfigureAwait(false);

                // ## [Версия X.X.X]
                Match match = Regex.Match(changeLog, @"\#\# \[[\p{L}\s]*(?<version>[\d.]+)\]");
                if (!match.Success)
                {
                    return null;
                }

                return match.Groups["version"].Value;
            }
            catch
            {
                return null;
            }
        }

        public static String GetApkDownloadUrl(String version)
        {
            return $"https://github.com/shults-s/SmtuSchedule/releases/download/{version}/Shults.SmtuSchedule-{version}.apk";
        }
    }
}