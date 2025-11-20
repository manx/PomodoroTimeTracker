namespace PomodoroTimeTracker.Application.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ClientId { get; set; }
}

public class UpdateProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ClientId { get; set; }
}
