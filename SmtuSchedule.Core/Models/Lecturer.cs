using System;
using Newtonsoft.Json;

namespace SmtuSchedule.Core.Models
{
    public class Lecturer
    {
        [JsonProperty(Required = Required.Always)]
        public Int32 ScheduleId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public String Name { get; set; }

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