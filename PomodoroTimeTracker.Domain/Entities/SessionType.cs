namespace PomodoroTimeTracker.Domain.Entities;

public enum SessionType
{
    Work,
    ShortBreak,
    LongBreak,
    Regular,
    /// <summary>
    /// Session recorded using the StopWatch timer (counts up, no duration limit).
    /// </summary>
    StopWatch
}
