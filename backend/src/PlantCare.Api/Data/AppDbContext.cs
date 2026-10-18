using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Domain;

namespace PlantCare.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<CareEvent> CareEvents => Set<CareEvent>();
    public DbSet<PlantPhoto> PlantPhotos => Set<PlantPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(254);
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Plant>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100);
            e.Property(p => p.Species).HasMaxLength(150);
            e.HasIndex(p => p.UserId);
            e.HasOne(p => p.User).WithMany(u => u.Plants).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            // Database-level guard for spec AC5, in addition to the API validation.
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Plants_WateringIntervalDays_Positive", "\"WateringIntervalDays\" > 0");
                t.HasCheckConstraint("CK_Plants_FeedingIntervalDays_Positive", "\"FeedingIntervalDays\" > 0");
            });
        });

        // Children are removed by ON DELETE CASCADE when their plant is deleted (spec AC8).
        modelBuilder.Entity<CareEvent>(e =>
        {
            e.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(c => c.PlantId);
            e.HasOne<Plant>().WithMany(p => p.CareEvents).HasForeignKey(c => c.PlantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlantPhoto>(e =>
        {
            e.Property(p => p.ContentType).HasMaxLength(50);
            e.HasIndex(p => p.PlantId);
            e.HasOne<Plant>().WithMany(p => p.Photos).HasForeignKey(p => p.PlantId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
