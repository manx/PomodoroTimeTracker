namespace PomodoroTimeTracker.Domain.Entities;

public class TimeEntry
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Project? Project { get; set; }
}
