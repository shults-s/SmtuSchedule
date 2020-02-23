using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;

namespace SmtuSchedule.Android.Utilities
{
    [IntentFilter(new String[] { "com.google.firebase.MESSAGING_EVENT" })]
    [Service]
    public class MessagingService : FirebaseMessagingService
    {
#if DEBUG
        private const String FirebasePreferencesName = "Firebase";

        private const String FirebaseTokenKey = "Token";

        public static String GetToken(Context context)
        {
            return context.GetSharedPreferences(FirebasePreferencesName, FileCreationMode.Private)
                .GetString(FirebaseTokenKey, null);
        }

        public override void OnNewToken(String token)
        {
            base.OnNewToken(token);

            GetSharedPreferences(FirebasePreferencesName, FileCreationMode.Private)
                .Edit()
                .PutString(FirebaseTokenKey, token)
                .Apply();
        }
#endif

        public override void OnMessageReceived(RemoteMessage message)
        {
            RemoteMessage.Notification notification = message.GetNotification();

            // На случай, если сообщение содержит только данные (data message).
            if (notification == null)
            {
                return ;
            }

            String title = notification.Title;
            String text = notification.Body;
            String channelId = notification.ChannelId;

            NotificationUtilities.DisplayNotification(this, channelId, 0, title, text, message.Data);
        }
    }
}