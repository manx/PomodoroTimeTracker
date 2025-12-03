using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of <see cref="IPomodoroStateService"/> for managing Pomodoro cycle state.
/// </summary>
/// <remarks>
/// Registered as singleton in DI to share state across all timer ViewModels.
/// </remarks>
public class PomodoroStateService : IPomodoroStateService
{
    private int _currentPomodoroCount;

    /// <inheritdoc/>
    public int CurrentPomodoroCount
    {
        get => _currentPomodoroCount;
        set => _currentPomodoroCount = value;
    }

    /// <inheritdoc/>
    public void ResetCycle()
    {
        _currentPomodoroCount = 0;
    }

    /// <inheritdoc/>
    public bool IncrementAndCheckLongBreak()
    {
        _currentPomodoroCount++;

        if (_currentPomodoroCount >= 4)
        {
            _currentPomodoroCount = 0;
            return true; // Long break
        }

        return false; // Short break
    }
}
