using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class LecturersDownloaderException : Exception
    {
        public LecturersDownloaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}