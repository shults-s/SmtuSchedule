using System;
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

        public ILogger Logger { get; set; }

        public SchedulesManager(String storagePath) => _storagePath = storagePath;

        public async Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests)
        {
            return await Task.Run(async () =>
            {
                IsDownloadingInProgress = true;

                ServerSchedulesLoader loader = new ServerSchedulesLoader()
                {
                    Logger = Logger,
                    Lecturers = _lecturers
                };

                Dictionary<Int32, Schedule> schedules = await loader.DownloadAsync(searchRequests)
                    .ConfigureAwait(false);

                LocalSchedulesWriter writer = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                Boolean haveSavingErrors = false;

                foreach ((Int32 scheduleId, Schedule schedule) in schedules)
                {
                    if (!writer.Save(schedule))
                    {
                        haveSavingErrors = true;
                    }
                    else
                    {
                        _schedules[scheduleId] = schedule;
                    }
                }

                IsDownloadingInProgress = false;
                return loader.HaveDownloadingErrors || haveSavingErrors;
            });
        }

        public async Task<IEnumerable<String>> DownloadLecturersNamesAsync()
        {
            if (_lecturers != null)
            {
                return _lecturers.Keys;
            }

            _lecturers = await LecturersLoader.Download(Logger);
            return _lecturers?.Keys;
        }

        public async Task<Boolean> MigrateSchedulesAsync()
        {
            return await Task.Run(() =>
            {
                IEnumerable<Schedule> affected = SchedulesMigrator.Migrate(_schedules.Values);

                LocalSchedulesWriter writer = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                Boolean haveSavingErrors = false;

                foreach (Schedule schedule in affected)
                {
                    if (!writer.Save(schedule))
                    {
                        haveSavingErrors = true;
                    }
                }

                return haveSavingErrors;
            });
        }

        public async Task<Boolean> RemoveScheduleAsync(Int32 scheduleId)
        {
            return await Task.Run(() =>
            {
                LocalSchedulesWriter writer = new LocalSchedulesWriter(_storagePath)
                {
                    Logger = Logger
                };

                if (!writer.Remove(_schedules[scheduleId]))
                {
                    return true;
                }

                _schedules.TryRemove(scheduleId, out Schedule schedule);
                return false;
            });
        }

        public async Task<Boolean> ReadSchedulesAsync()
        {
            return await Task.Run(() =>
            {
                LocalSchedulesReader reader = new LocalSchedulesReader()
                {
                    Logger = Logger
                };

                _schedules = new ConcurrentDictionary<Int32, Schedule>(reader.Read(_storagePath));
                return reader.HaveReadingErrors;
            });
        }

        private readonly String _storagePath;
        private Dictionary<String, Int32> _lecturers;
        private ConcurrentDictionary<Int32, Schedule> _schedules;
    }
}