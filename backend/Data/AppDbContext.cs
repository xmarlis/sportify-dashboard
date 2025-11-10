using Microsoft.EntityFrameworkCore;
using RunningDashboard.Models;

namespace RunningDashboard.Data;

/// <summary>
/// Database context for the Running Dashboard application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Activity> Activities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Activity entity
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DistanceMeters).IsRequired();
            entity.Property(e => e.MovingTimeSeconds).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
        });
    }
}
