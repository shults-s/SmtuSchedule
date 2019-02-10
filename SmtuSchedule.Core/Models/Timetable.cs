using System;
using Newtonsoft.Json;

namespace SmtuSchedule.Core.Models
{
    public class Timetable
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Monday { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Tuesday { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Wednesday { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Thursday { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Friday { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Saturday { get; set; }

        public Subject[] GetSubjects(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday:
                    return Monday;

                case DayOfWeek.Tuesday:
                    return Tuesday;

                case DayOfWeek.Wednesday:
                    return Wednesday;

                case DayOfWeek.Thursday:
                    return Thursday;

                case DayOfWeek.Friday:
                    return Friday;

                case DayOfWeek.Saturday:
                    return Saturday;

                default:
                    return null;
            }
        }

        public void SetSubjects(DayOfWeek day, Subject[] subjects)
        {
            switch (day)
            {
                case DayOfWeek.Monday:
                    Monday = subjects;
                    break;

                case DayOfWeek.Tuesday:
                    Tuesday = subjects;
                    break;

                case DayOfWeek.Wednesday:
                    Wednesday = subjects;
                    break;

                case DayOfWeek.Thursday:
                    Thursday = subjects;
                    break;

                case DayOfWeek.Friday:
                    Friday = subjects;
                    break;

                case DayOfWeek.Saturday:
                    Saturday = subjects;
                    break;
            }
        }
    }
}