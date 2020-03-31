using System;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Utilities
{
    public sealed class SchedulesComparer : IComparer<Schedule>
    {
        public Int32 Compare(Schedule schedule1, Schedule schedule2)
        {
            // Сначала расписания групп с большим номером, затем с меньшим.
            if (schedule1.Type == ScheduleType.Group && schedule2.Type == ScheduleType.Group)
            {
                return (schedule2.ScheduleId - schedule1.ScheduleId);
            }

            // Сначала расписания групп, затем преподавателей.
            if (schedule1.Type == ScheduleType.Lecturer && schedule2.Type == ScheduleType.Group)
            {
                return 1;
            }

            if (schedule1.Type == ScheduleType.Group && schedule2.Type == ScheduleType.Lecturer)
            {
                return -1;
            }

            return schedule1.DisplayedName.CompareTo(schedule2.DisplayedName);
        }
    }
}