using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Infrastructure.Configurations;

public class SessionTypeConfiguration : IEntityTypeConfiguration<SessionType>
{
    public void Configure(EntityTypeBuilder<SessionType> builder)
    {
        builder.HasKey(st => st.Id);

        builder.Property(st => st.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(st => st.Description)
            .HasMaxLength(200);

        builder.Property(st => st.IsTimerType)
            .IsRequired()
            .HasDefaultValue(true);

        // Seed data
        builder.HasData(
            new SessionType { Id = 1, Name = "Pomodoro", Description = "Pomodoro work session", IsTimerType = true },
            new SessionType { Id = 2, Name = "ShortBreak", Description = "Short break between pomodoros", IsTimerType = true },
            new SessionType { Id = 3, Name = "LongBreak", Description = "Long break after 4 pomodoros", IsTimerType = true },
            new SessionType { Id = 4, Name = "Regular", Description = "Regular countdown timer session", IsTimerType = true },
            new SessionType { Id = 5, Name = "StopWatch", Description = "Stopwatch timer session", IsTimerType = true },
            new SessionType { Id = 6, Name = "Manual", Description = "Manually entered time entry", IsTimerType = false }
        );
    }
}
