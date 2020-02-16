using System;
using System.Linq;
using System.Diagnostics;

namespace SmtuSchedule.Core.Utilities
{
    internal static class PerformanceTester
    {
        public static String Test(Action callback, Int32 testIterationsNumber = 1000)
        {
            Int64[] times = new Int64[testIterationsNumber];
            Stopwatch sw = new Stopwatch();

            for (Int32 i = 0; i < testIterationsNumber; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

                sw.Restart();
                callback();
                sw.Stop();

                times[i] = (Int64)(sw.ElapsedTicks * 1.0e6 / Stopwatch.Frequency + 0.499);
            }

            return String.Format(
                "First: {0} us, minimum: {1} us, maximum: {2} us, average: {3:f3} us",
                times[0],
                times.Skip(1).Min(),
                times.Skip(1).Max(),
                times.Skip(1).Average()
            );
        }
    }
}