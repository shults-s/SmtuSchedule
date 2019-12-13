using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.OS;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Android.Support.V7.App;

using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace SmtuSchedule.Android.Views
{
    [Activity(ScreenOrientation = ScreenOrientation.Portrait)]
    internal class DownloadActivity : AppCompatActivity
    {
        public const String IntentShouldDownloadRelatedSchedulesKey = "shouldDownloadRelatedSchedules";

        public const String IntentSearchRequestsKey = "requests";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            String[] SplitSearchRequest(String request)
            {
                request = Regex.Replace(request, @"\t|\r|\n", String.Empty);
                return request.Split(',').Select(r => r.Trim()).Where(r => r != String.Empty).ToArray();
            }

            _application = ApplicationContext as ScheduleApplication;

            SetTheme(_application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.downloadActivity);

            _downloadRelatedSchedulesCheckBox = FindViewById<CheckBox>(
                Resource.Id.downloadRelatedSchedulesCheckBox
            );

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.downloadActivityToolbar);
            toolbar.NavigationClick += (s, e) => OnBackPressed();

            toolbar.InflateMenu(Resource.Menu.downloadMenu);
            toolbar.MenuItemClick += (s, e) =>
            {
                Intent intent = new Intent();

                intent.PutExtra(
                    IntentShouldDownloadRelatedSchedulesKey,
                    _downloadRelatedSchedulesCheckBox.Checked
                );

                intent.PutExtra(IntentSearchRequestsKey, SplitSearchRequest(_searchRequestTextView.Text));

                SetResult(Result.Ok, intent);
                Finish();
            };

            _searchRequestTextView = FindViewById<MultiAutoCompleteTextView>(
                Resource.Id.downloadMultiAutoCompleteTextView
            );

            if (_searchRequestTextView.RequestFocus())
            {
                Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);
            }

            _downloadMenuItem = toolbar.Menu.GetItem(0);
            _downloadMenuItem.SetEnabled(false);

            _searchRequestTextView.SetTokenizer(new MultiAutoCompleteTextView.CommaTokenizer());
            _searchRequestTextView.TextChanged += (s, e) =>
            {
                Boolean isUserInputValid = !String.IsNullOrWhiteSpace(_searchRequestTextView.Text);
                _downloadMenuItem.SetEnabled(isUserInputValid);
            };

            DownloadLecturersNamesAsync();
        }

        private async void DownloadLecturersNamesAsync()
        {
            IEnumerable<String> lecturers = await _application.Manager.DownloadLecturersNamesAsync();
            if (lecturers != null)
            {
                _ = _application.SaveLogAsync();

                _downloadRelatedSchedulesCheckBox.Visibility = ViewStates.Gone;
                _downloadRelatedSchedulesCheckBox.Checked = false;

                _downloadMenuItem.SetEnabled(false);
                _searchRequestTextView.Enabled = false;

                TextView error = FindViewById<TextView>(Resource.Id.downloadLecturersErrorTextView);
                error.Visibility = ViewStates.Visible;

                return ;
            }

            Int32 layoutId = Resource.Layout.support_simple_spinner_dropdown_item;
            _searchRequestTextView.Adapter = new ArrayAdapter<String>(this, layoutId, lecturers.ToArray());
        }

        private ScheduleApplication _application;

        private IMenuItem _downloadMenuItem;
        private CheckBox _downloadRelatedSchedulesCheckBox;
        private MultiAutoCompleteTextView _searchRequestTextView;
    }
}