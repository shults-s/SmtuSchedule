using System;
using Android.OS;
using Android.App;

namespace SmtuSchedule.Android.Utilities
{
    internal class ApplicationLifecycleListener : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public event Action Started;
        public event Action Stopped;

        public ApplicationLifecycleListener() => _activitiesReferencesNumber = 0;

        public void OnActivityStarted(Activity activity)
        {
            if (++_activitiesReferencesNumber == 1)
            {
                Started?.Invoke();
            }
        }

        public void OnActivityStopped(Activity activity)
        {
            if (--_activitiesReferencesNumber == 0)
            {
                Stopped?.Invoke();
            }
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {
        }

        public void OnActivityResumed(Activity activity)
        {
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        private Int32 _activitiesReferencesNumber;
    }
}