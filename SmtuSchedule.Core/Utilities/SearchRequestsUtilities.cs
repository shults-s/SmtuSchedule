using System;
using System.Linq;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    public static class SearchRequestsUtilities
    {
        public static IReadOnlyCollection<Int32> GetSchedulesIdsBySearchRequests(
            IEnumerable<String> requests, IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            Int32 GetScheduleIdBySearchRequest(String request)
            {
                if (String.IsNullOrWhiteSpace(request))
                {
                    throw new ArgumentException(
                        "String cannot be null, empty or whitespace.", nameof(request));
                }

                if (Int32.TryParse(request, out Int32 scheduleId))
                {
                    return scheduleId;
                }

                return lecturersMap.ContainsKey(request) ? lecturersMap[request] : 0;
            }

            return requests.Select(r => GetScheduleIdBySearchRequest(r)).Where(id => id > 0).ToHashSet();
        }
    }
}