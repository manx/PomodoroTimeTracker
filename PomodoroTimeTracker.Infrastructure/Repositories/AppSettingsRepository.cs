using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

/// <summary>
/// Repository for managing general application settings.
/// Implements singleton pattern - only one settings record should exist.
/// </summary>
public class AppSettingsRepository(ApplicationDbContext context) : Repository<AppSettings>(context), IAppSettingsRepository
{
    /// <summary>
    /// Gets the application settings. Creates default settings if none exist.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The application settings entity.</returns>
    public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // There should only be one settings record
        var settings = await _dbSet.FirstOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            // Create default settings if none exist
            settings = new AppSettings
            {
                LastOpenedSettingsTab = "General",
                LanguageOverride = null,
                DateFormatOverride = null,
                WeekStartDay = DayOfWeek.Sunday,
                WeekYearStandard = WeekYearStandard.Iso8601,
                LastModified = DateTime.UtcNow
            };

            await _dbSet.AddAsync(settings, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }
}
