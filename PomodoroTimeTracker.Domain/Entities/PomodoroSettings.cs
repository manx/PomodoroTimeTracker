namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Represents user preferences and configuration for the Pomodoro timer.
/// Singleton entity - only one settings record should exist per user.
/// </summary>
public class PomodoroSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for settings.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the default duration for work sessions in minutes.
    /// Default is 25 minutes (traditional Pomodoro length).
    /// </summary>
    public int WorkDurationMinutes { get; set; } = 25;

    /// <summary>
    /// Gets or sets the duration for short breaks in minutes.
    /// Default is 5 minutes. Occurs after pomodoros 1, 2, and 3.
    /// </summary>
    public int ShortBreakDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration for long breaks in minutes.
    /// Default is 15 minutes. Occurs after the 4th pomodoro.
    /// </summary>
    public int LongBreakDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets how many work sessions before a long break.
    /// Default is 4 (long break after every 4th pomodoro).
    /// </summary>
    public int LongBreakInterval { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to show Windows toast notifications on completion.
    /// </summary>
    public bool ShowNotification { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to play a sound alert on completion.
    /// </summary>
    public bool PlaySound { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to flash the window on completion to grab attention.
    /// </summary>
    public bool FlashWindow { get; set; } = false;

    /// <summary>
    /// Gets or sets the wrap up period duration in minutes.
    /// Default is 3 minutes. This period allows finishing up work after the work session completes.
    /// A notification plays when work ends, and the main alarm plays when the wrap up period expires.
    /// </summary>
    public int WrapUpPeriodMinutes { get; set; } = 3;

    /// <summary>
    /// Gets or sets the volume for the wrap up notification (0-100).
    /// Default is 50. This notification plays when the work period ends.
    /// </summary>
    public int WrapUpNotificationVolume { get; set; } = 50;

    /// <summary>
    /// Gets or sets the sound file name for the wrap up notification.
    /// Default is "Windows Notify.wav".
    /// </summary>
    public string WrapUpNotificationSound { get; set; } = "Windows Notify.wav";

    /// <summary>
    /// Gets or sets whether to play the completion alarm when timer reaches zero.
    /// </summary>
    public bool UseAlarm { get; set; } = true;

    /// <summary>
    /// Gets or sets the volume for the completion alarm (0-100).
    /// Default is 50.
    /// </summary>
    public int AlarmVolume { get; set; } = 50;

    /// <summary>
    /// Gets or sets the sound file name for the completion alarm.
    /// Default is "Alarm01.wav".
    /// </summary>
    public string AlarmSound { get; set; } = "Alarm01.wav";

    /// <summary>
    /// Gets or sets when the settings were last modified (UTC).
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the default short break duration based on work duration (workDuration / 5)
    /// </summary>
    public static int CalculateDefaultShortBreak(int workDurationMinutes)
    {
        return (int)Math.Round(workDurationMinutes / 5.0);
    }

    /// <summary>
    /// Calculates the default long break duration based on work duration ((workDuration / 5) * 3)
    /// </summary>
    public static int CalculateDefaultLongBreak(int workDurationMinutes)
    {
        return (int)Math.Round((workDurationMinutes / 5.0) * 3);
    }
}
