using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.OS;
using Android.App;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Android.Support.V7.App;
using Android.Views;
using Android.Views.InputMethods;

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
            static String[] SplitSearchRequest(String request)
            {
                request = Regex.Replace(request, @"\t|\r|\n", String.Empty);
                return request.Split(',').Select(r => r.Trim()).Where(r => r.Length != 0).ToArray();
            }

            _application = ApplicationContext as SmtuScheduleApplication;

            SetTheme(_application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.downloadActivity);

            _downloadRelatedSchedulesCheckBox = FindViewById<CheckBox>(
                Resource.Id.downloadRelatedSchedulesCheckBox
            );

            _progressBarLayout = FindViewById<RelativeLayout>(
                Resource.Id.downloadProgressBarRelativeLayout
            );

            _downloadLecturersErrorRetryButton = FindViewById<Button>(
                Resource.Id.downloadLecturersErrorRetryButton
            );

            _downloadLecturersErrorRetryButton.Click += (s, e) =>
            {
                DownloadLecturersNamesAsync();
                _progressBarLayout.Visibility = ViewStates.Visible;
            };

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.downloadActivityToolbar);
            toolbar.NavigationClick += (s, e) => OnBackPressed();
            toolbar.InflateMenu(Resource.Menu.downloadMenu);
            toolbar.MenuItemClick += (s, e) =>
            {
                if (!_searchRequestTextView.Enabled)
                {
                    return ;
                }

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

            _searchRequestTextView.SetTokenizer(new MultiAutoCompleteTextView.CommaTokenizer());

            IMenuItem downloadMenuItem = toolbar.Menu.GetItem(0);
            _searchRequestTextView.TextChanged += (s, e) =>
            {
                Boolean isUserInputValid = !String.IsNullOrWhiteSpace(_searchRequestTextView.Text);
                downloadMenuItem.SetEnabled(isUserInputValid);
            };

            DownloadLecturersNamesAsync();
        }

        private void ShowKeyboardForSearchRequestTextView()
        {
            if (_searchRequestTextView.RequestFocus())
            {
                Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

                InputMethodManager manager = (InputMethodManager)GetSystemService(InputMethodService);
                manager.ShowSoftInput(_searchRequestTextView, ShowFlags.Implicit);
            }
        }

        private async void DownloadLecturersNamesAsync()
        {
            TextView errorTextView = FindViewById<TextView>(Resource.Id.downloadLecturersErrorTextView);

            IEnumerable<String> lecturers = (await _application.Manager.DownloadLecturersMapAsync())?.Keys;

            _progressBarLayout.Visibility = ViewStates.Gone;

            if (lecturers == null)
            {
                _ = _application.SaveLogAsync();

                _searchRequestTextView.Enabled = false;
                _downloadRelatedSchedulesCheckBox.Visibility = ViewStates.Gone;

                errorTextView.Visibility = ViewStates.Visible;
                _downloadLecturersErrorRetryButton.Visibility = ViewStates.Visible;

                return ;
            }

            errorTextView.Visibility = ViewStates.Gone;
            _downloadLecturersErrorRetryButton.Visibility = ViewStates.Gone;

            _searchRequestTextView.Enabled = true;
            _downloadRelatedSchedulesCheckBox.Visibility = ViewStates.Visible;

            Int32 layoutId = Resource.Layout.support_simple_spinner_dropdown_item;
            _searchRequestTextView.Adapter = new ArrayAdapter<String>(this, layoutId, lecturers.ToArray());

            ShowKeyboardForSearchRequestTextView();
        }

        private SmtuScheduleApplication _application;

        private RelativeLayout _progressBarLayout;
        private Button _downloadLecturersErrorRetryButton;
        private CheckBox _downloadRelatedSchedulesCheckBox;
        private MultiAutoCompleteTextView _searchRequestTextView;
    }
}