namespace SmtuSchedule.Android.Enumerations
{
    [System.Flags]
    internal enum LessonRemindTime
    {
        Never = 0,
        FiveMinutes = 1,
        TenMinutes = 2,
        ThirtyMinutes = 4,
        OneHour = 8,
        OneAndHalfHour = 16,
        // TwoHours = 32,
        ThreeHours = 64,
        Midnight = 128,
        OvernightAtNineHours = 256,
        OvernightAtElevenHours = 512
    }
}