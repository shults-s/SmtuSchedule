using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Converters;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    public sealed class Schedule
    {
        // private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
        // {
        //     NullValueHandling = NullValueHandling.Ignore,
        //     Formatting = Formatting.Indented,
        //
        //     // При сборке в релиз с параметром Связывание = Сборки пакета SDK и пользователя,
        //     // конвертеры, указанные в атрибутах, падают при попытке создания объекта.
        //     // Вероятно, компилятор удаляет эти классы, считая, что они не используются.
        //     Converters = new JsonConverter[]
        //     {
        //          new DateTimeConverter("HH:mm"),
        //          new StringEnumConverter()
        //     }
        // };

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreNullValues = true,
            WriteIndented = true
        };

        [JsonIgnore]
        public Boolean IsActual => LastUpdate >= DateTime.Now.AddHours(-12);

        // [JsonProperty(Required = Required.Always)]
        public String DisplayedName { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public Int32 ScheduleId { get; set; }

        // [JsonProperty(Required = Required.Default)]
        public ScheduleType Type { get; set; }

        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime LastUpdate { get; set; }

        // [JsonProperty(Required = Required.Always)]
        public Timetable Timetable { get; set; }

        static Schedule()
        {
            // Библиотечный конвертер указан не в атрибутах по причине, описанной выше.
            Options.Converters.Add(new JsonStringEnumConverter());
        }

        public void Validate()
        {
            if (String.IsNullOrEmpty(DisplayedName))
            {
                throw new ValidationException("Property DisplayedName must be set.");
            }

            if (ScheduleId == default(Int32))
            {
                throw new ValidationException("Property ScheduleId must be set.");
            }

            Timetable.Validate();
        }

        // public String ToJson() => JsonConvert.SerializeObject(this, Settings);

        // public static Schedule FromJson(String json)
        // {
        //     return JsonConvert.DeserializeObject<Schedule>(json, Settings);
        // }

        public String ToJson() => JsonSerializer.Serialize<Schedule>(this, Options);

        public static Schedule FromJson(String json)
        {
            return JsonSerializer.Deserialize<Schedule>(json, Options);
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

        public String GetFormattedLastUpdate() => LastUpdate.ToString("dd.MM.yyyy HH:mm");
    }
}