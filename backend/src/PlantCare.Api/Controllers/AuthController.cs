using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Contracts;
using PlantCare.Api.Data;
using PlantCare.Api.Domain;
using PlantCare.Api.Infrastructure;
using PlantCare.Api.Validation;

namespace PlantCare.Api.Controllers;

[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    TokenService tokens,
    IAppClock clock) : ApiControllerBase
{
    /// <summary>Creates an account and signs the user in (spec AC1).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest? request, CancellationToken ct)
    {
        var errors = AuthInputValidator.ValidateRegistration(request?.Email, request?.Password);
        if (errors.Count > 0) return ValidationError(errors);

        var email = AuthInputValidator.NormalizeEmail(request!.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Error(StatusCodes.Status409Conflict, "Ezzel az e-mail címmel már létezik fiók.");

        var user = new User { Id = Guid.NewGuid(), Email = email, CreatedAt = clock.Now };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new AuthResponse(tokens.CreateToken(user), user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest? request, CancellationToken ct)
    {
        var errors = AuthInputValidator.ValidateLogin(request?.Email, request?.Password);
        if (errors.Count > 0) return ValidationError(errors);

        var email = AuthInputValidator.NormalizeEmail(request!.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        // Same message for unknown e-mail and wrong password, so accounts cannot be enumerated.
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) == PasswordVerificationResult.Failed)
            return Error(StatusCodes.Status401Unauthorized, "Hibás e-mail cím vagy jelszó.");

        return Ok(new AuthResponse(tokens.CreateToken(user), user.Email));
    }
}
