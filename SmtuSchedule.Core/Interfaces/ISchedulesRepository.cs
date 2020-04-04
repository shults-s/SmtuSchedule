using System;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ISchedulesRepository
    {
        ILogger? Logger { get; set; }

        Boolean RemoveSchedule(String displayedName);

        Boolean SaveSchedules(
            IEnumerable<Schedule> schedules,
            Action<Schedule>? scheduleSavedSuccessfulCallback = null
        );

        IEnumerable<Schedule>? ReadSchedules(out Boolean haveNoReadingErrors);
    }
}