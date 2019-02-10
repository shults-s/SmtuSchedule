using System;
using Java.Lang;
using Android.OS;
using Android.Text;

namespace SmtuSchedule.Android.Utilities
{
    internal static class ICharSequenceExtension
    {
        public static ICharSequence Trim(this ICharSequence sequence)
        {
            System.String charSequence = sequence.ToString();

            Int32 start;
            for(start = 0; start < charSequence.Length; start++)
            {
                if (!Char.IsWhiteSpace(charSequence[start]))
                {
                    break;
                }
            }

            Int32 end;
            for (end = charSequence.Length - 1; end >= 0; end--)
            {
                if (!Char.IsWhiteSpace(charSequence[end]))
                {
                    break;
                }
            }

            return sequence.SubSequenceFormatted(start, end + 1);
        }

        public static ICharSequence FromHtml(this System.String html)
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