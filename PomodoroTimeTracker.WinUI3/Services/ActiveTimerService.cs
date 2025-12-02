using System.ComponentModel;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of <see cref="IActiveTimerService"/> that tracks which timer is active
/// and notifies subscribers when the state changes.
/// </summary>
internal sealed class ActiveTimerService : IActiveTimerService
{
    private ActiveTimerType _activeTimer = ActiveTimerType.None;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public ActiveTimerType ActiveTimer
    {
        get => _activeTimer;
        private set
        {
            if (_activeTimer != value)
            {
                _activeTimer = value;
                OnPropertyChanged(nameof(ActiveTimer));
                OnPropertyChanged(nameof(HasActiveTimer));
                OnPropertyChanged(nameof(IsPomodoroEnabled));
                OnPropertyChanged(nameof(IsRegularTimerEnabled));
                OnPropertyChanged(nameof(IsStopWatchEnabled));
            }
        }
    }

    /// <inheritdoc/>
    public bool HasActiveTimer => ActiveTimer != ActiveTimerType.None;

    /// <inheritdoc/>
    public bool IsPomodoroEnabled => !HasActiveTimer || ActiveTimer == ActiveTimerType.Pomodoro;

    /// <inheritdoc/>
    public bool IsRegularTimerEnabled => !HasActiveTimer || ActiveTimer == ActiveTimerType.RegularTimer;

    /// <inheritdoc/>
    public bool IsStopWatchEnabled => !HasActiveTimer || ActiveTimer == ActiveTimerType.StopWatch;

    /// <inheritdoc/>
    public bool TrySetActiveTimer(ActiveTimerType timerType)
    {
        if (HasActiveTimer && ActiveTimer != timerType)
        {
            return false;
        }

        ActiveTimer = timerType;
        return true;
    }

    /// <inheritdoc/>
    public void ClearActiveTimer()
    {
        ActiveTimer = ActiveTimerType.None;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
