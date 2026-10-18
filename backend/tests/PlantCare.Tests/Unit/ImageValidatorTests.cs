using System.Text;
using PlantCare.Api.Validation;

namespace PlantCare.Tests.Unit;

/// <summary>AC6 / AC7 – only valid image files are accepted.</summary>
[Trait("AC", "AC6")]
public class ImageValidatorTests
{
    [Fact]
    public void Jpeg_is_detected_from_magic_bytes()
    {
        var result = ImageValidator.Validate(TestImages.Jpeg);
        Assert.True(result.IsValid);
        Assert.Equal("image/jpeg", result.ContentType);
    }

    [Fact]
    public void Png_is_detected_from_magic_bytes()
    {
        var result = ImageValidator.Validate(TestImages.Png);
        Assert.True(result.IsValid);
        Assert.Equal("image/png", result.ContentType);
    }

    [Fact]
    public void Text_file_renamed_to_jpg_is_rejected()
    {
        var result = ImageValidator.Validate(Encoding.UTF8.GetBytes("ez nem kép"));
        Assert.False(result.IsValid);
        Assert.Contains("JPEG vagy PNG", result.Error);
    }

    [Fact]
    public void Gif_is_rejected()
    {
        Assert.False(ImageValidator.Validate("GIF89a....."u8).IsValid);
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.False(ImageValidator.Validate([]).IsValid);
    }

    [Fact]
    public void Size_limit_boundary()
    {
        var atLimit = new byte[ImageValidator.MaxBytes];
        TestImages.Png.CopyTo(atLimit, 0);
        Assert.True(ImageValidator.Validate(atLimit).IsValid);

        var overLimit = new byte[ImageValidator.MaxBytes + 1];
        TestImages.Png.CopyTo(overLimit, 0);
        Assert.False(ImageValidator.Validate(overLimit).IsValid);
    }
}
