using System;

namespace SmtuSchedule.Core.Utilities
{
    public sealed class DateRange
    {
        public const Int32 DefaultHalfRangeInDays = 15;

        public Int32 TotalDaysNumber => (Int32)(_maximumDate - _minimumDate).TotalDays + 1;

        public DateRange(DateTime medianDate, Int32 halfRangeInDays = DefaultHalfRangeInDays)
        {
            Recompute(medianDate, halfRangeInDays);
        }

        public void Recompute(DateTime medianDate, Int32 halfRangeInDays = DefaultHalfRangeInDays)
        {
            if (halfRangeInDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(halfRangeInDays), "Number must be positive.");
            }

            _minimumDate = medianDate.Date.AddDays(-halfRangeInDays);
            _maximumDate = medianDate.Date.AddDays(halfRangeInDays);
        }

        public DateTime GetDateByIndex(Int32 index)
        {
            if (index < 0 || index >= TotalDaysNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), $"Number must be inside the range [{index} ... {TotalDaysNumber - 1}].");
            }

            return _minimumDate.AddDays(index);
        }

        public Int32 GetIndexByDate(DateTime date)
        {
            if (!IsDateInsideRange(date))
            {
                String minimumDate = _minimumDate.ToString("dd.MM.yyyy");
                String maximumDate = _maximumDate.ToString("dd.MM.yyyy");
                throw new ArgumentOutOfRangeException(
                    nameof(date), $"Date must be inside the range [{minimumDate} ... {maximumDate}].");
            }

            return (Int32)(date - _minimumDate).TotalDays;
        }

        public Boolean IsDateInsideRange(DateTime date) => (date >= _minimumDate) && (date <= _maximumDate);

        private DateTime _minimumDate;
        private DateTime _maximumDate;
    }
}