using System;
using SmtuSchedule.Android.Enumerations;

namespace SmtuSchedule.Android.Interfaces
{
    internal interface IPreferences
    {
        FeatureDiscoveryState FeatureDiscoveryState { get; }

        Boolean DisplayAnotherWeekSubjects { get; }

        Boolean ReplayFeatureDiscovery { get; }

        Boolean UpdateSchedulesOnStart { get; }

        Boolean DisplaySubjectEndTime { get; }

        Boolean CheckUpdatesOnStart { get; }

        Boolean UseFabDateSelector { get; }

        Boolean UseDarkTheme { get; }

        DateTime UpperWeekDate { get; }

        Int32 LastMigrationVersion { get; }

        Int32 LastSeenUpdateVersion { get; }

        Int32 CurrentScheduleId { get; }

        DateTime CurrentScheduleDate { get; set; }

        LessonRemindTime LessonRemindTimes { get; }

        event Action ThemeChanged;

        event Action LessonRemindTimesChanged;

        event Action UpdateSchedulesOnStartChanged;

        void SetCurrentScheduleId(Int32 value);

        void SetLastMigrationVersion(Int32 value);

        void SetLastSeenUpdateVersion(Int32 value);

        void SetReplayFeatureDiscovery(Boolean value);

        void SetFeatureDiscoveryState(FeatureDiscoveryState value);
    }
}