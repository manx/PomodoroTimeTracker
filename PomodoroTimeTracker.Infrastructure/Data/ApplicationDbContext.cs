using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Configurations;

namespace PomodoroTimeTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<SessionType> SessionTypes { get; set; } = null!;
    public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
    public DbSet<PomodoroSettings> PomodoroSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new SessionTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TimeEntryConfiguration());
        modelBuilder.ApplyConfiguration(new PomodoroSettingsConfiguration());
    }
}
