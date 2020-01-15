using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
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

        public static Boolean IsUniversitySiteConnectionAvailable(out String failReason)
        {
            // Эмулятор Android, построенный на основе QEMU, не поддерживает ICMP-запросы, поэтому ping в нем не работает.
#if DEBUG
            failReason = null;
            return true;
#endif

            IPStatus status = IPStatus.Success;

            try
            {
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