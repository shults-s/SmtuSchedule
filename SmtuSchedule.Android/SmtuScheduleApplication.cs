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
using SmtuSchedule.Android.Exceptions;
using SmtuSchedule.Android.Enumerations;

using Environment = Android.OS.Environment;

namespace SmtuSchedule.Android
{
    [Application]
    internal class SmtuScheduleApplication : Application
    {
        public const String SchedulesDirectoryName = "Schedules";

        public SchedulesManager Manager { get; private set; }

        public Preferences Preferences { get; private set; }

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
                Build.VERSION.SdkInt,
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

            Preferences = new Preferences(this);

            IsInitialized = false;

            _logsDirectoryPath = GetModernExternalStoragePath() + "/Logs/";
        }

        private Boolean ShouldTrackError(Exception exception) =>
            exception switch
            {
                LecturersDownloaderException _ => true,
                SchedulesDownloaderException _ => true,
                LecturersRepositoryException _ => true,
                SchedulesRepositoryException _ => true,
                ApplicationException         _ => true,
                WorkerException              _ => true,
                UiException                  _ => true,
                _ => false
            };

        private ErrorAttachmentLog[] GetErrorAttachmentsWithLog()
        {
            FileInfo[] files = null;
            try
            {
                files = new DirectoryInfo(_logsDirectoryPath).GetFiles("*.log");
            }
            catch
            {
                return null;
            }

            if (files.Length == 0)
            {
                return null;
            }

            FileInfo file = files.OrderByDescending(f => f.LastWriteTime).First();
            if (file == null)
            {
                return null;
            }

            String lastCrashLogText = String.Empty;
            try
            {
                lastCrashLogText = File.ReadAllText(file.FullName);
            }
            catch
            {
                return null;
            }

            return new ErrorAttachmentLog[]
            {
                ErrorAttachmentLog.AttachmentWithText(lastCrashLogText, file.Name)
            };
        }

        public override void OnCreate()
        {
            base.OnCreate();

            NotificationUtilities.CreateNotificationChannels(this);

#if !DEBUG
            ApplicationLifecycleListener listener = new ApplicationLifecycleListener();
            listener.Started += () => Analytics.TrackEvent("The application is started");
            listener.Stopped += () => Analytics.TrackEvent("The application is stopped");
            RegisterActivityLifecycleCallbacks(listener);

            Logger.ExceptionLogged += (e) =>
            {
                if (ShouldTrackError(e))
                {
                    Crashes.TrackError(e);
                    // Crashes.TrackError(e, null, GetErrorAttachmentsWithLog());
                }
            };

            Crashes.GetErrorAttachments = (ErrorReport _) => GetErrorAttachmentsWithLog();

            AppCenter.Start(PrivateKeys.AppCenterKey, typeof(Analytics), typeof(Crashes));
#endif
        }

        public Int64 GetVersionCode()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            {
#pragma warning disable CS0618
                return PackageManager.GetPackageInfo(PackageName, 0).VersionCode;
#pragma warning restore CS0618
            }

            return PackageManager.GetPackageInfo(PackageName, 0).LongVersionCode;
        }

        public String GetVersionName()
        {
            return PackageManager.GetPackageInfo(PackageName, 0).VersionName;
        }

//         public String GetLegacyExternalStoragePath()
//         {
// #pragma warning disable CS0618
//             return String.Format(
//                 "{0}/{1}/",
//                 Environment.ExternalStorageDirectory.AbsolutePath,
//                 Resources.GetString(Resource.String.applicationCompleteName)
//             );
// #pragma warning restore CS0618
//         }

        public String GetModernExternalStoragePath()
        {
            return Context.GetExternalFilesDir(null)?.AbsolutePath + "/";
        }

        public String GetInternalStoragePath() => FilesDir.AbsolutePath + "/";

        public Boolean Initialize(out InitializationStatus status)
        {
            String modernStoragePath = GetModernExternalStoragePath();
            String modernSchedulesPath = $"{modernStoragePath}/{SchedulesDirectoryName}/";

            status = InitializationStatus.Success;

            if (!Directory.Exists(modernSchedulesPath))
            {
                try
                {
                    Directory.CreateDirectory(modernSchedulesPath);
                }
                catch(Exception exception)
                {
                    status = InitializationStatus.FailedToCreateDirectory;
#if !DEBUG
                    Logger.Log(
                        new ApplicationException(
                            "Error of creating modern schedules directory.", exception));
#endif
                    return false;
                }
            }

//             String legacySchedulesPath = GetLegacyExternalStoragePath();
//
//             if (Directory.Exists(legacySchedulesPath))
//             {
//                 try
//                 {
//                     FileInfo[] files = new DirectoryInfo(legacySchedulesPath)
//                         .GetFiles("*.json");
//
//                     foreach (FileInfo file in files)
//                     {
//                         file.MoveTo(modernSchedulesPath + file.Name);
//                     }
//                 }
//                 catch(Exception exception)
//                 {
//                     status = InitializationStatus.FailedToMoveSchedules;
// #if !DEBUG
//                     Logger.Log(
//                         new ApplicationException(
//                             "Error of moving schedules to modern directory.", exception));
// #endif
//                     return false;
//                 }
//
//                 try
//                 {
//                     String legacyLogsPath = legacySchedulesPath + "/Logs/";
//                     if (Directory.Exists(legacyLogsPath))
//                     {
//                         Directory.Delete(legacyLogsPath, true);
//                     }
//
//                     Directory.Delete(legacySchedulesPath);
//                 }
//                 catch (Exception exception)
//                 {
//                     status = InitializationStatus.FailedToRemoveDirectory;
// #if !DEBUG
//                     Logger.Log(
//                         new ApplicationException(
//                             "Error of removing legacy schedules directory.", exception));
// #endif
//                 }
//             }

            Manager = new SchedulesManager(modernStoragePath, SchedulesDirectoryName)
            {
                Logger = Logger
            };

            return (IsInitialized = true);
        }

        public Task SaveLogAsync(Boolean isCrashLog = false)
        {
            return Task.Run(() =>
            {
                String timestamp = DateTime.Now.ToString("dd.MM.yyyy HH-mm");
                String prefix = isCrashLog ? "CRASH " : String.Empty;

                if (!Directory.Exists(_logsDirectoryPath))
                {
                    Directory.CreateDirectory(_logsDirectoryPath);
                }

                String fileName = prefix + timestamp + ".log";
                File.WriteAllText(_logsDirectoryPath + fileName, Logger.ToString());
            });
        }

        public Task ClearLogsAsync()
        {
            return Task.Run(() =>
            {
                FileInfo[] files = new DirectoryInfo(_logsDirectoryPath).GetFiles("*.log");
                if (files.Length == 0)
                {
                    return ;
                }

                DateTime storingTime = DateTime.Today.AddDays(-7);
                files.Where(f => f.LastWriteTime < storingTime).ForEach(f => f.Delete());
            });
        }

        // Этот метод ненадежен, поскольку в случае с эмулятором Android на котором установлены
        // Google APIs, но не установлен Google Play Store метод вернет true, но при попытке
        // открыть URL вида market:// будет выброшено исключение ActivityNotFoundException.
        // В данный момент целесообразно использовать StartActivity(...) в блоке try-catch.
        public Boolean IsPlayStoreInstalled()
        {
            try
            {
                PackageManager.GetPackageInfo(GooglePlayServicesUtil.GooglePlayStorePackage, 0);
                return true;
            }
            catch (PackageManager.NameNotFoundException)
            {
                return false;
            }
        }

        private readonly String _logsDirectoryPath;
    }
}