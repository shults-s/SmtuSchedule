using System;
using Android.Util;
using Android.Content;

namespace SmtuSchedule.Android.Utilities
{
    internal static class UiUtilities
    {
        public static Int32 GetAttributePixelSize(Context context, Int32 attributeId)
        {
            TypedValue value = new TypedValue();

            return context.Theme.ResolveAttribute(attributeId, value, true)
                ? TypedValue.ComplexToDimensionPixelSize(value.Data, context.Resources.DisplayMetrics)
                : 0;
        }
    }
}