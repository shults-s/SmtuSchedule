using System;
using System.Collections.Generic;
using Android.Content;

using Uri = Android.Net.Uri;

namespace SmtuSchedule.Android.Utilities
{
    internal static class IntentUtilities
    {
        // Ключ в словаре данных, наличие которого означает, что необходимо открыть страницу указанного
        // приложения в Google Play Store.
        private const String DataGooglePlayStoreKey = "GooglePlayStore";

        // Ключ в словаре данных, наличие которого означает, что необходимо открыть указанный URL.
        private const String DataUrlKey = "Url";

        public static Boolean IsDataKeysCollectionValidToCreateViewIntent(ICollection<String> collection)
        {
            return collection.Contains(DataUrlKey) || collection.Contains(DataGooglePlayStoreKey);
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

        public static Intent CreateViewIntentFromData(Context context, IDictionary<String, String> data)
        {
            if (data == null)
            {
                return null;
            }

            if (data.ContainsKey(DataUrlKey))
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