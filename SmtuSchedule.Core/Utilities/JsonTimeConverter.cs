using System;

namespace SmtuSchedule.Core.Utilities
{
    internal class JsonTimeConverter : JsonDateTimeConverter
    {
        public new const String DefaultFormat = "HH:mm";

        public JsonTimeConverter() : base(DefaultFormat)
        {
        }

        public JsonTimeConverter(String format) : base(format)
        {
        }
    }
}