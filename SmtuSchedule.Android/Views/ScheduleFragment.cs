using System;
using System.Linq;
using System.Collections.Generic;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Graphics;
using Android.Text.Method;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;
using SmtuSchedule.Android.Utilities;
using SmtuSchedule.Android.Interfaces;

namespace SmtuSchedule.Android.Views
{
    public class ScheduleFragment : Fragment
    {
        public DateTime Date { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState)
        {
            Schedule schedule = _application.Manager.Schedules[_application.Preferences.CurrentScheduleId];
            Subject[] subjects = schedule.GetSubjects(_application.Preferences.UpperWeekDate, Date);

            Int32 FindCurrentSubjectIndex()
            {
                if (subjects == null)
                {
                    return -1;
                }

                DateTime now = DateTime.Now;
                return Array.FindIndex(subjects, e => e.IsTimeInside(now));
            }

            Int32 currentIndex = (Date == DateTime.Today) ? FindCurrentSubjectIndex() : -1;

            View layout = null;

            if (subjects == null)
            {
                layout = inflater.Inflate(Resource.Layout.message, container, false);

                TextView message = layout.FindViewById<TextView>(Resource.Id.messageTextView);
                message.SetText(Resource.String.weekendMessage);

                return layout;
            }

            layout = inflater.Inflate(Resource.Layout.schedule, container, false);

            TableLayout table = layout.FindViewById<TableLayout>(Resource.Id.scheduleTableLayout);
            for (Int32 i = 0; i < subjects.Length; )
            {
                Subject subject = subjects[i];

                if (!subject.IsDisplayed)
                {
                    continue;
                }

                // В расписании группы или аудитории может возникнуть ситуация, когда в одно и то же время
                // одно и то же занятие проходит у нескольких групп (поток). Для удобства восприятия имеет
                // смысл свернуть их отображение в одну строку таблицы.
                IEnumerable<Subject> relatedSubjects = null;
                Int32 numberOfRelatedSubjects = 0;

                if (schedule.Type == ScheduleType.Lecturer || schedule.Type == ScheduleType.Audience)
                {
                    relatedSubjects = subjects.Skip(i + 1).Where(
                        s => s.From == subject.From
                        && s.To == subject.To
                        && s.Audience == subject.Audience
                    );

                    numberOfRelatedSubjects = relatedSubjects.Count();
                }

                table.AddView(CreateSubjectView(
                    inflater,
                    table,
                    subject,
                    (numberOfRelatedSubjects != 0) ? relatedSubjects : null,
                    i == currentIndex
                ));

                i += (numberOfRelatedSubjects == 0) ? 1 : numberOfRelatedSubjects + 1;
            }

            return layout;
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            if (Activity is ISchedulesViewer viewer)
            {
                _switchScheduleCallback = viewer.ShowSchedule;
            }

            _application = Context.ApplicationContext as ScheduleApplication;

            _multiGroupPrefix = Context.GetString(Resource.String.multiGroupSubjectPrefix);
            _primaryText = new Color(ContextCompat.GetColor(Context, Resource.Color.primaryText));
            _secondaryText = new Color(ContextCompat.GetColor(Context, Resource.Color.secondaryText));
        }

        public override void OnDetach()
        {
            base.OnDetach();

            _application = null;
            _multiGroupPrefix = null;
            _switchScheduleCallback = null;
        }

        private View CreateSubjectView(LayoutInflater inflater, ViewGroup container, Subject current,
            IEnumerable<Subject> relatedSubjects, Boolean needHighlight)
        {
            View layout = inflater.Inflate(Resource.Layout.subject, container, false);

            if (needHighlight)
            {
                layout.SetBackgroundResource(Resource.Color.accent);
            }

            TextView times = layout.FindViewById<TextView>(Resource.Id.subjectTimesTextView);
            times.Text = current.From.ToString("HH:mm");

            TextView title = layout.FindViewById<TextView>(Resource.Id.subjectTitleTextView);
            title.Text = current.Title;

            TextView lecturer = layout.FindViewById<TextView>(Resource.Id.subjectLecturerTextView);
            lecturer.MovementMethod = LinkMovementMethod.Instance;
            lecturer.Text = @"¯\_(ツ)_/¯";

            TextView audience = layout.FindViewById<TextView>(Resource.Id.subjectAudienceTextView);
            audience.Text = current.Audience;
 
            if (_application.Preferences.DisplaySubjectEndTime)
            {
                // Высота левой ячейки (match_parent) определяется высотой правой ячейки (wrap_content),
                // с целью выровнять их по высоте для корректного позиционирования номера аудитории.
                // Если фактическая высота левой ячейки меньше, чем требуется ее содержимому,
                // то оно будет перекрываться. На этапе рендеринга, когда уже известно сколько места
                // при данном тексте и ширине экрана займет название предмета, высота его контейнера
                // задается так, чтобы высота правой ячейки превосходила высоту содержимого левой.
                // Эта ситуация возникает только если включено отображаение времени окончания занятий
                // и при этом название предмета умещается в одну строку.
                title.ViewTreeObserver.PreDraw += (s, e) =>
                {
                    if (title.LineCount < 2)
                    {
                        title.SetLines(2);
                    }

                    e.Handled = true;
                };

                times.Append("\n");
                times.Append(current.To.ToString("HH:mm").ToColored(_secondaryText));
            }

            Java.Lang.ICharSequence CreateSwitchScheduleClickableLink(String text, Int32 scheduleId)
            {
                SpannableString spannable = new SpannableString(text);

                CustomClickableSpan span = new CustomClickableSpan(_primaryText);
                span.Click += () => _switchScheduleCallback(scheduleId);

                spannable.SetSpan(span, 0, spannable.Length(), SpanTypes.ExclusiveExclusive);
                return spannable;
            }

            if (relatedSubjects != null)
            {
                lecturer.Text = _multiGroupPrefix + " ";

                Int32 scheduleId = current.Group.ScheduleId;
                lecturer.Append(CreateSwitchScheduleClickableLink(scheduleId.ToString(), scheduleId));

                foreach (Subject subject in relatedSubjects)
                {
                    scheduleId = subject.Group.ScheduleId;

                    lecturer.Append(", ");
                    lecturer.Append(CreateSwitchScheduleClickableLink(scheduleId.ToString(), scheduleId));
                }
            }
            else
            {
                Lecturer lecturerOrGroup = current.Lecturer ?? current.Group;
                if (lecturerOrGroup != null)
                {
                    lecturer.SetText(
                        CreateSwitchScheduleClickableLink(lecturerOrGroup.Name, lecturerOrGroup.ScheduleId),
                        TextView.BufferType.Normal
                    );
                }
            }

            return layout;
        }

        private Color _primaryText;
        private Color _secondaryText;
        private String _multiGroupPrefix;

        private ScheduleApplication _application;
        private Action<Int32> _switchScheduleCallback;
    }
}