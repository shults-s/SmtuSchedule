using System;

namespace SmtuSchedule.Android.Enumerations
{
    [Flags]
    internal enum FeatureDiscoveryState
    {
        NothingDiscovered = 0,
        ConfigureApplication = 1,
        DownloadSchedules = 2,
        ManageSchedules = 4,
        ChangeViewingDate = 8,
        [Obsolete] UpdateSchedulesOnStart = 16
    }
}