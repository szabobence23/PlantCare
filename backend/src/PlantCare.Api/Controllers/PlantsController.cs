using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Contracts;
using PlantCare.Api.Data;
using PlantCare.Api.Domain;
using PlantCare.Api.Infrastructure;
using PlantCare.Api.Validation;

namespace PlantCare.Api.Controllers;

/// <summary>
/// Every query is filtered by the signed-in user's id; another user's plant answers 404,
/// exactly as if it did not exist.
/// </summary>
[Authorize]
[Route("api/plants")]
public class PlantsController(AppDbContext db, IAppClock clock) : ApiControllerBase
{
    /// <summary>The user's plants, most urgent task first (spec AC2, AC3).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlantSummaryDto>>> List(CancellationToken ct)
    {
        var today = clock.Today;
        var rows = await db.Plants
            .Where(p => p.UserId == CurrentUserId)
            .Select(p => new
            {
                p.Id, p.Name, p.Species,
                p.WateringIntervalDays, p.LastWateredOn,
                p.FeedingIntervalDays, p.LastFedOn,
                LatestPhotoId = p.Photos.OrderByDescending(ph => ph.UploadedAt).Select(ph => (Guid?)ph.Id).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var plants = rows
            .Select(r => new PlantSummaryDto(
                r.Id, r.Name, r.Species,
                CareTaskDto.Create(r.LastWateredOn, r.WateringIntervalDays, today),
                CareTaskDto.Create(r.LastFedOn, r.FeedingIntervalDays, today),
                r.LatestPhotoId))
            .OrderBy(p => Math.Min(p.Watering.DaysUntilDue, p.Feeding.DaysUntilDue))
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return Ok(plants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlantDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var detail = await LoadDetailAsync(id, ct);
        return detail is null ? PlantNotFound() : Ok(detail);
    }

    /// <summary>Creates a plant; the optional photo becomes its first growth photo (spec AC2, AC5, AC6).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PlantDetailDto>> Create([FromForm] CreatePlantForm form, CancellationToken ct)
    {
        var errors = PlantInputValidator.Validate(
            new PlantInput(form.Name, form.Species, form.WateringIntervalDays, form.FeedingIntervalDays, form.LastWateredOn, form.LastFedOn),
            clock.Today);

        byte[]? photoData = null;
        string? photoType = null;
        if (form.Photo is not null)
        {
            var (data, check) = await ImageValidator.ReadAsync(form.Photo, ct);
            if (check.IsValid) (photoData, photoType) = (data, check.ContentType);
            else errors["photo"] = [check.Error!];
        }

        if (errors.Count > 0) return ValidationError(errors);

        var plant = new Plant
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            Name = form.Name!.Trim(),
            Species = string.IsNullOrWhiteSpace(form.Species) ? null : form.Species.Trim(),
            WateringIntervalDays = form.WateringIntervalDays!.Value,
            FeedingIntervalDays = form.FeedingIntervalDays!.Value,
            LastWateredOn = form.LastWateredOn!.Value,
            LastFedOn = form.LastFedOn!.Value,
            CreatedAt = clock.Now,
        };
        if (photoData is not null)
        {
            plant.Photos.Add(new PlantPhoto
            {
                Id = Guid.NewGuid(),
                PlantId = plant.Id,
                ContentType = photoType!,
                Data = photoData,
                SizeBytes = photoData.LongLength,
                UploadedAt = clock.Now,
            });
        }

        db.Plants.Add(plant);
        await db.SaveChangesAsync(ct);

        var detail = await LoadDetailAsync(plant.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = plant.Id }, detail);
    }

    /// <summary>"Meglocsoltam" / "Megtápoztam": records today's care and moves the next due date (spec AC4).</summary>
    [HttpPost("{id:guid}/care-events")]
    public async Task<ActionResult<PlantSummaryDto>> RecordCare(Guid id, [FromBody] RecordCareRequest? request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request?.Type is null)
            return ValidationError(new() { ["type"] = ["Ismeretlen gondozási típus (watering vagy feeding)."] });

        var plant = await db.Plants.SingleOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId, ct);
        if (plant is null) return PlantNotFound();

        var careEvent = plant.RecordCare(request.Type.Value, clock.Today, clock.Now);
        db.CareEvents.Add(careEvent);
        await db.SaveChangesAsync(ct);

        var latestPhotoId = await db.PlantPhotos
            .Where(ph => ph.PlantId == id)
            .OrderByDescending(ph => ph.UploadedAt)
            .Select(ph => (Guid?)ph.Id)
            .FirstOrDefaultAsync(ct);
        var today = clock.Today;
        return StatusCode(StatusCodes.Status201Created, new PlantSummaryDto(
            plant.Id, plant.Name, plant.Species,
            CareTaskDto.Create(plant.LastWateredOn, plant.WateringIntervalDays, today),
            CareTaskDto.Create(plant.LastFedOn, plant.FeedingIntervalDays, today),
            latestPhotoId));
    }

    /// <summary>Deletes the plant; care events and photos go with it via ON DELETE CASCADE (spec AC8).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await db.Plants
            .Where(p => p.Id == id && p.UserId == CurrentUserId)
            .ExecuteDeleteAsync(ct);
        return deleted == 0 ? PlantNotFound() : NoContent();
    }

    private async Task<PlantDetailDto?> LoadDetailAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Plants
            .Where(p => p.Id == id && p.UserId == CurrentUserId)
            .Select(p => new
            {
                p.Id, p.Name, p.Species, p.CreatedAt,
                p.WateringIntervalDays, p.LastWateredOn,
                p.FeedingIntervalDays, p.LastFedOn,
                History = p.CareEvents
                    .OrderByDescending(e => e.Date).ThenByDescending(e => e.RecordedAt)
                    .Select(e => new CareEventDto(e.Id, e.Type, e.Date, e.RecordedAt))
                    .ToList(),
                // Metadata only: the image bytes are served by PlantPhotosController.
                Photos = p.Photos
                    .OrderBy(ph => ph.UploadedAt)
                    .Select(ph => new PhotoDto(ph.Id, ph.UploadedAt, ph.SizeBytes))
                    .ToList(),
            })
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (row is null) return null;

        var today = clock.Today;
        return new PlantDetailDto(
            row.Id, row.Name, row.Species, row.CreatedAt,
            CareTaskDto.Create(row.LastWateredOn, row.WateringIntervalDays, today),
            CareTaskDto.Create(row.LastFedOn, row.FeedingIntervalDays, today),
            row.History, row.Photos);
    }
}
