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
    public class DownloadActivity : AppCompatActivity
    {
        public const String IntentSearchRequestsKey = "requests";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            String[] SplitSearchRequest(String request)
            {
                request = Regex.Replace(request, @"\t|\r|\n", String.Empty);
                return request.Split(',').Select(r => r.Trim()).Where(r => r != String.Empty).ToArray();
            }

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.downloadActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.downloadToolbar);
            toolbar.NavigationClick += (s, e) => OnBackPressed();

            toolbar.InflateMenu(Resource.Menu.downloadMenu);
            toolbar.MenuItemClick += (s, e) =>
            {
                Intent intent = new Intent();
                intent.PutExtra(IntentSearchRequestsKey, SplitSearchRequest(_downloadRequest.Text));

                SetResult(Result.Ok, intent);
                Finish();
            };

            _downloadRequest = FindViewById<MultiAutoCompleteTextView>(
                Resource.Id.downloadMultiAutoCompleteTextView
            );

            if (_downloadRequest.RequestFocus())
            {
                Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);
            }

            IMenuItem downloadMenuItem = toolbar.Menu.GetItem(0);
            downloadMenuItem.SetEnabled(false);

            _downloadRequest.SetTokenizer(new MultiAutoCompleteTextView.CommaTokenizer());
            _downloadRequest.TextChanged += (s, e) =>
            {
                Boolean isUserInputValid = !String.IsNullOrWhiteSpace(_downloadRequest.Text);
                downloadMenuItem.SetEnabled(isUserInputValid);
            };

            DownloadLecturersAsync();
        }

        private async void DownloadLecturersAsync()
        {
            ScheduleApplication application = ApplicationContext as ScheduleApplication;

            IEnumerable<String> lecturers = await application.Manager.DownloadLecturersAsync();

            if (lecturers == null)
            {
                application.SaveLog();

                TextView error = FindViewById<TextView>(Resource.Id.downloadLecturersErrorTextView);
                error.Visibility = ViewStates.Visible;
            }
            else
            {
                Int32 layoutId = Resource.Layout.support_simple_spinner_dropdown_item;
                _downloadRequest.Adapter = new ArrayAdapter<String>(this, layoutId, lecturers.ToArray());
            }
        }

        private MultiAutoCompleteTextView _downloadRequest;
    }
}