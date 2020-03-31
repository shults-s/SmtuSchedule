using System;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Utilities
{
    public sealed class InMemoryLogger : ILogger
    {
        public event Action<Exception> ExceptionLogged;

        public InMemoryLogger() => _entries = new BlockingCollection<String>();

        public void Log(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Write(exception.Format());

            ExceptionLogged?.Invoke(exception);
        }

        public void Log(String message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Write(message);
        }

        public void Log(String message, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Write(message + exception.Format());

            ExceptionLogged?.Invoke(exception);
        }

        public void Log(String format, params Object[] parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

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