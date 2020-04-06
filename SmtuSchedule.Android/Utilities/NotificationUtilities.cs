using System;
using Android.OS;
using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace SmtuSchedule.Android.Utilities
{
    internal static class NotificationUtilities
    {
        public const String UniversityChannelId = "University";

        public const String GeneralChannelId = "General";

        public const String UpdatesChannelId = "Updates";

        private static class UniqueIdGenerator
        {
            public static Int32 GenerateId() => _id++;

            private static Int32 _id = 1;
        }

        public static Int32 DisplayNotification(Context context, String channelId, Int32 notificationId,
            String notificationTitle, String notificationText, Intent intent = null)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            NotificationManagerCompat manager = NotificationManagerCompat.From(context);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(
                context,
                channelId ?? GeneralChannelId
            );

            notificationId = (notificationId != 0) ? notificationId : UniqueIdGenerator.GenerateId();

            if (intent != null)
            {
                builder.SetContentIntent(PendingIntent.GetActivity(
                    context,
                    notificationId,
                    intent,
                    PendingIntentFlags.OneShot
                ));
            }

            notificationTitle ??= context.Resources.GetString(Resource.String.applicationCompleteName);

            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notificationText))
                .SetContentTitle(notificationTitle)
                .SetSmallIcon(Resource.Mipmap.launcherIcon)
                .SetAutoCancel(true);

            manager.Notify(notificationId, builder.Build());

            return notificationId;
        }

        public static void CreateNotificationChannels(Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return ;
            }

            NotificationManager manager = context.GetSystemService(Context.NotificationService)
                as NotificationManager;

            NotificationChannel updatesChannel = new NotificationChannel(
                UpdatesChannelId,
                context.GetString(Resource.String.updatesNotificationChannelName),
                NotificationImportance.Default
            )
            {
                Description = context.GetString(Resource.String.updatesNotificationChannelDescription)
            };

            NotificationChannel generalChannel = new NotificationChannel(
                GeneralChannelId,
                context.GetString(Resource.String.generalNotificationChannelName),
                NotificationImportance.High
            )
            {
                Description = context.GetString(Resource.String.generalNotificationChannelDescription)
            };

            NotificationChannel universityChannel = new NotificationChannel(
                UniversityChannelId,
                context.GetString(Resource.String.universityNotificationChannelName),
                NotificationImportance.Default
            )
            {
                Description = context.GetString(Resource.String.universityNotificationChannelDescription)
            };

            manager.CreateNotificationChannel(updatesChannel);
            manager.CreateNotificationChannel(generalChannel);
            manager.CreateNotificationChannel(universityChannel);
        }
    }
}