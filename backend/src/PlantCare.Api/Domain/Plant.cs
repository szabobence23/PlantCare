namespace PlantCare.Api.Domain;

public class Plant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Species { get; set; }
    public int WateringIntervalDays { get; set; }
    public int FeedingIntervalDays { get; set; }
    public DateOnly LastWateredOn { get; set; }
    public DateOnly LastFedOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<CareEvent> CareEvents { get; set; } = [];
    public List<PlantPhoto> Photos { get; set; } = [];

    /// <summary>
    /// Records a watering/feeding done on <paramref name="date"/> (spec AC4):
    /// moves the "last done" date, which in turn moves the next due date.
    /// </summary>
    public CareEvent RecordCare(CareType type, DateOnly date, DateTimeOffset recordedAt)
    {
        if (type == CareType.Watering) LastWateredOn = date;
        else LastFedOn = date;

        var careEvent = new CareEvent
        {
            Id = Guid.NewGuid(),
            PlantId = Id,
            Type = type,
            Date = date,
            RecordedAt = recordedAt,
        };
        CareEvents.Add(careEvent);
        return careEvent;
    }
}
