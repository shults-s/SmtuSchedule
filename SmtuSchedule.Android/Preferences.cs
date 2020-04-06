using System;
using System.Linq;
using System.Collections.Generic;
using Android.Content;
using AndroidX.Preference;
using SmtuSchedule.Android.Interfaces;
using SmtuSchedule.Android.Enumerations;

namespace SmtuSchedule.Android
{
    internal sealed class Preferences : Java.Lang.Object, ISharedPreferencesOnSharedPreferenceChangeListener,
        IPreferences
    {
        public FeatureDiscoveryState FeatureDiscoveryState { get; private set; }

        public LessonRemindTime LessonRemindTimes { get; private set; }

        public Boolean ReplayFeatureDiscovery { get; private set; }

        public Boolean UpdateSchedulesOnStart { get; private set; }

        public Boolean CheckUpdatesOnStart { get; private set; }

        public Boolean DisplayAnotherWeekSubjects { get; private set; }

        public Boolean DisplaySubjectEndTime { get; private set; }

        public Boolean UseFabDateSelector { get; private set; }

        public Boolean UseDarkTheme { get; private set; }

        public DateTime UpperWeekDate { get; private set; }

        public Int32 CurrentScheduleId { get; private set; }

        public DateTime CurrentScheduleDate { get; set; }

        public Int32 LastMigrationVersion { get; private set; }

        public Int32 LastSeenUpdateVersion { get; private set; }

        public event Action ThemeChanged;

        public event Action LessonRemindTimesChanged;

        public event Action UpdateSchedulesOnStartChanged;

        public Preferences(Context context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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

#pragma warning disable CS0612
            // Сброс флага UpdateSchedulesOnStart, который более не используется по назначению
            // и в дальнейшем может быть переиспользован для другого обучающего экрана.
            if (FeatureDiscoveryState.HasFlag(FeatureDiscoveryState.UpdateSchedulesOnStart))
            {
                FeatureDiscoveryState &= ~FeatureDiscoveryState.UpdateSchedulesOnStart;
                SetFeatureDiscoveryState(FeatureDiscoveryState);
            }
#pragma warning restore CS0612

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

            CheckUpdatesOnStart = _preferences.GetBoolean("CheckUpdatesOnStart", true);
            UpdateSchedulesOnStart = _preferences.GetBoolean("UpdateSchedulesOnStart", true);

            UseDarkTheme = _preferences.GetBoolean("UseDarkTheme", false);
            UseFabDateSelector = _preferences.GetBoolean("UseFabDateSelector", true);
            DisplaySubjectEndTime = _preferences.GetBoolean("DisplaySubjectEndTime", true);
            DisplayAnotherWeekSubjects = _preferences.GetBoolean("DisplayAnotherWeekSubjects", true);

            LessonRemindTimes = ParseLessonRemindTimes(_preferences.GetStringSet("LessonRemindTimes", null));
        }

        public void SetFeatureDiscoveryState(FeatureDiscoveryState state)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("FeatureDiscoveryState", (Int32)state);
            editor.Apply();

            FeatureDiscoveryState = state;
        }

        public void SetReplayFeatureDiscovery(Boolean replay)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutBoolean("ReplayFeatureDiscovery", replay);
            editor.Apply();

            ReplayFeatureDiscovery = replay;
        }

        public void SetLastMigrationVersion(Int32 version)
        {
            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "Number must be non-negative.");
            }

            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("LastMigrationVersion", version);
            editor.Apply();

            LastMigrationVersion = version;
        }

        public void SetLastSeenUpdateVersion(Int32 version)
        {
            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "Number must be non-negative.");
            }

            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("LastSeenUpdateVersion", version);
            editor.Apply();

            LastSeenUpdateVersion = version;
        }

        public void SetCurrentScheduleId(Int32 scheduleId)
        {
            if (scheduleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scheduleId), "Number must be positive.");
            }

            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("CurrentScheduleId", scheduleId);
            editor.Apply();

            CurrentScheduleId = scheduleId;
        }

        private static LessonRemindTime ParseLessonRemindTimes(IEnumerable<String> values)
        {
            if (values == null)
            {
                return LessonRemindTime.Never;
            }

            return (LessonRemindTime)values.Select(v => Int32.Parse(v)).Sum();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences preferences, String key)
        {
            switch (key)
            {
                case "CheckUpdatesOnStart":
                    CheckUpdatesOnStart = preferences.GetBoolean("CheckUpdatesOnStart", true);
                    break;

                case "UpdateSchedulesOnStart":
                    UpdateSchedulesOnStart = preferences.GetBoolean("UpdateSchedulesOnStart", true);
                    UpdateSchedulesOnStartChanged?.Invoke();
                    break;

                case "ReplayFeatureDiscovery":
                    ReplayFeatureDiscovery = preferences.GetBoolean("ReplayFeatureDiscovery", false);
                    break;

                case "UpperWeekDate":
                    UpperWeekDate = new DateTime(preferences.GetLong("UpperWeekDate", 0));
                    break;

                case "UseDarkTheme":
                    UseDarkTheme = preferences.GetBoolean("UseDarkTheme", false);
                    ThemeChanged?.Invoke();
                    break;

                case "UseFabDateSelector":
                    UseFabDateSelector = preferences.GetBoolean("UseFabDateSelector", true);
                    break;

                case "DisplaySubjectEndTime":
                    DisplaySubjectEndTime = preferences.GetBoolean("DisplaySubjectEndTime", true);
                    break;

                case "DisplayAnotherWeekSubjects":
                    DisplayAnotherWeekSubjects = preferences.GetBoolean("DisplayAnotherWeekSubjects", true);
                    break;

                case "LessonRemindTimes":
                    LessonRemindTimes = ParseLessonRemindTimes(preferences.GetStringSet("LessonRemindTimes", null));
                    LessonRemindTimesChanged?.Invoke();
                    break;
            }
        }

        private readonly ISharedPreferences _preferences;
    }
}