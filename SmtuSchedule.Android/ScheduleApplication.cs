using System;
using System.IO;
using Android.OS;
using Android.App;
using Android.Runtime;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Android.Utilities;

using Environment = Android.OS.Environment;

namespace SmtuSchedule.Android
{
    [Application]
    public sealed class ScheduleApplication : Application
    {
        public SchedulesManager Manager { get; private set; }

        public InMemoryLogger Logger { get; private set; }

        public Preferences Preferences { get; private set; }

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

            // У AndroidEnvironment.UnhandledExceptionRaiser трассировка стека подробнее,
            // чем у AppDomain.CurrentDomain.UnhandledException.
#if !DEBUG
            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                Logger.Log(e.Exception);
                SaveLog(true);
            };
#endif

            Preferences = new Preferences(this);

            IsInitialized = false;
        }

        public override void OnCreate()
        {
            base.OnCreate();

#if !DEBUG && !USER_THAT_IS_WELL_MEOWS
            AppCenter.Start(PrivateKeys.AppCenterKey, typeof(Analytics), typeof(Crashes));

            ProcessLifecycleListener listener = new ProcessLifecycleListener();
            listener.Started += () => Analytics.TrackEvent("The application is started");
            listener.Stopped += () => Analytics.TrackEvent("The application is stopped");
            RegisterActivityLifecycleCallbacks(listener);
#endif
        }

        public Int32 GetVersion()
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

        public void SaveLog(Boolean isCrashLog = false)
        {
            String logsPath = GetExternalStoragePath() + "Logs/";
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            String prefix = isCrashLog ? "CRASH " : String.Empty;

            String fileName = prefix + DateTime.Now.ToString("dd.MM.yyyy HH-mm") + ".log";
            _ = Logger.Save(logsPath + fileName);
        }
    }
}