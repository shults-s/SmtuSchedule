using System;

namespace SmtuSchedule.Core.Exceptions
{
    public sealed class SchedulesDownloaderException : Exception
    {
        public SchedulesDownloaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SchedulesDownloaderException(String message) : base(message)
        {
        }
    }
}