using System;

namespace SmtuSchedule.Core.Utilities
{
    public class DateTimeHelper
    {
        public Int32 TotalDaysNumber
        {
            get => (Int32)(_maximumDate - _minimumDate).TotalDays;
        }

        public DateTimeHelper(DateTime date, Int32 rangeInDays)
        {
            _minimumDate = date.AddDays(-rangeInDays);
            _maximumDate = date.AddDays(rangeInDays);
        }

        public DateTime GetDateByIndex(Int32 index)
        {
            return _minimumDate.AddDays(index);
        }

        public Int32 GetIndexByDate(DateTime date)
        {
            return (Int32)(date - _minimumDate).TotalDays;
        }

        public Boolean IsDateInsideRange(DateTime date)
        {
            return date >= _minimumDate && date <= _maximumDate;
        }

        private readonly DateTime _minimumDate;
        private readonly DateTime _maximumDate;
    }
}