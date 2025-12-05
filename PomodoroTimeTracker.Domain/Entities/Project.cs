namespace PomodoroTimeTracker.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ClientId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Client? Client { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
