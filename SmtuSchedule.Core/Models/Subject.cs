using System;
using System.Diagnostics;
using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    [DebuggerDisplay("{From.ToShortTimeString()}, {Week}: {Title}")]
    public class Subject
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public Boolean IsDisplayed { get; set; }

        //[JsonConverter(typeof(DateTimeConverter), "HH:mm")]
        [JsonProperty(Required = Required.Always)]
        public DateTime From { get; set; }

        //[JsonConverter(typeof(DateTimeConverter), "HH:mm")]
        [JsonProperty(Required = Required.Always)]
        public DateTime To { get; set; }

        [JsonProperty(Required = Required.Always)]
        public String Audience { get; set; }

        //[JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always)]
        public WeekType Week { get; set; }

        [JsonProperty(Required = Required.Always)]
        public String Title { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Lecturer Lecturer { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Lecturer Group { get; set; }

        // Определяет, принадлежит ли данный момент времени занятию.
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