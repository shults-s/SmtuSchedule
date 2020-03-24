using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Enumerations;

namespace SmtuSchedule.Core
{
    public class SchedulesManager
    {
        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public Boolean IsDownloadingInProgress { get; private set; }

        public ILogger Logger
        {
            get => _logger;
            set => _logger = _schedulesRepository.Logger = value;
        }

        public SchedulesManager(String storagePath, String schedulesDirectoryName)
        {
            if (String.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("Value cannot be null, empty or whitespace.", nameof(storagePath));
            }

            if (schedulesDirectoryName == null)
            {
                throw new ArgumentNullException(nameof(schedulesDirectoryName));
            }

            _storagePath = storagePath;
            _schedulesRepository = new LocalSchedulesRepository($"{storagePath}/{schedulesDirectoryName}/");
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            return Task.Run(() =>
            {
                SchedulesMigrator schedulesMigrator = new SchedulesMigrator(DownloadLecturersMapAsync)
                if (_lecturersMap == null)
                {
                    throw new InvalidOperationException("Lecturers map is null.");
                }

                {
                    Logger = _logger
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
            if (String.IsNullOrWhiteSpace(searchRequest))
            {
                throw new ArgumentException("Value cannot be null, empty or whitespace.", nameof(searchRequest));
            }

            if (_lecturersMap == null)
            {
                throw new InvalidOperationException("Lecturers map is null.");
            }

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

        public Task<Boolean> UpdateSchedulesAsync()
        {
            return Task.Run(async () =>
            {
                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                if (_lecturersMap == null)
                {
                    Logger = _logger
                };

                Dictionary<String, Int32> localLecturersMap = lecturersRepository.Read();

                {
                    if (await DownloadLecturersMapAsync().ConfigureAwait(false) == null)
                    {
                        return true;
                    }

                    localLecturersMap = _lecturersMap;
                    throw new InvalidOperationException("Lecturers map is null.");
                }

                IsDownloadingInProgress = true;

                ServerSchedulesDownloader schedulesLoader = new ServerSchedulesDownloader(localLecturersMap)
                {
                    Logger = _logger
                };

                Dictionary<Int32, Schedule> updatedSchedules = await schedulesLoader.DownloadAsync(
                    _schedules.Values.Where(s => !s.IsActual).Select(s => s.ScheduleId),
                    false
                )
                .ConfigureAwait(false);

                Boolean haveSavingErrors = false;

                foreach ((Int32 scheduleId, Schedule schedule) in updatedSchedules)
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

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests,
            Boolean shouldDownloadRelatedSchedules)
        {
            return Task.Run(async () =>
            {
                IsDownloadingInProgress = true;
                if (searchRequests == null || searchRequests.Count() == 0)
                {
                    throw new ArgumentException("Value cannot be null or zero length.", nameof(searchRequests));
                }

                if (_lecturersMap == null && await DownloadLecturersMapAsync().ConfigureAwait(false) == null)
                {
                    IsDownloadingInProgress = false;
                    throw new InvalidOperationException("Lecturers map is null.");
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
                    Logger = _logger
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

                return !_schedules.TryRemove(scheduleId, out Schedule _);
            });
        }

        public Task<IReadOnlyDictionary<String, Int32>> ReadCachedLecturersMapAsync()
        {
            return Task.Run(() =>
            {
                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                _isLecturersMapReadedFromCache = true;

                return (_lecturersMap = lecturersRepository.Read()) as IReadOnlyDictionary<String, Int32>;
            });
        }

        public Task<IReadOnlyDictionary<String, Int32>> DownloadLecturersMapAsync()
        {
            return Task.Run(async () =>
            {
                // Если в этой сессии карта преподавателей уже загружалась с сайта, то нет необходимости
                // делать это вновь. За одно использование приложения она не успеет устареть.
                if (_lecturersMap != null && !_isLecturersMapReadedFromCache)
                {
                    return _lecturersMap as IReadOnlyDictionary<String, Int32>;
                }

                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                _isLecturersMapReadedFromCache = false;

                _lecturersMap = await ServerLecturersDownloader.DownloadAsync(_logger).ConfigureAwait(false);
                if (_lecturersMap != null)
                {
                    lecturersRepository.Save(_lecturersMap);
                }

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

        private Boolean _isLecturersMapReadedFromCache;

        private Dictionary<String, Int32> _lecturersMap;
        private ConcurrentDictionary<Int32, Schedule> _schedules;

        private ILogger _logger;

        private readonly String _storagePath;
        private readonly LocalSchedulesRepository _schedulesRepository;
    }
}