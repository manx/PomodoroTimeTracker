namespace PomodoroTimeTracker.Application.DTOs;

public class SessionTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsTimerType { get; set; }
}
