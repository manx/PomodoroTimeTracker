namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Defines the standard for determining week numbers and week year assignment.
/// </summary>
public enum WeekYearStandard
{
    /// <summary>
    /// ISO 8601: Week 1 contains the first Thursday of the year. Week starts on Monday.
    /// </summary>
    Iso8601 = 0,

    /// <summary>
    /// US/North American: Week 1 contains January 1st.
    /// </summary>
    UsStandard = 1
}
