using System;
using System.Linq;
using System.Timers;
using System.Collections.Generic;
using Android;
using Android.OS;
using Android.App;
using Android.Util;
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

        private static readonly String[] ExternalStoragePermissions = new String[]
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage
        };

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
            base.OnCreate(savedInstanceState);

            _application = ApplicationContext as ScheduleApplication;

            _currentlyUsedDarkTheme = _application.Preferences.UseDarkTheme;
            _isThemeChanged = false;
            _application.Preferences.ThemeChanged += () =>
            {
                _isThemeChanged = (_currentlyUsedDarkTheme != _application.Preferences.UseDarkTheme);
            };

            SetTheme(_application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            _activityState = MainActivityState.NotInitialized;

            SetContentView(Resource.Layout.mainActivity);

            _contentLayout = FindViewById<RelativeLayout>(Resource.Id.mainContentRelativeLayout);

            _toolbar = FindViewById<Toolbar>(Resource.Id.mainActivityToolbar);
            _toolbar.InflateMenu(Resource.Menu.mainMenu);
            _toolbar.Title = null;
            _toolbar.MenuItemClick += (s, e) =>
            {
                switch (e.Item.ItemId)
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
                        StartPreferencesActivity();
                        break;

                    case Resource.Id.aboutApplicationMenuItem:
                        new CustomAlertDialog(this)
                            .SetTitle(Resource.String.aboutApplicationDialogTitle)
                            .SetMessage(Resource.String.aboutApplicationMessage)
                            .SetPositiveButton(Resource.String.thanksActionText)
                            .Show();
                        break;
                }
            };

            if (IsPermissionDenied(Manifest.Permission.WriteExternalStorage))
            {
                RequestPermissions(ExternalStoragePermissionsRequestCode, ExternalStoragePermissions);
            }
            else
            {
                ContinueActivityInitializationAsync();
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
                if (_isThemeChanged)
                {
                    Recreate();
                }

                if (IsPreferencesValid())
                {
                    RestartSchedulesRenderingSubsystem();
                }
                else
                {
                    ShowDialogWithSuggestionToConfigureApplication();
                }
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

            Int32 currentVersion = _application.GetVersion();
            if (_application.Preferences.LastSeenUpdateVersion == 0)
            {
                _application.Preferences.SetLastSeenUpdateVersion(currentVersion);
            }

            MigrateSchedulesAsync(currentVersion);

            _toolbarTitle = FindViewById<TextView>(Resource.Id.mainToolbarTitleTextView);
            _schedulesMenu = new PopupMenu(this, _toolbarTitle);
            _schedulesMenu.MenuItemClick += (s, e) => ShowSchedule(e.Item.ItemId);
            _toolbarTitle.Click += (s, e) => _schedulesMenu.Show();
            _toolbarTitle.LongClick += (s, e) => ShowCurrentScheduleActionsDialog();

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
            _fab.LongClick += (s, e) => ShowViewingWeekTypeSnackbar();

            _activityState = MainActivityState.Initialized;

            if (!IsPreferencesValid())
            {
                ShowDialogWithSuggestionToConfigureApplication();
                //return ;
            }

            RestartSchedulesRenderingSubsystem();

            if (_application.Preferences.CheckUpdatesOnStart)
            {
                CheckForUpdatesAsync(currentVersion);
            }

            if (_application.Preferences.LastSeenWelcomeVersion != currentVersion)
            {
                new CustomAlertDialog(this)
                    .SetTitle(Resource.String.introductionDialogTitle)
                    .SetMessage(Resource.String.introductionMessage)
                    .SetPositiveButton(
                        Resource.String.gotItActionText,
                        () => _application.Preferences.SetLastSeenWelcomeVersion(currentVersion)
                    )
                    .SetPositiveButtonEnabledOnlyWhenMessageScrolledToBottom()
                    .Show();
            }
        }

        private Boolean IsPreferencesValid()
        {
            return _application.Preferences.UpperWeekDate != default(DateTime);
        }

        private async void MigrateSchedulesAsync(Int32 currentVersion)
        {
            if (_application.Preferences.LastMigrationVersion == currentVersion)
            {
                return ;
            }

            Boolean haveMigrationErrors = await _application.Manager.MigrateSchedulesAsync();
            if (haveMigrationErrors)
            {
                ShowSnackbar(Resource.String.schedulesMigrationErrorMessage);
                _application.SaveLog();
            }
            else
            {
                _application.Preferences.SetLastMigrationVersion(currentVersion);
            }
        }

        private async void CheckForUpdatesAsync(Int32 currentVersion)
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            ReleaseDescription latest = await ApplicationHelper.GetLatestReleaseDescription();
            if (latest == null)
            {
                return ;
            }

            String packageId = latest.GooglePlayMarketPackageId;
            if (packageId == null)
            {
                if (latest.VersionCode == _application.Preferences.LastSeenUpdateVersion
                    || latest.VersionCode <= currentVersion)
                {
                    return ;
                }

                new CustomAlertDialog(this)
                    .SetTitle(Resource.String.applicationUpdateAvailableDialogTitle)
                    .SetMessage(Resource.String.applicationUpdateAvailableMessage)
                    .SetPositiveButton(
                        Resource.String.openUpdateDownloadPageActionText,
                        () =>
                        {
                            String url = ApplicationHelper.LatestReleaseUrl;
                            StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));
                        }
                    )
                    .SetNegativeButton(
                        Resource.String.gotItActionText,
                        () => _application.Preferences.SetLastSeenUpdateVersion(latest.VersionCode)
                    )
                    .Show();

                return ;
            }

            Int32 messageId = (packageId != PackageName)
                ? Resource.String.googlePlayMarketReleaseAvailableMessage
                : Resource.String.googlePlayMarketReleaseReloadedMessage;

            new CustomAlertDialog(this)
                .SetTitle(Resource.String.googlePlayMarketReleaseAvailableDialogTitle)
                .SetMessage(messageId)
                .SetPositiveButton(
                    Resource.String.openPlayMarketActionText,
                    () =>
                    {
                        try
                        {
                            String url = "market://details?id=" + packageId;
                            StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));
                        }
                        catch (ActivityNotFoundException) // Google Play не установлен.
                        {
                            String url = "https://play.google.com/store/apps/details?id=" + packageId;
                            StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));
                        }
                    }
                )
                .Show();
        }

        private void RestartSchedulesRenderingSubsystem()
        {
            IReadOnlyDictionary<Int32, Schedule> schedules = _application.Manager.Schedules;

            UpdateToolbarMenu();

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

            ShowViewPager();
            _viewPager = FindViewById<ViewPager>(Resource.Id.scheduleViewPager);

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

            // Пересоздавать адаптер необходимо при каждом перезапуске подсистемы рендеринга, иначе
            // на активной в момент перезапуска вкладке не отрисуется фрагмент.
            _pagerAdapter = new SchedulesPagerAdapter(
                SupportFragmentManager,
                _application.Preferences.CurrentScheduleDate
            );

            _tabLayout.Visibility = ViewStates.Visible;
            _tabLayout.SetupWithViewPager(_viewPager);

            Int32 SelectScheduleToDisplay()
            {
                Int32 currentId = _application.Preferences.CurrentScheduleId;
                if (currentId == 0)
                {
                    return schedules.Keys.First();
                }

                return schedules.ContainsKey(currentId) ? currentId : schedules.Keys.First();
            }

            Int32 scheduleId = SelectScheduleToDisplay();
            ShowSchedule(scheduleId);

            _fab.Visibility = _application.Preferences.UseFabDateSelector ? ViewStates.Visible
                : ViewStates.Gone;

            _currentSubjectHighlightTimer.Start();
        }

        private void UpdateToolbarMenu()
        {
            // Если этот метод вызван до инициализации приложения или до окончания считывания расписаний.
            if (_activityState == MainActivityState.NotInitialized)
            {
                return ;
            }

            IMenu menu = _toolbar.Menu;
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
        }

        private void RequestPermissions(Int32 requestCode, params String[] permissions)
        {
            ActivityCompat.RequestPermissions(this, permissions, requestCode);
        }

        private Boolean IsPermissionDenied(String permission)
        {
            return ActivityCompat.CheckSelfPermission(this, permission) != Permission.Granted;
        }

        private void StartPreferencesActivity()
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

            if (!ApplicationHelper.IsUniversitySiteConnectionAvailable(out String failReason))
            {
                ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
                _application.Logger.Log(failReason);
                _application.SaveLog();
                return ;
            }

            if (_application.Manager.IsDownloadingInProgress)
            {
                ShowSnackbar(Resource.String.waitUntilSchedulesFinishDownloading);
                return ;
            }

            StartActivityForResult(typeof(DownloadActivity), StartDownloadActivityRequestCode);
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
            if (!ApplicationHelper.IsUniversitySiteConnectionAvailable(out String failReason))
            {
                ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
                _application.Logger.Log(failReason);
                _application.SaveLog();
                return ;
            }

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

        private void ShowProgressBar()
        {
            _contentLayout.RemoveAllViews();
            View.Inflate(this, Resource.Layout.progress, _contentLayout);
        }

        private void ShowViewPager()
        {
            _contentLayout.RemoveAllViews();
            View.Inflate(this, Resource.Layout.pager, _contentLayout);
        }

        private void ShowLayoutMessage(Int32 messageId)
        {
            _contentLayout.RemoveAllViews();

            View layout = View.Inflate(this, Resource.Layout.message, _contentLayout);

            TextView message = layout.FindViewById<TextView>(Resource.Id.messageTextView);
            message.SetText(messageId);
        }

        private void ShowCurrentScheduleActionsDialog()
        {
            new CustomAlertDialog(this)
                .SetTitle(Resource.String.currentScheduleActionsDialogTitle)
                .SetActions(
                    Resources.GetStringArray(Resource.Array.currentScheduleActionsTitles),
                    (index) =>
                    {
                        Int32 scheduleId = _application.Preferences.CurrentScheduleId;
                        switch (index)
                        {
                            case 0:
                                ShowScheduleRemoveDialog(scheduleId);
                                break;

                            case 1:
                                DownloadScheduleWithCheckPermission(scheduleId);
                                break;
                        }
                    }
                )
                .Show();
        }

        private void ShowScheduleRemoveDialog(Int32 scheduleId)
        {
            if (!_application.Manager.Schedules.TryGetValue(scheduleId, out Schedule schedule))
            {
                return;
            }

            String displayedName = schedule.DisplayedName;

            String message = Resources.GetString(
                Resource.String.removeCurrentScheduleMessage,
                displayedName
            );

            new CustomAlertDialog(this)
                .SetMessage(message)
                .SetPositiveButton(Resource.String.removeActionText, RemoveCurrentScheduleAsync)
                .SetNegativeButton(Resource.String.cancelActionText)
                .Show();
        }

        private void ShowDialogWithSuggestionToConfigureApplication()
        {
            new CustomAlertDialog(this)
                .SetPositiveButton(Resource.String.configureActionText, StartPreferencesActivity)
                .SetMessage(Resource.String.configureApplicationMessage)
                .SetCancelable(false)
                .Show();
        }

        private void ShowViewingWeekTypeSnackbar()
        {
            DateTime viewingDate = _application.Preferences.CurrentScheduleDate;

            Int32 type = (Int32)Schedule.GetWeekType(_application.Preferences.UpperWeekDate, viewingDate);
            String[] weeksNames = Resources.GetStringArray(Resource.Array.weeksTypesInNominativeCase);

            ShowSnackbar(String.Format("{0}: {1}.", viewingDate.ToString("d MMMM"), weeksNames[type - 1]));
        }

        private void ShowCustomDatePickerDialog()
        {
            CustomDatePickerDialog dialog = new CustomDatePickerDialog(
                this,
                _application.Preferences.CurrentScheduleDate
            );

            dialog.DateChanged += (date) => ViewPagerMoveToDate(date);
            dialog.Show();
        }

        private void ShowSnackbar(Int32 messageId, Int32 actionId = 0, Action callback = null)
        {
            ShowSnackbar(Resources.GetString(messageId), actionId, callback);
        }

        private void ShowSnackbar(String message, Int32 actionId = 0, Action callback = null)
        {
            Snackbar snackbar = Snackbar.Make(_contentLayout, message, Snackbar.LengthLong);

            if (actionId != 0)
            {
                snackbar.SetAction(actionId, (v) => callback());
            }

            TextView text = snackbar.View.FindViewById<TextView>(Resource.Id.snackbar_text);
            text.SetTextSize(ComplexUnitType.Px, Resources.GetDimension(Resource.Dimension.normalTextSize));
            text.SetMaxLines(3);

            snackbar.Show();
        }

        private Boolean _isThemeChanged;
        private Boolean _currentlyUsedDarkTheme;

        private MainActivityState _activityState;
        private ScheduleApplication _application;
        private Timer _currentSubjectHighlightTimer;

        private Toolbar _toolbar;
        private ViewPager _viewPager;
        private TabLayout _tabLayout;
        private TextView _toolbarTitle;
        private PopupMenu _schedulesMenu;
        private FloatingActionButton _fab;
        private RelativeLayout _contentLayout;
        private SchedulesPagerAdapter _pagerAdapter;
    }
}