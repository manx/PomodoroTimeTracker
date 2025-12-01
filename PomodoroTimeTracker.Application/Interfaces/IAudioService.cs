namespace PomodoroTimeTracker.Application.Interfaces;

/// <summary>
/// Service for playing audio notifications and alarms.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Plays the wrap-up notification sound.
    /// </summary>
    /// <param name="volume">Volume level from 0 to 100.</param>
    Task PlayWrapUpNotificationAsync(int volume);

    /// <summary>
    /// Plays the main alarm sound.
    /// </summary>
    /// <param name="volume">Volume level from 0 to 100.</param>
    Task PlayAlarmAsync(int volume);

    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    Task StopAsync();
}
