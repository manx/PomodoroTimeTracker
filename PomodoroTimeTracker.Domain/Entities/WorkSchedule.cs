namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Represents work schedule configuration for a Client OR Project (exclusively).
/// Only one of ClientId or ProjectId should be set (XOR relationship).
/// </summary>
public class WorkSchedule
{
    public int Id { get; set; }

    /// <summary>
    /// Client this schedule applies to. Mutually exclusive with ProjectId.
    /// </summary>
    public int? ClientId { get; set; }

    /// <summary>
    /// Project this schedule applies to. Mutually exclusive with ClientId.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Work percentage where 100 = full-time (e.g., 80 = part-time 80%).
    /// </summary>
    public int WorkPercentage { get; set; } = 100;

    /// <summary>
    /// Base hours per workday (e.g., 8.0).
    /// </summary>
    public decimal BaseHoursPerDay { get; set; } = 8.0m;

    /// <summary>
    /// Bit flags for work days using WorkDaysFlags enum.
    /// Default: Mon-Fri (62).
    /// </summary>
    public int WorkDays { get; set; } = (int)WorkDaysFlags.WeekdaysOnly;

    /// <summary>
    /// Whether to include public holidays as work days.
    /// If false, holidays reduce expected work hours.
    /// </summary>
    public bool IncludePublicHolidays { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code for public holidays (e.g., "SE", "US").
    /// </summary>
    public string? CountryCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Client? Client { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// Checks if a specific day of week is a work day.
    /// </summary>
    public bool IsWorkDay(DayOfWeek dayOfWeek)
    {
        var flag = dayOfWeek switch
        {
            DayOfWeek.Sunday => WorkDaysFlags.Sunday,
            DayOfWeek.Monday => WorkDaysFlags.Monday,
            DayOfWeek.Tuesday => WorkDaysFlags.Tuesday,
            DayOfWeek.Wednesday => WorkDaysFlags.Wednesday,
            DayOfWeek.Thursday => WorkDaysFlags.Thursday,
            DayOfWeek.Friday => WorkDaysFlags.Friday,
            DayOfWeek.Saturday => WorkDaysFlags.Saturday,
            _ => WorkDaysFlags.None
        };

        return (WorkDays & (int)flag) != 0;
    }
}
