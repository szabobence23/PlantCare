namespace PlantCare.Api.Recognition;

public record SpeciesSuggestion(string ScientificName, IReadOnlyList<string> CommonNames, double Score);

/// <summary>Identifies a plant species from a photo using an external service (spec AC6).</summary>
public interface IPlantRecognitionService
{
    /// <returns>Suggestions ordered by descending score; empty if the service recognised nothing.</returns>
    /// <exception cref="RecognitionUnavailableException">The service is not configured or not reachable.</exception>
    Task<IReadOnlyList<SpeciesSuggestion>> IdentifyAsync(byte[] image, string contentType, CancellationToken ct);
}

public class RecognitionUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
