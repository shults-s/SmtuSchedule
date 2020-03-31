using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public sealed class SchedulesManager
    {
        public IReadOnlyDictionary<String, Int32> LecturersMap { get; private set; }

        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public Boolean IsLecturersMapReadedFromCache { get; private set; }

        public ILogger Logger
        {
            get => _logger;
            set => _logger = _schedulesRepository.Logger = value;
        }

        public SchedulesManager(String storagePath, String schedulesDirectoryName, IHttpClient client)
        {
            if (String.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(storagePath));
            }

            if (String.IsNullOrWhiteSpace(schedulesDirectoryName))
            {
                throw new ArgumentException(
                    "String cannot be null, empty or whitespace.", nameof(schedulesDirectoryName));
            }

            if (!Directory.Exists(storagePath))
            {
                throw new DirectoryNotFoundException("Storage directory does not exists or is not accessible.");
            }

            String schedulesPath = $"{storagePath}/{schedulesDirectoryName}/";
            if (!Directory.Exists(schedulesPath))
            {
                throw new DirectoryNotFoundException("Schedules directory does not exists or is not accessible.");
            }

            _httpClient = client ?? throw new ArgumentNullException(nameof(client));

            _storagePath = storagePath;
            _schedules = new ConcurrentDictionary<Int32, Schedule>();
            _schedulesRepository = new LocalSchedulesRepository(schedulesPath);
        }

        public SchedulesManager(String storagePath, String schedulesDirectoryName)
            : this(storagePath, schedulesDirectoryName, new HttpClientProxy())
        {
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            if (LecturersMap == null || LecturersMap.Count == 0)
            {
                throw new InvalidOperationException("Lecturers map is null or empty.");
            }

            return Task.Run(() =>
            {
                SchedulesMigrator schedulesMigrator = new SchedulesMigrator()
                {
                    Logger = _logger
                };

                IEnumerable<Schedule> affectedSchedules = schedulesMigrator.Migrate(
                    _schedules.Values,
                    LecturersMap
                );

                Boolean haveNoSavingErrors = _schedulesRepository.SaveSchedules(affectedSchedules);
                return haveNoSavingErrors && schedulesMigrator.HaveNoMigrationErrors;
            });
        }

        public Task<Boolean> UpdateSchedulesAsync()
        {
            if (LecturersMap == null || LecturersMap.Count == 0)
            {
                throw new InvalidOperationException("Lecturers map is null or empty.");
            }

            return Task.Run(async () =>
            {
                ServerSchedulesDownloader schedulesDownloader = new ServerSchedulesDownloader(_httpClient)
                {
                    Logger = _logger
                };

                IEnumerable<Schedule> updatedSchedules = await schedulesDownloader.DownloadSchedulesAsync(
                    _schedules.Values.Where(s => !s.IsActual).Select(s => s.ScheduleId).ToArray(),
                    LecturersMap,
                    false
                )
                .ConfigureAwait(false);

                Boolean haveNoSavingErrors = _schedulesRepository.SaveSchedules(
                    updatedSchedules,
                    (schedule) => _schedules[schedule.ScheduleId] = schedule
                );

                return schedulesDownloader.HaveNoDownloadingErrors && haveNoSavingErrors;
            });
        }

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<Int32> schedulesIds,
            Boolean shouldDownloadRelatedLecturersSchedules)
        {
            if (schedulesIds == null || schedulesIds.Count() == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(schedulesIds));
            }

            if (LecturersMap == null || LecturersMap.Count == 0)
            {
                throw new InvalidOperationException("Lecturers map is null or empty.");
            }

            return Task.Run(async () =>
            {
                ServerSchedulesDownloader schedulesDownloader = new ServerSchedulesDownloader(_httpClient)
                {
                    Logger = _logger
                };

                IEnumerable<Schedule> schedules = await schedulesDownloader.DownloadSchedulesAsync(
                    schedulesIds,
                    LecturersMap,
                    shouldDownloadRelatedLecturersSchedules
                )
                .ConfigureAwait(false);

                Boolean haveNoSavingErrors = _schedulesRepository.SaveSchedules(
                    schedules,
                    (schedule) => _schedules[schedule.ScheduleId] = schedule
                );

                return schedulesDownloader.HaveNoDownloadingErrors && haveNoSavingErrors;
            });
        }

        public Task<Boolean> RemoveScheduleAsync(Int32 scheduleId)
        {
            if (scheduleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scheduleId), "Number must be positive.");
            }

            return Task.Run(() =>
            {
                Boolean hasRemovingError = _schedulesRepository.RemoveSchedule(
                    _schedules[scheduleId].DisplayedName);

                return hasRemovingError || !_schedules.TryRemove(scheduleId, out Schedule _);
            });
        }

        public Task<Boolean> ReadCachedLecturersMapAsync()
        {
            return Task.Run(() =>
            {
                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                IsLecturersMapReadedFromCache = true;

                LecturersMap = lecturersRepository.ReadLecturersMap(out Boolean hasReadingError);

                return hasReadingError;
            });
        }

        public Task<Boolean> DownloadLecturersMapAsync()
        {
            return Task.Run(async () =>
            {
                // Если в этой сессии карта преподавателей уже загружалась с сайта, то нет необходимости
                // делать это вновь. За одно использование приложения она не успеет устареть.
                if (LecturersMap != null && !IsLecturersMapReadedFromCache)
                {
                    return false;
                }

                LocalLecturersRepository lecturersRepository = new LocalLecturersRepository(_storagePath)
                {
                    Logger = _logger
                };

                IsLecturersMapReadedFromCache = false;

                ServerLecturersDownloader lecturersDownloader = new ServerLecturersDownloader(_httpClient)
                {
                    Logger = _logger
                };

                LecturersMap = await lecturersDownloader.DownloadLecturersMapAsync().ConfigureAwait(false);

                if (lecturersDownloader.HaveNoDownloadingErrors)
                {
                    lecturersRepository.SaveLecturersMap(LecturersMap);
                }

                return lecturersDownloader.HaveNoDownloadingErrors;
            });
        }

        public Task<Boolean> ReadSchedulesAsync()
        {
            return Task.Run(() =>
            {
                IReadOnlyDictionary<Int32, Schedule> schedules = _schedulesRepository.ReadSchedules(
                    out Boolean haveNoReadingErrors);

                if (schedules == null)
                {
                    return true;
                }

                _schedules = new ConcurrentDictionary<Int32, Schedule>(schedules);

                return haveNoReadingErrors;
            });
        }

        public IEnumerable<Int32> GetSchedulesIdsBySearchRequests(IEnumerable<String> searchRequests)
        {
            if (LecturersMap == null || LecturersMap.Count == 0)
            {
                throw new InvalidOperationException("Lecturers map is null or empty.");
            }

            return searchRequests.Select(r => GetScheduleIdBySearchRequest(r)).Where(id => id != 0);
        }

        private Int32 GetScheduleIdBySearchRequest(String searchRequest)
        {
            if (String.IsNullOrWhiteSpace(searchRequest))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(searchRequest));
            }

            if (Int32.TryParse(searchRequest, out Int32 number))
            {
                return number;
            }

            return LecturersMap.ContainsKey(searchRequest) ? LecturersMap[searchRequest] : 0;
        }

        private readonly IHttpClient _httpClient;

        private ILogger _logger;

        private ConcurrentDictionary<Int32, Schedule> _schedules;

        private readonly String _storagePath;
        private readonly LocalSchedulesRepository _schedulesRepository;
    }
}