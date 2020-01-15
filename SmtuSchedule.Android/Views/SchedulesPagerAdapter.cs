using System;
using System.Globalization;
using Android.Support.V4.App;
using SmtuSchedule.Core.Utilities;

namespace SmtuSchedule.Android.Views
{
    internal class SchedulesPagerAdapter : FragmentStatePagerAdapter
    {
        public override Int32 Count => RenderingDateRange.TotalDaysNumber;

        public DateRange RenderingDateRange { get; private set; }

        public SchedulesPagerAdapter(FragmentManager manager, DateTime medianDate) : base(manager)
        {
            RenderingDateRange = new DateRange(medianDate);
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(Int32 position)
        {
            DateTime date = RenderingDateRange.GetDateByIndex(position);
            return new Java.Lang.String(date.ToString("ddd\ndd.MM", _culture));
        }

        public override Fragment GetItem(Int32 position)
        {
            return new ScheduleFragment() { Date = RenderingDateRange.GetDateByIndex(position) };
        }

        private static readonly CultureInfo _culture = new CultureInfo("ru-RU");
    }
}