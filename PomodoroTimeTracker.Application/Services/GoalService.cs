using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class GoalService(
    IUnitOfWork unitOfWork,
    IWorkScheduleService workScheduleService,
    IAppSettingsService appSettingsService) : IGoalService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IWorkScheduleService _workScheduleService = workScheduleService;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;

    public async Task<GoalComparisonDto?> GetDailyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        return await GetComparisonAsync(clientId, projectId, targetDate, targetDate, "Daily", cancellationToken);
    }

    public async Task<GoalComparisonDto?> GetWeeklyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateTime.Today;
        var weekStart = _appSettingsService.GetWeekStart(targetDate);
        var weekEnd = _appSettingsService.GetWeekEnd(targetDate);

        return await GetComparisonAsync(clientId, projectId, weekStart, weekEnd, "Weekly", cancellationToken);
    }

    public async Task<GoalComparisonDto?> GetMonthlyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateTime.Today;
        var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return await GetComparisonAsync(clientId, projectId, monthStart, monthEnd, "Monthly", cancellationToken);
    }

    public async Task<GoalComparisonDto?> GetCustomRangeComparisonAsync(
        int? clientId, int? projectId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await GetComparisonAsync(clientId, projectId, startDate.Date, endDate.Date, "Custom", cancellationToken);
    }

    private async Task<GoalComparisonDto?> GetComparisonAsync(
        int? clientId, int? projectId, DateTime startDate, DateTime endDate, string period,
        CancellationToken cancellationToken)
    {
        // Get the work schedule
        var schedule = await GetScheduleAsync(clientId, projectId, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        // Calculate expected hours
        var expectedHours = await _workScheduleService.CalculateExpectedHoursAsync(
            schedule.Id, startDate, endDate, cancellationToken);

        // Calculate actual hours from time entries
        var actualHours = await GetActualHoursAsync(clientId, projectId, startDate, endDate, cancellationToken);

        return new GoalComparisonDto
        {
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            ExpectedHours = Math.Round(expectedHours, 2),
            ActualHours = Math.Round(actualHours, 2)
        };
    }

    private async Task<WorkScheduleDto?> GetScheduleAsync(
        int? clientId, int? projectId, CancellationToken cancellationToken)
    {
        // Project schedule takes precedence if both are specified
        if (projectId.HasValue)
        {
            var projectSchedule = await _workScheduleService.GetByProjectIdAsync(projectId.Value, cancellationToken);
            if (projectSchedule is not null)
            {
                return projectSchedule;
            }

            // Fall back to client schedule if project doesn't have one
            if (clientId.HasValue)
            {
                return await _workScheduleService.GetByClientIdAsync(clientId.Value, cancellationToken);
            }

            // Get the project's client and check for client schedule
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId.Value, cancellationToken);
            if (project?.ClientId is not null)
            {
                return await _workScheduleService.GetByClientIdAsync(project.ClientId.Value, cancellationToken);
            }

            return null;
        }

        if (clientId.HasValue)
        {
            return await _workScheduleService.GetByClientIdAsync(clientId.Value, cancellationToken);
        }

        return null;
    }

    private async Task<decimal> GetActualHoursAsync(
        int? clientId, int? projectId, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken)
    {
        var entries = await _unitOfWork.TimeEntries.GetByDateRangeAsync(startDate, endDate, cancellationToken);

        // Filter by project or client if specified
        if (projectId.HasValue)
        {
            entries = entries.Where(e => e.ProjectId == projectId.Value);
        }
        else if (clientId.HasValue)
        {
            // Get all projects for the client
            var clientProjects = await _unitOfWork.Projects.FindAsync(p => p.ClientId == clientId.Value, cancellationToken);
            var projectIds = clientProjects.Select(p => p.Id).ToHashSet();

            entries = entries.Where(e => e.ProjectId.HasValue && projectIds.Contains(e.ProjectId.Value));
        }

        // Sum up total duration in hours (DurationMinutes is nullable)
        return entries.Sum(e => (e.DurationMinutes ?? 0) / 60.0m);
    }
}
