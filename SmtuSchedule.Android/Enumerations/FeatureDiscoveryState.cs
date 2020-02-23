namespace SmtuSchedule.Android.Enumerations
{
    [System.Flags]
    internal enum FeatureDiscoveryState
    {
        NothingDiscovered = 0,
        ApplicationSettings = 1,
        SchedulesDownload = 2,
        SchedulesManagement = 4,
        ScheduleChangeDate = 8,
        UpdateSchedulesOnStart = 16
    }
}