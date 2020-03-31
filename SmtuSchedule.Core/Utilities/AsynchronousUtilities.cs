using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmtuSchedule.Core.Utilities
{
    public static class AsynchronousUtilities
    {
        private static readonly TaskFactory Factory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default
        );

        public static void RunSynchronously(Func<Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            Factory.StartNew(callback).Unwrap().GetAwaiter().GetResult();
        }

        public static T RunSynchronously<T>(Func<Task<T>> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return Factory.StartNew(callback).Unwrap().GetAwaiter().GetResult();
        }
    }
}