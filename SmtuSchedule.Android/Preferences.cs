using System;
using Android.Content;
using Android.Support.V7.Preferences;

namespace SmtuSchedule.Android
{
    public class Preferences
    {
        public Boolean CheckUpdatesOnStart { get; private set; }

        public Boolean UseFabDateSelector { get; private set; }

        public Boolean ShowSubjectEndTime { get; private set; }

        public DateTime UpperWeekDate { get; private set; }

        public Int32 CurrentScheduleId { get; private set; }

        public DateTime CurrentScheduleDate { get; set; }

        public String LastSeenVersion { get; private set; }

        public String LastMigrationVersion { get; private set; }

        public Preferences(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            CurrentScheduleDate = DateTime.Today;
        }

        public void SetLastMigrationVersion(String lastMigrationVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutString("LastMigrationVersion", lastMigrationVersion);
            editor.Apply();

            LastMigrationVersion = lastMigrationVersion;
        }

        public void SetLastSeenVersion(String lastSeenVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutString("LastSeenVersion", lastSeenVersion);
            editor.Apply();

            LastSeenVersion = lastSeenVersion;
        }

        public void SetCurrentScheduleId(Int32 currentScheduleId)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("CurrentScheduleId", CurrentScheduleId);
            editor.Apply();

            CurrentScheduleId = currentScheduleId;
        }

        public void Read()
        {
            UpperWeekDate = new DateTime(_preferences.GetLong("UpperWeekDate", 0));

            CurrentScheduleId = _preferences.GetInt("CurrentScheduleId", 0);
            LastSeenVersion = _preferences.GetString("LastSeenVersion", null);
            UseFabDateSelector = _preferences.GetBoolean("UseFabDateSelector", false);
            ShowSubjectEndTime = _preferences.GetBoolean("ShowSubjectEndTime", false);
            CheckUpdatesOnStart = _preferences.GetBoolean("CheckUpdatesOnStart", true);
        }

        private readonly ISharedPreferences _preferences;
    }
}