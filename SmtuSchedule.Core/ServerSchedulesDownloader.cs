using System;
using System.Linq;
using System.Net.Http;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;
using SmtuSchedule.Core.Enumerations;

using Group = SmtuSchedule.Core.Models.Group;

namespace SmtuSchedule.Core
{
    internal class ServerSchedulesDownloader
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
            ["Лекция"] = "лекция"
        };

        private static readonly CultureInfo Culture = new CultureInfo("ru-RU");

        private const String GroupNamePrefixInLecturerSchedule = "Группа ";

        public Boolean HaveNoDownloadingErrors { get; private set; }

        public ILogger? Logger { get; set; }

        public ServerSchedulesDownloader(IHttpClient client)
        {
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IEnumerable<Schedule>> DownloadSchedulesAsync(IEnumerable<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap, Boolean shouldDownloadRelatedSchedules)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException(
                    "Provided lecturers map is null or empty, therefore, in any loaded schedule, all of "
                    + "lecturers id's will be zero. So, switching between schedules will be impossible.",
                    nameof(lecturersMap)
                );
            }

            if (schedulesIds == null)
            {
                throw new ArgumentNullException(nameof(schedulesIds));
            }

            HaveNoDownloadingErrors = true;

            async Task<List<Schedule>> DownloadSchedulesAsync(IEnumerable<Int32> schedulesIdsLocal)
            {
                List<Schedule> schedulesLocal = new List<Schedule>(schedulesIdsLocal.Count());

                foreach (Int32 scheduleId in schedulesIdsLocal)
                {
                    try
                    {
                        Schedule schedule = await DownloadScheduleAsync(scheduleId, lecturersMap)
                            .ConfigureAwait(false);

                        schedule.Validate();

                        schedulesLocal.Add(schedule);
                    }
                    catch (Exception exception) when (
                        exception is HttpRequestException
                        || exception is ValidationException
                        || exception is SchedulesDownloaderException
                    )
                    {
                        HaveNoDownloadingErrors = false;
                        Logger?.Log(
                            new SchedulesDownloaderException(
                                $"Error of downloading schedule with id {scheduleId}.", exception)
                        );
                    }
                }

                return schedulesLocal;
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

            List<Schedule> schedules = await DownloadSchedulesAsync(schedulesIds).ConfigureAwait(false);

            if (shouldDownloadRelatedSchedules)
            {
                IEnumerable<Int32> relatedSchedulesIds = GetRelatedLecturersSchedulesIds(schedules);
                schedules.AddRange(await DownloadSchedulesAsync(relatedSchedulesIds).ConfigureAwait(false));
            }

            return schedules;
        }

        private async Task<Schedule> DownloadScheduleAsync(Int32 scheduleId,
            IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            const String LecturerScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/teacher/";
            const String GroupScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/";

            // На входе: ЧЧ:ММ-ЧЧ:ММ[<br><span class="s_small">Вид недели</span>]
            static void ParseTime(HtmlNode td, out DateTime from, out DateTime to, out WeekType type)
            {
                String? weekType = td.Element("span")?.InnerText.Trim();
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
            void ParseLecturerOrGroup(HtmlNode td, out String? name, out Int32 id)
            {
                if (Int32.TryParse(td.InnerText, out Int32 groupId))
                {
                    id = groupId;
                    name = GroupNamePrefixInLecturerSchedule + groupId;
                }
                else
                {
                    name = td.Element("a")?.InnerText ?? td.InnerText;
                    name = name?.Trim();

                    if (name == String.Empty)
                    {
                        name = null;
                    }

                    Boolean isLecturerScheduleExists = name != null
                        && lecturersMap != null
                        && lecturersMap.ContainsKey(name);

                    id = isLecturerScheduleExists ? lecturersMap![name!] : 0;
                }
            }

            // Групп с большим номером у нас нет, значит это расписание преподавателя.
            ScheduleType scheduleType = (scheduleId > 100000) ? ScheduleType.Lecturer : ScheduleType.Group;

            Schedule schedule = new Schedule()
            {
                Type = scheduleType,
                ScheduleId = scheduleId,
                LastUpdate = DateTime.Now,
                Timetable = new Timetable()
            };

            String baseUrl = (scheduleType == ScheduleType.Group) ? GroupScheduleBaseUrl
                : LecturerScheduleBaseUrl;

            String html = await _httpClient.GetAsync(baseUrl + scheduleId + "/").ConfigureAwait(false);

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
                throw new SchedulesDownloaderException(
                    "Timetable is empty, therefore, such a schedule does not exist.");
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
                        out String? lecturerOrGroupName,
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

        private readonly IHttpClient _httpClient;
    }
}