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
using SmtuSchedule.Core.Interfaces;
using SmtuSchedule.Core.Enumerations;
using SmtuSchedule.Android.Utilities;
using SmtuSchedule.Android.Interfaces;

namespace SmtuSchedule.Android.Views
{
    [DebuggerDisplay("ScheduleFragment {Date.ToShortDateString()}")]
    internal class ScheduleFragment : Fragment
    {
        private const String DateSavedInstanceStateKey = "Date";

        public DateTime Date { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState)
        {
            // В фоновом режиме система может завершить процесс приложения, а при повторном запуске порядок
            // создания объектов изменится.
            // Холодный запуск:
            //     ScheduleApplication --> MainActivity --> ScheduleFragment #1 ... ScheduleFragment #N.
            // Возврат из фонового режима:
            //     ScheduleApplication --> ScheduleFragment #1 ... ScheduleFragment #N --> MainActivity.
            // В итоге данный метод может быть вызван еще до того как расписания будут считаны из памяти.
            if (_application.Manager?.Schedules == null)
            {
                return null;
            }

            if (Date == default(DateTime))
            {
                if (savedInstanceState != null && savedInstanceState.ContainsKey(DateSavedInstanceStateKey))
                {
                    Date = new DateTime(savedInstanceState.GetLong(DateSavedInstanceStateKey));
                }
                else
                {
                    throw new InvalidOperationException("Date property value is not set.");
                }
            }

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

            layout = inflater.Inflate(Resource.Layout.schedulePage, container, false);

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

                if (schedule.Type == ScheduleType.Lecturer || schedule.Type == ScheduleType.Auditorium)
                {
                    relatedSubjects = subjects.Skip(i + 1).Where(
                        s => s.From == subject.From
                        && s.To == subject.To
                        && s.Auditorium == subject.Auditorium
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

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutLong(DateSavedInstanceStateKey, Date.Ticks);
        }

        public override void OnAttach(Context context)
        {
            base.OnAttach(context);

            _application = Context.ApplicationContext as SmtuScheduleApplication;

            if (Activity is ISchedulesViewer viewer)
            {
                _schedulesSwitcherCallback = viewer.ShowSchedule;
            }

            _dividerColor = new Color(UiUtilities.GetAttributeValue(
                Context,
                Resource.Attribute.colorDivider
            ));

            _tertiaryTextColor = new Color(UiUtilities.GetAttributeValue(
                Context,
                Resource.Attribute.textColorSubjectTertiary
            ));

            _secondaryTextColor = new Color(UiUtilities.GetAttributeValue(
                Context,
                Resource.Attribute.textColorSubjectSecondary
            ));

            _multiGroupsPrefix = Context.GetString(Resource.String.multiGroupSubjectPrefix);
        }

        public override void OnDetach()
        {
            base.OnDetach();

            _application = null;
            _multiGroupsPrefix = null;
            _schedulesSwitcherCallback = null;
        }

        private View CreateSubjectView(LayoutInflater inflater, ViewGroup container, Subject subject,
            IEnumerable<Subject> relatedSubjects, Boolean shouldHighlightSubject)
        {
            View layout = inflater.Inflate(Resource.Layout.subject, container, false);

            if (shouldHighlightSubject)
            {
                layout.SetBackgroundColor(_dividerColor);
            }

            TextView times = layout.FindViewById<TextView>(Resource.Id.subjectTimesTextView);
            times.Text = subject.From.ToString("HH:mm");

            TextView title = layout.FindViewById<TextView>(Resource.Id.subjectTitleTextView);
            title.Text = subject.Title;

            TextView lecturer = layout.FindViewById<TextView>(Resource.Id.subjectLecturerTextView);
            lecturer.MovementMethod = LinkMovementMethod.Instance;
            lecturer.Text = @"¯\_(ツ)_/¯";

            // Из-за одинаковых идентификаторов в разных фрагментах в LinkMovementMethod вероятно возникает
            // конфликт, в результате которого при переходе на другое расписание по щелчку на нем, все поля
            // lecturer нового фрагмента принимают значение, которое содержалось в поле, по которому ранее
            // щелкнули. Проблема решается присвоением каждому экземпляру поля уникального идентификатора.
            lecturer.Id = View.GenerateViewId();

            TextView auditorium = layout.FindViewById<TextView>(Resource.Id.subjectAuditoriumTextView);
            auditorium.Text = subject.Auditorium;

            if (_application.Preferences.DisplaySubjectEndTime)
            {
                // Поле lecturer выравнивается на одной высоте с audience при однострочном значении title.
                title.SetMinLines(2);

                times.Append("\n");
                times.Append(subject.To.ToString("HH:mm").ToColored(_tertiaryTextColor));
            }

            if (relatedSubjects == null)
            {
                IScheduleReference lecturerOrGroup = subject.Lecturer as IScheduleReference
                    ?? subject.Group as IScheduleReference;

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

            using SpannableStringBuilder builder = new SpannableStringBuilder(_multiGroupsPrefix + " ");

            Int32 scheduleId = subject.Group.ScheduleId;

            CustomClickableSpan schedulesSwitcherSpan = CreateSwitchSchedulesClickableSpan(scheduleId);
            builder.Append(scheduleId.ToString(), schedulesSwitcherSpan, SpanTypes.ExclusiveExclusive);

            foreach (Subject relatedSubject in relatedSubjects)
            {
                scheduleId = relatedSubject.Group.ScheduleId;

                builder.Append(", ");

                schedulesSwitcherSpan = CreateSwitchSchedulesClickableSpan(scheduleId);
                builder.Append(scheduleId.ToString(), schedulesSwitcherSpan, SpanTypes.ExclusiveExclusive);
            }

            lecturer.SetText(builder, TextView.BufferType.Spannable);

            return layout;
        }

        private String _multiGroupsPrefix;

        private Color _dividerColor;
        private Color _tertiaryTextColor;
        private Color _secondaryTextColor;

        private SmtuScheduleApplication _application;
        private Action<Int32> _schedulesSwitcherCallback;
    }
}