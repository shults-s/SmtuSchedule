using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmtuSchedule.Core.Utilities
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        public const String DefaultFormat = "dd.MM.yyyy HH:mm";

        public JsonDateTimeConverter() => _format = DefaultFormat;

        public JsonDateTimeConverter(String format)
        {
            if (String.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("String cannot be null, empty or whitespace.", nameof(format));
            }

            _format = format;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), _format, CultureInfo.InvariantCulture);
        }

        private readonly String _format;
    }
}