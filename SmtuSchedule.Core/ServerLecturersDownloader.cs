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

        public ILogger Logger { get; set; }

        public ServerLecturersDownloader(IHttpClient client)
        {
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<IReadOnlyDictionary<String, Int32>> DownloadLecturersMapAsync()
        {
            return TryDownloadLecturersMapAsync(1);
        }

        private async Task<IReadOnlyDictionary<String, Int32>> TryDownloadLecturersMapAsync(Int32 attemptNumber)
        {
            const String SearchPageUrl = "https://www.smtu.ru/ru/searchschedule/";

            // На входе: Фамилия Имя Отчество (Должность в университете)
            static String GetPureLecturerName(String name) => name.Substring(0, name.IndexOf('(') - 1);

            // На входе: /ru/viewschedule/teacher/Идентификатор/
            static Int32 GetScheduleIdFromUrl(String url)
            {
                url = url.TrimEnd('/');
                return Int32.Parse(url.Substring(url.LastIndexOf('/') + 1));
            }

            HaveNoDownloadingErrors = true;

            try
            {
                String html = await _httpClient.GetAsync(SearchPageUrl).ConfigureAwait(false);
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

                IEnumerable<HtmlNode> links = document.DocumentNode.Descendants("article")
                    .First()
                    .Elements("li")
                    .Select(e => e.Element("a"))
                    .Distinct(new LinksEqualityComparer());

                Dictionary<String, Int32> lecturers = new Dictionary<String, Int32>();

                foreach (HtmlNode link in links)
                {
                    Int32 scheduleId = GetScheduleIdFromUrl(link.Attributes["href"].Value);
                    String name = GetPureLecturerName(link.InnerHtml);
                    lecturers[name] = scheduleId;
                }

                if (lecturers.Count == 0)
                {
                    throw new LecturersDownloaderException(
                        $"The list of lecturers at the end of download is empty in {attemptNumber} attempts.");
                }

                return lecturers;
            }
            catch (Exception exception)
                when (exception is HttpRequestException || exception is LecturersDownloaderException)
            {
                HaveNoDownloadingErrors = false;
                Logger?.Log(
                    new LecturersDownloaderException("Error of downloading the list of lecturers.", exception));

                return null;
            }
        }

        private readonly IHttpClient _httpClient;
    }
}