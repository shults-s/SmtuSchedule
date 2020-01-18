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
            }
        };

        [JsonProperty(Required = Required.Always)]
        public String DisplayedName { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Int32 ScheduleId { get; set; }

        [JsonProperty(Required = Required.Default)]
        public ScheduleType Type { get; set; }

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

            WeekType currentWeekType = date.GetWeekType(upperWeekDate);
            subjects = subjects.Where(s => s.Week.HasFlag(currentWeekType)).ToArray();

            return (subjects.Length == 0) ? null : subjects;
        }
    }
}