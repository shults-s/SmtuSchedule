using System;
using System.Linq;
using System.Collections.Generic;
// using Newtonsoft.Json;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Models
{
    public class Timetable
    {
        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Monday { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Tuesday { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Wednesday { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Thursday { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Friday { get; set; }

        // [JsonProperty(Required = Required.DisallowNull)]
        public Subject[] Saturday { get; set; }

        public Subject[] GetSubjects(DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                DayOfWeek.Monday    => Monday,
                DayOfWeek.Tuesday   => Tuesday,
                DayOfWeek.Wednesday => Wednesday,
                DayOfWeek.Thursday  => Thursday,
                DayOfWeek.Friday    => Friday,
                DayOfWeek.Saturday  => Saturday,
                _ => null
            };

        public void SetSubjects(DayOfWeek dayOfWeek, Subject[] subjects)
        {
            switch (dayOfWeek)
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

                default:
                    throw new ArgumentException("Sunday is a day off.");
            }
        }

        public IEnumerable<IScheduleReference> GetLecturers()
        {
            List<Subject> subjects = new List<Subject>();

            if (Monday != null)
            {
                subjects.AddRange(Monday);
            }

            if (Tuesday != null)
            {
                subjects.AddRange(Tuesday);
            }

            if (Wednesday != null)
            {
                subjects.AddRange(Wednesday);
            }

            if (Thursday != null)
            {
                subjects.AddRange(Thursday);
            }

            if (Friday != null)
            {
                subjects.AddRange(Friday);
            }

            if (Saturday != null)
            {
                subjects.AddRange(Saturday);
            }

            return subjects.Select(s => s.Lecturer).Where(l => l != null);
        }
    }
}