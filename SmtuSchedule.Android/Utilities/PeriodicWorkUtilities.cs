using System;
using AndroidX.Work;
using Android.Content;

namespace SmtuSchedule.Android.Utilities
{
    internal static class PeriodicWorkUtilities
    {
        public static void CreateAndEnqueueWork<T>(Context context, String tag, TimeSpan repeatInterval,
            Boolean requiredNetwork) where T : Worker
        {
            PeriodicWorkRequest work;
            if (requiredNetwork)
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

            ExistingPeriodicWorkPolicy existingWorkPolicy = ExistingPeriodicWorkPolicy.Keep;
            WorkManager.GetInstance(context).EnqueueUniquePeriodicWork(tag, existingWorkPolicy, work);
        }

        public static void CancelWork(Context context, String tag) => WorkManager.GetInstance(context)
            .CancelUniqueWork(tag);
    }
}