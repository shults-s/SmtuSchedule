using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.OS;
using Android.App;
using Android.Runtime;
using Android.Content.PM;
using Android.Gms.Common;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Android.Utilities;
using SmtuSchedule.Android.Interfaces;
using SmtuSchedule.Android.Exceptions;
using SmtuSchedule.Android.Enumerations;

using Environment = Android.OS.Environment;

namespace SmtuSchedule.Android
{
    [Register("shults.smtuschedule.android.SmtuScheduleApplication")]
    internal class SmtuScheduleApplication : Application, IApplicationManager
    {
        public const String SchedulesDirectoryName = "Schedules";

        public ISchedulesManager SchedulesManager { get; private set; }

        public ILecturersManager LecturersManager { get; private set; }

        public IPreferences Preferences { get; private set; }

        public ILogger Logger { get; private set; }

        public Boolean IsInitialized { get; private set; }

        public SmtuScheduleApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Logger = new InMemoryLogger();
            Logger.Log(
                "Shults.SmtuSchedule version {0}, running on {1} {2} (Android {4} – API {3}).",
                GetVersionName(),
                Build.Manufacturer,
                Build.Model,
                Build.VERSION.Sdk,
                Build.VERSION.Release
            );

            // В AndroidEnvironment.UnhandledExceptionRaiser трассировка стека подробнее,
            // чем в AppDomain.CurrentDomain.UnhandledException.
            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                Logger.Log(e.Exception);

                // WaitAny используется для подавления исключения, которое может возникнуть
                // при сохранении лога. Если не подавить его, в AppCenter не придет
                // краш репорт с исключением-первопричиной.
                Task.WaitAny(SaveLogAsync(true));
            };

            IsInitialized = false;

            Preferences = new Preferences(this);

