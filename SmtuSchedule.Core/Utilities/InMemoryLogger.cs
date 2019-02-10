using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Utilities
{
    public class InMemoryLogger : ILogger, IDisposable
    {
        public InMemoryLogger() => _entries = new BlockingCollection<String>();

        public void Log(Exception exception) => Write(exception.Format());

        public void Log(String message) => Write(message);

        public void Log(String message, Exception exception)
        {
            Write(message + exception.Format());
        }

        public void Log(String format, params Object[] parameters)
        {
            Write(format, parameters);
        }

        public Task Save(String fileName)
        {
            return Task.Run(() => File.WriteAllLines(fileName, _entries));
        }

        public void Dispose() => _entries.Dispose();

        private void Write(String format, params Object[] parameters)
        {
            String label = DateTime.Now.ToString("[dd.MM.yyyy HH:mm:ss.fff] ");
            _entries.Add(String.Format(label + format, parameters));
        }

        private readonly BlockingCollection<String> _entries;
    }
}