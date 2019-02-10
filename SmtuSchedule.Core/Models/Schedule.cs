using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    public class Schedule
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,

            // При сборке в релиз с параметром Связывание = Сборки пакета SDK и пользователя,
            // конвертеры, указанные в аттрибутах, пададают при попытке создания объекта.
            // Вероятно, компилятор удаляет эти классы, считая, что они не используются.
            Converters = new JsonConverter[]
            {
                new DateTimeConverter("HH:mm"),
                new StringEnumConverter()
            },
        };

        [JsonProperty(Required = Required.Always)]
        public String DisplayedName { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Int32 ScheduleId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Timetable Timetable { get; set; }

        public String ToJson() => JsonConvert.SerializeObject(this, Settings);

        public static Schedule FromJson(String json)
        {
            return JsonConvert.DeserializeObject<Schedule>(json, Settings);
        }

        public Subject[] GetSubjects(DateTime upperWeekDate, DateTime date)
        {
            Subject[] subjects = Timetable.GetSubjects(date.DayOfWeek);

            if (subjects == null || subjects.Length == 0)
            {
                return null;
            }

            WeekType currentWeek = GetWeekType(upperWeekDate, date);
            subjects = subjects.Where(s => s.Week.HasFlag(currentWeek)).ToArray();

            return (subjects.Length == 0) ? null : subjects;
        }

        private static WeekType GetWeekType(DateTime upperWeekDate, DateTime date)
        {
            DayOfWeek upperWeekDayType = upperWeekDate.DayOfWeek;
            DayOfWeek targetDayType = date.DayOfWeek;

            Int32 numberOfDaysBetweenDates;
            if (targetDayType == upperWeekDayType)
            {
                numberOfDaysBetweenDates = (upperWeekDate - date).Days;
            }
            else
            {
                // Вычисляем день, который относится к той же неделе, что и date,
                // но имеет день недели, совпадающий с днем недели upperWeekDate.
                Int32 difference = upperWeekDayType - targetDayType;
                DateTime normalizedDate = date.AddDays(difference);

                numberOfDaysBetweenDates = (upperWeekDate - normalizedDate).Days;
            }

            Int32 numberOfWeeksBetweenDates = numberOfDaysBetweenDates / 7;
            return (numberOfWeeksBetweenDates % 2) == 0 ? WeekType.Upper : WeekType.Lower;
        }
    }
}