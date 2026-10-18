using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Contracts;
using PlantCare.Api.Domain;

namespace PlantCare.Tests.Integration;

/// <summary>AC2 (create), AC3 (due status in the list), AC5 (invalid interval), AC8 (delete), ownership.</summary>
public class PlantsApiTests(PlantCareApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    [Trait("AC", "AC2")]
    public async Task Valid_plant_is_created_and_appears_in_own_list()
    {
        var client = await CreateUserClientAsync();

        var created = await CreatePlantAsync(client, PlantForm(name: "  Fikusz  ", wateringIntervalDays: 7, feedingIntervalDays: 14));

        Assert.Equal("Fikusz", created.Name); // trimmed
        Assert.Equal("Ficus elastica", created.Species);
        var list = await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json);
        var plant = Assert.Single(list!);
        Assert.Equal(created.Id, plant.Id);
        Assert.Equal(7, plant.Watering.IntervalDays);
        Assert.Equal(14, plant.Feeding.IntervalDays);
    }

    [Fact]
    [Trait("AC", "AC6")]
    public async Task Species_can_be_given_manually_or_left_empty_without_photo()
    {
        var client = await CreateUserClientAsync();

        var manual = await CreatePlantAsync(client, PlantForm(species: "Monstera deliciosa"));
        var empty = await CreatePlantAsync(client, PlantForm(species: null));

        Assert.Equal("Monstera deliciosa", manual.Species);
        Assert.Null(empty.Species);
        Assert.Empty(manual.Photos);
    }

    [Theory]
    [Trait("AC", "AC5")]
    [InlineData(0, 14, "wateringIntervalDays")]
    [InlineData(-3, 14, "wateringIntervalDays")]
    [InlineData(7, 0, "feedingIntervalDays")]
    [InlineData(7, -1, "feedingIntervalDays")]
    public async Task Zero_or_negative_interval_is_rejected_and_nothing_is_saved(int watering, int feeding, string field)
    {
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plants", PlantForm(wateringIntervalDays: watering, feedingIntervalDays: feeding));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadValidationErrorsAsync(response);
        Assert.Equal([field], problem.Errors.Keys);
        Assert.Empty((await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json))!);
    }

    [Fact]
    [Trait("AC", "AC5")]
    public async Task Non_numeric_interval_is_rejected()
    {
        var client = await CreateUserClientAsync();
        var form = PlantForm(wateringIntervalDays: null);
        form.Add(new StringContent("hét"), "wateringIntervalDays");

        var response = await client.PostAsync("/api/plants", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("wateringIntervalDays", (await ReadValidationErrorsAsync(response)).Errors.Keys);
    }

    [Fact]
    [Trait("AC", "AC5")]
    public async Task Database_rejects_non_positive_interval_even_if_validation_is_bypassed()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => QueryDbAsync(async db =>
        {
            var entity = await db.Plants.SingleAsync(p => p.Id == plant.Id);
            entity.WateringIntervalDays = 0;
            return await db.SaveChangesAsync();
        }));
        Assert.Contains("CK_Plants_WateringIntervalDays_Positive", ex.InnerException!.Message);
    }

    [Fact]
    [Trait("AC", "AC2")]
    public async Task Missing_name_and_future_dates_are_rejected()
    {
        var client = await CreateUserClientAsync();

        var response = await client.PostAsync("/api/plants", PlantForm(name: " ", lastWateredOn: Today.AddDays(1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["lastWateredOn", "name"], (await ReadValidationErrorsAsync(response)).Errors.Keys.Order());
    }

    [Fact]
    [Trait("AC", "AC3")]
    public async Task List_shows_due_overdue_upcoming_and_ok_tasks_most_urgent_first()
    {
        var client = await CreateUserClientAsync();
        // Next watering: today (due). Next feeding: yesterday (overdue).
        await CreatePlantAsync(client, PlantForm(name: "Ma esedékes", wateringIntervalDays: 7, lastWateredOn: Today.AddDays(-7),
            feedingIntervalDays: 14, lastFedOn: Today.AddDays(-15)));
        // Next watering: in 2 days (upcoming boundary). Next feeding: in 3 days (ok boundary).
        await CreatePlantAsync(client, PlantForm(name: "Közeledik", wateringIntervalDays: 3, lastWateredOn: Today.AddDays(-1),
            feedingIntervalDays: 3, lastFedOn: Today));

        var list = (await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json))!;

        Assert.Equal(["Ma esedékes", "Közeledik"], list.Select(p => p.Name));
        Assert.Equal((Today, 0, CareStatus.Due), (list[0].Watering.NextDueOn, list[0].Watering.DaysUntilDue, list[0].Watering.Status));
        Assert.Equal((Today.AddDays(-1), -1, CareStatus.Overdue), (list[0].Feeding.NextDueOn, list[0].Feeding.DaysUntilDue, list[0].Feeding.Status));
        Assert.Equal((Today.AddDays(2), CareStatus.Upcoming), (list[1].Watering.NextDueOn, list[1].Watering.Status));
        Assert.Equal((Today.AddDays(3), CareStatus.Ok), (list[1].Feeding.NextDueOn, list[1].Feeding.Status));
    }

    [Fact]
    [Trait("AC", "AC8")]
    public async Task Deleted_plant_disappears_together_with_its_events_and_photos()
    {
        var client = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(client, PlantForm(photo: TestImages.Jpeg));
        var keep = await CreatePlantAsync(client, PlantForm(name: "Marad", photo: TestImages.Png));
        await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "watering" });
        await client.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "feeding" });
        await client.PostAsync($"/api/plants/{plant.Id}/photos", PhotoForm(TestImages.Png));
        Assert.Equal((2, 2), await QueryDbAsync(async db => (
            await db.CareEvents.CountAsync(e => e.PlantId == plant.Id),
            await db.PlantPhotos.CountAsync(p => p.PlantId == plant.Id))));

        var delete = await client.DeleteAsync($"/api/plants/{plant.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/plants/{plant.Id}")).StatusCode);
        var list = (await client.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json))!;
        Assert.Equal([keep.Id], list.Select(p => p.Id));
        // Nothing about the plant is left in the database; the other plant is untouched.
        Assert.Equal((0, 0, 0), await QueryDbAsync(async db => (
            await db.Plants.CountAsync(p => p.Id == plant.Id),
            await db.CareEvents.CountAsync(e => e.PlantId == plant.Id),
            await db.PlantPhotos.CountAsync(p => p.PlantId == plant.Id))));
        Assert.Equal(1, await QueryDbAsync(db => db.PlantPhotos.CountAsync(p => p.PlantId == keep.Id)));
    }

    [Fact]
    [Trait("AC", "AC8")]
    public async Task Deleting_a_missing_plant_returns_404()
    {
        var client = await CreateUserClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/plants/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    [Trait("AC", "Security")]
    public async Task Other_users_plant_is_invisible_and_cannot_be_changed()
    {
        var owner = await CreateUserClientAsync();
        var intruder = await CreateUserClientAsync();
        var plant = await CreatePlantAsync(owner, PlantForm(photo: TestImages.Jpeg));
        var photoId = plant.Photos[0].Id;

        Assert.Empty((await intruder.GetFromJsonAsync<List<PlantSummaryDto>>("/api/plants", Json))!);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/plants/{plant.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync($"/api/plants/{plant.Id}/care-events", new { type = "watering" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/plants/{plant.Id}/photos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/plants/{plant.Id}/photos/{photoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsync($"/api/plants/{plant.Id}/photos", PhotoForm(TestImages.Png))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/plants/{plant.Id}")).StatusCode);

        // The owner's data is unchanged.
        var detail = await owner.GetFromJsonAsync<PlantDetailDto>($"/api/plants/{plant.Id}", Json);
        Assert.Empty(detail!.History);
        Assert.Single(detail.Photos);
    }
}
