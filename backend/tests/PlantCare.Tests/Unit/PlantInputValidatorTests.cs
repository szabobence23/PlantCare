using PlantCare.Api.Validation;

namespace PlantCare.Tests.Unit;

/// <summary>AC2 (valid plant) and AC5 (invalid interval).</summary>
public class PlantInputValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);

    private static PlantInput Valid() => new("Fikusz", "Ficus elastica", 7, 14, Today.AddDays(-1), Today.AddDays(-3));

    [Fact]
    [Trait("AC", "AC2")]
    public void Valid_input_has_no_errors()
    {
        Assert.Empty(PlantInputValidator.Validate(Valid(), Today));
    }

    [Fact]
    [Trait("AC", "AC6")]
    public void Species_is_optional()
    {
        Assert.Empty(PlantInputValidator.Validate(Valid() with { Species = null }, Today));
    }

    [Theory]
    [Trait("AC", "AC2")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_name_is_rejected(string? name)
    {
        var errors = PlantInputValidator.Validate(Valid() with { Name = name }, Today);
        Assert.Equal(["name"], errors.Keys);
    }

    [Fact]
    [Trait("AC", "AC2")]
    public void Name_length_boundary()
    {
        Assert.Empty(PlantInputValidator.Validate(Valid() with { Name = new string('a', 100) }, Today));
        Assert.Contains("name", PlantInputValidator.Validate(Valid() with { Name = new string('a', 101) }, Today).Keys);
    }

    [Theory]
    [Trait("AC", "AC5")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-365)]
    public void Zero_or_negative_watering_interval_is_rejected(int days)
    {
        var errors = PlantInputValidator.Validate(Valid() with { WateringIntervalDays = days }, Today);
        Assert.Equal(["wateringIntervalDays"], errors.Keys);
        Assert.Contains("pozitív", errors["wateringIntervalDays"][0]);
    }

    [Theory]
    [Trait("AC", "AC5")]
    [InlineData(0)]
    [InlineData(-7)]
    public void Zero_or_negative_feeding_interval_is_rejected(int days)
    {
        var errors = PlantInputValidator.Validate(Valid() with { FeedingIntervalDays = days }, Today);
        Assert.Equal(["feedingIntervalDays"], errors.Keys);
    }

    [Fact]
    [Trait("AC", "AC5")]
    public void Missing_interval_is_rejected()
    {
        var errors = PlantInputValidator.Validate(Valid() with { WateringIntervalDays = null, FeedingIntervalDays = null }, Today);
        Assert.Equal(["wateringIntervalDays", "feedingIntervalDays"], errors.Keys);
    }

    [Theory]
    [Trait("AC", "AC5")]
    [InlineData(1, true)]   // smallest positive interval (boundary)
    [InlineData(365, true)] // largest allowed
    [InlineData(366, false)]
    public void Interval_boundaries(int days, bool valid)
    {
        var errors = PlantInputValidator.Validate(Valid() with { WateringIntervalDays = days, FeedingIntervalDays = days }, Today);
        Assert.Equal(valid, errors.Count == 0);
    }

    [Fact]
    [Trait("AC", "AC2")]
    public void Last_care_dates_today_are_accepted_but_future_dates_are_not()
    {
        Assert.Empty(PlantInputValidator.Validate(Valid() with { LastWateredOn = Today, LastFedOn = Today }, Today));

        var errors = PlantInputValidator.Validate(Valid() with { LastWateredOn = Today.AddDays(1), LastFedOn = null }, Today);
        Assert.Equal(["lastWateredOn", "lastFedOn"], errors.Keys);
    }
}
