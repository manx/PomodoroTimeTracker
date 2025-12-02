using System.ComponentModel;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Represents which timer type is currently active.
/// </summary>
internal enum ActiveTimerType
{
    /// <summary>
    /// No timer is currently running.
    /// </summary>
    None,

    /// <summary>
    /// Pomodoro timer is active.
    /// </summary>
    Pomodoro,

    /// <summary>
    /// Regular timer is active.
    /// </summary>
    RegularTimer,

    /// <summary>
    /// StopWatch is active.
    /// </summary>
    StopWatch
}

/// <summary>
/// Service for coordinating active timer state across the application.
/// Ensures only one timer can run at a time and provides state for menu item enabling.
/// </summary>
internal interface IActiveTimerService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the currently active timer type (None if no timer running).
    /// </summary>
    ActiveTimerType ActiveTimer { get; }

    /// <summary>
    /// Gets a value indicating whether any timer is currently active.
    /// </summary>
    bool HasActiveTimer { get; }

    /// <summary>
    /// Attempts to set the active timer. Returns false if a different timer is already active.
    /// </summary>
    /// <param name="timerType">The timer type to set as active.</param>
    /// <returns>True if the timer was set as active; false if another timer is active.</returns>
    bool TrySetActiveTimer(ActiveTimerType timerType);

    /// <summary>
    /// Clears the active timer, allowing any timer to start.
    /// </summary>
    void ClearActiveTimer();

    /// <summary>
    /// Gets whether the Pomodoro menu item should be enabled.
    /// True if no timer is active OR Pomodoro is the active timer.
    /// </summary>
    bool IsPomodoroEnabled { get; }

    /// <summary>
    /// Gets whether the Regular Timer menu item should be enabled.
    /// True if no timer is active OR Regular Timer is the active timer.
    /// </summary>
    bool IsRegularTimerEnabled { get; }

    /// <summary>
    /// Gets whether the StopWatch menu item should be enabled.
    /// True if no timer is active OR StopWatch is the active timer.
    /// </summary>
    bool IsStopWatchEnabled { get; }
}
