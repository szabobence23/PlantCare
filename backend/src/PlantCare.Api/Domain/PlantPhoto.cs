namespace PlantCare.Api.Domain;

public class PlantPhoto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = [];
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
