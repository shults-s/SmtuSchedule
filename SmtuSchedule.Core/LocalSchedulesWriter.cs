using System;
using System.IO;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal class LocalSchedulesWriter
    {
        public ILogger Logger { get; set; }

        public LocalSchedulesWriter(String storagePath) => _storagePath = storagePath;

        public Boolean Remove(Schedule schedule)
        {
            Boolean hasNoRemovingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                File.Delete(_storagePath + fileName);
            }
            catch (Exception exception)
            {
                hasNoRemovingError = false;
                Logger?.Log(
                    new SchedulesWriterException($"Error of removing schedule file {fileName}.", exception));
            }

            return hasNoRemovingError;
        }

        public Boolean Save(Schedule schedule)
        {
            Boolean hasNoSavingError = true;

            String fileName = schedule.DisplayedName + ".json";
            try
            {
                File.WriteAllText(_storagePath + fileName, schedule.ToJson());
            }
            catch (Exception exception)
            {
                hasNoSavingError = false;
                Logger?.Log(
                    new SchedulesWriterException($"Error of saving schedule to file {fileName}.", exception));
            }

            return hasNoSavingError;
        }

        private readonly String _storagePath;
    }
}