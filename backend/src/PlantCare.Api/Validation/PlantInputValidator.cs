namespace PlantCare.Api.Validation;

public record PlantInput(
    string? Name,
    string? Species,
    int? WateringIntervalDays,
    int? FeedingIntervalDays,
    DateOnly? LastWateredOn,
    DateOnly? LastFedOn);

/// <summary>Validation rules for creating a plant (spec AC2, AC5). Keys match the JSON/form field names.</summary>
public static class PlantInputValidator
{
    public const int NameMaxLength = 100;
    public const int SpeciesMaxLength = 150;
    /// <summary>Upper bound so date arithmetic can never overflow; a year is plenty for house plants.</summary>
    public const int MaxIntervalDays = 365;

    public static Dictionary<string, string[]> Validate(PlantInput input, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            errors["name"] = ["A név megadása kötelező."];
        else if (name.Length > NameMaxLength)
            errors["name"] = [$"A név legfeljebb {NameMaxLength} karakter lehet."];

        if (input.Species?.Trim().Length > SpeciesMaxLength)
            errors["species"] = [$"A faj legfeljebb {SpeciesMaxLength} karakter lehet."];

        ValidateInterval(errors, "wateringIntervalDays", "locsolási", input.WateringIntervalDays);
        ValidateInterval(errors, "feedingIntervalDays", "tápozási", input.FeedingIntervalDays);
        ValidateLastDate(errors, "lastWateredOn", "locsolás", input.LastWateredOn, today);
        ValidateLastDate(errors, "lastFedOn", "tápozás", input.LastFedOn, today);

        return errors;
    }

    private static void ValidateInterval(Dictionary<string, string[]> errors, string key, string label, int? days)
    {
        if (days is null)
            errors[key] = [$"Add meg a {label} gyakoriságot napokban (pozitív egész szám)."];
        else if (days <= 0)
            errors[key] = [$"A {label} gyakoriságnak pozitív egész számnak kell lennie."];
        else if (days > MaxIntervalDays)
            errors[key] = [$"A {label} gyakoriság legfeljebb {MaxIntervalDays} nap lehet."];
    }

    private static void ValidateLastDate(Dictionary<string, string[]> errors, string key, string label, DateOnly? date, DateOnly today)
    {
        if (date is null)
            errors[key] = [$"Add meg a legutóbbi {label} dátumát."];
        else if (date > today)
            errors[key] = [$"A legutóbbi {label} dátuma nem lehet a jövőben."];
    }
}
