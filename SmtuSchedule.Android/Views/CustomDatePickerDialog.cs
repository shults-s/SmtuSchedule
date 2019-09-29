using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;

namespace SmtuSchedule.Android.Views
{
    internal class CustomDatePickerDialog : CustomAlertDialog
    {
        public event Action<DateTime> DateChanged;

        private class DateChangeListener : Java.Lang.Object, DatePicker.IOnDateChangedListener
        {
            public event Action<DateTime> DateChanged;

            public DateChangeListener(DateTime initialDate) => _last = initialDate;

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

        public CustomDatePickerDialog(Context context, DateTime initialDate) : base(context)
        {
            DateChangeListener listener = new DateChangeListener(initialDate);
            listener.DateChanged += (selectedDate) =>
            {
                Dismiss();
                DateChanged?.Invoke(selectedDate);
            };

            // В View.Inflate(...) передается Dialog.Context, к которому уже (!) применена тема.
            View pickerView = View.Inflate(Context, Resource.Layout.customDatePicker, null);
            SetView(pickerView);

            DatePicker picker = pickerView.FindViewById<DatePicker>(Resource.Id.customDatePicker);
            picker.Init(initialDate.Year, initialDate.Month - 1, initialDate.Day, listener);

            // Из-за бага в Android 5.0 событие изменения даты не срабатывает, если DatePicker
            // отображается в режиме календаря, поэтому без кнопок здесь не обойтись.
            if (Build.VERSION.SdkInt < BuildVersionCodes.LollipopMr1)
            {
                SetNegativeButton(global::Android.Resource.String.Cancel, () => Dismiss());

                SetPositiveButton(
                    global::Android.Resource.String.Ok,
                    () => listener.OnDateChanged(picker, picker.Year, picker.Month, picker.DayOfMonth)
                );
            }
        }
    }
}