using System;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Utilities
{
    public static class DateTimeExtensions
    {
        public static WeekType GetWeekType(this DateTime date, DateTime upperWeekDate)
        {
            // Неприятность заключается в том, что у нас неделя начинается с понедельника,
            // а в DayOfWeek – с воскресенья. Из-за чего воскресенье считается не по той
            // неделе, к которой оно в действительности относится.
            static Int32 GetCorrectDayOfWeekIndex(DayOfWeek dayOfWeek)
            {
                return (dayOfWeek == DayOfWeek.Sunday) ? 6 : (Int32)(dayOfWeek - 1);
            }

            Int32 upperWeekDayIndex = GetCorrectDayOfWeekIndex(upperWeekDate.DayOfWeek);
            Int32 targetDayIndex = GetCorrectDayOfWeekIndex(date.DayOfWeek);

            Int32 numberOfDaysBetweenDates;
            if (targetDayIndex == upperWeekDayIndex)
            {
                numberOfDaysBetweenDates = (upperWeekDate - date).Days;
            }
            else
            {
                // Вычисляем день, который относится к той же неделе, что и date,
                // но имеет день недели, совпадающий с днем недели upperWeekDate.
                Int32 difference = upperWeekDayIndex - targetDayIndex;
                DateTime normalizedDate = date.AddDays(difference);

                numberOfDaysBetweenDates = (upperWeekDate - normalizedDate).Days;
            }

            Int32 numberOfWeeksBetweenDates = numberOfDaysBetweenDates / 7;
            return (numberOfWeeksBetweenDates % 2 == 0) ? WeekType.Upper : WeekType.Lower;
        }
    }
}