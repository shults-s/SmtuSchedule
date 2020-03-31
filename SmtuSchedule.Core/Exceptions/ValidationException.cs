using System;

namespace SmtuSchedule.Core.Exceptions
{
    public sealed class ValidationException : Exception
    {
        public ValidationException(String message) : base(message)
        {
        }
    }
}