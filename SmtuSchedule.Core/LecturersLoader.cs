using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal static class LecturersLoader
    {
        private const Int32 IntervalBetweenRequestsInMilliseconds = 300;

        private const Int32 MaxAttemptsNumber = 5;

        public static Task<Dictionary<String, Int32>> DownloadAsync(ILogger logger)
        {
            return TryDownloadAsync(logger, 1);
        }

        private static async Task<Dictionary<String, Int32>> TryDownloadAsync(ILogger logger, Int32 attempt)
        {
            const String SearchScheduleUrl = "https://www.smtu.ru/ru/searchschedule/";

            // На входе: Фамилия Имя Отчество (Должность в университете)
            static String GetPureLecturerName(String name) => name.Substring(0, name.IndexOf('(') - 1);

            // На входе: /ru/viewschedule/teacher/Идентификатор/
            static Int32 GetScheduleIdFromUrl(String url)
            {
                url = url.TrimEnd('/');
                return Int32.Parse(url.Substring(url.LastIndexOf('/') + 1));
            }

            Dictionary<String, Int32> lecturers = null;
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

                // Первая попытка без задержки.
                if (attempt > 1)
                {
                    // Bugfix: "unexpected end of stream" или "\\n not found: size=1 content=0d..."
                    System.Threading.Thread.Sleep(IntervalBetweenRequestsInMilliseconds);
                }

                try
                {
                    html = await HttpHelper.PostAsync(SearchScheduleUrl, parameters).ConfigureAwait(false);
                }
                // Предотвращаем бесконечную рекурсию в случае, если ошибка произошла в каждой из попыток.
                catch when (attempt <= MaxAttemptsNumber)
                {
                    return await TryDownloadAsync(logger, attempt + 1).ConfigureAwait(false);
                }

                document.LoadHtml(html);

                IEnumerable<HtmlNode> links = document.DocumentNode.Descendants("article")
                    .First()
                    .Elements("li")
                    .Select(e => e.Element("a"))
                    .Distinct(new UrlComparer());

                lecturers = new Dictionary<String, Int32>();

                foreach (HtmlNode link in links)
                {
                    Int32 scheduleId = GetScheduleIdFromUrl(link.Attributes["href"].Value);
                    String name = GetPureLecturerName(link.InnerHtml);
                    lecturers[name] = scheduleId;
                }

                if (lecturers.Count == 0)
                {
                    throw new Exception(
                        $"The list of lecturers at the end of download is empty in {attempt} attempts.");
                }

                return lecturers;
            }
            catch (Exception exception)
            {
                logger?.Log(
                    new LecturersLoaderException("Error of downloading list of the lecturers.", exception));

                return null;
            }
        }
    }
}