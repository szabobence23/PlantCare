namespace PlantCare.Api.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PlantCare";
    public string Audience { get; set; } = "PlantCare";
    /// <summary>HMAC-SHA256 signing key, at least 32 characters.</summary>
    public string Key { get; set; } = "";
    public int ExpiryHours { get; set; } = 24;
}
