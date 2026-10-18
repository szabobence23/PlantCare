using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Contracts;
using PlantCare.Api.Data;
using PlantCare.Api.Domain;
using PlantCare.Api.Infrastructure;
using PlantCare.Api.Validation;

namespace PlantCare.Api.Controllers;

/// <summary>Growth tracking photos of a plant (spec AC7).</summary>
[Authorize]
[Route("api/plants/{plantId:guid}/photos")]
public class PlantPhotosController(AppDbContext db, IAppClock clock) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PhotoDto>>> List(Guid plantId, CancellationToken ct)
    {
        if (!await OwnsPlantAsync(plantId, ct)) return PlantNotFound();

        var photos = await db.PlantPhotos
            .Where(ph => ph.PlantId == plantId)
            .OrderBy(ph => ph.UploadedAt)
            .Select(ph => new PhotoDto(ph.Id, ph.UploadedAt, ph.SizeBytes))
            .ToListAsync(ct);
        return Ok(photos);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PhotoDto>> Upload(Guid plantId, IFormFile? photo, CancellationToken ct)
    {
        if (!await OwnsPlantAsync(plantId, ct)) return PlantNotFound();

        var (data, check) = await ImageValidator.ReadAsync(photo, ct);
        if (!check.IsValid) return ValidationError(new() { ["photo"] = [check.Error!] });

        var entity = new PlantPhoto
        {
            Id = Guid.NewGuid(),
            PlantId = plantId,
            ContentType = check.ContentType!,
            Data = data!,
            SizeBytes = data!.LongLength,
            UploadedAt = clock.Now,
        };
        db.PlantPhotos.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = new PhotoDto(entity.Id, entity.UploadedAt, entity.SizeBytes);
        return CreatedAtAction(nameof(Download), new { plantId, photoId = entity.Id }, dto);
    }

    [HttpGet("{photoId:guid}")]
    public async Task<ActionResult> Download(Guid plantId, Guid photoId, CancellationToken ct)
    {
        var userId = CurrentUserId;
        var photo = await db.PlantPhotos
            .Where(ph => ph.Id == photoId && ph.PlantId == plantId)
            .Where(ph => db.Plants.Any(p => p.Id == plantId && p.UserId == userId))
            .Select(ph => new { ph.ContentType, ph.Data })
            .SingleOrDefaultAsync(ct);
        return photo is null
            ? Error(StatusCodes.Status404NotFound, "A kép nem található.")
            : File(photo.Data, photo.ContentType);
    }

    private Task<bool> OwnsPlantAsync(Guid plantId, CancellationToken ct)
    {
        var userId = CurrentUserId;
        return db.Plants.AnyAsync(p => p.Id == plantId && p.UserId == userId, ct);
    }
}
