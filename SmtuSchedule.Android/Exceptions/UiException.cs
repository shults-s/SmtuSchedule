using System;

namespace SmtuSchedule.Android.Exceptions
{
    public class UiException : Exception
    {
        public UiException(String message) : base(message)
        {
        }
    }
}