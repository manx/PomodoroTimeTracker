using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Application.DTOs;

public class PomodoroSessionDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ClientName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public SessionType SessionType { get; set; }
    public string? Objective { get; set; }
    public string? Notes { get; set; }
}

public class CreatePomodoroSessionDto
{
    public int? ProjectId { get; set; }
    public int DurationMinutes { get; set; }
    public SessionType SessionType { get; set; }
    public string? Objective { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePomodoroSessionDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public SessionType SessionType { get; set; }
    public string? Objective { get; set; }
    public string? Notes { get; set; }
}
