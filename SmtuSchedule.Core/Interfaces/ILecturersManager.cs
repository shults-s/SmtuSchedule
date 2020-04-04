using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ILecturersManager
    {
        IReadOnlyDictionary<String, Int32>? LecturersMap { get; }

        Boolean IsLecturersMapReadedFromCache { get; }

        ILogger? Logger { get; set; }

        Task<Boolean> DownloadLecturersMapAsync();

        Task<Boolean> ReadCachedLecturersMapAsync();
    }
}