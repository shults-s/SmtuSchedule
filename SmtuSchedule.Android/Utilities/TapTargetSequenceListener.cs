using System;
using Com.Getkeepsafe.Taptargetview;

namespace SmtuSchedule.Android.Utilities
{
    internal class TapTargetSequenceListener : Java.Lang.Object, TapTargetSequence.IListener
    {
        public event Action<Int32> Clicked;

        public void OnSequenceCanceled(TapTarget lastTarget)
        {
        }

        public void OnSequenceFinish()
        {
        }

        public void OnSequenceStep(TapTarget lastTarget, Boolean targetClicked)
        {
            Clicked?.Invoke(lastTarget.Id());
        }
    }
}