using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using SmtuSchedule.Core.Models;

namespace SmtuSchedule.Android.Views
{
    public class ScheduleFragment : Fragment
    {
        public ScheduleFragment(Subject[] subjects, Boolean highlightCurrentSubject,
            Action<Int32> switchScheduleCallback)
        {
            _subjects = subjects;
            _switchScheduleCallback = switchScheduleCallback;
            _highlightCurrentSubject = highlightCurrentSubject;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState)
        {
            Int32 currentIndex = _highlightCurrentSubject ? FindCurrentSubjectIndex(_subjects) : -1;

            View layout;

            if (_subjects == null)
            {
                layout = inflater.Inflate(Resource.Layout.message, container, false);

                TextView message = layout.FindViewById<TextView>(Resource.Id.messageTextView);
                message.SetText(Resource.String.weekendMessage);

                return layout;
            }

            layout = inflater.Inflate(Resource.Layout.schedule, container, false);

            TableLayout table = layout.FindViewById<TableLayout>(Resource.Id.scheduleTableLayout);
            for (Int32 i = 0; i < _subjects.Length; i++)
            {
                if (!_subjects[i].IsDisplayed)
                {
                    continue;
                }

                table.AddView(GetSubjectViewByIndex(inflater, table, i, currentIndex));
            }

            return layout;
        }

        private Int32 FindCurrentSubjectIndex(Subject[] subjects)
        {
            if (subjects == null)
            {
                return -1;
            }

            DateTime now = DateTime.Now;
            return Array.FindIndex(subjects, e => e.IsTimeInside(now));
        }

        private View GetSubjectViewByIndex(LayoutInflater inflater, ViewGroup container, Int32 index,
            Int32 currentIndex)
        {
            View layout = inflater.Inflate(Resource.Layout.subject, container, false);

            if (index == currentIndex)
            {
                layout.SetBackgroundResource(Resource.Color.accent);
            }

            TextView from = layout.FindViewById<TextView>(Resource.Id.subjectFromTextView);
            from.Text = _subjects[index].From.ToString("HH:mm");

            TextView title = layout.FindViewById<TextView>(Resource.Id.subjectTitleTextView);
            title.Text = _subjects[index].Title;

            TextView lecturer = layout.FindViewById<TextView>(Resource.Id.subjectLecturerTextView);
            lecturer.Text = @"¯\_(ツ)_/¯";

            Lecturer lecturerOrGroup = _subjects[index].Lecturer ?? _subjects[index].Group;
            if (lecturerOrGroup != null)
            {
                lecturer.Text = lecturerOrGroup.Name;
                lecturer.Click += (s, e) => _switchScheduleCallback(lecturerOrGroup.ScheduleId);
            }

            TextView audience = layout.FindViewById<TextView>(Resource.Id.subjectAudienceTextView);
            audience.Text = _subjects[index].Audience;

            return layout;
        }

        private Subject[] _subjects;
        private readonly Boolean _highlightCurrentSubject;
        private readonly Action<Int32> _switchScheduleCallback;
    }
}