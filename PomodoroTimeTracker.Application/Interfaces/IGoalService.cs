using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IGoalService
{
    Task<GoalComparisonDto?> GetDailyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default);

    Task<GoalComparisonDto?> GetWeeklyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default);

    Task<GoalComparisonDto?> GetMonthlyComparisonAsync(
        int? clientId, int? projectId, DateTime? date = null, CancellationToken cancellationToken = default);

    Task<GoalComparisonDto?> GetCustomRangeComparisonAsync(
        int? clientId, int? projectId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
