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
        public static async Task<Dictionary<String, Int32>> Download(ILogger logger)
        {
            Dictionary<String, Int32> lecturers = null;

            const String SearchScheduleUrl = "https://www.smtu.ru/ru/searchschedule/";

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

                lecturers = new Dictionary<String, Int32>();

                foreach (HtmlNode link in links)
                {
                    Int32 scheduleId = GetScheduleIdFromUrl(link.Attributes["href"].Value);
                    String name = GetPureLecturerName(link.InnerHtml);
                    lecturers[name] = scheduleId;
                }

                return lecturers;
            }
            catch (Exception exception)
            {
                logger.Log(
                    new LecturersLoaderException("Error of downloading list of lecturers.", exception));

                return null;
            }
        }
    }
}