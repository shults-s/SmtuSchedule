using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal class LocalLecturersRepository
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private const String LecturersMapFileName = "Lecturers.json";

        public ILogger Logger { get; set; }

        public LocalLecturersRepository(String storagePath) => _storagePath = storagePath;

        public Boolean Save(Dictionary<String, Int32> lecturers)
        {
            Boolean hasNoSavingError = true;

            String filePath = _storagePath + LecturersMapFileName;
            try
            {
                String json = JsonSerializer.Serialize<Dictionary<String, Int32>>(lecturers, Options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception exception)
            {
                hasNoSavingError = false;
                Logger?.Log(new LecturersRepositoryException($"Error of saving lecturers map file.", exception));
            }

            return hasNoSavingError;
        }

        public Dictionary<String, Int32> Read()
        {
            String filePath = _storagePath + LecturersMapFileName;
            try
            {
                return JsonSerializer.Deserialize<Dictionary<String, Int32>>(File.ReadAllText(filePath), Options);
            }
            catch (Exception exception)
            {
                Logger?.Log(new LecturersRepositoryException($"Error of reading lecturers map file.", exception));
                return null;
            }
        }

        private readonly String _storagePath;
    }
}