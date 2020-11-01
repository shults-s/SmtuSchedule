using System;
using System.IO;
using System.Threading;

namespace SmtuSchedule.Core.Utilities
{
    internal static class FileLocker
    {
        private const Int32 LockAttemptsIntervalInMilliseconds = 100;

        private const Int32 LockAttemptsNumber = 10;

        public static void OpenWithLock(String filePath, FileMode openMode, FileAccess openAccess,
            Action<FileStream> onOpened, Action<Exception> onException)
        {
            Int32 attemptNumber = 0;

            while (true)
            {
                try
                {
                    // Пытаемся открыть файл и заблокировать доступ к нему для других процессов.
                    using FileStream stream = File.Open(
                        filePath,
                        openMode,
                        openAccess,
                        FileShare.None
                    );

                    try
                    {
                        onOpened?.Invoke(stream);
                    }
                    catch (Exception exception)
                    {
                        onException?.Invoke(exception);
                    }
                    finally
                    {
                        stream.Close();
                    }

                    break;
                }
                catch (FileNotFoundException exception)
                {
                    throw exception;
                }
                catch (IOException exception)
                {
                    if (++attemptNumber == LockAttemptsNumber)
                    {
                        throw new IOException(
                            $"Failed to open and lock file '{filePath}' in {attemptNumber} attempts.",
                            exception
                        );
                    }

                    Thread.Sleep(LockAttemptsIntervalInMilliseconds);
                }
            }
        }
    }
}