using System;

namespace SmtuSchedule.Core.Exceptions
{
    public sealed class LecturersRepositoryException : Exception
    {
        public LecturersRepositoryException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}