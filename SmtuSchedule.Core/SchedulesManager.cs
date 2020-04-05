using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public sealed class SchedulesManager : ISchedulesManager
    {
        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public ILogger? Logger
        {
            get => _logger;
            set
            {
                _logger = value;
                _repository.Logger = value;
            }
        }

        public SchedulesManager(String storagePath) : this()
        {
            if (String.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(storagePath));
            }

            _repository = new LocalSchedulesRepository(storagePath);
        }

        internal SchedulesManager(ISchedulesRepository repository) : this()
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        private SchedulesManager()
        {
            _repository = null!;
            _httpClient = new HttpClientProxy();
            _schedules = new Dictionary<Int32, Schedule>();
        }

        public Task<Boolean> ReadSchedulesAsync()
        {
            return Task.Run(() =>
            {
                IEnumerable<Schedule>? schedules = _repository.ReadSchedules(out Boolean haveNoReadingErrors);
                if (schedules != null)
                {
                    schedules.ForEach(schedule => _schedules[schedule.ScheduleId] = schedule);
                }

                return haveNoReadingErrors;
            });
        }

        public Task<Boolean> MigrateSchedulesAsync(IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            return MigrateSchedulesAsync(new SchedulesMigrator() { Logger = _logger }, lecturersMap);
        }

        internal Task<Boolean> MigrateSchedulesAsync(ISchedulesMigrator migrator,
            IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            return Task.Run(() =>
            {
                IEnumerable<Schedule> affectedSchedules = migrator.Migrate(
                    _schedules.Values,
                    lecturersMap
                );

                Boolean haveNoSavingErrors = _repository.SaveSchedules(affectedSchedules);
                return migrator.HaveNoMigrationErrors && haveNoSavingErrors;
            });
        }

        public Task<Boolean> UpdateSchedulesAsync(IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            return UpdateSchedulesAsync(
                new ServerSchedulesDownloader(_httpClient) { Logger = _logger },
                lecturersMap
            );
        }

        internal Task<Boolean> UpdateSchedulesAsync(ISchedulesDownloader downloader,
            IReadOnlyDictionary<String, Int32> lecturersMap)
        {
            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            return Task.Run(async () =>
            {
                IEnumerable<Schedule> updatedSchedules = await downloader.DownloadSchedulesAsync(
                    _schedules.Values.Where(s => !s.IsActual).Select(s => s.ScheduleId).ToArray(),
                    lecturersMap,
                    false
                )
                .ConfigureAwait(false);

                Boolean haveNoSavingErrors = _repository.SaveSchedules(
                    updatedSchedules,
                    (schedule) => _schedules[schedule.ScheduleId] = schedule
                );

                return downloader.HaveNoDownloadingErrors && haveNoSavingErrors;
            });
        }

        public Task<Boolean> DownloadSchedulesAsync(
            IReadOnlyCollection<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap,
            Boolean shouldDownloadGroupsRelatedLecturersSchedules
        )
        {
            return DownloadSchedulesAsync(
                new ServerSchedulesDownloader(_httpClient) { Logger = _logger },
                schedulesIds,
                lecturersMap,
                shouldDownloadGroupsRelatedLecturersSchedules
            );
        }

        internal Task<Boolean> DownloadSchedulesAsync(
            ISchedulesDownloader downloader,
            IReadOnlyCollection<Int32> schedulesIds,
            IReadOnlyDictionary<String, Int32> lecturersMap,
            Boolean shouldDownloadGroupsRelatedLecturersSchedules
        )
        {
            if (schedulesIds == null || schedulesIds.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(schedulesIds));
            }

            if (lecturersMap == null || lecturersMap.Count == 0)
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(lecturersMap));
            }

            return Task.Run(async () =>
            {
                IEnumerable<Schedule> schedules = await downloader.DownloadSchedulesAsync(
                    schedulesIds,
                    lecturersMap,
                    shouldDownloadGroupsRelatedLecturersSchedules
                )
                .ConfigureAwait(false);

                Boolean haveNoSavingErrors = _repository.SaveSchedules(
                    schedules,
                    (schedule) => _schedules[schedule.ScheduleId] = schedule
                );

                return downloader.HaveNoDownloadingErrors && haveNoSavingErrors;
            });
        }

        public Task<Boolean> RemoveScheduleAsync(Int32 scheduleId)
        {
            if (scheduleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scheduleId), "Number must be positive.");
            }

            if (!_schedules.ContainsKey(scheduleId))
            {
                throw new ArgumentException("No schedule with this id.", nameof(scheduleId));
            }

            return Task.Run(() =>
            {
                Boolean hasNoRemovingError = _repository.RemoveSchedule(_schedules[scheduleId].DisplayedName);
                return hasNoRemovingError && _schedules.Remove(scheduleId, out Schedule _);
            });
        }

        private ILogger? _logger;
        private readonly IHttpClient _httpClient;
        private readonly ISchedulesRepository _repository;
        private readonly Dictionary<Int32, Schedule> _schedules;
    }
}