using System;
using System.Threading.Tasks;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Android.Enumerations;

namespace SmtuSchedule.Android.Interfaces
{
    internal interface IApplicationManager
    {
        ILecturersManager LecturersManager { get; }

        ISchedulesManager SchedulesManager { get; }

        IPreferences Preferences { get; }

        ILogger Logger { get; }

        Boolean IsInitialized { get; }

        Int32 GetVersionCode();

        String GetVersionName();

        Task<Boolean> ClearLogsAsync();

        Task<Boolean> SaveLogAsync(Boolean isCrashLog);

        Boolean Initialize(out InitializationStatus status);
    }
}