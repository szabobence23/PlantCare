using Microsoft.EntityFrameworkCore;

namespace PlantCare.Tests.Integration;

/// <summary>
/// Regression guard for schema changes (e.g. the later change request): the migrations must
/// build exactly the current model, so an upgrade of an existing database keeps working.
/// </summary>
[Trait("AC", "Regression")]
public class MigrationTests(PlantCareApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task All_migrations_are_applied_and_model_has_no_unmigrated_changes()
    {
        _ = CreateAnonymousClient(); // starts the app, which migrates the database

        var (pending, hasModelChanges) = await QueryDbAsync(async db => (
            (await db.Database.GetPendingMigrationsAsync()).ToList(),
            db.Database.HasPendingModelChanges()));

        Assert.Empty(pending);
        Assert.False(hasModelChanges, "The model changed without a migration: run 'dotnet ef migrations add <Name>'.");
    }
}
