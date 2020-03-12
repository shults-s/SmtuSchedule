using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
// using Newtonsoft.Json.Converters;

namespace SmtuSchedule.Core.Utilities
{
    // internal class JsonDateTimeConverter : IsoDateTimeConverter
    // {
    //     public JsonDateTimeConverter(String format) => DateTimeFormat = format;
    // }

    internal class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        public const String DefaultFormat = "dd.MM.yyyy HH:mm";

        public JsonDateTimeConverter() => _format = DefaultFormat;

        public JsonDateTimeConverter(String format) => _format = format;

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            String value = reader.GetString();
            return DateTime.ParseExact(value, _format, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
        }

        private readonly String _format;
    }
}