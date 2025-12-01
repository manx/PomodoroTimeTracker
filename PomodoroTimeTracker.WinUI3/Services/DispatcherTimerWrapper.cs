using Microsoft.UI.Dispatching;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Wrapper around DispatcherQueueTimer for production use.
/// Implements IDispatcherTimer to enable unit testing of ViewModels.
/// </summary>
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
