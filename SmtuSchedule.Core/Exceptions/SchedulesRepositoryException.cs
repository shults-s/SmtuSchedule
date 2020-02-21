using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class SchedulesRepositoryException : Exception
    {
        public SchedulesRepositoryException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}