using System;
using System.Collections.Generic;
using Android.Content;
using SmtuSchedule.Android.Views;

using Uri = Android.Net.Uri;

namespace SmtuSchedule.Android.Utilities
{
    internal static class IntentUtilities
    {
        // Ключи в словаре данных, наличие которых означает, что необходимо открыть указанное расписание
        // на указанную дату.
        public const String DataUpcomingLessonScheduleIdKey = "UpcomingLessonScheduleId";
        public const String DataUpcomingLessonDateKey = "UpcomingLessonDate";

        // Ключ в словаре данных, наличие которого означает, что необходимо открыть страницу указанного
        // приложения в Google Play Store.
        private const String DataGooglePlayStoreKey = "GooglePlayStore";

        // Ключ в словаре данных, наличие которого означает, что необходимо открыть указанный URL.
        private const String DataUrlKey = "Url";

        public static Boolean IsDataKeysCollectionValidToCreateViewIntent(ICollection<String> collection)
        {
            return collection.Contains(DataUrlKey) || collection.Contains(DataGooglePlayStoreKey);
        }

        public static Boolean IsDataKeysCollectionValidToCreateUpcomingLessonIntent(
            ICollection<String> collection)
        {
            return collection.Contains(DataUpcomingLessonDateKey)
                && collection.Contains(DataUpcomingLessonScheduleIdKey);
        }

        public static Boolean IsDataKeysCollectionValidToCreateIntent(ICollection<String> collection)
        {
            return IsDataKeysCollectionValidToCreateViewIntent(collection)
                || IsDataKeysCollectionValidToCreateUpcomingLessonIntent(collection);
        }

        public static Intent CreateIntentForUpcomingLesson(Context context,
            String dataUpcomingLessonDateValue, String dataUpcomingLessonScheduleIdValue)
        {
            Intent intent = new Intent(context, typeof(MainActivity));
            intent.PutExtra(DataUpcomingLessonDateKey, dataUpcomingLessonDateValue);
            intent.PutExtra(DataUpcomingLessonScheduleIdKey, dataUpcomingLessonScheduleIdValue);

            return intent;
        }

        public static Intent CreateViewIntentFromUrl(String url)
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

        public static Intent CreateIntentFromData(Context context, IDictionary<String, String> data)
        {
            if (data == null)
            {
                return null;
            }

            if (data.ContainsKey(DataUpcomingLessonDateKey))
            {
                return CreateIntentForUpcomingLesson(
                    context,
                    data[DataUpcomingLessonDateKey],
                    data[DataUpcomingLessonScheduleIdKey]
                );
            }
            else if (data.ContainsKey(DataUrlKey))
            {
                return CreateViewIntentFromUrl(data[DataUrlKey]);
            }
            else if (data.ContainsKey(DataGooglePlayStoreKey))
            {
                return CreateGooglePlayStoreViewIntent(context, data[DataGooglePlayStoreKey]);
            }

            return null;
        }

        public static Intent CreateGooglePlayStoreViewIntent(Context context, String packageId,
            Boolean useBrowserForced = false)
        {
            SmtuScheduleApplication application = context.ApplicationContext as SmtuScheduleApplication;
            if (!useBrowserForced && application.IsPlayStoreInstalled())
            {
                return CreateViewIntentFromUrl("market://details?id=" + packageId);
            }

            return CreateViewIntentFromUrl("https://play.google.com/store/apps/details?id=" + packageId);
        }
    }
}