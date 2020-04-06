using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Exceptions;

namespace SmtuSchedule.Core
{
    internal sealed class LocalLecturersRepository : ILecturersRepository
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private const String LecturersMapFileName = "Lecturers.json";

        public ILogger? Logger { get; set; }

        public LocalLecturersRepository(String storagePath)
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

        public Boolean SaveLecturersMap(IReadOnlyDictionary<String, Int32> lecturers)
        {
            Boolean hasNoSavingError = true;

            String filePath = Path.Join(_storagePath, LecturersMapFileName);
            try
            {
                String json = JsonSerializer.Serialize<IReadOnlyDictionary<String, Int32>>(lecturers, Options);
                File.WriteAllText(filePath, json);
            }
            catch (IOException exception)
            {
                hasNoSavingError = false;
                Logger?.Log(new LecturersRepositoryException($"Error of saving lecturers map file.", exception));
            }

            return hasNoSavingError;
        }

        public IReadOnlyDictionary<String, Int32>? ReadLecturersMap(out Boolean hasNoReadingError)
        {
            hasNoReadingError = true;

            String filePath = Path.Join(_storagePath, LecturersMapFileName);
            try
            {
                return JsonSerializer.Deserialize<Dictionary<String, Int32>>(File.ReadAllText(filePath), Options);
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException)
            {
                hasNoReadingError = false;
                Logger?.Log(new LecturersRepositoryException($"Error of reading lecturers map file.", exception));

                return null;
            }
        }

        private readonly String _storagePath;
    }
}