using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class SchedulesLoaderException : Exception
    {
        public SchedulesLoaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SchedulesLoaderException(String message) : base(message)
        {
        }
    }
}