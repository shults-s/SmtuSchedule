using System;
using Android.OS;
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
            // Вместо "?android:attr/dialogPreferredPadding" в customDialogLayout.axml для уровней API ниже 22.
            _preferredPadding = Build.VERSION.SdkInt < BuildVersionCodes.LollipopMr1
                ? Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogPreferredPaddingApiLess22)
                : UiUtilities.GetAttributeValuePixelSize(Context, Resource.Attribute.dialogPreferredPadding);

            ShowEvent += (s, e) =>
            {
                WindowManagerLayoutParams layoutParameters = new WindowManagerLayoutParams();
                layoutParameters.CopyFrom(Window.Attributes);

                IWindowManager windowManager = (context as AppCompatActivity).WindowManager;
                (Int32 width, Int32 height) = UiUtilities.GetScreenPixelSize(windowManager);

                Int32 maxDialogHeight = (Int32)(height * 0.9);
                if (Window.DecorView.Height > maxDialogHeight)
                {
                    layoutParameters.Height = maxDialogHeight;
                }

                Window.Attributes = layoutParameters;
            };

            _layout = View.Inflate(Context, Resource.Layout.customDialogLayout, null) as ViewGroup;
            _layout.SetPadding(_preferredPadding, _preferredPadding, _preferredPadding, _preferredPadding);

            SetView(_layout);
        }

        public Button GetButton(DialogButtonType type)
        {
            return base.GetButton((Int32)type);
        }

        public new CustomAlertDialog SetTitle(String title)
        {
            Int32 size = Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogContentTopPadding);
            SetLayoutTopPadding(size);

            base.SetTitle(title);
            return this;
        }

        public new CustomAlertDialog SetTitle(Int32 titleId)
        {
            Int32 size = Context.Resources.GetDimensionPixelSize(Resource.Dimension.dialogContentTopPadding);
            SetLayoutTopPadding(size);

            base.SetTitle(titleId);
            return this;
        }

        public new CustomAlertDialog SetCancelable(Boolean flag)
        {
            base.SetCancelable(flag);
            return this;
        }

        public CustomAlertDialog SetMessage(Int32 messageId, Boolean useMarkdownFormatting = true)
        {
            return SetMessage(Context.Resources.GetString(messageId), useMarkdownFormatting);
        }

        public CustomAlertDialog SetMessage(String message, Boolean useMarkdownFormatting = true)
        {
            if (useMarkdownFormatting)
            {
                return SetMessage(message.FromMarkdown(), true);
            }

            return SetMessage(new Java.Lang.String(message), false);
        }

        public CustomAlertDialog SetMessage(Java.Lang.ICharSequence message, Boolean enableLinks = true)
        {
            View.Inflate(Context, Resource.Layout.dialogMessage, _layout);

            TextView textView = _layout.FindViewById<TextView>(Resource.Id.dialogMessageTextView);

            if (enableLinks)
            {
                textView.MovementMethod = LinkMovementMethod.Instance;
                textView.TextFormatted = message.StripUrlUnderlines();
            }
            else
            {
                textView.TextFormatted = message;
            }

            textView.TextFormatted = textView.TextFormatted.Trim();

            return this;
        }

        public CustomAlertDialog SetActions(String[] actionsTitles, Action<Int32> callback)
        {
            View.Inflate(Context, Resource.Layout.dialogListView, _layout);

            ListView view = _layout.FindViewById<ListView>(Resource.Id.dialogListView);
            view.ItemClick += (s, e) =>
            {
                Dismiss();
                callback(e.Position);
            };

            view.Adapter = new CustomArrayAdapter<String>(
                Context,
                Resource.Layout.dialogListItem,
                actionsTitles,
                _preferredPadding
            );

            _layout.SetPadding(0, _layout.PaddingTop, 0, _layout.PaddingTop);

            return this;
        }

        public CustomAlertDialog SetPositiveButton(Int32 textId, Action callback = null)
        {
            SetButton(DialogButtonType.Positive, Context.Resources.GetString(textId), callback);
            return this;
        }

        public CustomAlertDialog SetPositiveButton(String text, Action callback = null)
        {
            SetButton(DialogButtonType.Positive, text, callback);
            return this;
        }

        public CustomAlertDialog SetNegativeButton(Int32 textId, Action callback = null)
        {
            SetButton(DialogButtonType.Negative, Context.Resources.GetString(textId), callback);
            return this;
        }

        public CustomAlertDialog SetNegativeButton(String text, Action callback = null)
        {
            SetButton(DialogButtonType.Negative, text, callback);
            return this;
        }

        public CustomAlertDialog SetPositiveButtonEnabledOnlyWhenMessageScrolledToBottom()
        {
            // В настоящий момент реализовать это поведение для API < 22 невозможно из-за бага в Xamarin.
            // Событие ScrollChange не поддерживается и приводит к исключению:
            // Java.Lang.ClassNotFoundException: mono.android.view.View_OnScrollChangeListenerImplementor.
            // Реализация интерфейса View.IOnScrollChangeListener где бы то ни было приводит к исключению:
            // Java.Lang.ClassNotFoundException: Didn't find class ... on path: DexPathList[...].
            if (Build.VERSION.SdkInt < BuildVersionCodes.LollipopMr1)
            {
                return this;
            }

            ScrollView view = _layout.FindViewById<ScrollView>(Resource.Id.dialogMessageScrollView);
            if (view == null)
            {
                throw new InvalidOperationException("To use this method, you must set a message.");
            }

            ShowEvent += (s, e) => GetButton(DialogButtonType.Positive).Enabled = false;

            view.ScrollChange += (s, e) =>
            {
                Double scrollingSpaceHeight = view.GetChildAt(0).Height - view.Height;
                GetButton(DialogButtonType.Positive).Enabled = (scrollingSpaceHeight <= e.ScrollY);
            };

            return this;
        }

        private void SetButton(DialogButtonType type, String text, Action callback)
        {
            SetLayoutBottomPadding(0);
            base.SetButton((Int32)type, text, (s, e) => callback?.Invoke());
        }

        private void SetLayoutBottomPadding(Int32 sizeInPixels)
        {
            _layout.SetPadding(_layout.PaddingLeft, _layout.PaddingTop, _layout.PaddingRight, sizeInPixels);
        }

        private void SetLayoutTopPadding(Int32 sizeInPixels)
        {
            _layout.SetPadding(_layout.PaddingLeft, sizeInPixels, _layout.PaddingRight, _layout.PaddingBottom);
        }

        private readonly ViewGroup _layout;
        private readonly Int32 _preferredPadding;
    }
}