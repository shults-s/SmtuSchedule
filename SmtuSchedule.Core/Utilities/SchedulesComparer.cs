using System;
using System.Globalization;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Utilities
{
    public sealed class SchedulesComparer : IComparer<Schedule>
    {
        private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

        public Int32 Compare(Schedule schedule1, Schedule schedule2)
        {
            if (schedule1 == null)
            {
                return -1;
            }

            if (schedule2 == null)
            {
                return 1;
            }

            if (schedule1 == schedule2)
            {
                return 0;
            }

            // Сначала расписания групп с большим номером, затем с меньшим.
            if (schedule1.Type == ScheduleType.Group && schedule2.Type == ScheduleType.Group)
            {
                return (schedule2.ScheduleId - schedule1.ScheduleId);
            }

            // Сначала расписания групп...
            if (schedule1.Type == ScheduleType.Lecturer && schedule2.Type == ScheduleType.Group)
            {
                return 1;
            }

            // ...затем преподавателей.
            if (schedule1.Type == ScheduleType.Group && schedule2.Type == ScheduleType.Lecturer)
            {
                return -1;
            }

            return String.Compare(schedule1.DisplayedName, schedule2.DisplayedName, false, Culture);
        }
    }
}