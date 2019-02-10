using System;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Text.Method;
using Android.Support.V7.App;
using SmtuSchedule.Android.Utilities;

namespace SmtuSchedule.Android.Views
{
    internal class CustomAlertDialog : AlertDialog
    {
        public CustomAlertDialog(Context context) : base(context)
        {
            _layout = View.Inflate(Context, Resource.Layout.customDialog, null);
            SetView(_layout);

            _content = _layout.FindViewById<FrameLayout>(Resource.Id.customDialogContentFrameLayout);
        }

        public new CustomAlertDialog SetTitle(String title)
        {
            Int32 size = Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogContentTopPadding);
            SetContentTopPadding(size);

            base.SetTitle(title);
            return this;
        }

        public new CustomAlertDialog SetTitle(Int32 titleId)
        {
            Int32 size = Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogContentTopPadding);
            SetContentTopPadding(size);

            base.SetTitle(titleId);
            return this;
        }

        public new CustomAlertDialog SetCancelable(Boolean flag)
        {
            base.SetCancelable(flag);
            return this;
        }

        public new CustomAlertDialog SetMessage(String message)
        {
            return SetMessage(new Java.Lang.String(message));
        }

        public CustomAlertDialog SetMessage(Int32 messageId)
        {
            return SetMessage(Context.GetTextFormatted(messageId));
        }

        public new CustomAlertDialog SetMessage(Java.Lang.ICharSequence message)
        {
            TextView textView = _layout.FindViewById<TextView>(Resource.Id.customDialogMessageTextView);
            textView.MovementMethod = LinkMovementMethod.Instance;
            textView.TextFormatted = message.Trim();
            return this;
        }

        public CustomAlertDialog SetPositiveButton(Int32 textId, Action callback = null)
        {
            SetButton(DialogButtonType.Positive, Context.GetString(textId), callback);
            return this;
        }

        public CustomAlertDialog SetPositiveButton(String text, Action callback = null)
        {
            SetButton(DialogButtonType.Positive, text, callback);
            return this;
        }

        public CustomAlertDialog SetNegativeButton(Int32 textId, Action callback = null)
        {
            SetButton(DialogButtonType.Negative, Context.GetString(textId), callback);
            return this;
        }

        public CustomAlertDialog SetNegativeButton(String text, Action callback = null)
        {
            SetButton(DialogButtonType.Negative, text, callback);
            return this;
        }

        private void SetButton(DialogButtonType type, String text, Action callback)
        {
            SetContentBottomPadding(0);
            base.SetButton((Int32)type, text, (s, e) => callback?.Invoke());
        }

        private void SetContentBottomPadding(Int32 size)
        {
            _content.SetPadding(_content.PaddingLeft, _content.PaddingTop, _content.PaddingRight, size);
        }

        private void SetContentTopPadding(Int32 size)
        {
            _content.SetPadding(_content.PaddingLeft, size, _content.PaddingRight, _content.PaddingBottom);
        }

        private View _layout;
        private FrameLayout _content;
    }
}