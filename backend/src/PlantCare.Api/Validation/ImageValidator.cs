namespace PlantCare.Api.Validation;

public record ImageValidationResult(string? ContentType, string? Error)
{
    public bool IsValid => Error is null;
}

/// <summary>
/// Accepts JPEG and PNG only (the formats Pl@ntNet supports), detected from the file's
/// magic bytes rather than the client-supplied Content-Type or extension.
/// </summary>
public static class ImageValidator
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageValidationResult Validate(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return new(null, "A feltöltött fájl üres.");
        if (data.Length > MaxBytes)
            return new(null, "A kép legfeljebb 10 MB lehet.");
        if (data.StartsWith(JpegSignature))
            return new("image/jpeg", null);
        if (data.StartsWith(PngSignature))
            return new("image/png", null);
        return new(null, "Csak JPEG vagy PNG kép tölthető fel.");
    }

    /// <summary>Reads an uploaded file, rejecting oversized files before buffering them.</summary>
    public static async Task<(byte[]? Data, ImageValidationResult Result)> ReadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return (null, new(null, "Válassz ki egy képet."));
        if (file.Length > MaxBytes)
            return (null, new(null, "A kép legfeljebb 10 MB lehet."));

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var data = buffer.ToArray();
        var result = Validate(data);
        return (result.IsValid ? data : null, result);
    }
}
