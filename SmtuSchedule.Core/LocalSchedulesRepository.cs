using System;
using System.IO;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
// using SmtuSchedule.Core.Utilities;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal class LocalSchedulesRepository
    {
        public ILogger Logger { get; set; }

        public LocalSchedulesRepository(String storagePath) => _storagePath = storagePath;

        public Boolean Remove(Schedule schedule)
        {
            Boolean hasNoRemovingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                String filePath = _storagePath + fileName;
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                hasNoRemovingError = false;
                Logger?.Log(
                    new SchedulesRepositoryException($"Error of removing file \"{fileName}\".", exception));
            }

            return hasNoRemovingError;
        }

        public Boolean Save(Schedule schedule)
        {
            Boolean hasNoSavingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                String filePath = _storagePath + fileName;
                // FileThreadSafeUtilities.WriteAllText(filePath, schedule.ToJson());
                File.WriteAllText(filePath, schedule.ToJson());
            }
            catch (Exception exception)
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
            catch (Exception exception)
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
                    // String json = FileThreadSafeUtilities.ReadAllText(filePath);
                    String json = File.ReadAllText(filePath);
                    Schedule schedule = Schedule.FromJson(json);

                    schedule.Validate();

                    if (schedules.ContainsKey(schedule.ScheduleId))
                    {
                        throw new Exception("Schedule with same id already loaded.");
                    }

                    schedules[schedule.ScheduleId] = schedule;
                }
                catch (Exception exception)
                {
                    haveReadingErrors = true;

                    String fileName = Path.GetFileName(filePath);

                    Logger?.Log(
                        new SchedulesRepositoryException($"Error of reading file \"{fileName}\".", exception));

                    // Костыль для отключения повторяющихся уведомлений о невозможности открыть расписание.
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception removingException)
                    {
                        String message = $"Error of removing corrupted file \"{fileName}\".";
                        Logger?.Log(new SchedulesRepositoryException(message, removingException));
                    }
                }
            }

            return schedules;
        }

        private readonly String _storagePath;
    }
}