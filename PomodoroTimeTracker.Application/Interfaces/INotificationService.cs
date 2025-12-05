namespace PomodoroTimeTracker.Application.Interfaces;

/// <summary>
/// Service for displaying Windows toast notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a toast notification when work session completes (entering wrap-up).
    /// </summary>
    /// <param name="objective">The work session objective text.</param>
    Task ShowWorkCompleteAsync(string objective);

    /// <summary>
    /// Shows a toast notification when wrap-up period completes (time for break).
    /// </summary>
    /// <param name="objective">The work session objective text.</param>
    /// <param name="isLongBreak">True if next break is a long break.</param>
    Task ShowBreakStartingAsync(string objective, bool isLongBreak);

    /// <summary>
    /// Shows a toast notification when break period completes.
    /// </summary>
    Task ShowBreakCompleteAsync();

    /// <summary>
    /// Gets whether toast notifications are supported on this system.
    /// </summary>
    bool IsSupported { get; }
}
