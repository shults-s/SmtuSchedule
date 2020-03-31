using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core.Utilities
{
    internal sealed class HttpClientProxy : IHttpClient
    {
        private static readonly HttpClient Client;

        // HttpClient использует cookies по-умолчанию, создавать объект HttpClientHandler нет необходимости.
        static HttpClientProxy() => Client = new HttpClient();

        public async Task<String> GetAsync(String url, IReadOnlyDictionary<String, String> parameters = null)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(url));
            }

            if (parameters != null)
            {
                using FormUrlEncodedContent content = new FormUrlEncodedContent(parameters);
                url += "?" + await content.ReadAsStringAsync().ConfigureAwait(false);
            }

            using HttpResponseMessage response = await Client.GetAsync(url).ConfigureAwait(false);
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<String> PostAsync(String url, IReadOnlyDictionary<String, String> parameters = null)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(url));
            }

            FormUrlEncodedContent content = null;

            if (parameters != null)
            {
                content = new FormUrlEncodedContent(parameters);
            }

            using HttpResponseMessage response = await Client.PostAsync(url, content).ConfigureAwait(false);
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}