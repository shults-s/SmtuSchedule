using System;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ILogger
    {
        // Срабатывает только при вызове Log(Exception exception).
        event Action<Exception> ExceptionLogged;

        void Log(String message);
        void Log(Exception exception);
        void Log(String message, Exception exception);
        void Log(String format, params Object[] parameters);
    }
}