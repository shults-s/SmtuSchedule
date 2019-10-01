using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Graphics;
using Android.Text.Method;
using Android.Support.V4.App;
using SmtuSchedule.Core.Models;
using SmtuSchedule.Core.Enumerations;
using SmtuSchedule.Android.Utilities;
using SmtuSchedule.Android.Interfaces;

namespace SmtuSchedule.Android.Views
{
    // To do: Добавить логгер и залоггировать все события каждого фрагмента, начиная с конструктора и OnAttach,
    // и заканчивая логгированием события, приводящего к вылету.
    [DebuggerDisplay("Schedule fragment for {Date.ToShortDateString()}")]
    public class ScheduleFragment : Fragment
    {
        public DateTime Date { get; set; }

        //
        private Core.Interfaces.ILogger _logger;
        private String _fragmentId;
        //

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState)
        {
            //
            _logger.Log(_fragmentId + "OnCreateView");
            //

            Schedule schedule = _application.Manager.Schedules[_application.Preferences.CurrentScheduleId];
            Subject[] subjects = schedule.GetSubjects(_application.Preferences.UpperWeekDate, Date);

            View layout = null;

            if (subjects == null)
            {
                layout = inflater.Inflate(Resource.Layout.message, container, false);

                TextView message = layout.FindViewById<TextView>(Resource.Id.messageTextView);
                message.SetText(Resource.String.weekendMessage);

                return layout;
            }

            layout = inflater.Inflate(Resource.Layout.schedule, container, false);

            Int32 currentSubjectIndex = (Date != DateTime.Today)
                ? -1
                : Array.FindIndex(subjects, s => s.IsTimeInside(DateTime.Now));

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
                    i == currentSubjectIndex
                ));

                i += (numberOfRelatedSubjects == 0) ? 1 : numberOfRelatedSubjects + 1;
            }

            return layout;
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            _application = Context.ApplicationContext as ScheduleApplication;

            //
            _fragmentId = $"Fragment[{_application.Preferences.CurrentScheduleId}, {Date.ToShortDateString()}, {this.Class}]: ";
            _logger = _application.Logger;
            _logger.Log(_fragmentId + "OnAttach");
            //

            if (Activity is ISchedulesViewer viewer)
            {
                _switchScheduleCallback = viewer.ShowSchedule;
            }
            else
            {
                ;
            }

            _multiGroupsPrefix = Context.GetString(Resource.String.multiGroupSubjectPrefix);

            _tertiaryTextColor = new Color(UiUtilities.GetAttribute(
                Context,
                Resource.Attribute.textColorSubjectTertiary
            ));

            _secondaryTextColor = new Color(UiUtilities.GetAttribute(
                Context,
                Resource.Attribute.textColorSubjectSecondary
            ));

            _dividerColor = new Color(UiUtilities.GetAttribute(Context, Resource.Attribute.colorDivider));
        }

        //public ScheduleFragment()
        //{
        //    ;
        //}

        //~ScheduleFragment()
        //{
        //    ;
        //}

        public override void OnDetach()
        {
            base.OnDetach();

            //
            _logger.Log(_fragmentId + "OnDetach");
            _fragmentId = null;
            //

            _application = null;
            _multiGroupsPrefix = null;
            _switchScheduleCallback = null;
        }

        private View CreateSubjectView(LayoutInflater inflater, ViewGroup container, Subject subject,
            IEnumerable<Subject> relatedSubjects, Boolean needHighlight)
        {
            String t = DateTime.Now.ToString("HH:mm:ss.fff");

            View layout = inflater.Inflate(Resource.Layout.subject, container, false);

            if (needHighlight)
            {
                layout.SetBackgroundColor(_dividerColor);
            }

            TextView times = layout.FindViewById<TextView>(Resource.Id.subjectTimesTextView);
            times.Text = subject.From.ToString("HH:mm");

            TextView title = layout.FindViewById<TextView>(Resource.Id.subjectTitleTextView);
            title.Text = subject.Title + " " + t;

            TextView lecturer = layout.FindViewById<TextView>(Resource.Id.subjectLecturerTextView);
            lecturer.MovementMethod = LinkMovementMethod.Instance;
            lecturer.Text = @"¯\_(ツ)_/¯" + " " + t;

            TextView audience = layout.FindViewById<TextView>(Resource.Id.subjectAudienceTextView);
            audience.Text = subject.Audience;

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
                times.Append(subject.To.ToString("HH:mm").ToColored(_tertiaryTextColor));
            }

            Java.Lang.ICharSequence CreateSwitchScheduleClickableLink(String text, Int32 scheduleId)
            {
                SpannableString spannable = new SpannableString(text);

                CustomClickableSpan span = new CustomClickableSpan(_secondaryTextColor);
                //span.Click += () => _switchScheduleCallback?.Invoke(scheduleId);
                span.Click += () => _switchScheduleCallback(scheduleId);

                spannable.SetSpan(span, 0, spannable.Length(), SpanTypes.ExclusiveExclusive);
                return spannable;
            }

            if (relatedSubjects != null)
            {
                lecturer.Text = _multiGroupsPrefix + " ";

                Int32 scheduleId = subject.Group.ScheduleId;
                lecturer.Append(CreateSwitchScheduleClickableLink(scheduleId.ToString(), scheduleId));

                foreach (Subject related in relatedSubjects)
                {
                    scheduleId = related.Group.ScheduleId;

                    lecturer.Append(", ");
                    lecturer.Append(CreateSwitchScheduleClickableLink(scheduleId.ToString(), scheduleId));
                }
            }
            else
            {
                Lecturer lecturerOrGroup = subject.Lecturer ?? subject.Group;
                if (lecturerOrGroup != null)
                {
                    lecturer.SetText(
                        CreateSwitchScheduleClickableLink(lecturerOrGroup.Name + " " + t, lecturerOrGroup.ScheduleId),
                        TextView.BufferType.Spannable
                    );
                }
            }

            return layout;
        }

        private String _multiGroupsPrefix;

        private Color _dividerColor;
        private Color _tertiaryTextColor;
        private Color _secondaryTextColor;

        private ScheduleApplication _application;
        private Action<Int32> _switchScheduleCallback;
    }
}