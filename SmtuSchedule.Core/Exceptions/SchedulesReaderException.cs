using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class SchedulesReaderException : Exception
    {
        public SchedulesReaderException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}