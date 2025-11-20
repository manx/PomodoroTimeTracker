using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class PomodoroSessionConfiguration : IEntityTypeConfiguration<PomodoroSession>
{
    public void Configure(EntityTypeBuilder<PomodoroSession> builder)
    {
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.StartTime)
            .IsRequired();

        builder.Property(ps => ps.DurationMinutes)
            .IsRequired();

        builder.Property(ps => ps.IsCompleted)
            .IsRequired();

        builder.Property(ps => ps.SessionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ps => ps.Notes)
            .HasMaxLength(500);

        builder.HasIndex(ps => ps.StartTime);
        builder.HasIndex(ps => ps.ProjectId);
    }
}
