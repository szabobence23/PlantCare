using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlantCare.Api.Recognition;

namespace PlantCare.Tests.Unit;

/// <summary>AC6 – Pl@ntNet client: request shape, response parsing and failure handling (no real network).</summary>
[Trait("AC", "AC6")]
public class PlantNetRecognitionServiceTests
{
    private const string SampleResponse = """
        {
          "bestMatch": "Ficus elastica Roxb. ex Hornem.",
          "results": [
            { "score": 0.12, "species": { "scientificNameWithoutAuthor": "Ficus lyrata", "commonNames": [] } },
            { "score": 0.81234567, "species": { "scientificNameWithoutAuthor": "Ficus elastica", "commonNames": ["Gumifa", "Rugalmas fikusz"] } }
          ],
          "remainingIdentificationRequests": 499
        }
        """;

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static PlantNetRecognitionService Create(StubHandler handler, string? apiKey = "test-key") =>
        new(new HttpClient(handler),
            Options.Create(new PlantNetOptions { ApiKey = apiKey }),
            NullLogger<PlantNetRecognitionService>.Instance);

    [Fact]
    public async Task Parses_suggestions_ordered_by_score()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SampleResponse);

        var suggestions = await Create(handler).IdentifyAsync(TestImages.Jpeg, "image/jpeg", CancellationToken.None);

        Assert.Equal(["Ficus elastica", "Ficus lyrata"], suggestions.Select(s => s.ScientificName));
        Assert.Equal(["Gumifa", "Rugalmas fikusz"], suggestions[0].CommonNames);
        Assert.Equal(0.8123, suggestions[0].Score);
    }

    [Fact]
    public async Task Sends_photo_and_key_to_identify_endpoint()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SampleResponse);

        await Create(handler).IdentifyAsync(TestImages.Jpeg, "image/jpeg", CancellationToken.None);

        var uri = handler.Request!.RequestUri!;
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("/v2/identify/all", uri.AbsolutePath);
        Assert.Contains("api-key=test-key", uri.Query);
        Assert.Contains("lang=hu", uri.Query);
        Assert.Contains("name=images", handler.RequestBody);
        Assert.Contains("name=organs", handler.RequestBody);
    }

    [Fact]
    public async Task Not_found_means_no_suggestions()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, """{"message":"Species not found"}""");
        Assert.Empty(await Create(handler).IdentifyAsync(TestImages.Png, "image/png", CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Api_errors_make_recognition_unavailable(HttpStatusCode status)
    {
        var handler = new StubHandler(status, "{}");
        await Assert.ThrowsAsync<RecognitionUnavailableException>(() =>
            Create(handler).IdentifyAsync(TestImages.Png, "image/png", CancellationToken.None));
    }

    [Fact]
    public async Task Missing_api_key_makes_recognition_unavailable_without_calling_the_api()
    {
        var handler = new StubHandler(HttpStatusCode.OK, SampleResponse);
        await Assert.ThrowsAsync<RecognitionUnavailableException>(() =>
            Create(handler, apiKey: null).IdentifyAsync(TestImages.Png, "image/png", CancellationToken.None));
        Assert.Null(handler.Request);
    }
}
