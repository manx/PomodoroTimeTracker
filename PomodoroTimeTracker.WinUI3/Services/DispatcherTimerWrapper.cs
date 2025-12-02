using Microsoft.UI.Dispatching;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Production implementation of IDispatcherTimer using WinUI 3's DispatcherQueueTimer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this wrapper exists:</b>
/// </para>
/// <para>
/// This class wraps WinUI 3's DispatcherQueueTimer behind the IDispatcherTimer interface.
/// The wrapper enables ViewModels to be unit tested by allowing mock timers to be injected
/// during tests instead of this production implementation.
/// </para>
/// <para>
/// <b>Important:</b> This class must be instantiated on the UI thread because
/// DispatcherQueue.GetForCurrentThread() requires a UI thread context.
/// In tests, this would fail with a NullReferenceException.
/// </para>
/// </remarks>
/// <seealso cref="IDispatcherTimer"/>
internal sealed class DispatcherTimerWrapper : IDispatcherTimer
{
    private readonly DispatcherQueueTimer _timer;

    public DispatcherTimerWrapper()
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = dispatcherQueue.CreateTimer();
        _timer.Tick += OnTimerTick;
    }

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool IsRunning => _timer.IsRunning;

    public event EventHandler? Tick;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}
