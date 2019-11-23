using System;
using Com.Getkeepsafe.Taptargetview;

namespace SmtuSchedule.Android.Utilities
{
    internal class TapTargetViewListener : TapTargetView.Listener
    {
        public event Action Clicked;

        public override void OnTargetClick(TapTargetView view)
        {
            base.OnTargetClick(view);
            Clicked?.Invoke();
        }

        public override void OnTargetLongClick(TapTargetView view)
        {
            base.OnTargetLongClick(view);
            Clicked?.Invoke();
        }
    }
}