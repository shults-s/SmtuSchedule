using System;

namespace SmtuSchedule.Core.Utilities
{
    public class DateRange
    {
        public const Int32 DefaultHalfRangeInDays = 15;

        public Int32 TotalDaysNumber => (Int32)(_maximumDate - _minimumDate).TotalDays;

        public DateRange(DateTime date, Int32 halfRangeInDays = DefaultHalfRangeInDays)
        {
            Recompute(date, halfRangeInDays);
        }

        public void Recompute(DateTime date, Int32 halfRangeInDays = DefaultHalfRangeInDays)
        {
            _minimumDate = date.AddDays(-halfRangeInDays);
            _maximumDate = date.AddDays(halfRangeInDays);
        }

        public DateTime GetDateByIndex(Int32 index) => _minimumDate.AddDays(index);

        public Int32 GetIndexByDate(DateTime date) => (Int32)(date - _minimumDate).TotalDays;

        public Boolean IsDateInside(DateTime date) => (date >= _minimumDate) && (date <= _maximumDate);

        private DateTime _minimumDate;
        private DateTime _maximumDate;
    }
}