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
    /// <param name="soundFileName">Sound file name to play.</param>
    Task PlayWrapUpNotificationAsync(int volume, string soundFileName);

    /// <summary>
    /// Plays the main alarm sound.
    /// </summary>
    /// <param name="volume">Volume level from 0 to 100.</param>
    /// <param name="soundFileName">Sound file name to play.</param>
    Task PlayAlarmAsync(int volume, string soundFileName);

    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets available wrap-up notification sounds.
    /// </summary>
    IReadOnlyList<string> GetAvailableWrapUpSounds();

    /// <summary>
    /// Gets available alarm sounds.
    /// </summary>
    IReadOnlyList<string> GetAvailableAlarmSounds();
}
