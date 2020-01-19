using System;

namespace SmtuSchedule.Core.Interfaces
{
    public interface IScheduleReference
    {
        public String Name { get; set; }

        public Int32 ScheduleId { get; set; }
    }
}