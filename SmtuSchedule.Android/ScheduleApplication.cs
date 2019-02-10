using System;
using System.IO;
using Android.OS;
using Android.App;
using Android.Runtime;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Utilities;

using Environment = Android.OS.Environment;

namespace SmtuSchedule.Android
{
    [Application]
    public class ScheduleApplication : Application
    {
        public Boolean IsInitialized { get; private set; }

        public SchedulesManager Manager { get; set; }

        public Preferences Preferences { get; set; }

        // Bugfix: Unable to activate instance of type ... from native handle ...
        public ScheduleApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            IsInitialized = false;
            _logger = new InMemoryLogger();
        }

        ~ScheduleApplication() => _logger.Dispose();

        public String GetVersion()
        {
            return Resources.GetString(Resource.String.applicationVersion);
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

        public void SaveLog()
        {
            String logsPath = GetExternalStoragePath() + "Logs/";

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            String fileName = DateTime.Now.ToString("dd.MM.yyyy HH-mm") + ".log";
            _ = _logger.Save(logsPath + fileName);
        }

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

            Preferences = new Preferences(this);

            Manager = new SchedulesManager(externalStoragePath);
            Manager.SetLogger(_logger);

            _logger.Log(
                "SmtuSchedule version {0}, running on {1} {2} (Android {4} – API {3}).",
                GetVersion(),
                Build.Manufacturer,
                Build.Model,
                Build.VERSION.Sdk,
                Build.VERSION.Release
            );

            return IsInitialized = true;
        }

        private InMemoryLogger _logger;
    }
}