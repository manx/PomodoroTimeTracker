namespace PomodoroTimeTracker.Domain.Entities;

public enum SessionType
{
    Work,
    ShortBreak,
    LongBreak,
    /// <summary>
    /// Session recorded using the Regular Timer (countdown with configurable duration).
    /// </summary>
    Regular,
    /// <summary>
    /// Session recorded using the StopWatch timer (counts up, no duration limit).
    /// </summary>
    StopWatch
}
