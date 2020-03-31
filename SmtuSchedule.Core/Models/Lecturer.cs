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

        public static String GetShortName(String fullName)
        {
            if (String.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(fullName));
            }

            String[] parts = fullName.Split();

            if (parts.Length < 2 || parts.Length > 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fullName), "String must match the format: 'LastName FirstName[ MiddleName]'.");
            }

            Boolean hasMiddleName = (parts.Length == 3);
            return hasMiddleName ? $"{parts[0]} {parts[1][0]}. {parts[2][0]}." : $"{parts[0]} {parts[1][0]}.";
        }
    }
}