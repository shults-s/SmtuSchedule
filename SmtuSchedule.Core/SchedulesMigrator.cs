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
        public Boolean HaveMigrationErrors { get; private set; }

        public ILogger Logger { get; set; }

        public SchedulesMigrator(IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            _lecturersMap = lecturersMap ?? throw new ArgumentNullException(nameof(lecturersMap));
        }

        public IEnumerable<Schedule> Migrate(IEnumerable<Schedule> schedules)
        {
            if (schedules == null)
            {
                throw new ArgumentNullException(nameof(schedules));
            }

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

            Boolean RecoverNonScheduledLecturers(Schedule schedule)
            {
                Boolean isScheduleAffected = false;

                IScheduleReference[] nonScheduledLecturers = schedule.Timetable.GetLecturers()
                    .Where(l => l.ScheduleId == 0)
                    .ToArray();

                if (nonScheduledLecturers.Length == 0)
                {
                    return false;
                }

                foreach (Lecturer lecturer in nonScheduledLecturers)
                {
                    if (_lecturersMap.ContainsKey(lecturer.Name))
                    {
                        isScheduleAffected = true;
                        lecturer.ScheduleId = _lecturersMap[lecturer.Name];
                    }
                }

                return isScheduleAffected;
            }

            foreach (Schedule schedule in schedules)
            {
                if (RecoverMissedScheduleType(schedule) || RecoverNonScheduledLecturers(schedule))
                {
                    yield return schedule;
                }
            }
        }

        private readonly IReadOnlyDictionary<String, Int32> _lecturersMap;
    }
}