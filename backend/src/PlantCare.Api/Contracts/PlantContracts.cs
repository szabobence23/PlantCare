using PlantCare.Api.Domain;
using PlantCare.Api.Recognition;

namespace PlantCare.Api.Contracts;

/// <summary>multipart/form-data body of POST /api/plants. Nullable so missing or malformed fields reach our validator.</summary>
public class CreatePlantForm
{
    public string? Name { get; set; }
    public string? Species { get; set; }
    public int? WateringIntervalDays { get; set; }
    public int? FeedingIntervalDays { get; set; }
    public DateOnly? LastWateredOn { get; set; }
    public DateOnly? LastFedOn { get; set; }
    /// <summary>Optional photo; stored as the plant's first growth photo.</summary>
    public IFormFile? Photo { get; set; }
}

public record RecordCareRequest(CareType? Type);

public record CareTaskDto(DateOnly LastDoneOn, int IntervalDays, DateOnly NextDueOn, int DaysUntilDue, CareStatus Status)
{
    public static CareTaskDto Create(DateOnly lastDone, int intervalDays, DateOnly today)
    {
        var next = CareSchedule.NextDue(lastDone, intervalDays);
        return new(lastDone, intervalDays, next, next.DayNumber - today.DayNumber, CareSchedule.StatusOf(next, today));
    }
}

public record PlantSummaryDto(
    Guid Id,
    string Name,
    string? Species,
    CareTaskDto Watering,
    CareTaskDto Feeding,
    Guid? LatestPhotoId);

public record CareEventDto(Guid Id, CareType Type, DateOnly Date, DateTimeOffset RecordedAt);

public record PhotoDto(Guid Id, DateTimeOffset UploadedAt, long SizeBytes);

public record PlantDetailDto(
    Guid Id,
    string Name,
    string? Species,
    DateTimeOffset CreatedAt,
    CareTaskDto Watering,
    CareTaskDto Feeding,
    IReadOnlyList<CareEventDto> History,
    IReadOnlyList<PhotoDto> Photos);

public record RecognitionResponse(IReadOnlyList<SpeciesSuggestion> Suggestions);
