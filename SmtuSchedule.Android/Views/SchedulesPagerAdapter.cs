using System;
using System.Globalization;
using Java.Lang;
using Android.Support.V4.App;
using SmtuSchedule.Core.Utilities;

namespace SmtuSchedule.Android.Views
{
    internal class SchedulesPagerAdapter : FragmentStatePagerAdapter
    {
        private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

        public Func<DateTime, Fragment> PageFactory { get; set; }

        public override Int32 Count => Helper.TotalDaysNumber;

        public DateTimeHelper Helper { get; set; }

        public SchedulesPagerAdapter(FragmentManager manager) : base(manager)
        {
        }

        public override Fragment GetItem(Int32 position)
        {
            return PageFactory(Helper.GetDateByIndex(position));
        }

        public override ICharSequence GetPageTitleFormatted(Int32 position)
        {
            DateTime date = Helper.GetDateByIndex(position);
            return new Java.Lang.String(date.ToString("ddd\ndd.MM", Culture));
        }
    }
}