using System;
using Android.OS;
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
            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                DisplayMetrics displayMetrics = new DisplayMetrics();

#pragma warning disable CS0618
                manager.DefaultDisplay.GetMetrics(displayMetrics);
#pragma warning restore CS0618

                return (displayMetrics.WidthPixels, displayMetrics.HeightPixels);
            }

            WindowMetrics windowMetrics = manager.CurrentWindowMetrics;
            WindowInsets windowInsets = windowMetrics.WindowInsets;

            Int32 mask = WindowInsets.Type.NavigationBars() | WindowInsets.Type.DisplayCutout();
            Insets insets = windowInsets.GetInsetsIgnoringVisibility(mask);

            Size insetsSize = new Size(insets.Right + insets.Left, insets.Top + insets.Bottom);
            Rect bounds = windowMetrics.Bounds;
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