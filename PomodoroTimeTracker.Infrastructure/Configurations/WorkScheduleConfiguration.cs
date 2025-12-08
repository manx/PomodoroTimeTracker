using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.HasKey(ws => ws.Id);

        builder.Property(ws => ws.BaseHoursPerDay)
            .IsRequired()
            .HasPrecision(4, 2)
            .HasDefaultValue(8.0m);

        builder.Property(ws => ws.WorkDays)
            .IsRequired()
            .HasDefaultValue((int)WorkDaysFlags.WeekdaysOnly);

        builder.Property(ws => ws.IncludePublicHolidays)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ws => ws.CountryCode)
            .HasMaxLength(2);

        builder.Property(ws => ws.CreatedAt)
            .IsRequired();

        builder.Property(ws => ws.LastModified)
            .IsRequired();

        // Check constraint: XOR - only one of ClientId or ProjectId can be set
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WorkSchedule_ClientOrProject",
            "(ClientId IS NOT NULL AND ProjectId IS NULL) OR (ClientId IS NULL AND ProjectId IS NOT NULL)"));

        // Unique index on ClientId (filtered to non-null)
        builder.HasIndex(ws => ws.ClientId)
            .IsUnique()
            .HasFilter("ClientId IS NOT NULL");

        // Unique index on ProjectId (filtered to non-null)
        builder.HasIndex(ws => ws.ProjectId)
            .IsUnique()
            .HasFilter("ProjectId IS NOT NULL");

        // Relationships
        builder.HasOne(ws => ws.Client)
            .WithOne()
            .HasForeignKey<WorkSchedule>(ws => ws.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ws => ws.Project)
            .WithOne()
            .HasForeignKey<WorkSchedule>(ws => ws.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
