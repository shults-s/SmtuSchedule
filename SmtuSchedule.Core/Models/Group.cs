using System;
// using Newtonsoft.Json;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Models
{
    public class Group : IScheduleReference
    {
        // [JsonProperty(Required = Required.Always)]
        public Int32 ScheduleId { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public String Name { get; set; }

        [Android.Runtime.Preserve]
        public Group()
        {
        }

        public Group(String name, Int32 scheduleId)
        {
            Name = name;
            ScheduleId = scheduleId;
        }
    }
}