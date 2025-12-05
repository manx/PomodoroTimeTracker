namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Represents a type of time tracking session.
/// This is a lookup table entity with pre-seeded values.
/// </summary>
public class SessionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsTimerType { get; set; } = true;

    // Navigation property
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    /// <summary>
    /// Well-known IDs for code reference. Use these constants instead of magic numbers.
    /// </summary>
    public static class Ids
    {
        public const int Pomodoro = 1;
        public const int ShortBreak = 2;
        public const int LongBreak = 3;
        public const int Regular = 4;
        public const int StopWatch = 5;
        public const int Manual = 6;
    }
}
