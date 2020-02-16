using System;
using Android.Content;
using Android.Support.V7.Preferences;
using SmtuSchedule.Android.Enumerations;

namespace SmtuSchedule.Android
{
    internal class Preferences : Java.Lang.Object, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public FeatureDiscoveryState FeatureDiscoveryState { get; private set; }

        // public Boolean CheckUpdatesOnStart { get; private set; }

        public Boolean ReplayFeatureDiscovery { get; private set; }

        public Boolean UseFabDateSelector { get; private set; }

        public Boolean UseDarkTheme { get; private set; }

        public Boolean DisplaySubjectEndTime { get; private set; }

        public DateTime UpperWeekDate { get; private set; }

        public Int32 CurrentScheduleId { get; private set; }

        public DateTime CurrentScheduleDate { get; set; }

        public Int32 LastMigrationVersion { get; private set; }

        public Int32 LastSeenUpdateVersion { get; private set; }

        public event Action ThemeChanged;

        public Preferences(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            _preferences.RegisterOnSharedPreferenceChangeListener(this);

            ReplayFeatureDiscovery = _preferences.GetBoolean("ReplayFeatureDiscovery", false);
            if (ReplayFeatureDiscovery)
            {
                SetFeatureDiscoveryState(FeatureDiscoveryState.NothingDiscovered);
                SetReplayFeatureDiscovery(false);
            }
            else
            {
                Int32 state = _preferences.GetInt("FeatureDiscoveryState", 0);
                FeatureDiscoveryState = (FeatureDiscoveryState)state;
            }

            // Обработка конфигурации предыдущих релизов, где версия представляла собой строку.
            try
            {
                LastSeenUpdateVersion = _preferences.GetInt("LastSeenUpdateVersion", 0);
            }
            catch (Java.Lang.ClassCastException)
            {
                SetLastSeenUpdateVersion(0);
            }

            try
            {
                LastMigrationVersion = _preferences.GetInt("LastMigrationVersion", 0);
            }
            catch (Java.Lang.ClassCastException)
            {
                SetLastMigrationVersion(0);
            }

            CurrentScheduleDate = DateTime.Today;
            CurrentScheduleId = _preferences.GetInt("CurrentScheduleId", 0);

            UpperWeekDate = new DateTime(_preferences.GetLong("UpperWeekDate", 0));

            UseDarkTheme = _preferences.GetBoolean("UseDarkTheme", false);
            UseFabDateSelector = _preferences.GetBoolean("UseFabDateSelector", true);
            // CheckUpdatesOnStart = _preferences.GetBoolean("CheckUpdatesOnStart", true);
            DisplaySubjectEndTime = _preferences.GetBoolean("DisplaySubjectEndTime", false);
        }

        public void SetFeatureDiscoveryState(FeatureDiscoveryState featureDiscoveryState)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("FeatureDiscoveryState", (Int32)featureDiscoveryState);
            editor.Apply();

            FeatureDiscoveryState = featureDiscoveryState;
        }

        public void SetReplayFeatureDiscovery(Boolean replayFeatureDiscovery)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutBoolean("ReplayFeatureDiscovery", replayFeatureDiscovery);
            editor.Apply();

            ReplayFeatureDiscovery = replayFeatureDiscovery;
        }

        public void SetLastMigrationVersion(Int32 lastMigrationVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("LastMigrationVersion", lastMigrationVersion);
            editor.Apply();

            LastMigrationVersion = lastMigrationVersion;
        }

        public void SetLastSeenUpdateVersion(Int32 lastSeenUpdateVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("LastSeenUpdateVersion", lastSeenUpdateVersion);
            editor.Apply();

            LastSeenUpdateVersion = lastSeenUpdateVersion;
        }

        public void SetCurrentScheduleId(Int32 currentScheduleId)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("CurrentScheduleId", currentScheduleId);
            editor.Apply();

            CurrentScheduleId = currentScheduleId;
        }

        public void OnSharedPreferenceChanged(ISharedPreferences preferences, String key)
        {
            switch (key)
            {
                case "FeatureDiscoveryState":
                case "CurrentScheduleId":
                case "LastMigrationVersion":
                case "LastSeenUpdateVersion":
                    break;

                case "UpperWeekDate":
                    UpperWeekDate = new DateTime(preferences.GetLong("UpperWeekDate", 0));
                    break;

                // case "CheckUpdatesOnStart":
                //     CheckUpdatesOnStart = preferences.GetBoolean("CheckUpdatesOnStart", true);
                //     break;

                case "UseFabDateSelector":
                    UseFabDateSelector = preferences.GetBoolean("UseFabDateSelector", true);
                    break;

                case "UseDarkTheme":
                    UseDarkTheme = preferences.GetBoolean("UseDarkTheme", false);
                    ThemeChanged?.Invoke();
                    break;

                case "DisplaySubjectEndTime":
                    DisplaySubjectEndTime = preferences.GetBoolean("DisplaySubjectEndTime", false);
                    break;

                case "ReplayFeatureDiscovery":
                    ReplayFeatureDiscovery = preferences.GetBoolean("ReplayFeatureDiscovery", false);
                    break;
            }
        }

        private readonly ISharedPreferences _preferences;
    }
}