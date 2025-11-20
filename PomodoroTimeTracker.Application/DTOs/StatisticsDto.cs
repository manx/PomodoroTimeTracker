namespace PomodoroTimeTracker.Application.DTOs;

public class DailyStatisticsDto
{
    public string Date { get; set; } = string.Empty;
    public PomodoroStatsDto PomodoroSessions { get; set; } = new();
    public TimeEntryStatsDto TimeEntries { get; set; } = new();
}

public class WeeklyStatisticsDto
{
    public string WeekStart { get; set; } = string.Empty;
    public string WeekEnd { get; set; } = string.Empty;
    public PomodoroStatsDto PomodoroSessions { get; set; } = new();
    public TimeEntryStatsDto TimeEntries { get; set; } = new();
}

public class ProjectStatisticsDto
{
    public ProjectInfoDto Project { get; set; } = new();
    public PomodoroStatsDto PomodoroSessions { get; set; } = new();
    public TimeEntryStatsDto TimeEntries { get; set; } = new();
}

public class PomodoroStatsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int WorkSessions { get; set; }
    public int TotalMinutes { get; set; }
    public List<ProjectStatsDto> ByProject { get; set; } = new();
    public List<DayStatsDto>? ByDay { get; set; }
    public DateTime? LastSession { get; set; }
}

public class TimeEntryStatsDto
{
    public int Total { get; set; }
    public int TotalMinutes { get; set; }
    public List<ProjectStatsDto> ByProject { get; set; } = new();
    public List<DayStatsDto>? ByDay { get; set; }
    public DateTime? LastEntry { get; set; }
}

public class ProjectStatsDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Minutes { get; set; }
}

public class DayStatsDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Minutes { get; set; }
}

public class ProjectInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ClientName { get; set; } = string.Empty;
}
