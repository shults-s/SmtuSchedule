using System;
using Android.Content;
using Android.Support.V7.Preferences;

namespace SmtuSchedule.Android
{
    public class Preferences
    {
        public Boolean CheckUpdatesOnStart { get; private set; }

        public Boolean UseFabDateSelector { get; private set; }

        public Boolean DisplaySubjectEndTime { get; private set; }

        public DateTime UpperWeekDate { get; private set; }

        public Int32 CurrentScheduleId { get; private set; }

        public DateTime CurrentScheduleDate { get; set; }

        public String LastMigrationVersion { get; private set; }

        public String LastSeenUpdateVersion { get; private set; }

        public String LastSeenWelcomeVersion { get; private set; }

        public Preferences(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            CurrentScheduleDate = DateTime.Today;
        }

        public void SetLastSeenWelcomeVersion(String lastSeenWelcomeVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutString("LastSeenWelcomeVersion", lastSeenWelcomeVersion);
            editor.Apply();

            LastSeenWelcomeVersion = lastSeenWelcomeVersion;
        }

        public void SetLastMigrationVersion(String lastMigrationVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutString("LastMigrationVersion", lastMigrationVersion);
            editor.Apply();

            LastMigrationVersion = lastMigrationVersion;
        }

        public void SetLastSeenUpdateVersion(String lastSeenUpdateVersion)
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutString("LastSeenUpdateVersion", lastSeenUpdateVersion);
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

        public void Read()
        {
            UpperWeekDate = new DateTime(_preferences.GetLong("UpperWeekDate", 0));
            UseFabDateSelector = _preferences.GetBoolean("UseFabDateSelector", false);
            CheckUpdatesOnStart = _preferences.GetBoolean("CheckUpdatesOnStart", true);
            DisplaySubjectEndTime = _preferences.GetBoolean("DisplaySubjectEndTime", false);

            CurrentScheduleId = _preferences.GetInt("CurrentScheduleId", 0);
            LastMigrationVersion = _preferences.GetString("LastMigrationVersion", null);
            LastSeenUpdateVersion = _preferences.GetString("LastSeenUpdateVersion", null);
            LastSeenWelcomeVersion = _preferences.GetString("LastSeenWelcomeVersion", null);
        }

        private readonly ISharedPreferences _preferences;
    }
}