using Com.Getkeepsafe.Taptargetview;

namespace SmtuSchedule.Android.Utilities
{
    internal static class TapTargetExtensions
    {
        public static TapTarget Stylize(this TapTarget target)
        {
            return target.DrawShadow(false)
                .TitleTextDimen(Resource.Dimension.extraLargeTextSize)
                .DescriptionTextDimen(Resource.Dimension.largeTextSize)
                .TextColor(Resource.Color.tapTargetViewText)
                .DimColor(Resource.Color.tapTargetViewDim)
                .TargetCircleColor(Resource.Color.tapTargetViewTarget)
                .OuterCircleColor(Resource.Color.tapTargetViewOuterCircle);
        }
    }
}