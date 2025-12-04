using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class StatisticsService(IUnitOfWork unitOfWork) : IStatisticsService
{
    public async Task<DailyStatisticsDto> GetDailyStatisticsAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDay = targetDate.AddDays(1);

        var sessions = await unitOfWork.PomodoroSessions.GetByDateRangeAsync(targetDate, nextDay);
        var entries = await unitOfWork.TimeEntries.GetByDateRangeAsync(targetDate, nextDay);

        return new DailyStatisticsDto
        {
            Date = targetDate.ToString("yyyy-MM-dd"),
            PomodoroSessions = BuildPomodoroStats(sessions.ToList()),
            TimeEntries = BuildTimeEntryStats(entries.ToList())
        };
    }

    public async Task<WeeklyStatisticsDto> GetWeeklyStatisticsAsync(DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var startOfWeek = targetDate.AddDays(-(int)targetDate.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);

        var sessions = await unitOfWork.PomodoroSessions.GetByDateRangeAsync(startOfWeek, endOfWeek);
        var entries = await unitOfWork.TimeEntries.GetByDateRangeAsync(startOfWeek, endOfWeek);

        var pomodoroStats = BuildPomodoroStats(sessions.ToList());
        pomodoroStats.ByDay = sessions
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(s => s.DurationMinutes)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var timeEntryStats = BuildTimeEntryStats(entries.ToList());
        timeEntryStats.ByDay = entries
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

        var sessions = await unitOfWork.PomodoroSessions.GetByDateRangeAsync(start, end);
        var entries = await unitOfWork.TimeEntries.GetByDateRangeAsync(start, end);

        var pomodoroStats = BuildPomodoroStats(sessions.ToList());
        pomodoroStats.ByDay = sessions
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new DayStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Minutes = g.Sum(s => s.DurationMinutes)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var timeEntryStats = BuildTimeEntryStats(entries.ToList());
        timeEntryStats.ByDay = entries
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
        var project = await unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {projectId} not found");

        var sessions = await unitOfWork.PomodoroSessions.GetByProjectIdAsync(projectId);
        var entries = await unitOfWork.TimeEntries.GetByProjectIdAsync(projectId);

        var pomodoroStats = BuildPomodoroStats(sessions.ToList());
        pomodoroStats.LastSession = sessions.Any() ? sessions.Max(s => s.StartTime) : null;

        var timeEntryStats = BuildTimeEntryStats(entries.ToList());
        timeEntryStats.LastEntry = entries.Any() ? entries.Max(e => e.StartTime) : null;

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

    private static PomodoroStatsDto BuildPomodoroStats(List<PomodoroSession> sessions)
    {
        return new PomodoroStatsDto
        {
            Total = sessions.Count,
            Completed = sessions.Count(s => s.IsCompleted),
            WorkSessions = sessions.Count(s => s.SessionType == SessionType.Work),
            TotalMinutes = sessions.Sum(s => s.DurationMinutes),
            ByProject = sessions
                .Where(s => s.Project != null)
                .GroupBy(s => new { s.Project!.Id, s.Project.Name, ClientName = s.Project.Client != null ? s.Project.Client.Name : "No Client" })
                .Select(g => new ProjectStatsDto
                {
                    ProjectId = g.Key.Id,
                    ProjectName = g.Key.Name,
                    ClientName = g.Key.ClientName,
                    Count = g.Count(),
                    Minutes = g.Sum(s => s.DurationMinutes)
                })
                .ToList()
        };
    }

    private static TimeEntryStatsDto BuildTimeEntryStats(List<TimeEntry> entries)
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
