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

        public SchedulesPagerAdapter(FragmentManager manager, DateTime initialDate) : base(manager)
        {
            RenderingDateRange = new DateRange(initialDate);
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(Int32 position)
        {
            DateTime date = RenderingDateRange.GetDateByIndex(position);
            return new Java.Lang.String(date.ToString("ddd\ndd.MM", _culture));
        }

        public override Fragment GetItem(Int32 position)
        {
            DateTime date = RenderingDateRange.GetDateByIndex(position);

            ScheduleFragment fragment = new ScheduleFragment() { Date = date };
            return fragment;
        }

        private static readonly CultureInfo _culture = new CultureInfo("ru-RU");
    }
}