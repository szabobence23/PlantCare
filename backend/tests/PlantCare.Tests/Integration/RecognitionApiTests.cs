using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts;
using PlantCare.Api.Recognition;

namespace PlantCare.Tests.Integration;

/// <summary>AC6 – species suggestion for an uploaded photo (the external API is faked here).</summary>
[Trait("AC", "AC6")]
public class RecognitionApiTests : ApiTestBase
{
    public RecognitionApiTests(PlantCareApiFactory factory) : base(factory) => factory.Recognition.Reset();

    [Fact]
    public async Task Valid_image_is_sent_to_the_recognition_service_and_suggestions_returned()
    {
        Factory.Recognition.Suggestions = [new SpeciesSuggestion("Ficus elastica", ["Gumifa"], 0.81)];
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plant-recognition", PhotoForm(TestImages.Jpeg));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<RecognitionResponse>(response);
        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal("Ficus elastica", suggestion.ScientificName);
        Assert.Equal(["Gumifa"], suggestion.CommonNames);
        Assert.Equal(1, Factory.Recognition.Calls);
    }

    [Fact]
    public async Task Invalid_file_is_rejected_without_calling_the_recognition_service()
    {
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plant-recognition", PhotoForm(TestImages.NotAnImage, "level.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("photo", (await ReadValidationErrorsAsync(response)).Errors.Keys);
        Assert.Equal(0, Factory.Recognition.Calls);
    }

    [Fact]
    public async Task Missing_file_is_rejected()
    {
        var client = await CreateUserClientAsync();
        var response = await client.PostAsync("/api/plant-recognition", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unavailable_service_returns_503_with_a_manual_entry_hint()
    {
        Factory.Recognition.Unavailable = true;
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plant-recognition", PhotoForm(TestImages.Png));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("add meg a fajt kézzel", (await ReadAsync<ProblemDetails>(response)).Detail);
    }

    [Fact]
    public async Task Nothing_recognised_returns_empty_suggestion_list()
    {
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plant-recognition", PhotoForm(TestImages.Png));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await ReadAsync<RecognitionResponse>(response)).Suggestions);
    }
}
