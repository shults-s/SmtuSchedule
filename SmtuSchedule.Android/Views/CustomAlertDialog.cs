using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Runtime;
using Android.Text.Method;
using Android.Support.V7.App;
using SmtuSchedule.Android.Utilities;

namespace SmtuSchedule.Android.Views
{
    internal class CustomAlertDialog : AlertDialog
    {
        // Bugfix: Unable to activate instance of type ... from native handle ...
        public CustomAlertDialog(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public CustomAlertDialog(Context context) : base(context)
        {
            _layout = View.Inflate(Context, Resource.Layout.customDialog, null);

            // Вместо android:padding = "?android:attr/dialogPreferredPadding" в customDialog.axml для API < 22.
            Int32 paddingInPx = Build.VERSION.SdkInt < BuildVersionCodes.LollipopMr1
                ? Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogContentPaddingApiLevelLess22)
                : UiUtilities.GetAttributePixelSize(Context, Resource.Attribute.dialogPreferredPadding);

            _layout.SetPadding(paddingInPx, paddingInPx, paddingInPx, _layout.PaddingBottom);

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
            textView.TextFormatted = message.Trim().StripUrlUnderlines();
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

        public CustomAlertDialog SetPositiveButtonEnabledOnlyThenContentScrolledToBottom()
        {
            ScrollView view = _layout.FindViewById<ScrollView>(Resource.Id.customDialogScrollView);
            if (view == null)
            {
                return this;
            }

            ShowEvent += (s, e) => GetButton((Int32)DialogButtonType.Positive).Enabled = false;

            view.ScrollChange += (s, e) =>
            {
                Double scrollingSpace = view.GetChildAt(0).Height - view.Height;
                GetButton((Int32)DialogButtonType.Positive).Enabled = (scrollingSpace <= e.ScrollY);
            };

            return this;
        }

        private void SetButton(DialogButtonType type, String text, Action callback)
        {
            SetContentBottomPadding(0);
            base.SetButton((Int32)type, text, (s, e) => callback?.Invoke());
        }

        private void SetContentBottomPadding(Int32 sizeInPx)
        {
            _content.SetPadding(_content.PaddingLeft, _content.PaddingTop, _content.PaddingRight, sizeInPx);
        }

        private void SetContentTopPadding(Int32 sizeInPx)
        {
            _content.SetPadding(_content.PaddingLeft, sizeInPx, _content.PaddingRight, _content.PaddingBottom);
        }

        private readonly View _layout;
        private readonly FrameLayout _content;
    }
}