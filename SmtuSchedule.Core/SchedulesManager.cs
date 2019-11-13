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

        public Task<Boolean> DownloadSchedulesAsync(IEnumerable<String> searchRequests)
        {
            return Task.Run(async () =>
            {
                IsDownloadingInProgress = true;

                if (_lecturers == null)
                {
                    _lecturers = await LecturersLoader.DownloadAsync(Logger).ConfigureAwait(false);
                }

                ServerSchedulesLoader schedulesLoader = new ServerSchedulesLoader(_lecturers)
                {
                    Logger = Logger
                };

                Dictionary<Int32, Schedule> schedules = await schedulesLoader.DownloadAsync(searchRequests)
                    .ConfigureAwait(false);

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

        public async Task<IEnumerable<String>> DownloadLecturersNamesAsync()
        {
            if (_lecturers != null)
            {
                return _lecturers.Keys;
            }

            _lecturers = await LecturersLoader.DownloadAsync(Logger).ConfigureAwait(false);
            return _lecturers?.Keys;
        }

        public Task<Boolean> MigrateSchedulesAsync()
        {
            return Task.Run(() =>
            {
                IEnumerable<Schedule> affectedSchedules = SchedulesMigrator.Migrate(_schedules.Values);

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

                return haveSavingErrors;
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
        private Dictionary<String, Int32> _lecturers;
        private ConcurrentDictionary<Int32, Schedule> _schedules;
    }
}