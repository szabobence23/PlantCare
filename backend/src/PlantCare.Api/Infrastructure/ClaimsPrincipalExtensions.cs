using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PlantCare.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? throw new InvalidOperationException("Authenticated user has no 'sub' claim."));
}
