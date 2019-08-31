using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;

namespace SmtuSchedule.Android.Views
{
    internal class CustomDatePickerDialog : Dialog
    {
        private class DateChangingListener : Java.Lang.Object, DatePicker.IOnDateChangedListener
        {
            public event Action<DateTime> DateChanged;

            public DateChangingListener(DateTime initialDate) => _last = initialDate;

            public void OnDateChanged(DatePicker view, Int32 year, Int32 month, Int32 day)
            {
                DateTime selected = new DateTime(year, month + 1, day);

                if (!IsOnlyYearChanged(selected))
                {
                    DateChanged?.Invoke(selected);
                }

                _last = selected;
            }

            private Boolean IsOnlyYearChanged(DateTime selected)
            {
                return selected.Year != _last.Year
                    && selected.Day == _last.Day && selected.Month == _last.Month;
            }

            private DateTime _last;
        }

        public CustomDatePickerDialog(Context context, DateTime initialDate,
            Action<DateTime> dateChangedCallback) : base(context)
        {
            DateChangingListener listener = new DateChangingListener(initialDate);
            listener.DateChanged += (selectedDate) =>
            {
                Dismiss();
                dateChangedCallback(selectedDate);
            };

            // В View.Inflate(...) передается Dialog.Context, к которому уже (!) применена тема.
            View pickerView = View.Inflate(Context, Resource.Layout.customDatePicker, null);
            SetContentView(pickerView);

            DatePicker picker = pickerView.FindViewById<DatePicker>(Resource.Id.customDatePicker);
            picker.Init(initialDate.Year, initialDate.Month - 1, initialDate.Day, listener);
        }
    }
}