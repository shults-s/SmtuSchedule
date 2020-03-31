using System;
using System.IO;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal sealed class LocalSchedulesRepository
    {
        public ILogger Logger { get; set; }

        public LocalSchedulesRepository(String storagePath) => _storagePath = storagePath;

        public Boolean Remove(Schedule schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            Boolean hasNoRemovingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                File.Delete(_storagePath + fileName);
            }
            {
                hasNoRemovingError = false;
                Logger?.Log(
                    new SchedulesRepositoryException($"Error of removing file \"{fileName}\".", exception));
                catch (IOException exception)
            }

            return hasNoRemovingError;
        }

        public Boolean Save(Schedule schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            Boolean hasNoSavingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                File.WriteAllText(_storagePath + fileName, schedule.ToJson());
            }
            catch (IOException exception)
            {
                hasNoSavingError = false;
                Logger?.Log(
                    new SchedulesRepositoryException($"Error of saving file \"{fileName}\".", exception));
            }

            return hasNoSavingError;
        }

        public Dictionary<Int32, Schedule> Read(out Boolean haveReadingErrors)
        {
            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>();

            haveReadingErrors = false;

            String[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(_storagePath, "*.json");
            }
            catch (IOException exception)
            {
                haveReadingErrors = true;
                Logger?.Log(
                    new SchedulesRepositoryException("Error of reading list of local schedules.", exception));

                return schedules;
            }

            foreach (String filePath in filePaths)
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
                    haveReadingErrors = true;

                    String fileName = Path.GetFileName(filePath);
                    Logger?.Log(
                        new SchedulesRepositoryException($"Error of reading file \"{fileName}\".", exception));
                }
            }

            return schedules;
        }

        private readonly String _storagePath;
    }
}