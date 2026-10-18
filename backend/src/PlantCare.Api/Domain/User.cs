namespace PlantCare.Api.Domain;

public class User
{
    public Guid Id { get; set; }
    /// <summary>Stored trimmed and lower-cased; unique.</summary>
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public List<Plant> Plants { get; set; } = [];
}
