using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlantCare.Api.Domain;

namespace PlantCare.Api.Infrastructure;

/// <remarks>
/// Uses the real system clock on purpose, not <see cref="IAppClock"/>: the JWT handler checks token
/// lifetime against the real clock, so a token issued with a faked business date (as in the
/// integration tests) would already be expired or not yet valid.
/// </remarks>
public class TokenService(IOptions<JwtOptions> options)
{
    public string CreateToken(User user)
    {
        var jwt = options.Value;
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddHours(jwt.ExpiryHours),
            SigningCredentials = new SigningCredentials(CreateSigningKey(jwt.Key), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static SymmetricSecurityKey CreateSigningKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
