using System;
using System.Globalization;

namespace SmtuSchedule.Core.Utilities
{
    public static class StringExtensions
    {
        public static String ToSentenceCase(this String @string, CultureInfo culture)
        {
            Char[] chars = @string.ToLower(culture).ToCharArray();
            chars[0] = Char.ToUpper(chars[0], culture);
            return new String(chars);
        }
    }
}