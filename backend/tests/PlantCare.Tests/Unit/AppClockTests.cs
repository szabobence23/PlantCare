using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using PlantCare.Api.Infrastructure;

namespace PlantCare.Tests.Unit;

/// <summary>AC3 – "today" is the Budapest calendar day, not the UTC day.</summary>
[Trait("AC", "AC3")]
public class AppClockTests
{
    private static AppClock ClockAt(DateTimeOffset utc) =>
        new(new FakeTimeProvider(utc), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:TimeZone"] = "Europe/Budapest" })
            .Build());

    [Fact]
    public void Shortly_after_midnight_in_Budapest_is_already_the_next_day()
    {
        // 22:30 UTC on 18 Oct = 00:30 CEST on 19 Oct.
        var clock = ClockAt(new DateTimeOffset(2026, 10, 18, 22, 30, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 10, 19), clock.Today);
    }

    [Fact]
    public void Winter_time_offset_is_one_hour()
    {
        // 23:30 UTC on 15 Dec = 00:30 CET on 16 Dec; 22:30 UTC is still 15 Dec.
        Assert.Equal(new DateOnly(2026, 12, 16), ClockAt(new DateTimeOffset(2026, 12, 15, 23, 30, 0, TimeSpan.Zero)).Today);
        Assert.Equal(new DateOnly(2026, 12, 15), ClockAt(new DateTimeOffset(2026, 12, 15, 22, 30, 0, TimeSpan.Zero)).Today);
    }
}
