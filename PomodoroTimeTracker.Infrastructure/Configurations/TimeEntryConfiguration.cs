using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(te => te.Id);

        builder.Property(te => te.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(te => te.StartTime)
            .IsRequired();

        builder.Property(te => te.CreatedAt)
            .IsRequired();

        builder.HasIndex(te => te.StartTime);
        builder.HasIndex(te => te.ProjectId);
    }
}
