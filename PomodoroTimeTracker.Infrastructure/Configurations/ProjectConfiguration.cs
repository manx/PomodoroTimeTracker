using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => new { p.Name, p.ClientId })
            .IsUnique();

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.ClientId);

        builder.HasMany(p => p.TimeEntries)
            .WithOne(te => te.Project)
            .HasForeignKey(te => te.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
