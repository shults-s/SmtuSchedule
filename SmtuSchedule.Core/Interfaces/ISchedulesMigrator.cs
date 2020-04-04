using System;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ISchedulesMigrator
    {
        Boolean HaveNoMigrationErrors { get; }

        ILogger? Logger { get; set; }

        IEnumerable<Schedule> Migrate(
            IEnumerable<Schedule> schedules,
            IReadOnlyDictionary<String, Int32> lecturersMap
        );
    }
}