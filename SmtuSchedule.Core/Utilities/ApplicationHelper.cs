using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace SmtuSchedule.Core.Utilities
{
    public static class ApplicationHelper
    {
        public const String LatestReleaseUrl = "https://github.com/shults-s/SmtuSchedule/releases/latest";

        public static async Task<String> GetCurrentVersionAsync()
        {
            const String Url = "https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/CHANGELOG.md";

            try
            {
                String changeLog = await HttpHelper.GetAsync(Url).ConfigureAwait(false);

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

        public static Boolean IsUniversitySiteConnectionAvailable()
        {
            try
            {
                return new Ping().Send("www.smtu.ru").Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public static String GetApkDownloadUrl(String version)
        {
            return $"https://github.com/shults-s/SmtuSchedule/releases/download/{version}/Shults.SmtuSchedule-{version}.apk";
        }
    }
}