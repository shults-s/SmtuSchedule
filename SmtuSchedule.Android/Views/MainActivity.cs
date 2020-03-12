using System;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Android;
using Android.OS;
using Android.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Android.Text.Method;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Com.Getkeepsafe.Taptargetview;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Enumerations;
using SmtuSchedule.Android.Utilities;
using SmtuSchedule.Android.Interfaces;
using SmtuSchedule.Android.Enumerations;

using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace SmtuSchedule.Android.Views
{
    [Activity(MainLauncher = true, Label = "@string/applicationLabel", LaunchMode = LaunchMode.SingleTop,
        ScreenOrientation = ScreenOrientation.Portrait)]
    internal class MainActivity : AppCompatActivity, ISchedulesViewer
    {
        private const String UpcomingLessonRemindWorkerTag = "Shults.SmtuSchedule.LessonsReminderWork";
        private const String UpdateSchedulesWorkerTag = "Shults.SmtuSchedule.UpdateSchedulesWork";

        private const Int32 ExternalStoragePermissionsRequestCode = 30;
        private const Int32 InternetPermissionRequestCode = 31;

        private const Int32 StartPreferencesActivityRequestCode = 33;
        private const Int32 StartDownloadActivityRequestCode = 35;

        private static readonly String[] ExternalStoragePermissions = new String[]
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage
        };

        private enum MainActivityState
        {
            NotInitialized,
            Initialized,
            WelcomeMessageDisplayed,
            ScheduleDisplayed,
            DownloadingScreenStarted
        }

        private class MainActivityStateManager
        {
            public MainActivityState CurrentState { get; private set; }

            public MainActivityStateManager(MainActivityState state)
            {
                CurrentState = state;
                _lastState = state;
            }

            public void SetState(MainActivityState state)
            {
                _lastState = CurrentState;
                CurrentState = state;
            }

            public void RestoreLastState() => CurrentState = _lastState;

            private MainActivityState _lastState;
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
                        Resource.String.grantAccessActionTitle,
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
                    ShowLayoutMessage(Resource.String.externalStoragePermissionsDeclinedErrorMessage);

                    String[] deniedPermissions = permissions.Where(p => IsPermissionDenied(p)).ToArray();
                    ShowSnackbar(
                        Resource.String.storagePermissionsRationaleMessage,
                        Resource.String.grantAccessActionTitle,
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
            // Если пользователь нажал на уведомление, содержащее полезную нагрузку и пришедшее, когда
            // приложение не было запущено, либо находилось в фоновом режиме.
            if (Intent.Extras != null)
            {
                ICollection<String> keys = Intent.Extras.KeySet();
                if (IntentUtilities.IsDataKeysCollectionValidToCreateViewIntent(keys))
                {
                    Dictionary<String, String> data = new Dictionary<String, String>();
                    foreach (String key in keys)
                    {
                        data[key] = Intent.Extras.GetString(key);
                    }

                    Intent intent = IntentUtilities.CreateIntentFromData(this, data);
                    if (intent != null)
                    {
                        StartActivity(intent);
                    }

                    Finish();
                }
                else if (IntentUtilities.IsDataKeysCollectionValidToCreateUpcomingLessonIntent(keys))
                {
                    const String dateKey = IntentUtilities.DataUpcomingLessonDateKey;
                    if (Int64.TryParse(Intent.Extras.GetString(dateKey), out Int64 ticks))
                    {
                        _application.Preferences.CurrentScheduleDate = new DateTime(ticks);
                    }

                    const String scheduleIdKey = IntentUtilities.DataUpcomingLessonScheduleIdKey;
                    if (Int32.TryParse(Intent.Extras.GetString(scheduleIdKey), out Int32 scheduleId))
                    {
                        _application.Preferences.SetCurrentScheduleId(scheduleId);
                    }
                }
            }

            _application = ApplicationContext as SmtuScheduleApplication;

            _currentlyUsedDarkTheme = _application.Preferences.UseDarkTheme;
            _isThemeChanged = false;
            _application.Preferences.ThemeChanged += () =>
            {
                _isThemeChanged = (_currentlyUsedDarkTheme != _application.Preferences.UseDarkTheme);
            };

            _application.Preferences.UpdateSchedulesOnStartChanged += () =>
            {
                if (_application.Preferences.UpdateSchedulesOnStart)
                {
                    PeriodicWorkUtilities.CreateWork<UpdateSchedulesWorker>(
                        UpdateSchedulesWorkerTag,
                        TimeSpan.FromHours(12),
                        true
                    );
                }
                else
                {
                    PeriodicWorkUtilities.CancelWorkByTag(UpdateSchedulesWorkerTag);
                }
            };

            _application.Preferences.LessonRemindTimesChanged += () =>
            {
                if (_application.Preferences.LessonRemindTimes != LessonRemindTime.Never)
                {
                    PeriodicWorkUtilities.CreateWork<LessonsRemindWorker>(
                        UpcomingLessonRemindWorkerTag,
                        TimeSpan.FromDays(1),
                        false
                    );
                }
                else
                {
                    PeriodicWorkUtilities.CancelWorkByTag(UpcomingLessonRemindWorkerTag);
                }
            };

            SetTheme(_application.Preferences.UseDarkTheme ? Resource.Style.Theme_SmtuSchedule_Dark
                : Resource.Style.Theme_SmtuSchedule_Light);

            base.OnCreate(savedInstanceState);

            _stateManager = new MainActivityStateManager(MainActivityState.NotInitialized);

            SetContentView(Resource.Layout.mainActivity);

            _contentLayout = FindViewById<RelativeLayout>(Resource.Id.mainContentRelativeLayout);

            _toolbar = FindViewById<Toolbar>(Resource.Id.mainActivityToolbar);
            _toolbar.InflateMenu(Resource.Menu.mainMenu);
            _toolbar.Title = null;
            _toolbar.MenuItemClick += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.selectViewingDateMenuItem:
                        ShowCustomDatePickerDialog();
                        break;

                    case Resource.Id.downloadSchedulesMenuItem:
                        StartDownloadActivityAsync();
                        break;

                    case Resource.Id.startPreferencesMenuItem:
                        StartPreferencesActivity();
                        break;

                    case Resource.Id.aboutApplicationMenuItem:
                        new CustomAlertDialog(this)
                            .SetTitle(Resource.String.aboutApplicationDialogTitle)
                            .SetMessage(Resource.String.aboutApplicationMessage)
                            .SetPositiveButton(Resource.String.thanksActionTitle)
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

            if (_stateManager.CurrentState == MainActivityState.ScheduleDisplayed)
            {
                _currentSubjectHighlightTimer?.Start();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (_stateManager.CurrentState == MainActivityState.ScheduleDisplayed)
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
                    return ;
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
            else if (requestCode == StartDownloadActivityRequestCode)
            {
                _stateManager.RestoreLastState();

                if (resultCode != Result.Ok)
                {
                    return ;
                }

                Boolean shouldDownloadRelatedSchedules = data.GetBooleanExtra(
                    DownloadActivity.IntentShouldDownloadRelatedSchedulesKey,
                    false
                );

                String[] requests = data.GetStringArrayExtra(DownloadActivity.IntentSearchRequestsKey);

                _ = DownloadSchedulesAsync(requests, shouldDownloadRelatedSchedules);
            }
        }

        private async void ContinueActivityInitializationAsync()
        {
            if (!_application.IsInitialized)
            {
                if (!_application.Initialize(out InitializationStatus status))
                {
                    String message = String.Format(
                        Resources.GetString(Resource.String.applicationInitializationErrorMessage),
                        status.ToString()
                    );

                    ShowLayoutMessage(message.FromMarkdown());
                    return ;
                }

                // if (status == InitializationStatus.FailedToRemoveDirectory)
                // {
                //     ShowSnackbar(Resource.String.failedToRemoveDirectoryErrorMessage);
                // }

                ShowProgressBar();

                Boolean haveReadingErrors = await _application.Manager.ReadSchedulesAsync();
                if (haveReadingErrors)
                {
                    ShowSnackbar(Resource.String.schedulesReadingErrorMessage);
                    _ = _application.SaveLogAsync();
                }

                Int32 schedulesNumber = _application.Manager.Schedules.Count;
                if (_application.Preferences.UpdateSchedulesOnStart && schedulesNumber != 0)
                {
                    await UpdateSchedulesWithCheckPermissionAsync();
                }
            }

            Int32 currentVersion = _application.GetVersionCode();
            if (_application.Preferences.LastSeenUpdateVersion == 0)
            {
                _application.Preferences.SetLastSeenUpdateVersion(currentVersion);
            }

            _toolbarTitle = FindViewById<TextView>(Resource.Id.mainToolbarTitleTextView);
            _schedulesMenu = new PopupMenu(this, _toolbarTitle);
            _schedulesMenu.MenuItemClick += (s, e) => ShowSchedule(e.Item.ItemId);

            _toolbarTitle.Click += (s, e) =>
            {
                if (_stateManager.CurrentState == MainActivityState.ScheduleDisplayed
                    && _application.Manager.Schedules.Count != 0)
                {
                    _schedulesMenu.Show();
                }
            };

            _toolbarTitle.LongClick += (s, e) =>
            {
                if (_stateManager.CurrentState == MainActivityState.ScheduleDisplayed)
                {
                    ShowCurrentScheduleActionsDialog();
                }
            };

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

            _stateManager.SetState(MainActivityState.Initialized);

            if (!IsPreferencesValid())
            {
                ShowDialogWithSuggestionToConfigureApplication();
                return ;
            }

            RestartSchedulesRenderingSubsystem();

            _ = _application.ClearLogsAsync();

            MigrateSchedulesAsync(currentVersion);
            CheckForCriticalUpdatesAsync(currentVersion);

#if DEBUG
            Log.Debug("Shults.SmtuSchedule.MessagingService", MessagingService.GetToken(this) ?? ":(");
#endif
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
                _ = _application.SaveLogAsync();
            }
            else
            {
                _application.Preferences.SetLastMigrationVersion(currentVersion);
            }
        }

        private async void CheckForCriticalUpdatesAsync(Int32 currentVersion)
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            ReleaseDescription latest = await ApplicationUtilities.GetLatestReleaseDescription();
            if (latest == null)
            {
                return ;
            }

            if (!latest.IsCriticalUpdate) // && !_application.Preferences.CheckUpdatesOnStart
            {
                return ;
            }

            if (latest.VersionCode == _application.Preferences.LastSeenUpdateVersion
                || latest.VersionCode <= currentVersion)
            {
                return ;
            }

            Java.Lang.ICharSequence dialogMessage = (latest.VersionNotes != null)
                ? latest.VersionNotes.FromHtml()
                : Resources.GetString(Resource.String.applicationUpdateAvailableMessage).FromMarkdown();

            Int32 dialogTitleId = latest.IsCriticalUpdate
                ? Resource.String.applicationCriticalUpdateAvailableDialogTitle
                : Resource.String.applicationUpdateAvailableDialogTitle;

            String packageId = latest.GooglePlayStorePackageId;
            if (packageId == null)
            {
                new CustomAlertDialog(this)
                    .SetTitle(dialogTitleId)
                    .SetMessage(dialogMessage)
                    .SetPositiveButton(
                        Resource.String.openUpdateDownloadPageActionTitle,
                        () =>
                        {
                            String url = ApplicationUtilities.LatestReleaseDownloadPageUrl;
                            StartActivity(IntentUtilities.CreateViewIntentFromUrl(url));
                        }
                    )
                    .SetNegativeButton(
                        Resource.String.gotItActionTitle,
                        () => _application.Preferences.SetLastSeenUpdateVersion(latest.VersionCode)
                    )
                    .Show();

                return ;
            }

            void OpenWithPlayStore()
            {
                try
                {
                    Intent intent = IntentUtilities.CreateGooglePlayStoreViewIntent(
                        this,
                        packageId
                    );

                    if (intent != null)
                    {
                        StartActivity(intent);
                    }
                }
                catch (ActivityNotFoundException)
                {
                    Intent intent = IntentUtilities.CreateGooglePlayStoreViewIntent(
                        this,
                        packageId,
                        true
                    );

                    if (intent != null)
                    {
                        StartActivity(intent);
                    }
                }
            }

            if (packageId == PackageName)
            {
                new CustomAlertDialog(this)
                    .SetTitle(dialogTitleId)
                    .SetMessage(dialogMessage)
                    .SetPositiveButton(
                        Resource.String.openPlayMarketActionTitle,
                        () => OpenWithPlayStore()
                    )
                    .SetNegativeButton(
                        Resource.String.gotItActionTitle,
                        () => _application.Preferences.SetLastSeenUpdateVersion(latest.VersionCode)
                    )
                    .Show();

                return ;
            }

            new CustomAlertDialog(this)
                .SetTitle(Resource.String.googlePlayStoreReleaseAvailableDialogTitle)
                .SetMessage(Resource.String.googlePlayStoreReleaseRelocatedMessage)
                .SetPositiveButton(Resource.String.openPlayMarketActionTitle, () => OpenWithPlayStore())
                .Show();
        }

        private void RestartSchedulesRenderingSubsystem(Int32 preferredScheduleId = 0)
        {
            if (_stateManager.CurrentState == MainActivityState.NotInitialized)
            {
                return ;
            }

            IReadOnlyDictionary<Int32, Schedule> schedules = _application.Manager.Schedules;

            IReadOnlyList<Schedule> sortedSchedules = schedules.Select(s => s.Value)
                .OrderBy(s => s, new SchedulesComparer())
                .ToList();

            UpdateToolbarMenu();

            ShowSchedulesFeatureDiscoveryTargetsSequence(sortedSchedules.Count);

            if (sortedSchedules.Count == 0)
            {
                _toolbarTitle.SetText(Resource.String.welcomeToolbarTitle);
                _tabLayout.Visibility = ViewStates.Gone;
                _fab.Visibility = ViewStates.Gone;

                _currentSubjectHighlightTimer.Stop();

                ShowLayoutMessage(Resource.String.welcomeMessage);
                _stateManager.SetState(MainActivityState.WelcomeMessageDisplayed);

                return ;
            }

            SetSchedulesMenu(sortedSchedules);

            ShowViewPager();

            CustomSwipeRefreshLayout scheduleRefreshLayout = FindViewById<CustomSwipeRefreshLayout>(
                Resource.Id.scheduleSwipeRefreshLayout);

            scheduleRefreshLayout.SetColorSchemeResources(Resource.Color.primaryDay);

            scheduleRefreshLayout.Refresh += async (s, e) =>
            {
                await DownloadScheduleWithCheckPermissionAsync(_application.Preferences.CurrentScheduleId);
                scheduleRefreshLayout.Refreshing = false;
            };

            _viewPager = FindViewById<ViewPager>(Resource.Id.scheduleViewPager);

            // Bug: если после запуска приложения перелистнуть страницу расписания влево или вправо, то
            // затем при изменении ориентации устройства событие по неизвестной причине срабатывает
            // дважды, причем оба раза с разными значениями позиции. В результате, при каждом повороте
            // экрана дата увеличивается/уменьшается (в зависимости от того, куда изначально свайпнуть),
            // но таблица с расписанием не обновляется.
            // Судя по нагугленному – это не баг, а "by design": перед переходом на запрошенную страницу
            // активной делается либо одна из крайних, либо соседняя с запрашиваемой страница.
            _viewPager.PageSelected += (s, e) =>
            {
                DateTime date = _pagerAdapter.RenderingDateRange.GetDateByIndex(e.Position);
                _application.Preferences.CurrentScheduleDate = date;
            };

            // При приближении к границам отрисовываемого диапазона дат необходимо пересчитать его,
            // чтобы пользователю не нужно было совершать лишние телодвижения для дальнейшего просмотра.
            _viewPager.PageScrollStateChanged += (s, e) =>
            {
                if (e.State != ViewPager.ScrollStateIdle)
                {
                    return ;
                }

                Int32 lastPageIndex = _pagerAdapter.RenderingDateRange.TotalDaysNumber - 1;

                // По одной странице с каждого края.
                if (_viewPager.CurrentItem == 0 || _viewPager.CurrentItem == lastPageIndex)
                {
                    ViewPagerMoveToDate(_application.Preferences.CurrentScheduleDate, true, true);
                }
            };

            // Пересоздавать адаптер необходимо при каждом перезапуске подсистемы рендеринга, иначе
            // на активной в момент перезапуска вкладке не отрисуется фрагмент.
            _pagerAdapter = new SchedulesPagerAdapter(
                SupportFragmentManager,
                _application.Preferences.CurrentScheduleDate
            );

            _tabLayout.Visibility = ViewStates.Visible;
            _tabLayout.SetupWithViewPager(_viewPager);

            _fab.Visibility = _application.Preferences.UseFabDateSelector ? ViewStates.Visible
                : ViewStates.Gone;

            Int32 SelectScheduleToDisplay()
            {
                if (preferredScheduleId != 0 && schedules.ContainsKey(preferredScheduleId))
                {
                    return preferredScheduleId;
                }

                Int32 currentId = _application.Preferences.CurrentScheduleId;
                if (currentId == 0)
                {
                    return sortedSchedules[0].ScheduleId;
                }

                return schedules.ContainsKey(currentId) ? currentId : sortedSchedules[0].ScheduleId;
            }

            Int32 scheduleId = SelectScheduleToDisplay();
            ShowSchedule(scheduleId);

            _currentSubjectHighlightTimer.Start();
        }

        private void ShowSchedulesFeatureDiscoveryTargetsSequence(Int32 numberOfSchedules)
        {
            FeatureDiscoveryState state = _application.Preferences.FeatureDiscoveryState;

            List<TapTarget> targets = new List<TapTarget>();

            if (!state.HasFlag(FeatureDiscoveryState.SchedulesDownload))
            {
                TapTarget downloadTarget;

                // Если пользователь выбрал для переключения даты в расписании плавающую кнопку, то элемент
                // меню для загрузки расписаний находится на тулбаре, а иначе – скрыт в выпадающем меню.
                if (_application.Preferences.UseFabDateSelector)
                {
                    downloadTarget = TapTarget.ForToolbarMenuItem(
                        _toolbar,
                        Resource.Id.downloadSchedulesMenuItem,
                        Resources.GetString(Resource.String.schedulesDownloadFeatureDiscoveryTitle),
                        Resources.GetString(Resource.String.schedulesDownloadFeatureDiscoveryMessage)
                    );
                }
                else
                {
                    downloadTarget = TapTarget.ForToolbarOverflow(
                        _toolbar,
                        Resources.GetString(Resource.String.schedulesDownloadFeatureDiscoveryTitle),
                        Resources.GetString(Resource.String.schedulesDownloadFeatureDiscoveryMessage)
                    );
                }

                downloadTarget.Stylize().Id((Int32)FeatureDiscoveryState.SchedulesDownload);
                targets.Add(downloadTarget);
            }

            if (numberOfSchedules != 0)
            {
                if (!state.HasFlag(FeatureDiscoveryState.SchedulesManagement))
                {
                    targets.Add(
                        TapTarget.ForView(
                            _toolbarTitle,
                            Resources.GetString(Resource.String.schedulesManagementFeatureDiscoveryTitle),
                            Resources.GetString(Resource.String.schedulesManagementFeatureDiscoveryMessage)
                        )
                        .Stylize()
                        .Id((Int32)FeatureDiscoveryState.SchedulesManagement)
                    );
                }

                if (!state.HasFlag(FeatureDiscoveryState.ScheduleChangeDate))
                {
                    TapTarget dateTarget;
                    if (_application.Preferences.UseFabDateSelector)
                    {
                        dateTarget = TapTarget.ForView(
                            _fab,
                            Resources.GetString(Resource.String.scheduleChangeDateFeatureDiscoveryTitle),
                            Resources.GetString(Resource.String.scheduleChangeDateFeatureDiscoveryMessage)
                        )
                        .TintTarget(false);
                    }
                    else
                    {
                        dateTarget = TapTarget.ForToolbarMenuItem(
                            _toolbar,
                            Resource.Id.selectViewingDateMenuItem,
                            Resources.GetString(Resource.String.scheduleChangeDateFeatureDiscoveryTitle),
                            Resources.GetString(Resource.String.scheduleChangeDateFeatureDiscoveryMessage)
                        );
                    }

                    dateTarget.Stylize().Id((Int32)FeatureDiscoveryState.ScheduleChangeDate);
                    targets.Add(dateTarget);
                }

                // if (!state.HasFlag(FeatureDiscoveryState.UpdateSchedulesOnStart))
                // {
                //     TapTarget updateTarget = TapTarget.ForToolbarOverflow(
                //         _toolbar,
                //         Resources.GetString(Resource.String.updateSchedulesOnStartFeatureDiscoveryTitle),
                //         Resources.GetString(Resource.String.updateSchedulesOnStartFeatureDiscoveryMessage)
                //     );
                //
                //     updateTarget.Stylize().Id((Int32)FeatureDiscoveryState.UpdateSchedulesOnStart);
                //     targets.Add(updateTarget);
                // }
            }

            if (targets.Count == 0)
            {
                return ;
            }

            TapTargetSequenceListener listener = new TapTargetSequenceListener();
            listener.Clicked += (Int32 id) => _application.Preferences.SetFeatureDiscoveryState(
                state |= (FeatureDiscoveryState)id
            );

            new TapTargetSequence(this).Targets(targets).Listener(listener).ContinueOnCancel(true).Start();
        }

        private void UpdateToolbarMenu()
        {
            // Если этот метод вызван до инициализации приложения или до окончания считывания расписаний.
            if (_stateManager.CurrentState == MainActivityState.NotInitialized)
            {
                return ;
            }

            IMenu menu = _toolbar.Menu;
            IMenuItem selectScheduleDateMenuItem = menu.FindItem(Resource.Id.selectViewingDateMenuItem);
            IMenuItem downloadScheduleMenuItem = menu.FindItem(Resource.Id.downloadSchedulesMenuItem);

            Boolean isToolbarDateSelectorVisible = !_application.Preferences.UseFabDateSelector
                && _application.Manager.Schedules.Count != 0;

            selectScheduleDateMenuItem.SetVisible(isToolbarDateSelectorVisible);

            downloadScheduleMenuItem.SetVisible(true);
            downloadScheduleMenuItem.SetShowAsAction(isToolbarDateSelectorVisible ? ShowAsAction.Never
                : ShowAsAction.Always);
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

        private Task<Boolean> IsUniversitySiteConnectionAvailableAsync()
        {
            return Task.Run(
                () => ApplicationUtilities.IsUniversitySiteConnectionAvailable(out String _));
        }

        private async void StartDownloadActivityAsync()
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            Boolean isConnected = await IsUniversitySiteConnectionAvailableAsync();
            if (!isConnected)
            {
                ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
                return ;
            }

            if (_application.Manager.IsDownloadingInProgress)
            {
                ShowSnackbar(Resource.String.waitUntilSchedulesFinishDownloading);
                return ;
            }

            if (_stateManager.CurrentState == MainActivityState.DownloadingScreenStarted)
            {
                return ;
            }

            _stateManager.SetState(MainActivityState.DownloadingScreenStarted);

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
                    Resource.String.downloadActionTitle,
                    () => _ = DownloadScheduleWithCheckPermissionAsync(scheduleId)
                );

                return ;
            }

            Schedule schedule = schedules[scheduleId];

            String scheduleTitle = schedule.DisplayedName;
            String suffix = $"({schedule.GetFormattedLastUpdate()})";
            _toolbarTitle.Text = schedule.IsActual ? scheduleTitle : scheduleTitle + suffix;

            _application.Preferences.SetCurrentScheduleId(scheduleId);
            ViewPagerMoveToDate(_application.Preferences.CurrentScheduleDate);

            _stateManager.SetState(MainActivityState.ScheduleDisplayed);
        }

        private void ViewPagerMoveToDate(DateTime date, Boolean adapterResetRequired = true,
            Boolean forceRecomputeDateRange = false)
        {
            // Предотвращаем ситуацию, когда диапазон отображаемых дат перерассчитывается каждый раз
            // при переходе между расписаниями, если перед этим перелистнуть страницу.
            if (!_pagerAdapter.RenderingDateRange.IsDateInside(date) || forceRecomputeDateRange)
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

        private void SetSchedulesMenu(IReadOnlyList<Schedule> schedules)
        {
            static IEnumerable<(Int32, String, Boolean)> Fetch(IEnumerable<Schedule> values)
            {
                return values.Select<Schedule, (Int32, String, Boolean)>(s => (
                    s.ScheduleId,
                    s.DisplayedName,
                    s.IsActual
                ));
            }

            _schedulesMenu.Menu.Clear();

            if (schedules.Count == 1)
            {
                _toolbarTitle.SetCompoundDrawables(null, null, null, null);
                return ;
            }

            foreach ((Int32 scheduleId, String displayedName, Boolean isActual) in Fetch(schedules))
            {
                String suffix = $"({schedules[scheduleId].GetFormattedLastUpdate()})";
                String scheduleTitle = isActual ? displayedName : displayedName + suffix;
                _schedulesMenu.Menu.Add(Menu.None, scheduleId, Menu.None, scheduleTitle);
            }

            _toolbarTitle.SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.arrowDown, 0);
        }

        private async Task UpdateSchedulesWithCheckPermissionAsync()
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            Boolean isConnected = await IsUniversitySiteConnectionAvailableAsync();
            if (!isConnected)
            {
                ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
                return ;
            }

            Boolean haveUpdatingErrors = await _application.Manager.UpdateSchedulesAsync();
            if (haveUpdatingErrors)
            {
                ShowSnackbar(Resource.String.schedulesUpdatingErrorMessage);
                _ = _application.SaveLogAsync();
            }
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

        private async Task DownloadScheduleWithCheckPermissionAsync(Int32 scheduleId)
        {
            if (IsPermissionDenied(Manifest.Permission.Internet))
            {
                RequestPermissions(InternetPermissionRequestCode, Manifest.Permission.Internet);
                return ;
            }

            await DownloadSchedulesAsync(new String[] { scheduleId.ToString() }, false);
        }

        private async Task DownloadSchedulesAsync(String[] requests, Boolean shouldDownloadRelatedSchedules)
        {
            Boolean isConnected = await IsUniversitySiteConnectionAvailableAsync();
            if (!isConnected)
            {
                ShowSnackbar(Resource.String.noUniversitySiteConnectionErrorMessage);
                return ;
            }

            ShowSnackbar(Resource.String.schedulesDownloadingStarted);

            DownloadingResult result = await _application.Manager.DownloadSchedulesAsync(
                requests,
                shouldDownloadRelatedSchedules
            );

            if (result == DownloadingResult.LecturersMapError)
            {
                ShowSnackbar(Resource.String.lecturersMapDownloadErrorShortMessage);
                return ;
            }

            Int32 preferredScheduleId = 0;
            if (requests.Length == 1)
            {
                preferredScheduleId = _application.Manager.GetScheduleIdBySearchRequest(requests[0]);
            }

            RestartSchedulesRenderingSubsystem(preferredScheduleId);

            if (result == DownloadingResult.WithErrors)
            {
                isConnected = await IsUniversitySiteConnectionAvailableAsync();
                if (!isConnected)
                {
                    ShowSnackbar(Resource.String.universitySiteConnectionLostErrorMessage);
                    return ;
                }
            }

            Boolean isSingularSchedule = (requests.Length == 1 && !shouldDownloadRelatedSchedules);
            Int32 messageId;
            if (result == DownloadingResult.WithErrors)
            {
                messageId = isSingularSchedule ? Resource.String.scheduleDownloadErrorMessage
                    : Resource.String.schedulesDownloadErrorMessage;

                _ = _application.SaveLogAsync();
            }
            else
            {
                messageId = isSingularSchedule ? Resource.String.scheduleDownloadedSuccessfullyMessage
                    : Resource.String.schedulesDownloadedSuccessfullyMessage;
            }

            ShowSnackbar(messageId);
        }

        private void ShowProgressBar()
        {
            _contentLayout.RemoveAllViews();
            View.Inflate(this, Resource.Layout.progressBar, _contentLayout);
        }

        private void ShowViewPager()
        {
            _contentLayout.RemoveAllViews();
            View.Inflate(this, Resource.Layout.scheduleViewPager, _contentLayout);
        }

        private void ShowLayoutMessage(Int32 messageId, Boolean useMarkdownFormatting = true)
        {
            if (useMarkdownFormatting)
            {
                ShowLayoutMessage(Resources.GetString(messageId).FromMarkdown(), true);
                return ;
            }

            ShowLayoutMessage(new Java.Lang.String(Resources.GetString(messageId)), false);
        }

        private void ShowLayoutMessage(Java.Lang.ICharSequence message, Boolean enableLinks = true)
        {
            _contentLayout.RemoveAllViews();

            View layout = View.Inflate(this, Resource.Layout.message, _contentLayout);

            TextView textView = layout.FindViewById<TextView>(Resource.Id.messageTextView);

            if (enableLinks)
            {
                textView.MovementMethod = LinkMovementMethod.Instance;
                textView.TextFormatted = message.StripUrlUnderlines();
            }
            else
            {
                textView.TextFormatted = message;
            }

            textView.TextFormatted = textView.TextFormatted.Trim();
            textView.SetMaxWidth((Int32)(UiUtilities.GetScreenPixelSize(WindowManager).width * 0.9));
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
                                _ = DownloadScheduleWithCheckPermissionAsync(scheduleId);
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
                return ;
            }

            String message = Resources.GetString(Resource.String.removeCurrentScheduleMessage);
            message = String.Format(message, schedule.DisplayedName);

            new CustomAlertDialog(this)
                .SetMessage(message, false)
                .SetPositiveButton(Resource.String.removeActionTitle, RemoveCurrentScheduleAsync)
                .SetNegativeButton(Resource.String.cancelActionTitle)
                .Show();
        }

        private void ShowDialogWithSuggestionToConfigureApplication()
        {
            CustomAlertDialog dialog = new CustomAlertDialog(this)
                .SetPositiveButton(Resource.String.configureActionTitle, StartPreferencesActivity)
                .SetMessage(Resource.String.configureApplicationMessage, false)
                .SetCancelable(false);

            dialog.Show();

            FeatureDiscoveryState state = _application.Preferences.FeatureDiscoveryState;
            if (!state.HasFlag(FeatureDiscoveryState.ApplicationSettings))
            {
                TapTargetViewListener listener = new TapTargetViewListener();
                listener.Clicked += () => _application.Preferences.SetFeatureDiscoveryState(
                    state | FeatureDiscoveryState.ApplicationSettings
                );

                Button positiveButton = dialog.GetButton(DialogButtonType.Positive);
                TapTarget settingsTarget = TapTarget.ForView(
                    positiveButton,
                    Resources.GetString(Resource.String.applicationSettingsFeatureDiscoveryTitle),
                    Resources.GetString(Resource.String.applicationSettingsFeatureDiscoveryMessage)
                )
                .Stylize();

                TapTargetView.ShowFor(dialog, settingsTarget, listener);
            }
        }

        private void ShowViewingWeekTypeSnackbar()
        {
            DateTime viewingDate = _application.Preferences.CurrentScheduleDate;

            Int32 type = (Int32)viewingDate.GetWeekType(_application.Preferences.UpperWeekDate);
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
            text.SetMaxLines(5);

            snackbar.Show();
        }

        private Boolean _isThemeChanged;
        private Boolean _currentlyUsedDarkTheme;

        private Toolbar _toolbar;
        private ViewPager _viewPager;
        private TabLayout _tabLayout;
        private TextView _toolbarTitle;
        private PopupMenu _schedulesMenu;
        private FloatingActionButton _fab;
        private RelativeLayout _contentLayout;
        private SchedulesPagerAdapter _pagerAdapter;

        private SmtuScheduleApplication _application;
        private Timer _currentSubjectHighlightTimer;
        private MainActivityStateManager _stateManager;
    }
}