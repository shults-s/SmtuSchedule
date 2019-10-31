using System;
using System.IO;
using Android.OS;
using Android.App;
using Android.Runtime;
using Android.Content.PM;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Utilities;

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
                GetVersion(),
                Build.Manufacturer,
                Build.Model,
                Build.VERSION.Sdk,
                Build.VERSION.Release
            );

            // У AndroidEnvironment.UnhandledExceptionRaiser трассировка стека подробнее,
            // чем у AppDomain.CurrentDomain.UnhandledException.
            AndroidEnvironment.UnhandledExceptionRaiser += (s, a) =>
            {
                Logger.Log(a.Exception);
                SaveLog(true);
            };

            Preferences = new Preferences(this);

            IsInitialized = false;
        }

        public Int32 GetVersion()
        {
            return PackageManager.GetPackageInfo(PackageName, PackageInfoFlags.Activities)
                .VersionCode;
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