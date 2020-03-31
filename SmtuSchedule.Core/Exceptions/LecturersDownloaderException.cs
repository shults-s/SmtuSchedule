using System;

namespace SmtuSchedule.Core.Exceptions
{
    public sealed class LecturersDownloaderException : Exception
    {
        public LecturersDownloaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public LecturersDownloaderException(String message) : base(message)
        {
        }
    }
}