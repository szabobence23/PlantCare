using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PlantCare.Api.Recognition;

/// <summary>Client for the Pl@ntNet identify API (POST /v2/identify/{project}).</summary>
public class PlantNetRecognitionService(
    HttpClient http,
    IOptions<PlantNetOptions> options,
    ILogger<PlantNetRecognitionService> logger) : IPlantRecognitionService
{
    private const string UnavailableMessage = "A növényfelismerés jelenleg nem érhető el, add meg a fajt kézzel.";

    public async Task<IReadOnlyList<SpeciesSuggestion>> IdentifyAsync(byte[] image, string contentType, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new RecognitionUnavailableException(UnavailableMessage + " (Nincs beállítva Pl@ntNet API kulcs.)");

        var url = $"{o.BaseUrl.TrimEnd('/')}/v2/identify/{Uri.EscapeDataString(o.Project)}" +
                  $"?api-key={Uri.EscapeDataString(o.ApiKey)}&lang={Uri.EscapeDataString(o.Language)}" +
                  $"&nb-results={o.MaxResults}&include-related-images=false";

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(image);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "images", contentType == "image/png" ? "plant.png" : "plant.jpg");
        content.Add(new StringContent("auto"), "organs");

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync(url, content, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Pl@ntNet request failed");
            throw new RecognitionUnavailableException(UnavailableMessage, ex);
        }

        using (response)
        {
            // Pl@ntNet answers 404 "Species not found" when nothing matches the photo.
            if (response.StatusCode == HttpStatusCode.NotFound)
                return [];

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Pl@ntNet returned {StatusCode}", (int)response.StatusCode);
                throw new RecognitionUnavailableException(UnavailableMessage);
            }

            var body = await response.Content.ReadFromJsonAsync<PlantNetResponse>(ct);
            return (body?.Results ?? [])
                .Where(r => !string.IsNullOrWhiteSpace(r.Species?.ScientificNameWithoutAuthor))
                .OrderByDescending(r => r.Score)
                .Take(o.MaxResults)
                .Select(r => new SpeciesSuggestion(
                    r.Species!.ScientificNameWithoutAuthor!,
                    r.Species.CommonNames ?? [],
                    Math.Round(r.Score, 4)))
                .ToList();
        }
    }

    private sealed record PlantNetResponse([property: JsonPropertyName("results")] List<PlantNetResult>? Results);

    private sealed record PlantNetResult(
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("species")] PlantNetSpecies? Species);

    private sealed record PlantNetSpecies(
        [property: JsonPropertyName("scientificNameWithoutAuthor")] string? ScientificNameWithoutAuthor,
        [property: JsonPropertyName("commonNames")] List<string>? CommonNames);
}
