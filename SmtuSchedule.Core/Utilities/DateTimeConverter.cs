using System;
using Newtonsoft.Json.Converters;

namespace SmtuSchedule.Core.Utilities
{
    internal class DateTimeConverter : IsoDateTimeConverter
    {
        public DateTimeConverter(String format) => DateTimeFormat = format;
    }
}