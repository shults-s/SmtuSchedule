using System;

namespace SmtuSchedule.Android.Exceptions
{
    public class WorkerException : Exception
    {
        public WorkerException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}