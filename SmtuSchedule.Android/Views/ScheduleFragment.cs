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
    [DebuggerDisplay("ScheduleFragment {Date.ToShortDateString()}")]
    public class ScheduleFragment : Fragment
    {
        public DateTime Date { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState)
        {
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

            if (Activity is ISchedulesViewer viewer)
            {
                _schedulesSwitcherCallback = viewer.ShowSchedule;
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

        public override void OnDetach()
        {
            base.OnDetach();

            _application = null;
            _multiGroupsPrefix = null;
            _schedulesSwitcherCallback = null;
        }

        private View CreateSubjectView(LayoutInflater inflater, ViewGroup container, Subject subject,
            IEnumerable<Subject> relatedSubjects, Boolean needHighlight)
        {
            View layout = inflater.Inflate(Resource.Layout.subject, container, false);

            if (needHighlight)
            {
                layout.SetBackgroundColor(_dividerColor);
            }

            TextView timesView = layout.FindViewById<TextView>(Resource.Id.subjectTimesTextView);
            timesView.Text = subject.From.ToString("HH:mm");

            TextView titleView = layout.FindViewById<TextView>(Resource.Id.subjectTitleTextView);
            titleView.Text = subject.Title;

            TextView lecturer = layout.FindViewById<TextView>(Resource.Id.subjectLecturerTextView);
            lecturer.MovementMethod = LinkMovementMethod.Instance;
            lecturer.Text = @"¯\_(ツ)_/¯";

            // Из-за одинаковых идентификаторов в разных фрагментах в LinkMovementMethod вероятно возникает
            // конфликт, в результате которого при переходе на другое расписание по щелчку на нем, все поля
            // lecturer нового фрагмента принимают значение, которое содержалось в поле, по которому ранее
            // щелкнули. Проблема решается присвоением каждому экземпляру поля уникального идентификатора.
            lecturer.Id = View.GenerateViewId();

            TextView audienceView = layout.FindViewById<TextView>(Resource.Id.subjectAudienceTextView);
            audienceView.Text = subject.Audience;

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
                titleView.ViewTreeObserver.PreDraw += (s, e) =>
                {
                    if (titleView.LineCount < 2)
                    {
                        titleView.SetLines(2);
                    }

                    e.Handled = true;
                };

                timesView.Append("\n");
                timesView.Append(subject.To.ToString("HH:mm").ToColored(_tertiaryTextColor));
            }

            if (relatedSubjects == null)
            {
                Lecturer lecturerOrGroup = subject.Lecturer ?? subject.Group;
                if (lecturerOrGroup != null)
                {
                    lecturer.Text = lecturerOrGroup.Name;
                    lecturer.Click += (s, e) => _schedulesSwitcherCallback(lecturerOrGroup.ScheduleId);
                }

                return layout;
            }

            CustomClickableSpan CreateSwitchSchedulesClickableSpan(Int32 scheduleId)
            {
                CustomClickableSpan span = new CustomClickableSpan(_secondaryTextColor);
                span.Click += () => _schedulesSwitcherCallback?.Invoke(scheduleId);
                return span;
            }

            using (SpannableStringBuilder builder = new SpannableStringBuilder(_multiGroupsPrefix + " "))
            {
                Int32 scheduleId = subject.Group.ScheduleId;

                CustomClickableSpan schedulesSwitcher = CreateSwitchSchedulesClickableSpan(scheduleId);
                builder.Append(scheduleId.ToString(), schedulesSwitcher, SpanTypes.ExclusiveExclusive);

                foreach (Subject relatedSubject in relatedSubjects)
                {
                    scheduleId = relatedSubject.Group.ScheduleId;

                    builder.Append(", ");

                    schedulesSwitcher = CreateSwitchSchedulesClickableSpan(scheduleId);
                    builder.Append(scheduleId.ToString(), schedulesSwitcher, SpanTypes.ExclusiveExclusive);
                }

                lecturer.SetText(builder, TextView.BufferType.Spannable);
            }

            return layout;
        }

        private String _multiGroupsPrefix;

        private Color _dividerColor;
        private Color _tertiaryTextColor;
        private Color _secondaryTextColor;

        private ScheduleApplication _application;
        private Action<Int32> _schedulesSwitcherCallback;
    }
}