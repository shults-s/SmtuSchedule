using System;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Runtime;
using Android.Content;
using Android.Content.Res;
using Android.Support.V7.Preferences;

using DatePicker = Android.Widget.DatePicker;

namespace SmtuSchedule.Android.Views
{
    // Объявление компонента DatePreference в XML не должно содержать атрибут android:defaultValue, иначе
    // выбрасывается исключение Unable to activate instance of type ... from native handle ...
    [Register("shults.smtuschedule.android.views.DatePreference")]
    internal class DatePreference : DialogPreference
    {
        internal class DatePreferenceDialogFragment : PreferenceDialogFragmentCompat
        {
            public static DatePreferenceDialogFragment Create(String preferenceKey)
            {
                DatePreferenceDialogFragment fragment = new DatePreferenceDialogFragment()
                {
                    Arguments = new Bundle()
                };

                fragment.Arguments.PutString(ArgKey, preferenceKey);
                return fragment;
            }

            public override void OnDialogClosed(Boolean positiveResult)
            {
                if (positiveResult)
                {
                    (Preference as DatePreference).SetDate(_picker.DateTime);
                }
            }

            protected override void OnBindDialogView(View view)
            {
                base.OnBindDialogView(view);

                DatePreference preference = Preference as DatePreference;

                DateTime initialDate = (preference.Date == default(DateTime))
                    ? DateTime.Today
                    : preference.Date;

                _picker = view.FindViewById<DatePicker>(Resource.Id.dialogDatePicker);
                _picker.DateTime = initialDate;
            }

            private DatePicker _picker;
        }

        public override Int32 DialogLayoutResource => Resource.Layout.dialogDatePicker;

        public DateTime Date { get; private set; }

        public DatePreference(Context context, IAttributeSet attributes) : base(context, attributes,
            Resource.Attribute.dialogPreferenceStyle, Resource.Attribute.dialogPreferenceStyle)
        {
            DialogTitle = null;
        }

        public void SetDate(DateTime date)
        {
            Date = date;
            PersistLong(date.Ticks);
            Summary = date.ToString("dd.MM.yyyy");
        }

        protected override Java.Lang.Object OnGetDefaultValue(TypedArray array, Int32 index)
        {
            return array.GetInt(index, 0);
        }

        protected override void OnSetInitialValue(Java.Lang.Object defaultValue)
        {
            Int64 ticks = (defaultValue == null) ? GetPersistedLong(Date.Ticks) : (Int64)defaultValue;
            SetDate(new DateTime(ticks));
        }
    }
}