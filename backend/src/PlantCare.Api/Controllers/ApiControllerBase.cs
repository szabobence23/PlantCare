using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Infrastructure;

namespace PlantCare.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId => User.GetUserId();

    /// <summary>400 with field-level messages: { "errors": { "field": ["message"] } }.</summary>
    protected ActionResult ValidationError(Dictionary<string, string[]> errors) =>
        ValidationProblem(new ValidationProblemDetails(errors) { Title = "Hibás adatok." });

    protected ActionResult Error(int statusCode, string message) =>
        Problem(detail: message, statusCode: statusCode, title: message);

    protected ActionResult PlantNotFound() => Error(StatusCodes.Status404NotFound, "A növény nem található.");
}
