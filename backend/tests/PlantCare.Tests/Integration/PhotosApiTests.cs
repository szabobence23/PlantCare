using System.Net;
using System.Net.Http.Json;
using PlantCare.Api.Contracts;

namespace PlantCare.Tests.Integration;

/// <summary>AC7 – growth photos are stored and can be viewed later.</summary>
[Trait("AC", "AC7")]
public class PhotosApiTests(PlantCareApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Photo_given_at_creation_is_the_first_growth_photo()
    {
        var client = await CreateUserClientAsync();

        var plant = await CreatePlantAsync(client, PlantForm(photo: TestImages.Jpeg));

        var photo = Assert.Single(plant.Photos);
        var download = await client.GetAsync($"/api/plants/{plant.Id}/photos/{photo.Id}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/jpeg", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal(TestImages.Jpeg, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Uploaded_photos_are_kept_in_chronological_order_and_retrievable()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client);

        var first = await client.PostAsync($"/api/plants/{plant.Id}/photos", PhotoForm(TestImages.Png, "elso.png"));
        Factory.Time.Advance(TimeSpan.FromDays(0.01));
        var second = await client.PostAsync($"/api/plants/{plant.Id}/photos", PhotoForm(TestImages.Jpeg, "masodik.jpg"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var firstId = (await ReadAsync<PhotoDto>(first)).Id;
        var secondId = (await ReadAsync<PhotoDto>(second)).Id;

        var photos = await client.GetFromJsonAsync<List<PhotoDto>>($"/api/plants/{plant.Id}/photos", Json);
        Assert.Equal([firstId, secondId], photos!.Select(p => p.Id));

        // Still there when the plant is opened again later.
        var detail = await client.GetFromJsonAsync<PlantDetailDto>($"/api/plants/{plant.Id}", Json);
        Assert.Equal([firstId, secondId], detail!.Photos.Select(p => p.Id));
        Assert.Equal(TestImages.Png, await client.GetByteArrayAsync($"/api/plants/{plant.Id}/photos/{firstId}"));

        var list = await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json);
        Assert.Equal(secondId, list!.Single().LatestPhotoId);
    }

    [Fact]
    public async Task Non_image_file_is_rejected_and_not_stored()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client);

        var response = await client.PostAsync($"/api/plants/{plant.Id}/photos", PhotoForm(TestImages.NotAnImage, "kep.jpg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("photo", (await ReadValidationErrorsAsync(response)).Errors.Keys);
        Assert.Empty((await client.GetFromJsonAsync<List<PhotoDto>>($"/api/plants/{plant.Id}/photos", Json))!);
    }

    [Fact]
    public async Task Plant_with_invalid_photo_is_not_created()
    {
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plants", PlantForm(photo: TestImages.NotAnImage));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json))!);
    }
}
