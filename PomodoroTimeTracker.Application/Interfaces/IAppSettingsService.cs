using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

/// <summary>
/// Service for managing application-level settings and week calculation helpers.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Retrieves the current application settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The application settings.</returns>
    Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="dto">The settings to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    Task<AppSettingsDto> UpdateSettingsAsync(UpdateAppSettingsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the last opened settings tab.
    /// </summary>
    /// <param name="tabName">The name of the tab that was last opened.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLastOpenedTabAsync(string tabName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured week start day (uses cached settings).
    /// </summary>
    /// <returns>The configured first day of the week.</returns>
    DayOfWeek GetWeekStartDay();

    /// <summary>
    /// Calculates the week number for a given date using the configured week year standard.
    /// </summary>
    /// <param name="date">The date to calculate the week number for.</param>
    /// <returns>The week number.</returns>
    int GetWeekNumber(DateTime date);

    /// <summary>
    /// Gets the start date of the week containing the given date.
    /// </summary>
    /// <param name="date">The date to find the week start for.</param>
    /// <returns>The start date of the week.</returns>
    DateTime GetWeekStart(DateTime date);

    /// <summary>
    /// Gets the end date of the week containing the given date.
    /// </summary>
    /// <param name="date">The date to find the week end for.</param>
    /// <returns>The end date of the week.</returns>
    DateTime GetWeekEnd(DateTime date);
}
