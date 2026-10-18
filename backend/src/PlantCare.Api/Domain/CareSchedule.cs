namespace PlantCare.Api.Domain;

/// <summary>
/// Pure due-date rules (spec AC3). Kept free of I/O so it can be unit tested directly.
/// </summary>
public static class CareSchedule
{
    /// <summary>A task due within this many days (but not today) is shown as "approaching".</summary>
    public const int UpcomingWindowDays = 2;

    public static DateOnly NextDue(DateOnly lastDone, int intervalDays) => lastDone.AddDays(intervalDays);

    public static CareStatus StatusOf(DateOnly nextDue, DateOnly today)
    {
        if (nextDue < today) return CareStatus.Overdue;
        if (nextDue == today) return CareStatus.Due;
        if (nextDue.DayNumber - today.DayNumber <= UpcomingWindowDays) return CareStatus.Upcoming;
        return CareStatus.Ok;
    }
}
