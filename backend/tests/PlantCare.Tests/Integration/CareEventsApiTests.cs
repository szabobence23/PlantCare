using System.Net;
using System.Net.Http.Json;
using PlantCare.Api.Contracts;
using PlantCare.Api.Domain;

namespace PlantCare.Tests.Integration;

/// <summary>AC4 – "Meglocsoltam" / "Megtápoztam".</summary>
[Trait("AC", "AC4")]
public class CareEventsApiTests(PlantCareApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Watering_records_todays_event_and_moves_next_due_date()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client, PlantForm(wateringIntervalDays: 7, lastWateredOn: Today.AddDays(-7)));
        Assert.Equal(CareStatus.Due, plant.Watering.Status);

        var response = await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "watering" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var summary = await ReadAsync<PlantSummaryDto>(response);
        Assert.Equal(Today, summary.Watering.LastDoneOn);
        Assert.Equal(Today.AddDays(7), summary.Watering.NextDueOn);
        Assert.Equal(CareStatus.Ok, summary.Watering.Status);

        var detail = await client.GetFromJsonAsync<PlantDetailDto>($"/api/plants/{plant.Id}", Json);
        var careEvent = Assert.Single(detail!.History);
        Assert.Equal((CareType.Watering, Today), (careEvent.Type, careEvent.Date));
        Assert.Equal(Today.AddDays(7), detail.Watering.NextDueOn);
    }

    [Fact]
    public async Task Feeding_moves_only_the_feeding_schedule()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client, PlantForm(
            wateringIntervalDays: 5, lastWateredOn: Today.AddDays(-2),
            feedingIntervalDays: 30, lastFedOn: Today.AddDays(-40)));
        Assert.Equal(CareStatus.Overdue, plant.Feeding.Status);

        var summary = await ReadAsync<PlantSummaryDto>(
            await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "feeding" }));

        Assert.Equal((Today.AddDays(30), CareStatus.Ok), (summary.Feeding.NextDueOn, summary.Feeding.Status));
        Assert.Equal(plant.Watering, summary.Watering);
    }

    [Fact]
    public async Task History_lists_every_event_newest_first()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client);

        await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "watering" });
        Factory.Time.Advance(TimeSpan.FromMinutes(1));
        await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "feeding" });

        var detail = await client.GetFromJsonAsync<PlantDetailDto>($"/api/plants/{plant.Id}", Json);
        Assert.Equal([CareType.Feeding, CareType.Watering], detail!.History.Select(e => e.Type));
    }

    [Theory]
    [InlineData("""{"type":"pruning"}""")]
    [InlineData("""{}""")]
    [InlineData("""nem json""")]
    public async Task Unknown_care_type_is_rejected(string body)
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client);

        var response = await client.PostAsync($"/api/plants/{plant.Id}/care-events",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var detail = await client.GetFromJsonAsync<PlantDetailDto>($"/api/plants/{plant.Id}", Json);
        Assert.Empty(detail!.History);
    }
}
