using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Enumerations;

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

        public Boolean HasDownloadingErrors { get; private set; }

        public ILogger Logger { get; set; }

        public async Task<Dictionary<Int32, Schedule>> DownloadAsync(IEnumerable<String> requests)
        {
            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>();
            HasDownloadingErrors = false;

            IEnumerable<Int32> ConvertRequestsToIds(IEnumerable<String> searchRequests)
            {
                foreach (String request in searchRequests)
                {
                    if (Int32.TryParse(request, out Int32 number))
                    {
                        yield return number;
                    }
                    else if (_lecturers != null && _lecturers.ContainsKey(request))
                    {
                        yield return _lecturers[request];
                    }
                }
            }

            foreach (Int32 scheduleId in ConvertRequestsToIds(requests))
            {
                try
                {
                    Schedule schedule = await DownloadScheduleAsync(scheduleId).ConfigureAwait(false);
                    schedules[scheduleId] = schedule;
                }
                catch(Exception exception)
                {
                    HasDownloadingErrors = true;
                    Logger?.Log($"Error of downloading schedule with id {scheduleId}: ", exception);
                }
            }

            return schedules;
        }

        public async Task<IEnumerable<String>> DownloadLecturers(Boolean forced = false)
        {
            const String SearchScheduleUrl = "https://www.smtu.ru/ru/searchschedule/";

            if (_lecturers != null && !forced)
            {
                return _lecturers.Keys;
            }

            // На входе: Фамилия Имя Отчество (Должность в университете)
            String GetPureLecturerName(String name) => name.Substring(0, name.IndexOf('(') - 1);

            // На входе: /ru/viewschedule/teacher/Идентификатор/
            Int32 GetScheduleIdFromUrl(String url)
            {
                url = url.TrimEnd('/');
                return Int32.Parse(url.Substring(url.LastIndexOf('/') + 1));
            }

            try
            {
                String html = await HttpHelper.GetAsync(SearchScheduleUrl).ConfigureAwait(false);

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(html);

                HtmlNode searchKeyField = document.DocumentNode.Descendants("input")
                    .Where(f => f.Attributes["name"]?.Value == "search_key")
                    .First();

                Dictionary<String, String> parameters = new Dictionary<String, String>()
                {
                    ["whatsearch"] = " ",
                    ["search_key"] = searchKeyField.Attributes["value"].Value
                };

                html = await HttpHelper.PostAsync(SearchScheduleUrl, parameters).ConfigureAwait(false);
                document.LoadHtml(html);

                IEnumerable<HtmlNode> links = document.DocumentNode.Descendants("article")
                    .First()
                    .Elements("li")
                    .Select(e => e.Element("a"))
                    .Distinct(new UrlComparer());

                _lecturers = new Dictionary<String, Int32>();

                foreach (HtmlNode link in links)
                {
                    Int32 scheduleId = GetScheduleIdFromUrl(link.Attributes["href"].Value);
                    String name = GetPureLecturerName(link.InnerHtml);
                    _lecturers[name] = scheduleId;
                }

                return _lecturers.Keys;
            }
            catch (Exception exception)
            {
                Logger?.Log($"Error of downloading lecturers with their schedules id's: ", exception);
                return null;
            }
        }

        private async Task<Schedule> DownloadScheduleAsync(Int32 scheduleId)
        {
            const String LecturerScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/teacher/";
            const String GroupScheduleBaseUrl = "https://www.smtu.ru/ru/viewschedule/";

            // На входе: ЧЧ:ММ-ЧЧ:ММ[<br><span class="s_small">Вид недели</span>]
            void ParseTime(HtmlNode td, out DateTime from, out DateTime to, out WeekType type)
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
            String ParseAudience(HtmlNode td)
            {
                String audience = td.InnerText.Trim().Replace(' ', '-').ToUpper(Culture);

                if (!audience.Contains('.'))
                {
                    return audience;
                }

                return audience.Substring(0, audience.IndexOf('.'));
            }

            // На входе: Название предмета<br><span class="s_small">Вид занятия</span>
            String ParseTitle(HtmlNode td)
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
            ScheduleType scheduleType = (scheduleId > 10000) ? ScheduleType.Lecturer : ScheduleType.Group;

            Schedule schedule = new Schedule()
            {
                ScheduleId = scheduleId,
                Timetable = new Timetable(),
            };

            String baseUrl = (scheduleType == ScheduleType.Group) ? GroupScheduleBaseUrl
                : LecturerScheduleBaseUrl;

            String html = await HttpHelper.GetAsync(baseUrl + scheduleId + "/").ConfigureAwait(false);

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
                    String audience = ParseAudience(cells[1]);
                    String title = ParseTitle(cells[2]);

                    Subject subject = new Subject()
                    {
                        IsDisplayed = true,
                        From = from,
                        To = to,
                        Week = week,
                        Title = title,
                        Audience = audience
                    };

                    ParseLecturerOrGroup(
                        cells[3],
                        out String lecturerOrGroupName,
                        out Int32 lecturerOrGroupScheduleId
                    );

                    if (scheduleType == ScheduleType.Lecturer)
                    {
                        subject.Group = new Lecturer(lecturerOrGroupName, lecturerOrGroupScheduleId);
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

        private Dictionary<String, Int32> _lecturers;
    }
}