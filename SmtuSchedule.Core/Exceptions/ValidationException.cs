using System;

namespace SmtuSchedule.Core.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException(String message) : base(message)
        {
        }
    }
}