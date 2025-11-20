using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class PomodoroSettingsRepository(ApplicationDbContext context) : Repository<PomodoroSettings>(context), IPomodoroSettingsRepository
{
    public async Task<PomodoroSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // There should only be one settings record
        var settings = await _dbSet.FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            // Create default settings if none exist
            settings = new PomodoroSettings
            {
                WorkDurationMinutes = 25,
                ShortBreakDurationMinutes = PomodoroSettings.CalculateDefaultShortBreak(25),
                LongBreakDurationMinutes = PomodoroSettings.CalculateDefaultLongBreak(25),
                LongBreakInterval = 4,
                LastModified = DateTime.UtcNow
            };

            await _dbSet.AddAsync(settings, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }
}
