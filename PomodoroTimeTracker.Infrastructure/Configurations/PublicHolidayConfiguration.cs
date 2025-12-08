using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.Date)
            .IsRequired();

        builder.Property(ph => ph.LocalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ph => ph.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ph => ph.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(ph => ph.IsGlobal)
            .IsRequired();

        builder.Property(ph => ph.FetchedAt)
            .IsRequired();

        // Composite unique index on Date + CountryCode
        builder.HasIndex(ph => new { ph.Date, ph.CountryCode })
            .IsUnique();

        // Index for efficient lookup by country
        builder.HasIndex(ph => ph.CountryCode);
    }
}
