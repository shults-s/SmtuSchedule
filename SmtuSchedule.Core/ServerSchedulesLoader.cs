using System;
using System.Linq;
using System.Net.Http;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Core.Enumerations;

using Group = SmtuSchedule.Core.Models.Group;

namespace SmtuSchedule.Core
{
    internal class ServerSchedulesLoader
    {
        private static readonly (String, String)[] ContentTitleReplacementPatterns = new (String, String)[]
        {
            (@"Расписание занятий преподавателя (?<lecturer>[\w\s]+)", "${lecturer}"),
            (@"Расписание занятий группы (?<group>\d+)", "Группа ${group}")
        };

        private static readonly Dictionary<String, DayOfWeek> Days = new Dictionary<String, DayOfWeek>()
        {
            ["Понедельник"] = DayOfWeek.Monday,
            ["Вторник"] = DayOfWeek.Tuesday,
            ["Среда"] = DayOfWeek.Wednesday,
            ["Четверг"] = DayOfWeek.Thursday,
            ["Пятница"] = DayOfWeek.Friday,
            ["Суббота"] = DayOfWeek.Saturday
        };

        private static readonly Dictionary<String, WeekType> Weeks = new Dictionary<String, WeekType>()
        {
            ["Верхняя неделя"] = WeekType.Upper,
            ["Нижняя неделя"] = WeekType.Lower
        };

        private static readonly Dictionary<String, String> Studies = new Dictionary<String, String>()
        {
            ["Лабораторное занятие"] = "лабораторная",
            ["Практическое занятие"] = "практика",
            ["Лекция"] = "лекция",
        };

        private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

        private const String GroupNamePrefixInLecturerSchedule = "Группа ";

        public Boolean HaveDownloadingErrors { get; private set; }

        public ILogger Logger { get; set; }

        public ServerSchedulesLoader(IReadOnlyDictionary<String, Int32> lecturers) => _lecturers = lecturers;

        public async Task<Dictionary<Int32, Schedule>> DownloadAsync(IEnumerable<Int32> schedulesIds,
            Boolean shouldDownloadRelatedLecturersSchedules)
        {
            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>();
            HaveDownloadingErrors = false;

            if (_lecturers == null)
            {
                Logger?.Log(
                    new SchedulesLoaderException(
                        "Provided lecturers list is null, therefore, in any loaded schedule, the "
                        + "lecturers id's will be zero. So, switching between schedules will be impossible."
                    )
                );

                HaveDownloadingErrors = true;
                return schedules;
            }

            async Task<Boolean> DownloadAsync(IEnumerable<Int32> schedulesIdsLocal)
            {
                foreach (Int32 scheduleId in schedulesIdsLocal)
                {
                    try
                    {
                        Schedule schedule = await DownloadScheduleAsync(scheduleId).ConfigureAwait(false);
                        schedule.Validate();

                        schedules[scheduleId] = schedule;
                    }
                    catch (HttpRequestException exception)
                    {
                        HaveDownloadingErrors = true;
                        Logger?.Log(
                            new SchedulesLoaderException(
                                $"Error of downloading schedules: network error.", exception)
                        );

                        return true;
                    }
                    catch (Exception exception)
                    {
                        HaveDownloadingErrors = true;
                        Logger?.Log(
                            new SchedulesLoaderException(
                                $"Error of downloading schedule with id {scheduleId}.", exception)
                        );
                    }
                }

                return false;
            }

            static IEnumerable<Int32> GetRelatedLecturersSchedulesIds(IEnumerable<Schedule> schedulesLocal)
            {
                List<Int32> schedulesIdsLocal = new List<Int32>();

                foreach (Schedule schedule in schedulesLocal)
                {
                    if (schedule.Type != ScheduleType.Group)
                    {
                        continue;
                    }

                    schedulesIdsLocal.AddRange(
                        schedule.Timetable.GetLecturers().Select(l => l.ScheduleId).Where(l => l != 0));
                }

                return schedulesIdsLocal;
            }

            Boolean hasNetworkError = await DownloadAsync(schedulesIds).ConfigureAwait(false);

            if (!hasNetworkError && shouldDownloadRelatedLecturersSchedules)
            {
                IEnumerable<Int32> relatedSchedulesIds = GetRelatedLecturersSchedulesIds(schedules.Values);
                await DownloadAsync(relatedSchedulesIds).ConfigureAwait(false);
            }

            return schedules;
        }

