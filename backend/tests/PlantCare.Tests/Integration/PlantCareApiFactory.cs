using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PlantCare.Api.Recognition;
using Testcontainers.PostgreSql;

namespace PlantCare.Tests.Integration;

/// <summary>
/// Runs the real API (controllers, EF Core migrations, JWT auth) against a throw-away PostgreSQL
/// container. Only the clock and the external recognition API are replaced.
/// </summary>
public sealed class PlantCareApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Fixed "now": 2026-10-18 12:00 in Budapest, so "today" is 2026-10-18 in every test.</summary>
    public static readonly DateOnly Today = new(2026, 10, 18);

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

    public FakeRecognitionService Recognition { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PlantCare", _db.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("App:TimeZone", "Europe/Budapest");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IPlantRecognitionService>();
            services.AddSingleton<IPlantRecognitionService>(Recognition);
        });
    }

    public Task InitializeAsync() => _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _db.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<PlantCareApiFactory>
{
    public const string Name = "api";
}

public sealed class FakeRecognitionService : IPlantRecognitionService
{
    public int Calls { get; private set; }
    public bool Unavailable { get; set; }
    public IReadOnlyList<SpeciesSuggestion> Suggestions { get; set; } = [];

    public void Reset()
    {
        Calls = 0;
        Unavailable = false;
        Suggestions = [];
    }

    public Task<IReadOnlyList<SpeciesSuggestion>> IdentifyAsync(byte[] image, string contentType, CancellationToken ct)
    {
        Calls++;
        if (Unavailable) throw new RecognitionUnavailableException("A növényfelismerés jelenleg nem érhető el, add meg a fajt kézzel.");
        return Task.FromResult(Suggestions);
    }
}
