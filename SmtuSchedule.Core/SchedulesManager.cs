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

        public SchedulesManager(String storagePath, String schedulesDirectoryName)
        {
            _schedulesRepository = new LocalSchedulesRepository($"{storagePath}/{schedulesDirectoryName}/")
            {
                Logger = Logger
            };

            _storagePath = storagePath;
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            return Task.Run(() =>
            {
                SchedulesMigrator schedulesMigrator = new SchedulesMigrator(DownloadLecturersMapAsync)
                {
                    Logger = Logger
                };

                IEnumerable<Schedule> affectedSchedules = schedulesMigrator.MigrateAsync(_schedules.Values)
                    .ToEnumerable();

                Boolean haveSavingErrors = false;

                foreach (Schedule schedule in affectedSchedules)
                {
                    if (!_schedulesRepository.Save(schedule))
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

            if (_lecturersMap.ContainsKey(searchRequest))
            {
                return _lecturersMap[searchRequest];
            }

            return 0;
        }

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests,
            Boolean shouldDownloadRelatedSchedules)
        {
            return Task.Run(async () =>
            {
                IsDownloadingInProgress = true;

                if (_lecturersMap == null && await DownloadLecturersMapAsync().ConfigureAwait(false) == null)
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

                ServerSchedulesDownloader schedulesLoader = new ServerSchedulesDownloader(_lecturersMap)
                {
                    Logger = Logger
                };

                Dictionary<Int32, Schedule> schedules = await schedulesLoader.DownloadAsync(
                    schedulesIds,
                    shouldDownloadRelatedSchedules
                )
                .ConfigureAwait(false);

                Boolean haveSavingErrors = false;

                foreach ((Int32 scheduleId, Schedule schedule) in schedules)
                {
                    if (!_schedulesRepository.Save(schedule))
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
                if (!_schedulesRepository.Remove(_schedules[scheduleId]))
                {
                    return true;
                }

                _schedules.TryRemove(scheduleId, out Schedule schedule);
                return false;
            });
        }

        public Task<IReadOnlyDictionary<String, Int32>> DownloadLecturersMapAsync()
        {
            return Task.Run(async () =>
            {
                if (_lecturersMap != null)
                {
                    return _lecturersMap as IReadOnlyDictionary<String, Int32>;
                }

                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = Logger
                };

                _lecturersMap = await ServerLecturersDownloader.DownloadAsync(Logger).ConfigureAwait(false);
                lecturersRepository.Save(_lecturersMap);

                return _lecturersMap as IReadOnlyDictionary<String, Int32>;
            });
        }

        public Task<Boolean> ReadSchedulesAsync()
        {
            return Task.Run(() =>
            {
                Dictionary<Int32, Schedule> schedules = _schedulesRepository.Read(out Boolean haveReadingErrors);
                _schedules = new ConcurrentDictionary<Int32, Schedule>(schedules);
                return haveReadingErrors;
            });
        }

        private Dictionary<String, Int32> _lecturersMap;
        private ConcurrentDictionary<Int32, Schedule> _schedules;

        private readonly String _storagePath;
        private readonly LocalSchedulesRepository _schedulesRepository;
    }
}