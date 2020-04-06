using System;
using Android.OS;
using Android.App;

namespace SmtuSchedule.Android.Utilities
{
    internal sealed class ApplicationLifecycleListener : Java.Lang.Object,
        Application.IActivityLifecycleCallbacks
    {
        public event Action ApplicationStarted;

        public event Action ApplicationStopped;

        public ApplicationLifecycleListener() => _activitiesReferencesNumber = 0;

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
        }

        public void OnActivityStarted(Activity activity)
        {
            if (++_activitiesReferencesNumber == 1)
            {
                ApplicationStarted?.Invoke();
            }
        }

        public void OnActivityResumed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {
        }

        public void OnActivityStopped(Activity activity)
        {
            if (--_activitiesReferencesNumber == 0)
            {
                ApplicationStopped?.Invoke();
            }
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        private Int32 _activitiesReferencesNumber;
    }
}