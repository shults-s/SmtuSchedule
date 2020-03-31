using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Models
{
    [DebuggerDisplay("{From.ToShortTimeString()}, {Week}: {Title} @ {Group?.Name ?? Lecturer?.Name}")]
    public sealed class Subject
    {
        public Boolean IsDisplayed { get; set; }

        [JsonConverter(typeof(JsonTimeConverter))]
        public DateTime From { get; set; }

        [JsonConverter(typeof(JsonTimeConverter))]
        public DateTime To { get; set; }

        [JsonPropertyName("Audience")]
        public String Auditorium { get; set; }

        public WeekType Week { get; set; }

        public String Title { get; set; }

        public Group Group { get; set; }

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

        public void Validate()
        {
            if (From == default(DateTime) || To == default(DateTime))
            {
                throw new ValidationException("Both properties From and To must be set.");
            }

            if (String.IsNullOrEmpty(Auditorium))
            {
                throw new ValidationException("Property Auditorium must be set.");
            }

            if (String.IsNullOrEmpty(Title))
            {
                throw new ValidationException("Property Title must be set.");
            }

            if (Group != null && Lecturer != null)
            {
                throw new ValidationException("Only one of properties Lecturer or Group must be set.");
            }

            (Group as IScheduleReference)?.Validate();
            (Lecturer as IScheduleReference)?.Validate();
        }
    }
}