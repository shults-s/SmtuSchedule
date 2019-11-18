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

            base.OnCreate(savedInstanceState);

            _application = ApplicationContext as ScheduleApplication;

            SetTheme(_application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            SetContentView(Resource.Layout.downloadActivity);

            _downloadRelatedSchedulesTextView = FindViewById<CheckedTextView>(
                Resource.Id.downloadRelatedSchedulesCheckedTextView
            );

            _downloadRelatedSchedulesTextView.Click += (s, e) =>
            {
                _downloadRelatedSchedulesTextView.Checked = !_downloadRelatedSchedulesTextView.Checked;
            };

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.downloadActivityToolbar);
            toolbar.NavigationClick += (s, e) => OnBackPressed();

            toolbar.InflateMenu(Resource.Menu.downloadMenu);
            toolbar.MenuItemClick += (s, e) =>
            {
                Intent intent = new Intent();

                intent.PutExtra(
                    IntentShouldDownloadRelatedSchedulesKey,
                    _downloadRelatedSchedulesTextView.Checked
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

            IMenuItem downloadMenuItem = toolbar.Menu.GetItem(0);
            downloadMenuItem.SetEnabled(false);

            _searchRequestTextView.SetTokenizer(new MultiAutoCompleteTextView.CommaTokenizer());
            _searchRequestTextView.TextChanged += (s, e) =>
            {
                Boolean isUserInputValid = !String.IsNullOrWhiteSpace(_searchRequestTextView.Text);
                downloadMenuItem.SetEnabled(isUserInputValid);
            };

            DownloadLecturersNamesAsync();
        }

        private async void DownloadLecturersNamesAsync()
        {
            IEnumerable<String> lecturers = await _application.Manager.DownloadLecturersNamesAsync();
            if (lecturers == null)
            {
                _application.SaveLog();

                _downloadRelatedSchedulesTextView.Visibility = ViewStates.Gone;
                _downloadRelatedSchedulesTextView.Checked = false;

                TextView error = FindViewById<TextView>(Resource.Id.downloadLecturersErrorTextView);
                error.Visibility = ViewStates.Visible;

                return ;
            }

            Int32 layoutId = Resource.Layout.support_simple_spinner_dropdown_item;
            _searchRequestTextView.Adapter = new ArrayAdapter<String>(this, layoutId, lecturers.ToArray());
        }

        private ScheduleApplication _application;
        private MultiAutoCompleteTextView _searchRequestTextView;
        private CheckedTextView _downloadRelatedSchedulesTextView;
    }
}