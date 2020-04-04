using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace SmtuSchedule.Core.Utilities
{
    public static class NetworkUtilities
    {
        public const String UniversitySiteHostName = "www.smtu.ru";

        public static Task<Boolean> IsUniversitySiteConnectionAvailableAsync()
        {
            return Task.Run(() => IsUniversitySiteConnectionAvailable(out String _));
        }

        public static Boolean IsUniversitySiteConnectionAvailable(out String? failReason)
        {
#pragma warning disable CS0162
            // Эмулятор Android, разработанный на основе QEMU, не поддерживает ICMP-запросы, поэтому ping не работает,
            // что делает невозможным тестирование некоторых возможностей приложения в режиме отладки.
#if DEBUG
            failReason = null;
            return true;
#endif

            PingReply? reply = null;
            try
            {
                reply = new Ping().Send(UniversitySiteHostName);

                failReason = (reply.Status != IPStatus.Success)
                    ? $"Ping failed with status {reply.Status} without throwing an exception."
                    : null;

                return (reply.Status == IPStatus.Success);
            }
            catch (PingException exception)
            {
                failReason = $"Ping failed with {reply?.Status} status and threw an exception:\n{exception.Format()}";
                return false;
            }
#pragma warning restore CS0162
        }
    }
}