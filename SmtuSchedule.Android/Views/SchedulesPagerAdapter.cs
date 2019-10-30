using System;
using System.Globalization;
//using System.Collections.Generic;
using Android.Support.V4.App;
using SmtuSchedule.Core.Utilities;

namespace SmtuSchedule.Android.Views
{
    internal class SchedulesPagerAdapter : FragmentStatePagerAdapter
    {
        public override Int32 Count => RenderingDateRange.TotalDaysNumber;

        public DateRange RenderingDateRange { get; private set; }

        //static SchedulesPagerAdapter()
        //{
        //    _cachedFragments = new Dictionary<(DateTime, Int32), ScheduleFragment>();
        //}

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

            //if (_cachedFragments.TryGetValue((date, scheduleId), out ScheduleFragment fragment))
            //{
            //    return fragment;
            //}

            ScheduleFragment fragment = new ScheduleFragment() { Date = date };
            //_cachedFragments[(date, scheduleId)] = fragment;

            return fragment;
        }

        //public override void NotifyDataSetChanged()
        //{
        //    _currentDayPosition = RenderingDateRange.GetIndexByDate(DateTime.Today);
        //    base.NotifyDataSetChanged();
        //}

        //public override Int32 GetItemPosition(Java.Lang.Object @object)
        //{
        //    Int32 position = base.GetItemPosition(@object);
        //    return (position != _currentDayPosition) ? PositionUnchanged : PositionNone;
        //}

        //private Int32 _currentDayPosition;

        private static readonly CultureInfo _culture = new CultureInfo("ru-RU");

        // (date, scheduleId) => fragment
        //private static readonly Dictionary<(DateTime, Int32), ScheduleFragment> _cachedFragments;
    }
}