using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using HtmlAgilityPack;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal sealed class ServerLecturersDownloader
    {
        private const Int32 IntervalBetweenRequestsInMilliseconds = 300;

        private const Int32 MaximumAttemptsNumber = 5;

        public Boolean HaveNoDownloadingErrors { get; private set; }

        public ILogger? Logger { get; set; }

        public ServerLecturersDownloader(IHttpClient client)
        {
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<IReadOnlyDictionary<String, Int32>?> DownloadLecturersMapAsync()
        {
            return TryDownloadLecturersMapAsync(1);
        }

        private async Task<IReadOnlyDictionary<String, Int32>?> TryDownloadLecturersMapAsync(Int32 attemptNumber)
        {
            const String SearchPageUrl = "https://www.smtu.ru/ru/searchschedule/";

            // На входе: <a href="/ru/viewschedule/teacher/ИдентификаторРасписания/">...</a>
            static Int32 ParseScheduleId(HtmlNode link)
            {
                String? scheduleUrl = link.Attributes["href"]?.Value;
                if (scheduleUrl == null)
                {
                    throw new HtmlParsingException("Schedule link URL is null. Missing 'href' attribute?", link);
                }

                scheduleUrl = scheduleUrl.TrimEnd('/');

                try
                {
                    // Обрезаем '/ru/viewschedule/teacher/'.
                    return Int32.Parse(scheduleUrl.Substring(scheduleUrl.LastIndexOf('/') + 1));
                }
                catch (Exception exception)
                    when (exception is ArgumentOutOfRangeException || exception is FormatException)
                {
                    throw new HtmlParsingException("Invalid format of schedule link URL.", exception, link);
                }
            }

            // На входе: <a href="...">Фамилия Имя Отчество (Должность в университете)</a>
            static String ParseLecturerName(HtmlNode link)
            {
                try
                {
                    // Обрезаем ' (Должность в университете)'.
                    return link.InnerHtml.Substring(0, link.InnerHtml.IndexOf('(') - 1);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    throw new HtmlParsingException("Invalid format of schedule link content.", exception, link);
                }
            }

            HaveNoDownloadingErrors = true;

            try
            {
                String html = await _httpClient.GetAsync(SearchPageUrl).ConfigureAwait(false);
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(html);

                HtmlNode? searchKeyField = document.DocumentNode.Descendants("input")
                    ?.Where(f => f.Attributes["name"]?.Value == "search_key")
                    .First();

                if (searchKeyField == null)
                {
                    throw new HtmlParsingException("Search page does not contains input field named 'search_key'.");
                }

                Dictionary<String, String> parameters = new Dictionary<String, String>()
                {
                    ["whatsearch"] = " ",
                    ["search_key"] = searchKeyField.Attributes["value"].Value
                };

                // Первая попытка без задержки.
                if (attemptNumber > 1)
                {
                    // Bugfix: "unexpected end of stream" или "\\n not found: size=1 content=0d..."
                    System.Threading.Thread.Sleep(IntervalBetweenRequestsInMilliseconds);
                }

                try
                {
                    html = await _httpClient.PostAsync(SearchPageUrl, parameters).ConfigureAwait(false);
                }
                // Предотвращаем бесконечную рекурсию в случае, если ошибка произошла в каждой из попыток.
                catch (HttpRequestException) when (attemptNumber <= MaximumAttemptsNumber)
                {
                    return await TryDownloadLecturersMapAsync(attemptNumber + 1).ConfigureAwait(false);
                }

                document.LoadHtml(html);

                IEnumerable<HtmlNode>? links = document.DocumentNode.Descendants("article")
                    ?.First()
                    ?.Elements("li")
                    .Select(e => e.Element("a"))
                    .Distinct(new LinksEqualityComparer());

                if (links == null)
                {
                    throw new HtmlParsingException("Search results page does not contains list of links.");
                }

                Dictionary<String, Int32> lecturers = new Dictionary<String, Int32>();

                foreach (HtmlNode link in links)
                {
                    if (link == null)
                    {
                        throw new HtmlParsingException("Schedule link is null. Missing 'a' tag?");
                    }

                    Int32 scheduleId = ParseScheduleId(link);
                    String name = ParseLecturerName(link);
                    lecturers[name] = scheduleId;
                }

                if (lecturers.Count == 0)
                {
                    throw new LecturersDownloaderException(
                        $"List of lecturers at end of download is empty in {attemptNumber} attempts.");
                }

                return lecturers;
            }
            catch (Exception exception) when (
                exception is HttpRequestException
                || exception is NullReferenceException
                || exception is HtmlParsingException
                || exception is LecturersDownloaderException
            )
            {
                HaveNoDownloadingErrors = false;
                Logger?.Log(new LecturersDownloaderException("Error of downloading list of lecturers.", exception));

                return null;
            }
        }

        private readonly IHttpClient _httpClient;
    }
}