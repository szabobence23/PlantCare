namespace PlantCare.Tests;

/// <summary>Minimal byte sequences that pass the magic-byte check; the content is not a decodable image.</summary>
public static class TestImages
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 5, 6, 7, 8];
    public static readonly byte[] NotAnImage = "hello, ez egy szövegfájl"u8.ToArray();
}