        private async Task<Schedule> DownloadScheduleAsync(Int32 scheduleId)
        {
            const String LecturerScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/teacher/";
            const String GroupScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/";

            // На входе: ЧЧ:ММ-ЧЧ:ММ[<br><span class="s_small">Вид недели</span>]
            static void ParseTime(HtmlNode td, out DateTime from, out DateTime to, out WeekType type)
            {
                String weekType = td.Element("span")?.InnerText.Trim();
                String timeRange = td.InnerHtml.Trim();

                if (weekType != null)
                {
                    type = Weeks[weekType];
                    timeRange = timeRange.Substring(0, timeRange.IndexOf('<'));
                }
                else
                {
                    type = WeekType.Upper | WeekType.Lower;
                }

                DateTime[] times = timeRange.Split('-').Select(t => DateTime.Parse(t)).ToArray();
                from = times[0];
                to = times[1];
            }

            // На входе: Корпус Аудитория[Литера]|Корпус каф.[ФВ|ВК|...]|Корпус Лаборатория
            static String ParseAuditorium(HtmlNode td)
            {
                String auditorium = td.InnerText.Trim().Replace(' ', '-').ToUpper(Culture);

                if (!auditorium.Contains('.'))
                {
                    return auditorium;
                }

                return auditorium.Substring(0, auditorium.IndexOf('.'));
            }

            // На входе: Название предмета<br><span class="s_small">Вид занятия</span>
            static String ParseTitle(HtmlNode td)
            {
                String subject = td.InnerHtml.Substring(0, td.InnerHtml.IndexOf('<'));
                String studyType = Studies[td.Element("span").InnerText.Trim()];

                return $"{subject.Trim()} ({studyType})";
            }

            // На входе: Номер группы|<a href="/ru/viewperson/Идентификатор/">ФИО</a>|ФИО
            void ParseLecturerOrGroup(HtmlNode td, out String name, out Int32 id)
            {
                if (Int32.TryParse(td.InnerText, out Int32 groupId))
                {
                    id = groupId;
                    name = GroupNamePrefixInLecturerSchedule + groupId;
                }
                else
                {
                    name = td.Element("a")?.InnerText ?? td.InnerText;
                    name = name.Trim();

                    if (name == String.Empty)
                    {
                        name = null;
                    }

                    Boolean isLecturerScheduleExists = name != null
                        && _lecturers != null
                        && _lecturers.ContainsKey(name);

                    id = isLecturerScheduleExists ? _lecturers[name] : 0;
                }
            }

            // Групп с большим номером у нас нет, значит это расписание преподавателя.
            ScheduleType scheduleType = (scheduleId > 100000) ? ScheduleType.Lecturer : ScheduleType.Group;

            Schedule schedule = new Schedule()
            {
                Type = scheduleType,
                ScheduleId = scheduleId,
                Timetable = new Timetable()
            };

            String baseUrl = (scheduleType == ScheduleType.Group) ? GroupScheduleBaseUrl
                : LecturerScheduleBaseUrl;

            String html = await HttpUtilities.GetAsync(baseUrl + scheduleId + "/").ConfigureAwait(false);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            String h1 = document.DocumentNode.SelectSingleNode("//h1[@class=\"c-main-content__title\"]")
                .InnerText;

            (String pattern, String replacement) = (scheduleType == ScheduleType.Lecturer)
                ? ContentTitleReplacementPatterns[0]
                : ContentTitleReplacementPatterns[1];

            schedule.DisplayedName = Regex.Replace(h1, pattern, replacement, RegexOptions.IgnoreCase);

            if (scheduleType == ScheduleType.Lecturer)
            {
                schedule.DisplayedName = Lecturer.GetShortName(schedule.DisplayedName);
            }

            HtmlNode table = document.DocumentNode.Descendants("table").First();
            HtmlNode[] heads = table.Elements("thead").ToArray();
            HtmlNode[] bodyes = table.Elements("tbody").ToArray();

            if (heads.Length == 1 && bodyes.Length == 1)
            {
                throw new Exception("Timetable is empty, therefore, such a schedule does not exist.");
            }

            // Первые элементы относятся к заголовку таблицы и интереса не представляют.
            for (Int32 i = 1; i < heads.Length; i++)
            {
                DayOfWeek day = Days[heads[i].Element("tr").Element("th").InnerText];

                List<Subject> subjects = new List<Subject>();

                foreach (HtmlNode row in bodyes[i].Elements("tr"))
                {
                    HtmlNode[] cells = row.Elements("td").ToArray();

                    ParseTime(cells[0], out DateTime from, out DateTime to, out WeekType week);
                    String auditorium = ParseAuditorium(cells[1]);
                    String title = ParseTitle(cells[2]);

                    Subject subject = new Subject()
                    {
                        IsDisplayed = true,
                        From = from,
                        To = to,
                        Week = week,
                        Title = title,
                        Auditorium = auditorium
                    };

                    ParseLecturerOrGroup(
                        cells[3],
                        out String lecturerOrGroupName,
                        out Int32 lecturerOrGroupScheduleId
                    );

                    if (scheduleType == ScheduleType.Lecturer)
                    {
                        subject.Group = new Group(lecturerOrGroupName, lecturerOrGroupScheduleId);
                    }
                    else
                    {
                        // У некоторых предметов в расписании группы преподаватель может быть не задан.
                        if (lecturerOrGroupName != null)
                        {
                            subject.Lecturer = new Lecturer(lecturerOrGroupName, lecturerOrGroupScheduleId);
                        }
                    }

                    subjects.Add(subject);
                }

                schedule.Timetable.SetSubjects(day, subjects.ToArray());
            }

            return schedule;
        }

        private readonly IReadOnlyDictionary<String, Int32> _lecturers;
    }
}