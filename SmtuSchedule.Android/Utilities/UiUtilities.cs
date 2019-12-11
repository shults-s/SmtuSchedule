using System;
using Android.Util;
using Android.Views;
using Android.Content;

namespace SmtuSchedule.Android.Utilities
{
    internal static class UiUtilities
    {
        public static (Int32 width, Int32 height) GetScreenPixelSize(IWindowManager manager)
        {
            DisplayMetrics metrics = new DisplayMetrics();
            manager.DefaultDisplay.GetMetrics(metrics);

            return (metrics.WidthPixels, metrics.HeightPixels);
        }

        public static Int32 GetAttributeValue(Context context, Int32 attributeId)
        {
            TypedValue value = new TypedValue();
            return context.Theme.ResolveAttribute(attributeId, value, true) ? value.Data : 0;
        }

        public static Int32 GetAttributeValuePixelSize(Context context, Int32 attributeId)
        {
            Int32 value = GetAttributeValue(context, attributeId);
            return TypedValue.ComplexToDimensionPixelSize(value, context.Resources.DisplayMetrics);
        }
    }
}