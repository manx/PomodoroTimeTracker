namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Unified time tracking entity for all session types (Pomodoro, Regular, StopWatch, Manual).
/// </summary>
public class TimeEntry
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public int SessionTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool? IsCompleted { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Project? Project { get; set; }
    public SessionType SessionType { get; set; } = null!;
}
