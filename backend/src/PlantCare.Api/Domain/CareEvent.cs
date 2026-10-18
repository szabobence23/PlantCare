namespace PlantCare.Api.Domain;

public class CareEvent
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public CareType Type { get; set; }
    /// <summary>Calendar day the care was done (Europe/Budapest).</summary>
    public DateOnly Date { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
