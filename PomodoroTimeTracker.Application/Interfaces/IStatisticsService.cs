using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IStatisticsService
{
    Task<DailyStatisticsDto> GetDailyStatisticsAsync(DateTime? date = null);
    Task<WeeklyStatisticsDto> GetWeeklyStatisticsAsync(DateTime? date = null);
    Task<DateRangeStatisticsDto> GetDateRangeStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<ProjectStatisticsDto> GetProjectStatisticsAsync(int projectId);
}
