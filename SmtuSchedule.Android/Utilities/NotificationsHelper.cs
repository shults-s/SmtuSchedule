using System;
using System.Collections.Generic;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Support.V4.App;

using Uri = Android.Net.Uri;

namespace SmtuSchedule.Android.Utilities
{
    internal static class NotificationsHelper
    {
        // Ключ в словаре данных, наличие которого означает, что необходимо открыть страницу указанного
        // приложения в Google Play Store.
        private const String DataGooglePlayStoreKey = "GooglePlayStore";

        // Ключ в словаре данных, наличие которого означает, что необходимо открыть указанный URL.
        private const String DataUrlKey = "Url";

        public const String UniversityChannelId = "University";

        public const String GeneralChannelId = "General";

        public const String UpdatesChannelId = "Updates";

        private static class UniqueIdGenerator
        {
            public static Int32 GenerateId() => _id++;

            private static Int32 _id = 1;
        }

        public static Int32 DisplayNotification(Context context, String channelId, Int32 notificationId,
            String title, String text, IDictionary<String, String> data = null)
        {
            NotificationManagerCompat manager = NotificationManagerCompat.From(context);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(
                context,
                channelId ?? GeneralChannelId
            );

            notificationId = (notificationId != 0) ? notificationId : UniqueIdGenerator.GenerateId();

            if (data != null)
            {
                Intent intent = CreateIntentFromNotificationData(data);
                if (intent == null)
                {
                    return -1;
                }

                builder.SetContentIntent(PendingIntent.GetActivity(
                    context,
                    notificationId,
                    intent,
                    PendingIntentFlags.OneShot
                ));
            }

            if (title != null)
            {
                builder.SetContentTitle(title);
            }

            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(text)).SetAutoCancel(true)
                .SetSmallIcon(Resource.Mipmap.launcherIcon);

            manager.Notify(notificationId, builder.Build());
            return notificationId;
        }

        public static Boolean IsKeysCollectionValidToCreateIntent(ICollection<String> collection)
        {
            return collection.Contains(DataUrlKey) || collection.Contains(DataGooglePlayStoreKey);
        }

        public static Intent CreateIntentFromNotificationData(IDictionary<String, String> data)
        {
            static Intent CreateIntentFromUrl(String url)
            {
                Uri uri;
                try
                {
                    uri = Uri.Parse(url);
                }
                catch
                {
                    return null;
                }

                return new Intent(Intent.ActionView, uri);
            }

            if (data == null)
            {
                return null;
            }

            if (data.ContainsKey(DataUrlKey))
            {
                return CreateIntentFromUrl(data[DataUrlKey]);
            }
            else if (data.ContainsKey(DataGooglePlayStoreKey))
            {
                return CreateIntentFromUrl("market://details?id=" + data[DataGooglePlayStoreKey]);
            }

            return null;
        }

        public static void CreateNotificationsChannels(Context context)
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