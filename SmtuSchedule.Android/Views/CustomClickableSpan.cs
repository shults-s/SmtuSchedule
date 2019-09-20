using System;
using Android.Text;
using Android.Views;
using Android.Graphics;
using Android.Text.Style;

namespace SmtuSchedule.Android.Views
{
    class CustomClickableSpan : ClickableSpan
    {
        public event Action Click;

        public CustomClickableSpan(Color color) => _color = color;

        public override void UpdateDrawState(TextPaint drawState)
        {
            base.UpdateDrawState(drawState);
            drawState.Color = _color;
            drawState.UnderlineText = false;
        }

        public override void OnClick(View widget) => Click?.Invoke();

        private Color _color;
    }
}