using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Interfaces
{
    internal interface ILecturersDownloader
    {
        Boolean HasNoDownloadingError { get; }

        ILogger? Logger { get; set; }

        Task<IReadOnlyDictionary<String, Int32>?> DownloadLecturersMapAsync();
    }
}