namespace SmtuSchedule.Core.Enumerations
{
    public enum ScheduleType
    {
        NotSet, // Тип расписания не задан (для миграции с версий ниже 0.9).
        Group,
        Lecturer,
        Auditorium
    }
}