using System;
using System.Linq;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core
{
    internal sealed class SchedulesMigrator
    {
        public Boolean HaveNoMigrationErrors { get; private set; }

        public ILogger Logger { get; set; }

        public IEnumerable<Schedule> Migrate(IEnumerable<Schedule> schedules,
            IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            if (schedules == null)
            {
                throw new ArgumentNullException(nameof(schedules));
            }

            HaveNoMigrationErrors = true;

            static Boolean HadToRecoverMissedScheduleType(Schedule schedule)
            {
                if (schedule.Type != ScheduleType.NotSet)
                {
                    return false;
                }

                schedule.Type = (schedule.ScheduleId > 100000) ? ScheduleType.Lecturer : ScheduleType.Group;
                return true;
            }

            Boolean HadToRecoverNonScheduledLecturers(Schedule schedule)
            {

                IScheduleReference[] nonScheduledLecturers = schedule.Timetable.GetLecturers()
                    .Where(l => l.ScheduleId == 0)
                    .ToArray();

                Boolean isScheduleAffected = false;

                if (nonScheduledLecturers.Length == 0)
                {
                    return isScheduleAffected;
                }

                foreach (Lecturer lecturer in nonScheduledLecturers)
                {
                    if (lecturersMap.ContainsKey(lecturer.Name))
                    {
                        isScheduleAffected = true;
                        lecturer.ScheduleId = lecturersMap[lecturer.Name];
                    }
                }

                return isScheduleAffected;
            }

            foreach (Schedule schedule in schedules)
            {
                if (HadToRecoverMissedScheduleType(schedule) || HadToRecoverNonScheduledLecturers(schedule))
                {
                    yield return schedule;
                }
            }
        }
    }
}