            _logsDirectoryPath = Path.Join(GetModernExternalStoragePath(), "Logs");
        }

        public override void OnCreate()
        {
            base.OnCreate();

            NotificationUtilities.CreateNotificationChannels(this);

#if !DEBUG
            ApplicationLifecycleListener listener = new ApplicationLifecycleListener();
            listener.ApplicationStarted += () => Analytics.TrackEvent("ApplicationStarted");
            listener.ApplicationStopped += () => Analytics.TrackEvent("ApplicationStopped");
            RegisterActivityLifecycleCallbacks(listener);

            static Boolean ShouldTrackError(Exception exception) =>
                exception switch
                {
                    LecturersDownloaderException _ => true,
                    SchedulesDownloaderException _ => true,
                    SchedulesRepositoryException _ => true,
                    ApplicationException _ => true,
                    WorkerException _ => true,
                    _ => false
                };

            ErrorAttachmentLog[] GetErrorAttachmentsWithLog()
            {
                FileInfo[] logsFiles = null;
                try
                {
                    logsFiles = new DirectoryInfo(_logsDirectoryPath).GetFiles("*.log");
                }
                catch (DirectoryNotFoundException)
                {
                    return null;
                }

                if (logsFiles.Length == 0)
                {
                    return null;
                }

                FileInfo file = logsFiles.OrderByDescending(f => f.LastWriteTime).First();
                if (file == null)
                {
                    return null;
                }

                String lastCrashLogText = String.Empty;
                try
                {
                    lastCrashLogText = File.ReadAllText(file.FullName);
                }
                catch (IOException)
                {
                    return null;
                }

                return new ErrorAttachmentLog[]
                {
                    ErrorAttachmentLog.AttachmentWithText(lastCrashLogText, file.Name)
                };
            }

            Logger.ExceptionLogged += (exception) =>
            {
                if (ShouldTrackError(exception))
                {
                    Crashes.TrackError(exception);
                    // Crashes.TrackError(exception, null, GetErrorAttachmentsWithLog());
                }
            };

            Crashes.GetErrorAttachments = (ErrorReport _) => GetErrorAttachmentsWithLog();

            AppCenter.Start(PrivateKeys.AppCenterKey, typeof(Analytics), typeof(Crashes));
#endif
        }

        public Int32 GetVersionCode()
        {
            return PackageManager.GetPackageInfo(PackageName, 0).VersionCode;
        }

        public String GetVersionName()
        {
            return PackageManager.GetPackageInfo(PackageName, 0).VersionName;
        }

        internal String GetInternalStoragePath() => FilesDir.AbsolutePath;

        internal String GetLegacyExternalStoragePath()
        {
#pragma warning disable CS0618
            return Path.Join(
                Environment.ExternalStorageDirectory.AbsolutePath,
                Resources.GetString(Resource.String.applicationCompleteName)
            );
#pragma warning restore CS0618
        }

        internal String GetModernExternalStoragePath() => GetExternalFilesDir(null).AbsolutePath;

        public Boolean Initialize(out InitializationStatus status)
        {
            status = InitializationStatus.Success;

            if (IsInitialized)
            {
                return true;
            }

            String legacyStoragePath = GetLegacyExternalStoragePath();
            String modernStoragePath = GetModernExternalStoragePath();

            String modernSchedulesPath = Path.Join(modernStoragePath, SchedulesDirectoryName);

            if (!Directory.Exists(modernSchedulesPath))
            {
                try
                {
                    Directory.CreateDirectory(modernSchedulesPath);
                }
#pragma warning disable CS0168
                catch (IOException exception)
                {
                    status = InitializationStatus.FailedToCreateDirectory;
#if !DEBUG
                    Logger.Log(
                        new ApplicationException(
                            "Error of creating modern schedules directory.", exception));
#endif
                    return false;
                }
#pragma warning restore CS0168
            }

            if (Directory.Exists(legacyStoragePath))
            {
                try
                {
                    FileInfo[] files = new DirectoryInfo(legacyStoragePath).GetFiles("*.json");

                    foreach (FileInfo file in files)
                    {
                        file.MoveTo(Path.Join(modernSchedulesPath, file.Name));
                    }
                }
#pragma warning disable CS0168
                catch (IOException exception)
                {
                    status = InitializationStatus.FailedToMoveSchedules;
#if !DEBUG
                    Logger.Log(
                        new ApplicationException(
                            "Error of moving schedules to modern directory.", exception));
#endif
                    return false;
                }
#pragma warning restore CS0168

                try
                {
                    String legacyLogsPath = Path.Join(legacyStoragePath, "Logs");
                    if (Directory.Exists(legacyLogsPath))
                    {
                        Directory.Delete(legacyLogsPath, true);
                    }

                    Directory.Delete(legacyStoragePath);
                }
#pragma warning disable CS0168
                catch (IOException exception)
                {
                    status = InitializationStatus.FailedToRemoveDirectory;
#if !DEBUG
                    Logger.Log(
                        new ApplicationException(
                            "Error of removing legacy schedules directory.", exception));
#endif
                }
#pragma warning restore CS0168
            }

            LecturersManager = new LecturersManager(modernStoragePath) { Logger = Logger };
            SchedulesManager = new SchedulesManager(modernSchedulesPath) { Logger = Logger };

            return (IsInitialized = true);
        }

        public Task<Boolean> ClearLogsAsync()
        {
            return Task.Run(() =>
            {
                if (!Directory.Exists(_logsDirectoryPath))
                {
                    return true;
                }

                FileInfo[] logsFiles = new DirectoryInfo(_logsDirectoryPath).GetFiles("*.log");
                if (logsFiles.Length == 0)
                {
                    return true;
                }

                DateTime storingTime = DateTime.Today.AddDays(-7);
                try
                {
                    logsFiles.Where(f => f.LastWriteTime < storingTime).ForEach(f => f.Delete());
                }
                catch (IOException)
                {
                    return false;
                }

                return true;
            });
        }

        public Task<Boolean> SaveLogAsync(Boolean isCrashLog = false)
        {
            return Task.Run(() =>
            {
                String timestamp = DateTime.Now.ToString("dd.MM.yyyy HH-mm");
                String prefix = isCrashLog ? "CRASH " : String.Empty;

                if (!Directory.Exists(_logsDirectoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_logsDirectoryPath);
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }

                String fileName = prefix + timestamp + ".log";
                try
                {
                    File.WriteAllText(Path.Join(_logsDirectoryPath, fileName), Logger.ToString());
                }
                catch (IOException)
                {
                    return false;
                }

                return true;
            });
        }

        private readonly String _logsDirectoryPath;
    }
}