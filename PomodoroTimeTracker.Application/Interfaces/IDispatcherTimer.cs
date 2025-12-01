namespace PomodoroTimeTracker.Application.Interfaces;

/// <summary>
/// Abstraction for dispatcher timer to enable unit testing of ViewModels.
/// Implementations should dispatch timer ticks on the UI thread.
/// </summary>
public interface IDispatcherTimer
{
    /// <summary>
    /// Gets or sets the interval between timer ticks.
    /// </summary>
    TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets a value indicating whether the timer is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void Stop();

    /// <summary>
    /// Event raised on each timer tick.
    /// </summary>
    event EventHandler? Tick;
}
