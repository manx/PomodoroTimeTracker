using System.ComponentModel;
using System.Windows.Input;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Interface for ViewModels that can be displayed in the TimerWindow.
/// Implemented by both PomodoroViewModel and RegularTimerViewModel.
/// </summary>
public interface ITimerWindowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the progress percentage (0-100) for the progress bar.
    /// </summary>
    double ProgressPercentage { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is paused.
    /// </summary>
    bool IsPausedState { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is running.
    /// </summary>
    bool IsRunningState { get; }

    /// <summary>
    /// Gets the timer display string in MM:SS format.
    /// </summary>
    string TimerDisplay { get; }

    /// <summary>
    /// Gets a value indicating whether the timer counts up (true) or down (false).
    /// </summary>
    bool CountsUp { get; }

    /// <summary>
    /// Gets the description/objective for the current session.
    /// </summary>
    string SessionDescription { get; }

    /// <summary>
    /// Gets the command to pause or resume the timer.
    /// </summary>
    ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets the elapsed time in seconds for the current session.
    /// </summary>
    int ElapsedSeconds { get; }

    /// <summary>
    /// Saves the current session and stops the timer.
    /// </summary>
    Task SaveAndStopAsync();

    /// <summary>
    /// Discards the current session and stops the timer.
    /// </summary>
    Task DiscardAndStopAsync();

    /// <summary>
    /// Adds time to the current session.
    /// </summary>
    /// <param name="minutes">Number of minutes to add.</param>
    void AddMinutes(int minutes);

    /// <summary>
    /// Gets a value indicating whether the progress meter should be shown.
    /// </summary>
    bool ShowProgressMeter { get; }
}
