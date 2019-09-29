using System;
using Android.Util;
using Android.Content;

namespace SmtuSchedule.Android.Utilities
{
    internal static class UiUtilities
    {
        public static Int32 GetAttribute(Context context, Int32 attributeId)
        {
            TypedValue value = new TypedValue();
            return context.Theme.ResolveAttribute(attributeId, value, true) ? value.Data : 0;
        }

        public static Int32 GetAttributePixelSize(Context context, Int32 attributeId)
        {
            Int32 value = GetAttribute(context, attributeId);
            return TypedValue.ComplexToDimensionPixelSize(value, context.Resources.DisplayMetrics);
        }
    }
}