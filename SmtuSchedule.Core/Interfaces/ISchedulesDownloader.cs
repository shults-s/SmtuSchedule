using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;

namespace SmtuSchedule.Core.Interfaces
{
    internal interface ISchedulesDownloader
    {
        Boolean HaveNoDownloadingErrors { get; }

        ILogger? Logger { get; set; }

        Task<IEnumerable<Schedule>> DownloadSchedulesAsync(
            IEnumerable<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap,
            Boolean shouldDownloadRelatedSchedules
        );
    }
}