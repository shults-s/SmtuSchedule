using System;
using System.IO;
using System.Collections.Generic;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;

namespace SmtuSchedule.Core
{
    internal class LocalSchedulesReader
    {
        public Boolean HasReadingErrors { get; private set; }

        public ILogger Logger { get; set; }

        public Dictionary<Int32, Schedule> Read(String storagePath)
        {
            Dictionary<Int32, Schedule> schedules = new Dictionary<Int32, Schedule>();
            HasReadingErrors = false;

            String[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(storagePath, "*.json");
            }
            catch(Exception exception)
            {
                HasReadingErrors = true;

                Logger?.Log("Error of reading local schedules list: ", exception);
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
                    HasReadingErrors = true;

                    String fileName = Path.GetFileName(filePath);
                    Logger?.Log($"Error of loading file \"{fileName}\": ", exception);
                }
            }

            return schedules;
        }
    }
}