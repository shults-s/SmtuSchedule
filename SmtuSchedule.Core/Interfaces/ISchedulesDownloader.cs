using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Interfaces
{
    internal interface ISchedulesDownloader
    {
        Boolean HaveNoDownloadingErrors { get; }

        ILogger? Logger { get; set; }

        Task<IEnumerable<Schedule>> DownloadSchedulesAsync(
            DownloadingOptions options,
            IReadOnlyCollection<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap
        );
    }
}