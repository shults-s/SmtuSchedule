using System;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Interfaces
{
    public interface ILecturersRepository
    {
        ILogger? Logger { get; set; }

        Boolean SaveLecturersMap(IReadOnlyDictionary<String, Int32> lecturers);

        IReadOnlyDictionary<String, Int32>? ReadLecturersMap(out Boolean hasNoReadingError);
    }
}