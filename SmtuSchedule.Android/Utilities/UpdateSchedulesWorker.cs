using System;
using AndroidX.Work;
using Android.Content;
using SmtuSchedule.Core;
using SmtuSchedule.Android.Exceptions;

namespace SmtuSchedule.Android.Utilities
{
    class UpdateSchedulesWorker : Worker
    {
        public UpdateSchedulesWorker(Context context, WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
            _application = ApplicationContext as SmtuScheduleApplication;

            _localSchedulesManager = new SchedulesManager(
                _application.GetModernExternalStoragePath(),
                SmtuScheduleApplication.SchedulesDirectoryName
            );
        }

        public override Result DoWork()
        {
            try
            {
                _localSchedulesManager.ReadSchedulesAsync().Wait();
                _localSchedulesManager.UpdateSchedulesAsync().Wait();
            }
            catch (Exception exception)
            {
                _application.Logger.Log(
                    new WorkerException("Error of idle updating schedules.", exception));
            }

            return Result.InvokeSuccess();
        }

        private readonly SmtuScheduleApplication _application;
        private readonly SchedulesManager _localSchedulesManager;
    }
}