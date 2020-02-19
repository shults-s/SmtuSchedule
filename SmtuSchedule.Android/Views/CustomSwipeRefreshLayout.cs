using System;
using Android.Util;
using Android.Views;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.Widget;

namespace SmtuSchedule.Android.Views
{
    [Register("shults.smtuschedule.SmtuSchedule.Android.Views.CustomSwipeRefreshLayout")]
    internal class CustomSwipeRefreshLayout : SwipeRefreshLayout
    {
        public CustomSwipeRefreshLayout(Context context, IAttributeSet attributes)
            : base(context, attributes)
        {
            _touchSlop = ViewConfiguration.Get(context).ScaledTouchSlop;
        }

        public override Boolean OnInterceptTouchEvent(MotionEvent @event)
        {
            switch (@event.Action)
            {
                case MotionEventActions.Down:
                    _isCurrentMotionRejected = false;
                    _previousX = @event.GetX();
                    break;

                case MotionEventActions.Move:
                    Single difference = Math.Abs(_previousX - @event.GetX());
                    if (_isCurrentMotionRejected || difference > _touchSlop)
                    {
                        _isCurrentMotionRejected = true;

                        // Если горизонтальное смещение при касании больше, чем то, которое
                        // интерпретируется как прокрутка (в данном случае горизонтальная),
                        // то это касание здесь не перехватывается, что позволяет дочернему
                        // представлению получить и обработать его.
                        return false;
                    }
                    break;
            }

            return base.OnInterceptTouchEvent(@event);
        }

        private readonly Int32 _touchSlop;

        private Single _previousX;
        private Boolean _isCurrentMotionRejected;
    }
}