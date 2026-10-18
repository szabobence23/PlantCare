namespace PlantCare.Api.Recognition;

public class PlantNetOptions
{
    public const string SectionName = "PlantNet";

    public string BaseUrl { get; set; } = "https://my-api.plantnet.org/";
    /// <summary>Personal key from my.plantnet.org. Never commit it: use user-secrets or the PlantNet__ApiKey env var.</summary>
    public string? ApiKey { get; set; }
    /// <summary>Flora to search in; "all" covers house plants of any origin.</summary>
    public string Project { get; set; } = "all";
    /// <summary>Language of the returned common names.</summary>
    public string Language { get; set; } = "hu";
    public int MaxResults { get; set; } = 3;
}
