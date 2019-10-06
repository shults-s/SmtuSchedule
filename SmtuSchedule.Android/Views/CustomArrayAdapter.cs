using System;
using Android.Views;
using Android.Widget;
using Android.Content;

namespace SmtuSchedule.Android.Views
{
    internal class CustomArrayAdapter<T> : ArrayAdapter<T>
    {
        public CustomArrayAdapter(Context context, Int32 textViewResourceId, T[] objects,
            Int32 preferredPaddingInPixels) : base(context, textViewResourceId, objects)
        {
            _preferredPadding = preferredPaddingInPixels;
        }

        public override View GetView(Int32 position, View convertView, ViewGroup parent)
        {
            View view = base.GetView(position, convertView, parent);
            view.SetPadding(_preferredPadding, view.PaddingTop, _preferredPadding, view.PaddingBottom);
            return view;
        }

        private readonly Int32 _preferredPadding;
    }
}