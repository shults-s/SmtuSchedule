using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ISchedulesManager
    {
        IReadOnlyDictionary<Int32, Schedule> Schedules { get; }

        ILogger? Logger { get; set; }

        Task<Boolean> RemoveScheduleAsync(Int32 scheduleId);

        Task<Boolean> ReadSchedulesAsync();

        Task<Boolean> DownloadSchedulesAsync(
            DownloadingOptions options,
            IReadOnlyCollection<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap
        );

        Task<Boolean> UpdateSchedulesAsync(IReadOnlyDictionary<String, Int32> lecturersMap);

        Task<Boolean> MigrateSchedulesAsync(IReadOnlyDictionary<String, Int32> lecturersMap);
    }
}