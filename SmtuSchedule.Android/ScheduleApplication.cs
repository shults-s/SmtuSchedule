using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.OS;
using Android.App;
using Android.Runtime;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Android.Utilities;

using Environment = Android.OS.Environment;

namespace SmtuSchedule.Android
{
    [Application]
    internal class ScheduleApplication : Application
    {
        public SchedulesManager Manager { get; private set; }

        public Preferences Preferences { get; private set; }

        public ILogger Logger { get; private set; }

        public Boolean IsInitialized { get; private set; }

        public ScheduleApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Logger = new InMemoryLogger();
            Logger.Log(
                "SmtuSchedule version {0}, running on {1} {2} (Android {4} – API {3}).",
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

            Preferences = new Preferences(this);

            IsInitialized = false;

            _logsDirectoryPath = GetExternalStoragePath() + "/Logs/";
        }

        public override void OnCreate()
        {
            base.OnCreate();

#if !DEBUG
            //if (!Preferences.AllowSendingCrashReports)
            //{
            //    return ;
            //}

            AppCenter.Start(PrivateKeys.AppCenterKey, typeof(Analytics), typeof(Crashes));

            Logger.ExceptionLogged += (e) =>
            {
                if (e is LecturersLoaderException || e is SchedulesLoaderException)
                {
                    Crashes.TrackError(e);
                }
            };

            Crashes.GetErrorAttachments = (ErrorReport report) =>
            {
                ErrorAttachmentLog[] emptyAttachment = new ErrorAttachmentLog[0];

                FileInfo[] files = null;
                try
                {
                    files = new DirectoryInfo(_logsDirectoryPath).GetFiles();
                }
                catch
                {
                    return emptyAttachment;
                }

                if (files.Length == 0)
                {
                    return emptyAttachment;
                }

                FileInfo file = files.OrderByDescending(f => f.LastWriteTime).First();
                if (file == null)
                {
                    return emptyAttachment;
                }

                String lastCrashLogText = String.Empty;
                try
                {
                    lastCrashLogText = File.ReadAllText(file.FullName);
                }
                catch
                {
                    return emptyAttachment;
                }

                return new ErrorAttachmentLog[]
                {
                    ErrorAttachmentLog.AttachmentWithText(lastCrashLogText, file.Name)
                };
            };

            ProcessLifecycleListener listener = new ProcessLifecycleListener();
            listener.Started += () => Analytics.TrackEvent("The application is started");
            listener.Stopped += () => Analytics.TrackEvent("The application is stopped");

            RegisterActivityLifecycleCallbacks(listener);
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

        public String GetExternalStoragePath()
        {
            return String.Format(
                "{0}/{1}/",
                Environment.ExternalStorageDirectory.AbsolutePath,
                Resources.GetString(Resource.String.applicationCompleteName)
            );
        }

        public String GetInternalStoragePath() => FilesDir.AbsolutePath + "/";

        public Boolean Initialize()
        {
            String externalStoragePath = GetExternalStoragePath();
            if (!Directory.Exists(externalStoragePath))
            {
                try
                {
                    Directory.CreateDirectory(externalStoragePath);
                }
                catch
                {
                    return false;
                }
            }

            Manager = new SchedulesManager(externalStoragePath) { Logger = Logger };

            return IsInitialized = true;
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

        private readonly String _logsDirectoryPath;
    }
}