using System;
using System.Linq;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    public static class SearchSchedulesUtilities
    {
        public static IEnumerable<Int32> FindSchedulesIdsBySearchRequests(IEnumerable<String> requests,
            IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            Int32 GetScheduleIdBySearchRequest(String searchRequest)
            {
                if (String.IsNullOrWhiteSpace(searchRequest))
                {
                    throw new ArgumentException(
                        "String cannot be null, empty or whitespace.", nameof(searchRequest));
                }

                if (Int32.TryParse(searchRequest, out Int32 scheduleId))
                {
                    return scheduleId;
                }

                return lecturersMap.ContainsKey(searchRequest) ? lecturersMap[searchRequest] : 0;
            }

            return requests.Select(r => GetScheduleIdBySearchRequest(r)).Where(id => id != 0).Distinct();
        }
    }
}