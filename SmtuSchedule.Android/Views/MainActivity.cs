using System;
using System.Linq;
using System.Timers;
using System.Collections.Generic;
using Android;
using Android.OS;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V4.View;
using Android.Support.Design.Widget;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Android.Interfaces;

using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace SmtuSchedule.Android.Views
{
    [Activity(MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity, ISchedulesViewer
    {
        private enum MainActivityState { NotInitialized, Initialized, DisplaysMessage, DisplaysSchedule }

        private const Int32 ExternalStoragePermissionsRequestCode = 30;
        private const Int32 InternetPermissionRequestCode = 31;

        private const Int32 StartPreferencesActivityRequestCode = 33;
        private const Int32 StartDownloadActivityRequestCode = 35;

        private static readonly String[] StoragePermissions = new String[]
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage
        };

        public override Boolean OnPrepareOptionsMenu(IMenu menu)
        {
            // Если этот метод вызван до инициализации приложения или до окончания считывания расписаний.
            if (_activityState == MainActivityState.NotInitialized)
            {
                return true;
            }

            IMenuItem selectScheduleDateMenuItem = menu.FindItem(Resource.Id.selectScheduleDateMenuItem);
            IMenuItem downloadScheduleMenuItem = menu.FindItem(Resource.Id.downloadSchedulesMenuItem);

            Boolean isToolbarDateSelectorVisible = !_application.Preferences.UseFabDateSelector
                && _application.Manager.Schedules.Count != 0;

            selectScheduleDateMenuItem.SetVisible(isToolbarDateSelectorVisible);

            downloadScheduleMenuItem.SetVisible(true);
            downloadScheduleMenuItem.SetShowAsAction(isToolbarDateSelectorVisible ? ShowAsAction.Never
                : ShowAsAction.Always);

            //IMenuItem removeScheduleMenuItem = menu.FindItem(Resource.Id.removeCurrentScheduleMenuItem);
            //removeScheduleMenuItem.SetVisible(_application.Manager.Schedules.Count != 0);

            return true;
        }

        public override Boolean OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.mainMenu, menu);
            return true;
        }

        public override Boolean OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                //case Resource.Id.removeCurrentScheduleMenuItem:
                //    ShowCurrentScheduleRemovingDialog();
                //    break;

                case Resource.Id.selectScheduleDateMenuItem:
                    ShowCustomDatePickerDialog();
                    break;

                case Resource.Id.downloadSchedulesMenuItem:
                    StartDownloadActivity();
                    break;

                case Resource.Id.openPreferencesMenuItem:
                    OpenPreferences();
                    break;

                case Resource.Id.aboutApplicationMenuItem:
                    using (CustomAlertDialog dialog = new CustomAlertDialog(this))
                    {
                        dialog.SetTitle(Resource.String.aboutApplicationTitle)
                            .SetMessage(Resource.String.aboutApplicationMessage)
                            .SetPositiveButton(Resource.String.thanksActionText)
                            .Show();
                    }
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(Int32 requestCode, String[] permissions,
            Permission[] grantResults)
        {
            if (requestCode == InternetPermissionRequestCode)
            {
                if (grantResults.Length != 1 || grantResults[0] != Permission.Granted)
                {
                    ShowSnackbar(
                        Resource.String.internetPermissionRationaleMessage,
                        Resource.String.grantAccessActionText,
                        () => RequestPermissions(InternetPermissionRequestCode, permissions)
                    );
                }
            }
            else if (requestCode == ExternalStoragePermissionsRequestCode)
            {
                if (grantResults.Length != 0
                    && Array.TrueForAll(grantResults, e => e == Permission.Granted))
                {
                    ContinueActivityInitializationAsync();
                }
                else
                {
                    ShowLayoutMessage(Resource.String.welcomeMessage);

                    String[] deniedPermissions = permissions.Where(p => IsPermissionDenied(p)).ToArray();

                    ShowSnackbar(
                        Resource.String.storagePermissionsRationaleMessage,
                        Resource.String.grantAccessActionText,
                        () => RequestPermissions(ExternalStoragePermissionsRequestCode, deniedPermissions)
                    );
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        public override void OnBackPressed()
        {
            if (_application.Preferences.CurrentScheduleDate == DateTime.Today)
            {
                base.OnBackPressed();
            }
            else
            {
                ViewPagerMoveToDate(DateTime.Today, false);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            _activityState = MainActivityState.NotInitialized;

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.mainActivity);

            _content = FindViewById<RelativeLayout>(Resource.Id.mainContentRelativeLayout);

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.mainToolbar));
            SupportActionBar.SetDisplayShowTitleEnabled(false);

            _application = ApplicationContext as ScheduleApplication;

            String[] deniedPermissions = StoragePermissions.Where(p => IsPermissionDenied(p))
                .ToArray();

            if (deniedPermissions.Length == 0)
            {
                ContinueActivityInitializationAsync();
            }
            else
            {
                RequestPermissions(ExternalStoragePermissionsRequestCode, deniedPermissions);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_activityState == MainActivityState.DisplaysSchedule)
            {
                _currentSubjectHighlightTimer?.Start();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (_activityState == MainActivityState.DisplaysSchedule)
            {
                _currentSubjectHighlightTimer?.Stop();
            }
        }

        protected override void OnActivityResult(Int32 requestCode, Result resultCode, Intent data)
        {
            if (requestCode == StartPreferencesActivityRequestCode && resultCode == Result.Ok)
            {
                _application.Preferences.Read();
                RestartSchedulesRenderingSubsystem();
            }
            else if (requestCode == StartDownloadActivityRequestCode && resultCode == Result.Ok)
            {
                String[] requests = data.GetStringArrayExtra(DownloadActivity.IntentSearchRequestsKey);
                DownloadSchedulesAsync(requests);
            }
        }

        private async void ContinueActivityInitializationAsync()
        {
            if (!_application.IsInitialized)
            {
                if (!_application.Initialize())
                {
                    ShowSnackbar(Resource.String.applicationInitializationErrorMessage);
                    return ;
                }

                ShowProgressBar();

                Boolean haveReadingErrors = await _application.Manager.ReadSchedulesAsync();
                if (haveReadingErrors)
                {
                    ShowSnackbar(Resource.String.schedulesReadingErrorMessage);
                    _application.SaveLog();
                }
            }

            _application.Preferences.Read();

            String current = _application.GetVersion();
            if (_application.Preferences.LastSeenUpdateVersion == null)
            {
                _application.Preferences.SetLastSeenUpdateVersion(current);
            }

            if (_application.Preferences.CheckUpdatesOnStart)
            {
                CheckForUpdatesAsync();
            }

            if (_application.Preferences.LastSeenWelcomeVersion != current)
            {
                using (CustomAlertDialog dialog = new CustomAlertDialog(this))
                {
                    dialog.SetTitle(Resource.String.introductionTitle)
                        .SetMessage(Resource.String.introductionMessage)
                        .SetPositiveButton(
                            Resource.String.gotItActionText,
                            () => _application.Preferences.SetLastSeenWelcomeVersion(current)
                        );

                    dialog.Show();
                }
            }

            MigrateSchedulesAsync();

            _toolbarTitle = FindViewById<TextView>(Resource.Id.mainToolbarTitleTextView);
            _schedulesMenu = new PopupMenu(this, _toolbarTitle);
            _schedulesMenu.MenuItemClick += (s, e) => ShowSchedule(e.Item.ItemId);
            _toolbarTitle.Click += (s, e) => _schedulesMenu.Show();
            _toolbarTitle.LongClick += (s, e) => ShowCurrentScheduleRemovingDialog();

            _tabLayout = FindViewById<TabLayout>(Resource.Id.mainTabLayout);

            void UpdateSubjectsHighlighting()
            {
                // Перерисовка фрагмента посредством программного перемещения к текущей дате.
                if (_application.Preferences.CurrentScheduleDate == DateTime.Today)
                {
                    RunOnUiThread(() => ViewPagerMoveToDate(DateTime.Today));
                }
            }

            const Int32 UpdatingIntervalInMilliseconds = 60000;
            _currentSubjectHighlightTimer = new Timer(UpdatingIntervalInMilliseconds);
            _currentSubjectHighlightTimer.Elapsed += (s, e) => UpdateSubjectsHighlighting();

            _fab = FindViewById<FloatingActionButton>(Resource.Id.mainSelectScheduleDateFab);
            _fab.Click += (s, e) => ShowCustomDatePickerDialog();

            _activityState = MainActivityState.Initialized;

            RestartSchedulesRenderingSubsystem();
        }

        private async void MigrateSchedulesAsync()
        {
            if (_application.Preferences.LastMigrationVersion == _application.GetVersion())
            {
                return ;
            }

            Boolean haveMigrationErrors = await _application.Manager.MigrateSchedulesAsync();

            Int32 messageId;
            if (haveMigrationErrors)
            {
                messageId = Resource.String.schedulesMigrationErrorMessage;

                _application.SaveLog();
            }
            else
            {
                messageId = Resource.String.schedulesMigratedSuccessfullyMessage;
                _application.Preferences.SetLastMigrationVersion(_application.GetVersion());
            }

            ShowSnackbar(messageId);
        }

        private async void CheckForUpdatesAsync()
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            String latest = await ApplicationHelper.GetLatestVersionAsync();
            if (latest == null || latest == _application.Preferences.LastSeenUpdateVersion)
            {
                return ;
            }

            if (ApplicationHelper.CompareVersions(latest, _application.GetVersion()) > 0)
            {
                using (CustomAlertDialog dialog = new CustomAlertDialog(this))
                {
                    dialog.SetTitle(Resource.String.updateApplicationTitle)
                        .SetMessage(Resource.String.applicationUpdateAvailableMessage)
                        .SetPositiveButton(
                            Resource.String.gotItActionText,
                            () => _application.Preferences.SetLastSeenUpdateVersion(latest)
                        )
                        .SetNegativeButton(
                            Resource.String.updateActionText,
                            () =>
                            {
                                String url = ApplicationHelper.GooglePlayUrl;
                                StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));
                            }
                        );

                    dialog.Show();
                }
            }
        }

        private void RestartSchedulesRenderingSubsystem()
        {
            if (_application.Preferences.UpperWeekDate == default(DateTime))
            {
                using (CustomAlertDialog dialog = new CustomAlertDialog(this))
                {
                    dialog.SetPositiveButton(Resource.String.configureActionText, OpenPreferences)
                        .SetMessage(Resource.String.configureApplicationMessage)
                        .SetCancelable(false)
                        .Show();
                }
            }

            IReadOnlyDictionary<Int32, Schedule> schedules = _application.Manager.Schedules;

            InvalidateOptionsMenu();

            if (schedules.Count == 0)
            {
                _toolbarTitle.SetText(Resource.String.welcomeToolbarTitle);
                _tabLayout.Visibility = ViewStates.Gone;
                _fab.Visibility = ViewStates.Gone;

                _currentSubjectHighlightTimer.Stop();

                ShowLayoutMessage(Resource.String.welcomeMessage);
                _activityState = MainActivityState.DisplaysMessage;
                return ;
            }

            SetSchedulesMenu(schedules);

            // Пересоздавать адаптер необходимо при каждом перезапуске MainActivity, иначе
            // на активной в момент перезапуска вкладке не отрисуется фрагмент.
            _pagerAdapter = new SchedulesPagerAdapter(
                SupportFragmentManager,
                _application.Preferences.CurrentScheduleDate
            );

            ShowViewPager();
            _viewPager = FindViewById<ViewPager>(Resource.Id.scheduleViewPager);
            _viewPager.OffscreenPageLimit = 1;

            // Bug: если после запуска приложения перелистнуть страницу расписания влево или вправо, то
            // затем при изменении ориентации устройства событие по неизвестной причине срабатывает
            // дважды, причем оба раза с разными значениями позиции. В результате, при каждом повороте
            // экрана дата увеличивается/уменьшается (в зависимости от того, куда изначально свайпнуть),
            // но таблица с расписанием не обновляется.
            _viewPager.PageSelected += (s, e) =>
            {
                DateTime date = _pagerAdapter.RenderingDateRange.GetDateByIndex(e.Position);
                _application.Preferences.CurrentScheduleDate = date;
            };

            _tabLayout.Visibility = ViewStates.Visible;
            _tabLayout.SetupWithViewPager(_viewPager);

            Int32 SelectScheduleIdToDisplay()
            {
                Int32 currentId = _application.Preferences.CurrentScheduleId;
                if (currentId == 0)
                {
                    return schedules.Keys.First();
                }

                return schedules.ContainsKey(currentId) ? currentId : schedules.Keys.First();
            }

            Int32 scheduleId = SelectScheduleIdToDisplay();
            ShowSchedule(scheduleId);

            _fab.Visibility = _application.Preferences.UseFabDateSelector ? ViewStates.Visible
                : ViewStates.Gone;

            _currentSubjectHighlightTimer.Start();
        }

        private void RequestPermissions(Int32 requestCode, params String[] permissions)
        {
            ActivityCompat.RequestPermissions(this, permissions, requestCode);
        }

        private Boolean IsPermissionDenied(String permission)
        {
            return ActivityCompat.CheckSelfPermission(this, permission) != Permission.Granted;
        }

        private void OpenPreferences()
        {
            StartActivityForResult(typeof(PreferencesActivity), StartPreferencesActivityRequestCode);
        }

        private void StartDownloadActivity()
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            //if (!ApplicationHelper.IsUniversitySiteConnectionAvailable(out String failReason))
            //{
            //    ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
            //    _application.Logger.Log(failReason);
            //    _application.SaveLog();
            //    return ;
            //}

            if (_application.Manager.IsDownloadingInProgress)
            {
                ShowSnackbar(Resource.String.waitUntilSchedulesFinishDownloading);
                return ;
            }

            StartActivityForResult(typeof(DownloadActivity), StartDownloadActivityRequestCode);
        }

        private void ShowCurrentScheduleRemovingDialog()
        {
            Int32 scheduleId = _application.Preferences.CurrentScheduleId;

            if (!_application.Manager.Schedules.TryGetValue(scheduleId, out Schedule schedule))
            {
                return ;
            }

            String displayedName = schedule.DisplayedName;

            String message = Resources.GetString(
                Resource.String.removeCurrentScheduleMessage,
                displayedName
            );

            using (CustomAlertDialog dialog = new CustomAlertDialog(this))
            {
                dialog.SetMessage(message)
                    .SetPositiveButton(Resource.String.removeActionText, RemoveCurrentScheduleAsync)
                    .SetNegativeButton(Resource.String.cancelActionText)
                    .Show();
            }
        }

        private void ShowCustomDatePickerDialog()
        {
            DateTime initialDate = _application.Preferences.CurrentScheduleDate;

            using (CustomDatePickerDialog dialog = new CustomDatePickerDialog(this, initialDate))
            {
                dialog.DateChanged += (date) => ViewPagerMoveToDate(date);
                dialog.Show();
            }
        }

        private void ShowSnackbar(Int32 messageId, Int32 actionId = 0, Action callback = null)
        {
            Snackbar snackbar = Snackbar.Make(_content, messageId, Snackbar.LengthLong);

            if (actionId != 0)
            {
                snackbar.SetAction(actionId, (v) => callback());
            }

            TextView message = snackbar.View.FindViewById<TextView>(Resource.Id.snackbar_text);
            message.TextSize = 16;
            message.SetMaxLines(3);

            snackbar.Show(); // RunOnUiThread(snackbar.Show);
        }

        private void ShowProgressBar()
        {
            _content.RemoveAllViews();
            View.Inflate(this, Resource.Layout.progress, _content);
        }

        private void ShowViewPager()
        {
            _content.RemoveAllViews();
            View.Inflate(this, Resource.Layout.pager, _content);
        }

        private void ShowLayoutMessage(Int32 messageId)
        {
            _content.RemoveAllViews();

            View layout = View.Inflate(this, Resource.Layout.message, _content);

            TextView message = layout.FindViewById<TextView>(Resource.Id.messageTextView);
            message.SetText(messageId);
        }

        public void ShowSchedule(Int32 scheduleId)
        {
            IReadOnlyDictionary<Int32, Schedule> schedules = _application.Manager.Schedules;

            if (scheduleId == 0)
            {
                ShowSnackbar(Resource.String.scheduleNotAvailableMessage);
                return ;
            }

            if (!schedules.ContainsKey(scheduleId))
            {
                ShowSnackbar(
                    Resource.String.scheduleNotYetDownloadedMessage,
                    Resource.String.downloadActionText,
                    () => DownloadScheduleWithCheckPermission(scheduleId)
                );

                return ;
            }

            _toolbarTitle.Text = schedules[scheduleId].DisplayedName;
            _application.Preferences.SetCurrentScheduleId(scheduleId);
            ViewPagerMoveToDate(_application.Preferences.CurrentScheduleDate);

            _activityState = MainActivityState.DisplaysSchedule;
        }

        private void ViewPagerMoveToDate(DateTime date, Boolean adapterResetRequired = true)
        {
            // Предотвращает ситуацию, когда диапазон отображаемых дат перерассчитывается каждый раз
            // при переходе между расписаниями, если перед этим перелистнуть страницу.
            if (!_pagerAdapter.RenderingDateRange.IsDateInside(date))
            {
                adapterResetRequired = true;
                _pagerAdapter.RenderingDateRange.Recompute(date);
            }

            if (adapterResetRequired)
            {
                _viewPager.Adapter = _pagerAdapter;
            }

            // Плавная прокрутка до выбранной вкладки возможна только без переназначения адаптера.
            _viewPager.SetCurrentItem(_pagerAdapter.RenderingDateRange.GetIndexByDate(date), true);
        }

        private void SetSchedulesMenu(IReadOnlyDictionary<Int32, Schedule> schedules)
        {
            IEnumerable<(Int32 scheduleId, String displayedName)> Fetch(IEnumerable<Schedule> values)
            {
                return values.Select<Schedule, (Int32, String)>(s => (s.ScheduleId, s.DisplayedName));
            }

            _schedulesMenu.Menu.Clear();

            if (schedules.Count == 1)
            {
                _toolbarTitle.SetCompoundDrawables(null, null, null, null);
                return ;
            }

            foreach ((Int32 scheduleId, String displayedName) in Fetch(schedules.Values))
            {
                _schedulesMenu.Menu.Add(Menu.None, scheduleId, Menu.None, displayedName);
            }

            _toolbarTitle.SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.arrowDown, 0);
        }

        private async void RemoveCurrentScheduleAsync()
        {
            Int32 scheduleId = _application.Preferences.CurrentScheduleId;

            Boolean hasRemovingError = await _application.Manager.RemoveScheduleAsync(scheduleId);
            if (hasRemovingError)
            {
                ShowSnackbar(Resource.String.scheduleRemovingErrorMessage);
                return ;
            }

            ShowSnackbar(Resource.String.scheduleRemovedSuccessfullyMessage);
            RestartSchedulesRenderingSubsystem();
        }

        private void DownloadScheduleWithCheckPermission(Int32 scheduleId)
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            DownloadSchedulesAsync(scheduleId.ToString());
        }

        private async void DownloadSchedulesAsync(params String[] requests)
        {
            //if (!ApplicationHelper.IsUniversitySiteConnectionAvailable(out String failReason))
            //{
            //    ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
            //    _application.Logger.Log(failReason);
            //    _application.SaveLog();
            //    return ;
            //}

            ShowSnackbar(Resource.String.schedulesDownloadingStarted);

            Boolean haveDownloadingErrors = await _application.Manager.DownloadSchedulesAsync(requests);
            RestartSchedulesRenderingSubsystem();

            Int32 messageId;
            if (haveDownloadingErrors)
            {
                messageId = (requests.Length == 1) ? Resource.String.scheduleDownloadErrorMessage
                    : Resource.String.schedulesDownloadErrorMessage;

                _application.SaveLog();
            }
            else
            {
                messageId = (requests.Length == 1) ? Resource.String.scheduleDownloadedSuccessfullyMessage
                    : Resource.String.schedulesDownloadedSuccessfullyMessage;
            }

            ShowSnackbar(messageId);
        }

        private MainActivityState _activityState;

        private ScheduleApplication _application;
        private Timer _currentSubjectHighlightTimer;

        private ViewPager _viewPager;
        private TabLayout _tabLayout;
        private TextView _toolbarTitle;
        private RelativeLayout _content;
        private PopupMenu _schedulesMenu;
        private FloatingActionButton _fab;
        private SchedulesPagerAdapter _pagerAdapter;
    }
}