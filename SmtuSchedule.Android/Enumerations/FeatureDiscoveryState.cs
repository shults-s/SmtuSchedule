namespace SmtuSchedule.Android.Enumerations
{
    [System.Flags]
    internal enum FeatureDiscoveryState
    {
        ApplicationSettings = 1,
        SchedulesDownload = 2,
        SchedulesManagement = 4,
        ScheduleChangeDate = 8
    }
}