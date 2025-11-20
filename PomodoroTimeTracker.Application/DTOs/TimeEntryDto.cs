namespace PomodoroTimeTracker.Application.DTOs;

public class TimeEntryDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ClientName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTimeEntryDto
{
    public int? ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateTimeEntryDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
}
