using System;
using Android.Content;
using Android.Support.V7.Preferences;

namespace SmtuSchedule.Android
{
    public class Preferences
    {
        public DateTime CurrentScheduleDate { get; set; }

        public Int32 CurrentScheduleId { get; set; }

        public Boolean CheckUpdatesOnStart { get; set; }

        public String LastSeenVersion { get; set; }

        public Boolean UseFabDateSelector { get; set; }

        public Boolean ShowSubjectEndTime { get; set; }

        public Boolean IsFirstRun { get; set; }

        public DateTime UpperWeekDate { get; set; }

        public Preferences(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
            CurrentScheduleDate = DateTime.Today;
        }

        public void Save()
        {
            ISharedPreferencesEditor editor = _preferences.Edit();
            editor.PutInt("CurrentScheduleId", CurrentScheduleId);
            editor.PutString("LastSeenVersion", LastSeenVersion);
            editor.PutBoolean("IsFirstRun", IsFirstRun);
            editor.Apply();
        }

        public void Read()
        {
            UpperWeekDate = new DateTime(_preferences.GetLong("UpperWeekDate", 0));

            CurrentScheduleId = _preferences.GetInt("CurrentScheduleId", 0);
            LastSeenVersion = _preferences.GetString("LastSeenVersion", null);
            IsFirstRun = _preferences.GetBoolean("IsFirstRun", true);
            UseFabDateSelector = _preferences.GetBoolean("UseFabDateSelector", false);
            ShowSubjectEndTime = _preferences.GetBoolean("ShowSubjectEndTime", false);
            CheckUpdatesOnStart = _preferences.GetBoolean("CheckUpdatesOnStart", true);
        }

        private ISharedPreferences _preferences;
    }
}