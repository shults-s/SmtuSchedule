using System;
using Java.Lang;
using Android.OS;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;

namespace SmtuSchedule.Android.Utilities
{
    internal static class ICharSequenceExtension
    {
        private class UrlSpanNoUnderlines : URLSpan
        {
            public UrlSpanNoUnderlines(System.String url) : base(url)
            {
            }

            public override void UpdateDrawState(TextPaint drawState)
            {
                base.UpdateDrawState(drawState);
                drawState.UnderlineText = false;
            }
        }

        public static ICharSequence StripUrlUnderlines(this ICharSequence formattedText)
        {
            SpannableStringBuilder builder = new SpannableStringBuilder(formattedText);

            Java.Lang.Object[] spans = builder.GetSpans(
                0,
                builder.Length(),
                Class.FromType(typeof(URLSpan))
            );

            foreach (URLSpan span in spans)
            {
                Int32 start = builder.GetSpanStart(span);
                Int32 end = builder.GetSpanEnd(span);
                builder.RemoveSpan(span);

                UrlSpanNoUnderlines customSpan = new UrlSpanNoUnderlines(span.URL);
                builder.SetSpan(customSpan, start, end, 0);
            }

            return builder;
        }

        public static ICharSequence Trim(this ICharSequence formattedText)
        {
            System.String text = formattedText.ToString();

            Int32 start;
            for(start = 0; start < text.Length; start++)
            {
                if (!Char.IsWhiteSpace(text[start]))
                {
                    break;
                }
            }

            Int32 end;
            for (end = text.Length - 1; end >= 0; end--)
            {
                if (!Char.IsWhiteSpace(text[end]))
                {
                    break;
                }
            }

            return formattedText.SubSequenceFormatted(start, end + 1);
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

        public static ICharSequence ToColored(this System.String text, Color color)
        {
            ForegroundColorSpan span = new ForegroundColorSpan(color);

            SpannableString spannable = new SpannableString(text);
            spannable.SetSpan(span, 0, spannable.Length(), SpanTypes.ExclusiveExclusive);

            return spannable;
        }
    }
}