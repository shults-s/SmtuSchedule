using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Interfaces
{
    internal interface IHttpClient
    {
        Task<String> GetAsync(String url, IReadOnlyDictionary<String, String>? parameters = null);
        Task<String> PostAsync(String url, IReadOnlyDictionary<String, String>? parameters = null);
    }
}