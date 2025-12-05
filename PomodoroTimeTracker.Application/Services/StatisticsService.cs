using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppSettingsService _appSettingsService;

    public StatisticsService(IUnitOfWork unitOfWork, IAppSettingsService appSettingsService)
    {
        _unitOfWork = unitOfWork;
        _appSettingsService = appSettingsService;
    }
    // Helper to identify timer-based work sessions
    private static readonly int[] TimerWorkTypes = [SessionType.Ids.Pomodoro, SessionType.Ids.Regular, SessionType.Ids.StopWatch];

    public async Task<DailyStatisticsDto> GetDailyStatisticsAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDay = targetDate.AddDays(1);

        var allEntries = await _unitOfWork.TimeEntries.GetByDateRangeAsync(targetDate, nextDay);
        var entriesList = allEntries.ToList();

        var timerEntries = entriesList.Where(e => TimerWorkTypes.Contains(e.SessionTypeId)).ToList();
        var manualEntries = entriesList.Where(e => e.SessionTypeId == SessionType.Ids.Manual).ToList();

        return new DailyStatisticsDto
        {
            Date = targetDate.ToString("yyyy-MM-dd"),
            PomodoroSessions = BuildTimerStats(timerEntries),
            TimeEntries = BuildManualEntryStats(manualEntries)
        };
    }

    public async Task<WeeklyStatisticsDto> GetWeeklyStatisticsAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var startOfWeek = _appSettingsService.GetWeekStart(targetDate);
        var endOfWeek = startOfWeek.AddDays(7);

        var allEntries = await _unitOfWork.TimeEntries.GetByDateRangeAsync(startOfWeek, endOfWeek);
        var entriesList = allEntries.ToList();

        var timerEntries = entriesList.Where(e => TimerWorkTypes.Contains(e.SessionTypeId)).ToList();
        var manualEntries = entriesList.Where(e => e.SessionTypeId == SessionType.Ids.Manual).ToList();

        var pomodoroStats = BuildTimerStats(timerEntries);
        pomodoroStats.ByDay = timerEntries
            .GroupBy(e => e.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(e => e.DurationMinutes ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var timeEntryStats = BuildManualEntryStats(manualEntries);
        timeEntryStats.ByDay = manualEntries
            .GroupBy(e => e.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(e => e.DurationMinutes ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new WeeklyStatisticsDto
        {
            WeekStart = startOfWeek.ToString("yyyy-MM-dd"),
            WeekEnd = endOfWeek.AddDays(-1).ToString("yyyy-MM-dd"),
            PomodoroSessions = pomodoroStats,
            TimeEntries = timeEntryStats
        };
    }

    public async Task<DateRangeStatisticsDto> GetDateRangeStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var allEntries = await _unitOfWork.TimeEntries.GetByDateRangeAsync(start, end);
        var entriesList = allEntries.ToList();

        var timerEntries = entriesList.Where(e => TimerWorkTypes.Contains(e.SessionTypeId)).ToList();
        var manualEntries = entriesList.Where(e => e.SessionTypeId == SessionType.Ids.Manual).ToList();

        var pomodoroStats = BuildTimerStats(timerEntries);
        pomodoroStats.ByDay = timerEntries
            .GroupBy(e => e.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(e => e.DurationMinutes ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var timeEntryStats = BuildManualEntryStats(manualEntries);
        timeEntryStats.ByDay = manualEntries
            .GroupBy(e => e.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(e => e.DurationMinutes ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new DateRangeStatisticsDto
        {
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = endDate.Date.ToString("yyyy-MM-dd"),
            PomodoroSessions = pomodoroStats,
            TimeEntries = timeEntryStats
        };
    }

    public async Task<ProjectStatisticsDto> GetProjectStatisticsAsync(int projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {projectId} not found");

        var allEntries = await _unitOfWork.TimeEntries.GetByProjectIdAsync(projectId);
        var entriesList = allEntries.ToList();

        var timerEntries = entriesList.Where(e => TimerWorkTypes.Contains(e.SessionTypeId)).ToList();
        var manualEntries = entriesList.Where(e => e.SessionTypeId == SessionType.Ids.Manual).ToList();

        var pomodoroStats = BuildTimerStats(timerEntries);
        pomodoroStats.LastSession = timerEntries.Any() ? timerEntries.Max(e => e.StartTime) : null;

        var timeEntryStats = BuildManualEntryStats(manualEntries);
        timeEntryStats.LastEntry = manualEntries.Any() ? manualEntries.Max(e => e.StartTime) : null;

        return new ProjectStatisticsDto
        {
            Project = new ProjectInfoDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ClientName = project.Client?.Name ?? "No Client"
            },
            PomodoroSessions = pomodoroStats,
            TimeEntries = timeEntryStats
        };
    }

    private static PomodoroStatsDto BuildTimerStats(List<TimeEntry> entries)
    {
        // Only count entries where IsCompleted is not null for completion percentage
        var completableEntries = entries.Where(e => e.IsCompleted.HasValue).ToList();

        return new PomodoroStatsDto
        {
            Total = entries.Count,
            Completed = completableEntries.Count(e => e.IsCompleted == true),
            WorkSessions = entries.Count(e => e.SessionTypeId == SessionType.Ids.Pomodoro),
            TotalMinutes = entries.Sum(e => e.DurationMinutes ?? 0),
            ByProject = entries
                .Where(e => e.Project != null)
                .GroupBy(e => new { e.Project!.Id, e.Project.Name, ClientName = e.Project.Client != null ? e.Project.Client.Name : "No Client" })
                .Select(g => new ProjectStatsDto
                {
                    ProjectId = g.Key.Id,
                    ProjectName = g.Key.Name,
                    ClientName = g.Key.ClientName,
                    Count = g.Count(),
                    Minutes = g.Sum(e => e.DurationMinutes ?? 0)
                })
                .ToList()
        };
    }

    private static TimeEntryStatsDto BuildManualEntryStats(List<TimeEntry> entries)
    {
        return new TimeEntryStatsDto
        {
            Total = entries.Count,
            TotalMinutes = entries.Sum(e => e.DurationMinutes ?? 0),
            ByProject = entries
                .Where(e => e.Project != null)
                .GroupBy(e => new { e.Project!.Id, e.Project.Name, ClientName = e.Project.Client != null ? e.Project.Client.Name : "No Client" })
                .Select(g => new ProjectStatsDto
                {
                    ProjectId = g.Key.Id,
                    ProjectName = g.Key.Name,
                    ClientName = g.Key.ClientName,
                    Count = g.Count(),
                    Minutes = g.Sum(e => e.DurationMinutes ?? 0)
                })
                .ToList()
        };
    }
}
