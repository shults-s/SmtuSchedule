using System;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Utilities
{
    public class InMemoryLogger : ILogger
    {
        public event Action<Exception> ExceptionLogged;

        public InMemoryLogger() => _entries = new BlockingCollection<String>();

        public void Log(Exception exception)
        {
            Write(exception.Format());
            ExceptionLogged?.Invoke(exception);
        }

        public void Log(String message) => Write(message);

        public void Log(String message, Exception exception)
        {
            Write(message + exception.Format());
        }

        public void Log(String format, params Object[] parameters)
        {
            Write(String.Format(format, parameters));
        }

        public override String ToString()
        {
            return String.Join(Environment.NewLine, _entries);
        }

        private void Write(String message)
        {
            String label = DateTime.Now.ToString("[dd.MM.yyyy HH:mm:ss.fff] ");
            _entries.Add(label + message);
        }

        private readonly BlockingCollection<String> _entries;
    }
}