using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    public class SchedulesManager
    {
        public IReadOnlyDictionary<Int32, Schedule> Schedules => _schedules;

        public Boolean IsDownloadingInProgress { get; private set; }

        public SchedulesManager(String storagePath)
        {
            _storagePath = storagePath;
            _reader = new LocalSchedulesReader();
            _loader = new ServerSchedulesLoader();
        }

        public void SetLogger(ILogger logger) => _logger = _reader.Logger = _loader.Logger = logger;

        public async Task<IEnumerable<String>> DownloadLecturersAsync()
        {
            return await _loader.DownloadLecturers();
        }

        public async Task<Boolean> RemoveScheduleAsync(Int32 scheduleId)
        {
            return await Task.Run(() =>
            {
                _schedules.TryRemove(scheduleId, out Schedule schedule);

                String fileName = schedule.DisplayedName + ".json";
                try
                {
                    File.Delete(_storagePath + fileName);
                    return false;
                }
                catch (Exception exception)
                {
                    _schedules[scheduleId] = schedule;
                    _logger?.Log($"Error of removing schedule file {fileName}: ", exception);
                    return true;
                }
            });
        }

        public async Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> requests)
        {
            IsDownloadingInProgress = true;

            Dictionary<Int32, Schedule> schedules = await _loader.DownloadAsync(requests)
                .ConfigureAwait(false);

            Boolean hasSavingErrors = false;

            foreach ((Int32 scheduleId, Schedule schedule) in schedules)
            {
                if (!SaveSchedule(schedule))
                {
                    hasSavingErrors = true;
                }
                else
                {
                    _schedules[scheduleId] = schedule;
                }
            }

            IsDownloadingInProgress = false;
            return _loader.HasDownloadingErrors || hasSavingErrors;
        }

        public async Task<Boolean> MigrateSchedulesAsync()
        {
            IEnumerable<Schedule> changedSchedules = MigrationUtility.Migrate(_schedules.Values);

            return await Task.Run(() =>
            {
                Boolean hasMigrationErrors = false;

                foreach (Schedule schedule in changedSchedules)
                {
                    if (!SaveSchedule(schedule))
                    {
                        hasMigrationErrors = true;
                    }
                }

                return hasMigrationErrors;
            });
        }

        public async Task<Boolean> ReadSchedulesAsync()
        {
            return await Task.Run(() =>
            {
                _schedules = new ConcurrentDictionary<Int32, Schedule>(_reader.Read(_storagePath));
                return _reader.HasReadingErrors;
            });
        }

        private Boolean SaveSchedule(Schedule schedule)
        {
            Boolean hasSavingErrors = false;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                File.WriteAllText(_storagePath + fileName, schedule.ToJson());
            }
            catch (Exception exception)
            {
                hasSavingErrors = true;
                _logger?.Log($"Error of saving schedule to file {fileName}: ", exception);
            }

            return hasSavingErrors;
        }

        private LocalSchedulesReader _reader;
        private ServerSchedulesLoader _loader;

        private ILogger _logger;
        private readonly String _storagePath;
        private ConcurrentDictionary<Int32, Schedule> _schedules;
    }
}