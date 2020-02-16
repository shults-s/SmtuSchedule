using System;
using Android.OS;
using Android.App;
using Android.Content.PM;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Preferences;

namespace SmtuSchedule.Android.Views
{
    [Activity(ScreenOrientation = ScreenOrientation.Portrait)]
    internal class PreferencesActivity : AppCompatActivity
    {
        private class PreferencesFragment : PreferenceFragmentCompat
        {
            public override void OnCreatePreferences(Bundle savedInstanceState, String rootKey)
            {
                AddPreferencesFromResource(Resource.Xml.preferences);
            }

            public override void OnDisplayPreferenceDialog(Preference preference)
            {
                PreferenceDialogFragmentCompat fragment = null;

                if (preference is DatePreference)
                {
                    fragment = new DatePreference.DatePreferenceDialogFragment(preference.Key);
                }

                if (fragment == null)
                {
                    base.OnDisplayPreferenceDialog(preference);
                    return ;
                }

                fragment.SetTargetFragment(this, 0);
                fragment.Show(FragmentManager, "Shults.SmtuSchedule.PreferenceDialogFragment");
            }
        }

        public override void OnBackPressed()
        {
            SetResult(Result.Ok);
            Finish();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ScheduleApplication application = ApplicationContext as ScheduleApplication;
            application.Preferences.ThemeChanged += Recreate;

            SetTheme(application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            SetContentView(Resource.Layout.preferencesActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.preferencesActivityToolbar);
            toolbar.NavigationClick += (s, e) => OnBackPressed();

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.preferencesContentFrameLayout, new PreferencesFragment())
                .Commit();
        }
    }
}