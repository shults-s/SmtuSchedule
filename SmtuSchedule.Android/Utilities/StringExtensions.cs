using System;
using System.Text.RegularExpressions;
using Android.OS;
using Android.Text;

namespace SmtuSchedule.Android.Utilities
{
    internal static class StringExtensions
    {
        private static readonly (String, String)[] MarkdownRegexes = new (String, String)[]
        {
            // Именованная ссылка: [Название](URL).
            (@"\[(?<content>.+?)\]\((?<url>.+?)\)", "<a href=\"${url}\">${content}</a>"),

            // Жирное начертание: **Текст**.
            (@"\*\*(?<content>.+?)\*\*", "<b>${content}</b>"),

            // Верхний индекс: ^(Текст).
            (@"\^\((?<content>.+?)\)", "<sup>${content}</sup>"),
        };

        public static Java.Lang.ICharSequence FromMarkdown(this String markdown)
        {
            foreach((String pattern, String replacement) in MarkdownRegexes)
            {
                markdown = Regex.Replace(markdown, pattern, replacement);
            }

            return markdown.FromHtml();
        }

        public static Java.Lang.ICharSequence FromHtml(this String html)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.N)
            {
#pragma warning disable CS0618
                return Html.FromHtml(html.Replace("\n", "<br>"));
#pragma warning restore CS0618
            }
            else
            {
                return Html.FromHtml(html.Replace("\n", "<br>"), FromHtmlOptions.ModeCompact);
            }
        }
    }
}