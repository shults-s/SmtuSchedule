using System;
using AndroidX.Work;

namespace SmtuSchedule.Android.Utilities
{
    internal static class PeriodicWorkUtilities
    {
        public static void CreateWork<T>(String tag, TimeSpan repeatInterval, Boolean requireNetwork)
            where T : Worker
        {
            PeriodicWorkRequest work;
            if (requireNetwork)
            {
                Constraints constraints = new Constraints.Builder()
                    .SetRequiredNetworkType(NetworkType.NotRoaming)
                    .Build();

                work = PeriodicWorkRequest.Builder.From<T>(repeatInterval)
                    .SetConstraints(constraints)
                    .Build();
            }
            else
            {
                work = PeriodicWorkRequest.Builder.From<T>(repeatInterval).Build();
            }

            ExistingPeriodicWorkPolicy existingPeriodicWorkPolicy = ExistingPeriodicWorkPolicy.Keep;
            WorkManager.Instance.EnqueueUniquePeriodicWork(tag, existingPeriodicWorkPolicy, work);
        }

        public static void CancelWorkByTag(String tag) => WorkManager.Instance.CancelUniqueWork(tag);
    }
}