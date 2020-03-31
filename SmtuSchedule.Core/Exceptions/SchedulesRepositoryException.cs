using System;

namespace SmtuSchedule.Core.Exceptions
{
    public sealed class SchedulesRepositoryException : Exception
    {
        public SchedulesRepositoryException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SchedulesRepositoryException(String message) : base(message)
        {
        }
    }
}