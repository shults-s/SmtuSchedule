using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core.Models
{
    [DebuggerDisplay(
        "[{Monday?.Length}], [{Tuesday?.Length}], [{Wednesday?.Length}], "
        + "[{Thursday?.Length}], [{Friday?.Length}], [{Saturday?.Length}]"
    )]
    public sealed class Timetable
    {
        public Subject[]? Monday { get; set; }

        public Subject[]? Tuesday { get; set; }

        public Subject[]? Wednesday { get; set; }

        public Subject[]? Thursday { get; set; }

        public Subject[]? Friday { get; set; }

        public Subject[]? Saturday { get; set; }

        public void Validate()
        {
            static Boolean IsEmpty(Subject[]? subjects)
            {
                return subjects == null || subjects.Length == 0;
            }

            if (IsEmpty(Monday) && IsEmpty(Tuesday) && IsEmpty(Wednesday)
                && IsEmpty(Thursday) && IsEmpty(Friday)
                && IsEmpty(Saturday))
            {
                throw new ValidationException("Timetable is empty.");
            }

            Monday?.ForEach(s => s.Validate());
            Tuesday?.ForEach(s => s.Validate());
            Wednesday?.ForEach(s => s.Validate());
            Thursday?.ForEach(s => s.Validate());
            Friday?.ForEach(s => s.Validate());
            Saturday?.ForEach(s => s.Validate());
        }

        public Subject[]? GetSubjects(DayOfWeek dayOfWeek) =>
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
            Int32 numberOfSubjects = Monday?.Length ?? 0
                + Tuesday?.Length ?? 0
                + Wednesday?.Length ?? 0
                + Thursday?.Length ?? 0
                + Friday?.Length ?? 0
                + Saturday?.Length ?? 0;

            List<Subject> subjects = new List<Subject>(numberOfSubjects);

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

            return subjects.Select(s => s.Lecturer).Where(l => l != null)!;
        }
    }
}