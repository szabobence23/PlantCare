using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.Contracts;
using PlantCare.Api.Data;

namespace PlantCare.Tests.Integration;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(PlantCareApiFactory factory)
{
    protected static readonly DateOnly Today = PlantCareApiFactory.Today;

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    protected PlantCareApiFactory Factory => factory;

    protected HttpClient CreateAnonymousClient() => factory.CreateClient();

    /// <summary>Registers a fresh user (unique e-mail) and returns a client carrying its token.</summary>
    protected async Task<HttpClient> CreateUserClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "jelszo123" });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    protected static MultipartFormDataContent PlantForm(
        string? name = "Fikusz",
        string? species = "Ficus elastica",
        int? wateringIntervalDays = 7,
        int? feedingIntervalDays = 14,
        DateOnly? lastWateredOn = null,
        DateOnly? lastFedOn = null,
        byte[]? photo = null)
    {
        var form = new MultipartFormDataContent();
        void Add(string key, string? value)
        {
            if (value is not null) form.Add(new StringContent(value), key);
        }
        Add("name", name);
        Add("species", species);
        Add("wateringIntervalDays", wateringIntervalDays?.ToString(CultureInfo.InvariantCulture));
        Add("feedingIntervalDays", feedingIntervalDays?.ToString(CultureInfo.InvariantCulture));
        Add("lastWateredOn", (lastWateredOn ?? Today.AddDays(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("lastFedOn", (lastFedOn ?? Today.AddDays(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (photo is not null) form.Add(new ByteArrayContent(photo), "photo", "plant.jpg");
        return form;
    }

    protected static MultipartFormDataContent PhotoForm(byte[] photo, string fileName = "photo.jpg")
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(photo), "photo", fileName);
        return form;
    }

    protected static async Task<PlantDetailDto> CreatePlantAsync(HttpClient client, MultipartFormDataContent? form = null)
    {
        var response = await client.PostAsync("/api/plants", form ?? PlantForm());
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlantDetailDto>(Json))!;
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    protected static Task<ValidationProblemDetails> ReadValidationErrorsAsync(HttpResponseMessage response) =>
        ReadAsync<ValidationProblemDetails>(response);

    /// <summary>Direct database access, to verify what was (or was not) persisted.</summary>
    protected async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
