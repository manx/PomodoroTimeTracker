using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class PomodoroSettingsConfiguration : IEntityTypeConfiguration<PomodoroSettings>
{
    public void Configure(EntityTypeBuilder<PomodoroSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkDurationMinutes)
            .IsRequired();

        builder.Property(s => s.ShortBreakDurationMinutes)
            .IsRequired();

        builder.Property(s => s.LongBreakDurationMinutes)
            .IsRequired();

        builder.Property(s => s.LongBreakInterval)
            .IsRequired();

        builder.Property(s => s.ShowNotification)
            .IsRequired();

        builder.Property(s => s.PlaySound)
            .IsRequired();

        builder.Property(s => s.FlashWindow)
            .IsRequired();

        builder.Property(s => s.WrapUpPeriodMinutes)
            .IsRequired();

        builder.Property(s => s.WrapUpNotificationVolume)
            .IsRequired();

        builder.Property(s => s.UseAlarm)
            .IsRequired();

        builder.Property(s => s.AlarmVolume)
            .IsRequired();

        builder.Property(s => s.LastModified)
            .IsRequired();
    }
}
