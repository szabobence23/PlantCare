namespace PlantCare.Api.Infrastructure;

public interface IAppClock
{
    DateTimeOffset Now { get; }
    /// <summary>Today's calendar date in the application's time zone.</summary>
    DateOnly Today { get; }
}

/// <summary>
/// "Today" must be the user's local calendar day, not the server's UTC day:
/// at 00:30 in Budapest it is still yesterday in UTC.
/// </summary>
public class AppClock(TimeProvider timeProvider, IConfiguration configuration) : IAppClock
{
    private readonly TimeZoneInfo _zone =
        TimeZoneInfo.FindSystemTimeZoneById(configuration["App:TimeZone"] ?? "Europe/Budapest");

    public DateTimeOffset Now => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, _zone).DateTime);
}
