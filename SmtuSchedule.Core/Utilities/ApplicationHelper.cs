using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
//using System.Text.RegularExpressions;
using SmtuSchedule.Core.Models;

namespace SmtuSchedule.Core.Utilities
{
    public static class ApplicationHelper
    {
        public const String LatestReleaseDownloadPageUrl = "https://github.com/shults-s/SmtuSchedule/releases/latest";

        public static async Task<ReleaseDescription> GetLatestReleaseDescription()
        {
            String url = "https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/SmtuSchedule.Android/Release.json";

            try
            {
                String json = await HttpHelper.GetAsync(url).ConfigureAwait(false);
                return ReleaseDescription.FromJson(json);
            }
            catch
            {
                return null;
            }
        }

        //public static async Task<String> GetLatestVersionAsync()
        //{
        //    const String Url = "https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/CHANGELOG.md";

        //    try
        //    {
        //        String changeLog = await HttpHelper.GetAsync(Url).ConfigureAwait(false);

        //        // ## [Версия X.X.X]
        //        Match match = Regex.Match(changeLog, @"\#\# \[[\p{L}\s]*(?<version>[\d.]+)\]");
        //        if (!match.Success)
        //        {
        //            return null;
        //        }

        //        return match.Groups["version"].Value;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        // Если v1 > v2, вернется 1; если v1 = v2, вернется 0; если v1 < v2, вернется -1.
        public static Int32 CompareVersions(String version1, String version2)
        {
            Int32[] v1 = version1.Split('.').Select(s => Int32.TryParse(s, out Int32 value) ? value : Int32.MaxValue)
                .ToArray();

            Int32[] v2 = version2.Split('.').Select(s => Int32.TryParse(s, out Int32 value) ? value : Int32.MaxValue)
                .ToArray();

            Int32 majorComparsion = (v1[0] > v2[0]) ? 1 : (v1[0] < v2[0] ? -1 : 0);
            Int32 minorComparsion = (v1[1] > v2[1]) ? 1 : (v1[1] < v2[1] ? -1 : 0);

            Int32 patchComparsion = (v1.Length == 3 && v2.Length == 3)
                ? (v1[2] > v2[2] ? 1 : v1[2] < v2[2] ? -1 : 0)
                : (v1.Length == v2.Length ? 0 : v1.Length > v2.Length ? 1 : -1);

            return (majorComparsion == 0) ? (minorComparsion == 0 ? patchComparsion : minorComparsion) : majorComparsion;
        }

        public static Boolean IsUniversitySiteConnectionAvailable(out String failReason)
        {
            IPStatus status = IPStatus.Success;

            try
            {
                // Эмулятор Android, построенный на основе QEMU, не поддерживает ICMP-запросы, поэтому ping не работает.
                status = new Ping().Send("www.smtu.ru").Status;

                failReason = (status != IPStatus.Success)
                    ? "Ping failed with status " + Enum.GetName(typeof(IPStatus), status) + " without throwing an exception."
                    : null;

                return status == IPStatus.Success;
            }
            catch (Exception exception)
            {
                failReason = "Ping failed with status " + Enum.GetName(typeof(IPStatus), status) + ": " + exception.Format();
                return false;
            }
        }
    }
}