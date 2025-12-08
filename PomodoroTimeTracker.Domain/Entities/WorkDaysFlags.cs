namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Bit flags for work days configuration.
/// </summary>
[Flags]
public enum WorkDaysFlags
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,

    /// <summary>
    /// Monday through Friday (2+4+8+16+32 = 62).
    /// </summary>
    WeekdaysOnly = Monday | Tuesday | Wednesday | Thursday | Friday,

    /// <summary>
    /// All seven days (127).
    /// </summary>
    AllDays = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}
