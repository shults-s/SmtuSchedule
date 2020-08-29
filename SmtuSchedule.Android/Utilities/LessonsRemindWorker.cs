using System;
using System.Linq;
using System.Collections.Generic;
using AndroidX.Work;
using Android.App;
using Android.Content;
using SmtuSchedule.Core;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Android.Exceptions;
using SmtuSchedule.Android.Enumerations;

namespace SmtuSchedule.Android.Utilities
{
    internal class LessonsRemindWorker : Worker
    {
        public LessonsRemindWorker(Context context, WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
            _context = context;
            _application = ApplicationContext as SmtuScheduleApplication;

            _localSchedulesManager = new SchedulesManager(
                _application.GetModernExternalStoragePath(),
                SmtuScheduleApplication.SchedulesDirectoryName
            );

            _currentUtcUnixTime = GetDateTimeInUtcUnixTime(DateTime.UtcNow);
        }

        public override Result DoWork()
        {
            try
            {
                Work();
            }
            catch (Exception exception)
            {
                _application.Logger.Log(
                    new WorkerException("Error of scheduling notifications about the lesson.", exception));
            }

            return Result.InvokeSuccess();
        }

        private Schedule GetScheduleById(Int32 scheduleId)
        {
            _localSchedulesManager.ReadSchedulesAsync().Wait();

            IReadOnlyDictionary<Int32, Schedule> schedules = _localSchedulesManager.Schedules;
            return schedules.ContainsKey(scheduleId) ? schedules[scheduleId] : null;
        }

        private Subject GetEarliestLesson(Schedule schedule, DateTime date)
        {
            DateTime upperWeekDate = _application.Preferences.UpperWeekDate;
            if (upperWeekDate == default(DateTime))
            {
                return null;
            }

            Subject[] subjects = schedule.GetSubjects(upperWeekDate, date);

            Int32 leastFromHour = subjects.Min(s => s.From.Hour);
            return subjects.FirstOrDefault(s => s.From.Hour <= leastFromHour);
        }

        private Int64 GetDateTimeInUtcUnixTime(DateTime dateTime)
        {
            return (Int64)(dateTime.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
        }

        private void Work()
        {
            Schedule schedule = GetScheduleById(_application.Preferences.CurrentScheduleId);
            if (schedule == null)
            {
                return ;
            }

            DateTime lessonDate = DateTime.Today.AddDays(1).Date;

            Subject subject = GetEarliestLesson(schedule, lessonDate);
            if (subject == null)
            {
                return ;
            }

            LessonRemindTime remindTimes = _application.Preferences.LessonRemindTimes;
            if (remindTimes == LessonRemindTime.Never)
            {
                return ;
            }

            String[] timesPlaceholders = _context.Resources.GetStringArray(
                Resource.Array.lessonRemindTimesPlaceholders);

            String title = _context.Resources.GetString(
                Resource.String.upcomingLessonRemindNotificationTitle);

            String messageFormat = _context.Resources.GetString(
                Resource.String.upcomingLessonRemindNotificationMessage);

            Int64 ComputeNotificationUnixTimeFromRelativeTime(Int32 relativeTimeInMinutes)
            {
                DateTime notificationDateTime = lessonDate.AddHours(subject.From.Hour)
                    .AddMinutes(subject.From.Minute)
                    .AddMinutes(relativeTimeInMinutes);

                return GetDateTimeInUtcUnixTime(notificationDateTime);
            }

            String FormatMessageWithTimePlaceholder(String time)
            {
                return String.Format(
                    messageFormat,
                    schedule.DisplayedName,
                    time,
                    subject.Auditorium,
                    subject.Title
                );
            }

            if (remindTimes.HasFlag(LessonRemindTime.FiveMinutes))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[0]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-5);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 331);
            }

            if (remindTimes.HasFlag(LessonRemindTime.TenMinutes))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[1]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-10);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 332);
            }

            if (remindTimes.HasFlag(LessonRemindTime.ThirtyMinutes))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[2]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-30);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 333);
            }

            if (remindTimes.HasFlag(LessonRemindTime.OneHour))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[3]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-60);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 334);
            }

            if (remindTimes.HasFlag(LessonRemindTime.OneAndHalfHour))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[4]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-90);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 335);
            }

            if (remindTimes.HasFlag(LessonRemindTime.ThreeHours))
            {
                String message = FormatMessageWithTimePlaceholder(timesPlaceholders[5]);
                Int64 time = ComputeNotificationUnixTimeFromRelativeTime(-180);
                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 336);
            }

            if (remindTimes.HasFlag(LessonRemindTime.Midnight))
            {
                String message = FormatMessageWithTimePlaceholder(
                    String.Format(timesPlaceholders[6], subject.From.ToString("HH:mm")));

                Int64 time = GetDateTimeInUtcUnixTime(lessonDate);

                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 337);
            }

            if (remindTimes.HasFlag(LessonRemindTime.OvernightAtNineHours))
            {
                String message = FormatMessageWithTimePlaceholder(
                    String.Format(timesPlaceholders[7], subject.From.ToString("HH:mm")));

                Int64 time = GetDateTimeInUtcUnixTime(lessonDate.AddDays(-1).AddHours(21));

                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 338);
            }

            if (remindTimes.HasFlag(LessonRemindTime.OvernightAtElevenHours))
            {
                String message = FormatMessageWithTimePlaceholder(
                    String.Format(timesPlaceholders[8], subject.From.ToString("HH:mm")));

                Int64 time = GetDateTimeInUtcUnixTime(lessonDate.AddDays(-1).AddHours(23));

                ScheduleNotification(time, title, message, schedule.ScheduleId, lessonDate, 339);
            }
        }

        private void ScheduleNotification(Int64 whenDisplayNotificationInUtcUnixTime, String title,
            String message, Int32 scheduleId, DateTime lessonDate, Int32 requestCode)
        {
            if (whenDisplayNotificationInUtcUnixTime <= _currentUtcUnixTime)
            {
                return ;
            }

            AlarmManager alarmManager = _context.GetSystemService(Context.AlarmService)
                as AlarmManager;

            Intent intent = new Intent(_context, typeof(LessonsRemindPublisher));
            intent.PutExtra(
                LessonsRemindPublisher.IntentNotificationScheduleIdKey,
                scheduleId.ToString()
            );
            intent.PutExtra(
                LessonsRemindPublisher.IntentNotificationLessonDateKey,
                lessonDate.Ticks.ToString()
            );
            intent.PutExtra(LessonsRemindPublisher.IntentNotificationTitleKey, title);
            intent.PutExtra(LessonsRemindPublisher.IntentNotificationMessageKey, message);

            PendingIntent pendingIntent = PendingIntent.GetBroadcast(
                _context,
                requestCode,
                intent,
                PendingIntentFlags.UpdateCurrent
            );

            Int64 timeInMilliseconds = whenDisplayNotificationInUtcUnixTime * 1000;
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, timeInMilliseconds, pendingIntent);
        }

        private readonly Int64 _currentUtcUnixTime;

        private readonly Context _context;
        private readonly SmtuScheduleApplication _application;
        private readonly SchedulesManager _localSchedulesManager;
    }
}