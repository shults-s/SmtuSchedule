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
            Write(format, parameters);
        }

        public override String ToString()
        {
            return String.Join(Environment.NewLine, _entries);
        }

        private void Write(String format, params Object[] parameters)
        {
            String label = DateTime.Now.ToString("[dd.MM.yyyy HH:mm:ss.fff] ");
            _entries.Add(String.Format(label + format, parameters));
        }

        private readonly BlockingCollection<String> _entries;
    }
}