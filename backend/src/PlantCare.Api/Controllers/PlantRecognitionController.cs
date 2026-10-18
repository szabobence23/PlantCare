using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts;
using PlantCare.Api.Recognition;
using PlantCare.Api.Validation;

namespace PlantCare.Api.Controllers;

/// <summary>
/// Suggests species for a photo before the plant is saved (spec AC6). Nothing is stored here:
/// the user accepts or edits the suggestion, then submits the plant form with the same photo.
/// </summary>
[Authorize]
[Route("api/plant-recognition")]
public class PlantRecognitionController(IPlantRecognitionService recognition) : ApiControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RecognitionResponse>> Identify(IFormFile? photo, CancellationToken ct)
    {
        var (data, check) = await ImageValidator.ReadAsync(photo, ct);
        if (!check.IsValid) return ValidationError(new() { ["photo"] = [check.Error!] });

        try
        {
            var suggestions = await recognition.IdentifyAsync(data!, check.ContentType!, ct);
            return Ok(new RecognitionResponse(suggestions));
        }
        catch (RecognitionUnavailableException ex)
        {
            return Error(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}
