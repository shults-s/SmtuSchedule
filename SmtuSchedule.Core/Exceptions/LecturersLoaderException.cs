using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class LecturersLoaderException : Exception
    {
        public LecturersLoaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}