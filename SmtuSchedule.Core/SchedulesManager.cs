using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public class SchedulesManager
    {
        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public Boolean IsDownloadingInProgress { get; private set; }

        public ILogger Logger { get; set; }

        public SchedulesManager(String storagePath) => _storagePath = storagePath;

        public async Task<IReadOnlyDictionary<String, Int32>> GetLecturersAsync()
        {
            if (_lecturers != null)
            {
                return _lecturers;
            }

            return (_lecturers = await LecturersLoader.DownloadAsync(Logger).ConfigureAwait(false));
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            return Task.Run(() =>
            {
                SchedulesMigrator schedulesMigrator = new SchedulesMigrator(GetLecturersAsync)
                {
                    Logger = Logger
                };

                IEnumerable<Schedule> affectedSchedules = schedulesMigrator.MigrateAsync(
                    _schedules.Values
                ).ToEnumerable();

                LocalSchedulesWriter schedulesWriter = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                Boolean haveSavingErrors = false;

                foreach (Schedule schedule in affectedSchedules)
                {
                    if (!schedulesWriter.Save(schedule))
                    {
                        haveSavingErrors = true;
                    }
                }

                return haveSavingErrors || schedulesMigrator.HaveMigrationErrors;
            });
        }

        public Int32 GetScheduleIdBySearchRequest(String searchRequest)
        {
            if (Int32.TryParse(searchRequest, out Int32 number))
            {
                return number;
            }

            if (_lecturers.ContainsKey(searchRequest))
            {
                return _lecturers[searchRequest];
            }

            return 0;
        }

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests,
            Boolean shouldDownloadRelatedSchedules)
        {
            return Task.Run(async () =>
            {
                IsDownloadingInProgress = true;

                _lecturers ??= await GetLecturersAsync().ConfigureAwait(false);
                if (_lecturers == null)
                {
                    return true;
                }

                Int32[] schedulesIds = searchRequests.Select(r => GetScheduleIdBySearchRequest(r))
                    .Where(id => id != 0)
                    .ToArray();

                if (schedulesIds.Length == 0)
                {
                    return true;
                }

                ServerSchedulesLoader schedulesLoader = new ServerSchedulesLoader(_lecturers)
                {
                    Logger = Logger
                };

                Dictionary<Int32, Schedule> schedules = await schedulesLoader.DownloadAsync(
                    schedulesIds,
                    shouldDownloadRelatedSchedules
                ).ConfigureAwait(false);

                LocalSchedulesWriter schedulesWriter = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                Boolean haveSavingErrors = false;

                foreach ((Int32 scheduleId, Schedule schedule) in schedules)
                {
                    if (!schedulesWriter.Save(schedule))
                    {
                        haveSavingErrors = true;
                    }
                    else
                    {
                        _schedules[scheduleId] = schedule;
                    }
                }

                IsDownloadingInProgress = false;

                return schedulesLoader.HaveDownloadingErrors || haveSavingErrors;
            });
        }

        public Task<Boolean> RemoveScheduleAsync(Int32 scheduleId)
        {
            return Task.Run(() =>
            {
                LocalSchedulesWriter schedulesWriter = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                if (!schedulesWriter.Remove(_schedules[scheduleId]))
                {
                    return true;
                }

                _schedules.TryRemove(scheduleId, out Schedule schedule);
                return false;
            });
        }

        public Task<Boolean> ReadSchedulesAsync()
        {
            return Task.Run(() =>
            {
                LocalSchedulesReader schedulesReader = new LocalSchedulesReader()
                {
                    Logger = Logger
                };

                _schedules = new ConcurrentDictionary<Int32, Schedule>(schedulesReader.Read(_storagePath));
                return schedulesReader.HaveReadingErrors;
            });
        }

        private readonly String _storagePath;

        private IReadOnlyDictionary<String, Int32> _lecturers;
        private ConcurrentDictionary<Int32, Schedule> _schedules;
    }
}