using System;
using System.IO;

namespace SmtuSchedule.Core.Utilities
{
    internal static class FileThreadSafeUtilities
    {
        public static String ReadAllText(String filePath)
        {
            String contents = null;

            FileLocker.OpenWithLock(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                onOpened: (stream) =>
                {
                    using StreamReader reader = new StreamReader(stream);
                    contents = reader.ReadToEnd();
                },
                onException: (exception) => throw exception
            );

            return contents;
        }

        public static void WriteAllText(String filePath, String contents)
        {
            FileLocker.OpenWithLock(
                filePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                onOpened: (stream) =>
                {
                    using StreamWriter writer = new StreamWriter(stream);
                    writer.Write(contents);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                },
                onException: (exception) => throw exception
            );
        }
    }
}