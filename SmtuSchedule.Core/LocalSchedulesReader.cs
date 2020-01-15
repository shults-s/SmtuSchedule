using System;
using System.IO;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal class LocalSchedulesReader
    {
        public Boolean HaveReadingErrors { get; private set; }

        public ILogger Logger { get; set; }

        public Dictionary<Int32, Schedule> Read(String storagePath)
        {
            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>();

            HaveReadingErrors = false;

            String[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(storagePath, "*.json");
            }
            catch(Exception exception)
            {
                HaveReadingErrors = true;

                Logger?.Log(
                    new SchedulesReaderException("Error of reading list of local schedules.", exception));

                return schedules;
            }

            foreach (String filePath in filePaths)
            {
                try
                {
                    Schedule schedule = Schedule.FromJson(File.ReadAllText(filePath));

                    if (schedules.ContainsKey(schedule.ScheduleId))
                    {
                        throw new Exception($"Schedule with same id already loaded.");
                    }

                    schedules[schedule.ScheduleId] = schedule;
                }
                catch(Exception exception)
                {
                    HaveReadingErrors = true;

                    String fileName = Path.GetFileName(filePath);
                    Logger?.Log(
                        new SchedulesReaderException($"Error of reading file \"{fileName}\".", exception));
                }
            }

            return schedules;
        }
    }
}