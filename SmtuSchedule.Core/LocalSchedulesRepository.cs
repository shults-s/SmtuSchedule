using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal sealed class LocalSchedulesRepository
    {
        public ILogger Logger { get; set; }

        public LocalSchedulesRepository(String storagePath)
        {
            if (String.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(storagePath));
            }

            if (!Directory.Exists(storagePath))
            {
                throw new DirectoryNotFoundException("Storage directory does not exists or is not accessible.");
            }

            _storagePath = storagePath;
        }

        public Boolean SaveSchedules(IEnumerable<Schedule> schedules,
            Action<Schedule> scheduleSavedSuccessfulCallback = null)
        {
            Boolean haveNoSavingErrors = true;

            foreach (Schedule schedule in schedules)
            {
                String fileName = schedule.DisplayedName + ".json";
                try
                {
                    File.WriteAllText(_storagePath + fileName, schedule.ToJson());
                    scheduleSavedSuccessfulCallback?.Invoke(schedule);
                }
                catch (IOException exception)
                {
                    haveNoSavingErrors = false;
                    Logger?.Log(new SchedulesRepositoryException($"Error of saving file '{fileName}'.", exception));
                }
            }

            return haveNoSavingErrors;
        }

        public Boolean RemoveSchedule(String displayedName)
        {
            Boolean hasNoRemovingError = true;

            String fileName = displayedName + ".json";
            try
            {
                File.Delete(_storagePath + fileName);
            }
            catch (IOException exception)
            {
                hasNoRemovingError = false;
                Logger?.Log(new SchedulesRepositoryException($"Error of removing file '{fileName}'.", exception));
            }

            return hasNoRemovingError;
        }

        public IReadOnlyDictionary<Int32, Schedule> ReadSchedules(out Boolean haveNoReadingErrors)
        {
            haveNoReadingErrors = true;

            String[] filesPaths;
            try
            {
                filesPaths = Directory.GetFiles(_storagePath, "*.json");
            }
            catch (IOException exception)
            {
                haveNoReadingErrors = false;
                Logger?.Log(new SchedulesRepositoryException("Error of reading list of local schedules.", exception));

                return null;
            }

            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>(filesPaths.Length);

            foreach (String filePath in filesPaths)
            {
                try
                {
                    Schedule schedule = Schedule.FromJson(File.ReadAllText(filePath));
                    schedule.Validate();

                    if (schedules.ContainsKey(schedule.ScheduleId))
                    {
                        throw new SchedulesRepositoryException(
                            $"Schedule with id '{schedule.ScheduleId}' already loaded.");
                    }

                    schedules[schedule.ScheduleId] = schedule;
                }
                catch (Exception exception) when (
                    exception is IOException
                    || exception is JsonException
                    || exception is SchedulesRepositoryException
                )
                {
                    haveNoReadingErrors = false;

                    String fileName = Path.GetFileName(filePath);
                    Logger?.Log(new SchedulesRepositoryException($"Error of reading file '{fileName}'.", exception));
                }
            }

            return schedules;
        }

        private readonly String _storagePath;
    }
}