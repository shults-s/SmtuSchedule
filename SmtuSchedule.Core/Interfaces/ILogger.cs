using System;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ILogger
    {
        void Log(String message);
        void Log(Exception exception);
        void Log(String message, Exception exception);
        void Log(String format, params Object[] parameters);
    }
}