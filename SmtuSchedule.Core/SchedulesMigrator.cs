using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core
{
    internal static class SchedulesMigrator
    {
        public static IEnumerable<Schedule> Migrate(IEnumerable<Schedule> schedules)
        {
            foreach (Schedule schedule in schedules)
            {
                if (schedule.Type != ScheduleType.NotSet)
                {
                    continue;
                }

                schedule.Type = (schedule.ScheduleId > 10000) ? ScheduleType.Lecturer : ScheduleType.Group;

                yield return schedule;
            }
        }
    }
}