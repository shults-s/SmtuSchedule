using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class SchedulesWriterException : Exception
    {
        public SchedulesWriterException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}