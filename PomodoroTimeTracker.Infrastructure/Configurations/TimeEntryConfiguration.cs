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

        builder.Property(te => te.Notes)
            .HasMaxLength(500);

        builder.Property(te => te.CreatedAt)
            .IsRequired();

        builder.Property(te => te.SessionTypeId)
            .IsRequired();

        // Relationships
        builder.HasOne(te => te.SessionType)
            .WithMany(st => st.TimeEntries)
            .HasForeignKey(te => te.SessionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(te => te.Project)
            .WithMany(p => p.TimeEntries)
            .HasForeignKey(te => te.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(te => te.StartTime);
        builder.HasIndex(te => te.ProjectId);
        builder.HasIndex(te => te.SessionTypeId);
    }
}
