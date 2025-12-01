namespace PomodoroTimeTracker.Application.Interfaces;

/// <summary>
/// Abstraction for dispatcher timer to enable unit testing of ViewModels.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this abstraction exists:</b>
/// </para>
/// <para>
/// WinUI 3's DispatcherQueueTimer requires a UI thread context to function.
/// When running unit tests, there is no UI thread, so DispatcherQueue.GetForCurrentThread()
/// returns null, making it impossible to create timers directly in ViewModels during tests.
/// </para>
/// <para>
/// By injecting IDispatcherTimer through dependency injection, we can:
/// <list type="bullet">
///   <item>Use DispatcherTimerWrapper in production (real timer on UI thread)</item>
///   <item>Use a mock implementation in tests (controllable, no UI thread needed)</item>
/// </list>
/// </para>
/// <para>
/// This follows the Dependency Inversion Principle: ViewModels depend on this abstraction
/// (in the Application layer), not on the concrete WinUI 3 implementation.
/// </para>
/// </remarks>
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
