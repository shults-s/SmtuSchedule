using System;

namespace SmtuSchedule.Core.Utilities
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal sealed class JsonDateConverter : JsonDateTimeConverter
    {
        public new const String DefaultFormat = "dd.MM.yyyy";

        public JsonDateConverter() : base(DefaultFormat)
        {
        }

        public JsonDateConverter(String format) : base(format)
        {
        }
    }
}