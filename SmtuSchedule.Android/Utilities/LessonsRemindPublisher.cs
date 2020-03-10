using System;
using System.Collections.Generic;
using Android.Content;

namespace SmtuSchedule.Android.Utilities
{
    [BroadcastReceiver]
    internal class LessonsRemindPublisher : BroadcastReceiver
    {
        public const String IntentNotificationScheduleIdKey = "NotificationScheduleIdTitle";
        public const String IntentNotificationLessonDateKey = "NotificationLessonDateTitle";
        public const String IntentNotificationMessageKey = "NotificationMessage";
        public const String IntentNotificationTitleKey = "NotificationTitle";

        public override void OnReceive(Context context, Intent intent)
        {
            String message = intent.Extras.GetString(IntentNotificationMessageKey);
            String title = intent.Extras.GetString(IntentNotificationTitleKey);

            String scheduleId = intent.Extras.GetString(IntentNotificationScheduleIdKey);
            String lessonDate = intent.Extras.GetString(IntentNotificationLessonDateKey);
            Dictionary<String, String> data = new Dictionary<String, String>()
            {
                [IntentUtilities.DataUpcomingLessonDateKey] = lessonDate,
                [IntentUtilities.DataUpcomingLessonScheduleIdKey] = scheduleId
            };

            const Int32 Id = 777;
            String channelId = NotificationUtilities.GeneralChannelId;
            NotificationUtilities.DisplayNotification(context, channelId, Id, title, message, data);
        }
    }
}