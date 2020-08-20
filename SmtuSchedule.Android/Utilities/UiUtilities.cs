using System;
using Android.Util;
using Android.Views;
using Android.Content;
using Android.Graphics;

namespace SmtuSchedule.Android.Utilities
{
    internal static class UiUtilities
    {
        public static (Int32 width, Int32 height) GetScreenPixelSize(IWindowManager manager)
        {
            WindowMetrics metrics = manager.CurrentWindowMetrics;
            WindowInsets windowInsets = metrics.WindowInsets;

            Int32 mask = WindowInsets.Type.NavigationBars() | WindowInsets.Type.DisplayCutout();
            Insets insets = windowInsets.GetInsetsIgnoringVisibility(mask);

            Size insetsSize = new Size(insets.Right + insets.Left, insets.Top + insets.Bottom);
            Rect bounds = metrics.Bounds;
            return (bounds.Width() - insetsSize.Width, bounds.Height() - insetsSize.Height);
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