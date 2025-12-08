using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Application.DTOs;

public class WorkScheduleDto
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal BaseHoursPerDay { get; set; }
    public WorkDaysFlags WorkDays { get; set; }
    public bool IncludePublicHolidays { get; set; }
    public string? CountryCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

public class CreateWorkScheduleDto
{
    public int? ClientId { get; set; }
    public int? ProjectId { get; set; }
    public decimal BaseHoursPerDay { get; set; } = 8.0m;
    public WorkDaysFlags WorkDays { get; set; } = WorkDaysFlags.WeekdaysOnly;
    public bool IncludePublicHolidays { get; set; }
    public string? CountryCode { get; set; }
}

public class UpdateWorkScheduleDto
{
    public int Id { get; set; }
    public decimal BaseHoursPerDay { get; set; }
    public WorkDaysFlags WorkDays { get; set; }
    public bool IncludePublicHolidays { get; set; }
    public string? CountryCode { get; set; }
}

/// <summary>
/// Result of checking for schedule conflicts between client and project levels.
/// </summary>
public class ScheduleConflictResult
{
    /// <summary>
    /// True if there are conflicting schedules that would be affected.
    /// </summary>
    public bool HasConflicts { get; set; }

    /// <summary>
    /// Type of conflict detected.
    /// </summary>
    public ScheduleConflictType ConflictType { get; set; }

    /// <summary>
    /// List of project names that have schedules (when enabling client-level schedule).
    /// </summary>
    public List<string> ConflictingProjectNames { get; set; } = [];

    /// <summary>
    /// Client name that has a schedule (when enabling project-level schedule).
    /// </summary>
    public string? ConflictingClientName { get; set; }

    /// <summary>
    /// Number of conflicting schedules.
    /// </summary>
    public int ConflictCount { get; set; }

    public static ScheduleConflictResult NoConflict => new() { HasConflicts = false };
}

public enum ScheduleConflictType
{
    None,
    /// <summary>
    /// Enabling client-level schedule will override project-level schedules.
    /// </summary>
    ProjectSchedulesExist,
    /// <summary>
    /// Enabling project-level schedule when client already has a schedule.
    /// </summary>
    ClientScheduleExists
}
