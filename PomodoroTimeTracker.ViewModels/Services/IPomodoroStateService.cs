namespace PomodoroTimeTracker.ViewModels.Services;

/// <summary>
/// Service for managing shared Pomodoro cycle state across timer ViewModels.
/// Replaces static CurrentPomodoroCount for better testability and DI.
/// </summary>
public interface IPomodoroStateService
{
    /// <summary>
    /// Gets or sets the current pomodoro count in the cycle (0-3).
    /// </summary>
    int CurrentPomodoroCount { get; set; }

    /// <summary>
    /// Resets the pomodoro cycle count to zero.
    /// </summary>
    void ResetCycle();

    /// <summary>
    /// Increments the count and checks if it's time for a long break.
    /// </summary>
    /// <returns>True if a long break should occur (after 4 pomodoros), false for short break.</returns>
    bool IncrementAndCheckLongBreak();
}
