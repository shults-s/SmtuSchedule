using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Android.Util;
using Android.Runtime;
using Android.Content;
using Android.Content.Res;
using Android.Support.V14.Preferences;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Android.Utilities;

namespace SmtuSchedule.Android.Views
{
    [Register("shults.smtuschedule.android.views.CustomMultiSelectListPreference")]
    class CustomMultiSelectListPreference : MultiSelectListPreference
    {
        private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

        public CustomMultiSelectListPreference(Context context, IAttributeSet attributes) : base(
            context,
            attributes,
            Resource.Attribute.dialogPreferenceStyle,
            Resource.Attribute.dialogPreferenceStyle
        )
        {
            PreferenceChange += (s, e) =>
            {
                SetSummaryFromValues((e.NewValue as JavaSet).ToIEnumerable<String>());
            };

            TypedArray styledAttributes = context.ObtainStyledAttributes(
                attributes,
                Resource.Styleable.CustomMultiSelectListPreference
            );

            _summaryWhenNothingIsSelected = styledAttributes.GetString(
                Resource.Styleable.CustomMultiSelectListPreference_summaryNothingSelected);
        }

        public void SetSummaryFromValues(IEnumerable<String> values)
        {
            if (values == null || values.Count() == 0)
            {
                Summary = _summaryWhenNothingIsSelected;
                return ;
            }

            String[] entries = GetEntries();
            IEnumerable<String> selectedEntries = values.OrderBy(v => FindIndexOfValue(v))
                .Select(v => entries[FindIndexOfValue(v)]);

            Summary = String.Join(", ", selectedEntries).ToSentenceCase(Culture);
        }

        protected override void OnSetInitialValue(Java.Lang.Object defaultValue)
        {
            base.OnSetInitialValue(defaultValue);
            SetSummaryFromValues((defaultValue as ICollection<String>) ?? GetPersistedStringSet(null));
        }

        private readonly String _summaryWhenNothingIsSelected;
    }
}