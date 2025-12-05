using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

/// <summary>
/// Repository interface for managing general application settings.
/// </summary>
public interface IAppSettingsRepository : IRepository<AppSettings>
{
    /// <summary>
    /// Gets the application settings. Creates default settings if none exist.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The application settings entity.</returns>
    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
}
