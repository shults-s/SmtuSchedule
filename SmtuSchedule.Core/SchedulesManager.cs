using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public sealed class SchedulesManager
    {
        public IReadOnlyDictionary<String, Int32> LecturersMap => _lecturersMap;

        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public Boolean IsLecturersMapReadedFromCache { get; private set; }

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
            _schedules = new ConcurrentDictionary<Int32, Schedule>();
            _schedulesRepository = new LocalSchedulesRepository($"{storagePath}/{schedulesDirectoryName}/");
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            return Task.Run(() =>
            {
                if (_lecturersMap == null || _lecturersMap.Count == 0)
                {
                    throw new InvalidOperationException("Lecturers map is null or zero length.");
                }

                SchedulesMigrator schedulesMigrator = new SchedulesMigrator(_lecturersMap)
                {
                    Logger = _logger
                };

                IEnumerable<Schedule> affectedSchedules = schedulesMigrator.Migrate(_schedules.Values);

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

            if (_lecturersMap == null || _lecturersMap.Count == 0)
            {
                throw new InvalidOperationException("Lecturers map is null or zero length.");
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
                if (_lecturersMap == null || _lecturersMap.Count == 0)
                {
                    throw new InvalidOperationException("Lecturers map is null or zero length.");
                }

                ServerSchedulesDownloader schedulesLoader = new ServerSchedulesDownloader(_lecturersMap)
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

                return schedulesLoader.HaveDownloadingErrors || haveSavingErrors;
            });
        }

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests,
            Boolean shouldDownloadRelatedSchedules)
        {
            return Task.Run(async () =>
            {
                if (searchRequests == null || searchRequests.Count() == 0)
                {
                    throw new ArgumentException("Value cannot be null or zero length.", nameof(searchRequests));
                }

                if (_lecturersMap == null || _lecturersMap.Count == 0)
                {
                    throw new InvalidOperationException("Lecturers map is null or zero length.");
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

        public Task<Boolean> ReadLecturersMapAsync()
        {
            return Task.Run(() =>
            {
                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                IsLecturersMapReadedFromCache = true;

                return (_lecturersMap = lecturersRepository.Read()) == null;
            });
        }

        public Task<Boolean> DownloadLecturersMapAsync()
        {
            return Task.Run(async () =>
            {
                // Если в этой сессии карта преподавателей уже загружалась с сайта, то нет необходимости
                // делать это вновь. За одно использование приложения она не успеет устареть.
                if (_lecturersMap != null && !IsLecturersMapReadedFromCache)
                {
                    return false;
                }

                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                IsLecturersMapReadedFromCache = false;

                ServerLecturersDownloader lecturersDownloader = new ServerLecturersDownloader()
                {
                    Logger = _logger
                };

                _lecturersMap = await lecturersDownloader.DownloadAsync().ConfigureAwait(false);
                if (!lecturersDownloader.HaveDownloadingErrors)
                {
                    lecturersRepository.Save(_lecturersMap);
                }

                return lecturersDownloader.HaveDownloadingErrors;
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

        private ILogger _logger;

        private Dictionary<String, Int32> _lecturersMap;
        private ConcurrentDictionary<Int32, Schedule> _schedules;

        private readonly String _storagePath;
        private readonly LocalSchedulesRepository _schedulesRepository;
    }
}