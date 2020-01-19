using System;
using System.Diagnostics;
using Newtonsoft.Json;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    [DebuggerDisplay("{From.ToShortTimeString()}, {Week}: {Title}")]
    public class Subject
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public Boolean IsDisplayed { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime From { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime To { get; set; }

        [JsonProperty(Required = Required.Always)]
        public String Audience { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WeekType Week { get; set; }

        [JsonProperty(Required = Required.Always)]
        public String Title { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Group Group { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Lecturer Lecturer { get; set; }

        // Проверяет, принадлежит ли данный момент времени занятию.
        public Boolean IsTimeInside(DateTime time)
        {
            if (time.Hour < From.Hour || time.Hour > To.Hour)
            {
                return false;
            }

            if (time.Hour == From.Hour && time.Minute < From.Minute
                || time.Hour == To.Hour && time.Minute > To.Minute)
            {
                return false;
            }

            return true;
        }
    }
}