using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    public sealed class Schedule
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreNullValues = true,
            WriteIndented = true
        };

        [JsonIgnore]
        public Boolean IsActual => LastUpdate >= DateTime.Now.AddHours(-12);

        public String DisplayedName { get; set; }

        public Int32 ScheduleId { get; set; }

        public ScheduleType Type { get; set; }

        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime LastUpdate { get; set; }

        public Timetable Timetable { get; set; }

        static Schedule()
        {
            // При сборке с параметром 'Связывание' = 'Сборки пакета SDK и пользователя',
            // конвертеры, указанные в атрибутах, падают при создании объекта.
            // Вероятно, компилятор удаляет эти классы, считая, что они не используются.
            // Поскольку конвертер JsonStringEnumConverter библиотечный, защитить его
            // атрибутом Android.Runtime.Preserve невозможно, поэтому единственное что
            // можно сделать – создать статическую ссылку.
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