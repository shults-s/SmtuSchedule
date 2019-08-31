using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    internal static class HttpHelper
    {
        static HttpHelper() => _client = new HttpClient();

        public static async Task<String> PostAsync(String url, Dictionary<String, String> parameters = null)
        {
            FormUrlEncodedContent content = null;
            if (parameters != null)
            {
                content = new FormUrlEncodedContent(parameters);
            }

            HttpResponseMessage message = await _client.PostAsync(url, content).ConfigureAwait(false);
            return await message.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task<String> GetAsync(String url, Dictionary<String, String> parameters = null)
        {
            if (parameters != null)
            {
                FormUrlEncodedContent content = new FormUrlEncodedContent(parameters);
                url += "?" + await content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return await _client.GetStringAsync(url).ConfigureAwait(false);
        }

        private static readonly HttpClient _client;
    }
}