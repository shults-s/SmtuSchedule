using System;

namespace SmtuSchedule.Core.Utilities
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal sealed class JsonTimeConverter : JsonDateTimeConverter
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