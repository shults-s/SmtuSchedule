using System;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Models
{
    public sealed class Group : IScheduleReference
    {
        public Int32 ScheduleId { get; set; }

        public String? Name { get; set; }

        [Android.Runtime.Preserve]
        public Group()
        {
        }

        public Group(String? name, Int32 scheduleId)
        {
            Name = name;
            ScheduleId = scheduleId;
        }
    }
}