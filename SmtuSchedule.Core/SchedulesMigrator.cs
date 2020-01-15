using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core
{
    internal class SchedulesMigrator
    {
        public Boolean HaveMigrationErrors { get; private set; }

        public ILogger Logger { get; set; }

        public SchedulesMigrator(Func<Task<IReadOnlyDictionary<String, Int32>>> lecturersDownloaderCallback)
        {
            _lecturersDownloaderCallback = lecturersDownloaderCallback;
        }

        public async Task<IEnumerable<Schedule>> MigrateAsync(IEnumerable<Schedule> schedules)
        {
            HaveMigrationErrors = false;

            static Boolean RecoverMissedScheduleType(Schedule schedule)
            {
                if (schedule.Type != ScheduleType.NotSet)
                {
                    return false;
                }

                schedule.Type = (schedule.ScheduleId > 100000) ? ScheduleType.Lecturer : ScheduleType.Group;
                return true;
            }

            IReadOnlyDictionary<String, Int32> lecturers = null;
            Boolean failedToLoadLecturers = false;

            async Task<Boolean> RecoverNonScheduledLecturersAsync(Schedule schedule)
            {
                if (failedToLoadLecturers)
                {
                    return false;
                }

                Boolean isScheduleAffected = false;

                Lecturer[] nonScheduledLecturers = schedule.Timetable.GetLecturers()
                    .Where(l => l.ScheduleId == 0)
                    .ToArray();

                if (nonScheduledLecturers.Length == 0)
                {
                    return false;
                }

                if (lecturers == null)
                {
                    lecturers = await _lecturersDownloaderCallback().ConfigureAwait(false);
                }

                if (lecturers == null)
                {
                    failedToLoadLecturers = true;
                    HaveMigrationErrors = true;

                    return false;
                }

                foreach (Lecturer lecturer in nonScheduledLecturers)
                {
                    if (lecturers.ContainsKey(lecturer.Name))
                    {
                        isScheduleAffected = true;
                        lecturer.ScheduleId = lecturers[lecturer.Name];
                    }
                }

                return isScheduleAffected;
            }

            List<Schedule> affectedSchedules = new List<Schedule>();

            foreach (Schedule schedule in schedules)
            {
                if (RecoverMissedScheduleType(schedule) || await RecoverNonScheduledLecturersAsync(schedule))
                {
                    affectedSchedules.Add(schedule);
                }
            }

            return affectedSchedules;
        }

        private readonly Func<Task<IReadOnlyDictionary<String, Int32>>> _lecturersDownloaderCallback;
    }
}