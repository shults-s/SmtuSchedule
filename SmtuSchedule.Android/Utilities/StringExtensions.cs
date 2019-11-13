using Java.Lang;
using Android.OS;
using Android.Text;

namespace SmtuSchedule.Android.Utilities
{
    internal static class StringExtensions
    {
        public static ICharSequence ParseHtml(this System.String html)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.N)
            {
                #pragma warning disable CS0618
                return Html.FromHtml(html);
                #pragma warning restore CS0618
            }
            else
            {
                return Html.FromHtml(html, FromHtmlOptions.ModeLegacy);
            }
        }
    }
}