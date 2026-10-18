namespace PlantCare.Api.Domain;

public enum CareType
{
    Watering,
    Feeding,
}

public enum CareStatus
{
    /// <summary>Next due date is more than <see cref="CareSchedule.UpcomingWindowDays"/> days away.</summary>
    Ok,
    /// <summary>Due within the next <see cref="CareSchedule.UpcomingWindowDays"/> days.</summary>
    Upcoming,
    /// <summary>Due today.</summary>
    Due,
    /// <summary>Due date already passed.</summary>
    Overdue,
}
