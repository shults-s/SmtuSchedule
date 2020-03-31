using System;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Models
{
    public sealed class Lecturer : IScheduleReference
    {
        public Int32 ScheduleId { get; set; }

        public String Name { get; set; }

        [Android.Runtime.Preserve]
        public Lecturer()
        {
        }

        public Lecturer(String name, Int32 scheduleId)
        {
            Name = name;
            ScheduleId = scheduleId;
        }

        public static String GetShortName(String name)
        {
            String[] parts = name.Split();

            if (parts.Length == 2)
            {
                return $"{parts[0]} {parts[1][0]}.";
            }

            return $"{parts[0]} {parts[1][0]}. {parts[2][0]}.";
        }
    }
}