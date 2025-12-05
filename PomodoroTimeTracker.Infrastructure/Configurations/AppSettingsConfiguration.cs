using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for AppSettings entity.
/// </summary>
public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.LastOpenedSettingsTab)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.LanguageOverride)
            .HasMaxLength(10);

        builder.Property(s => s.DateFormatOverride)
            .HasMaxLength(50);

        builder.Property(s => s.WeekStartDay)
            .IsRequired();

        builder.Property(s => s.WeekYearStandard)
            .IsRequired();

        builder.Property(s => s.LastModified)
            .IsRequired();
    }
}
