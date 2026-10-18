using PlantCare.Api.Domain;

namespace PlantCare.Tests.Unit;

/// <summary>AC3 – next due date and due/approaching status.</summary>
[Trait("AC", "AC3")]
public class CareScheduleTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);

    [Fact]
    public void NextDue_is_last_done_plus_interval()
    {
        Assert.Equal(new DateOnly(2026, 10, 25), CareSchedule.NextDue(new DateOnly(2026, 10, 18), 7));
    }

    [Fact]
    public void NextDue_crosses_month_and_year_boundaries()
    {
        Assert.Equal(new DateOnly(2027, 1, 3), CareSchedule.NextDue(new DateOnly(2026, 12, 20), 14));
    }

    [Fact]
    public void Task_falling_on_today_is_due()
    {
        // Watered 7 days ago with a 7-day interval -> due exactly today (boundary).
        var next = CareSchedule.NextDue(Today.AddDays(-7), 7);
        Assert.Equal(Today, next);
        Assert.Equal(CareStatus.Due, CareSchedule.StatusOf(next, Today));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void Task_before_today_is_overdue(int offset)
    {
        Assert.Equal(CareStatus.Overdue, CareSchedule.StatusOf(Today.AddDays(offset), Today));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)] // last day of the approaching window (boundary)
    public void Task_within_window_is_upcoming(int offset)
    {
        Assert.Equal(CareStatus.Upcoming, CareSchedule.StatusOf(Today.AddDays(offset), Today));
    }

    [Theory]
    [InlineData(3)] // first day outside the window (boundary)
    [InlineData(30)]
    public void Task_after_window_is_ok(int offset)
    {
        Assert.Equal(CareStatus.Ok, CareSchedule.StatusOf(Today.AddDays(offset), Today));
    }

    [Fact]
    public void Recording_care_moves_last_done_and_adds_event()
    {
        var plant = new Plant
        {
            Id = Guid.NewGuid(),
            WateringIntervalDays = 7,
            FeedingIntervalDays = 30,
            LastWateredOn = Today.AddDays(-7),
            LastFedOn = Today.AddDays(-10),
        };

        var careEvent = plant.RecordCare(CareType.Watering, Today, DateTimeOffset.UnixEpoch);

        Assert.Equal(Today, plant.LastWateredOn);
        Assert.Equal(Today.AddDays(-10), plant.LastFedOn); // feeding untouched
        Assert.Equal(CareType.Watering, careEvent.Type);
        Assert.Equal(Today, careEvent.Date);
        Assert.Single(plant.CareEvents);
        Assert.Equal(Today.AddDays(7), CareSchedule.NextDue(plant.LastWateredOn, plant.WateringIntervalDays));
    }
}
