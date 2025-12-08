using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IWorkScheduleService
{
    Task<WorkScheduleDto?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<WorkScheduleDto?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WorkScheduleDto> CreateAsync(CreateWorkScheduleDto dto, CancellationToken cancellationToken = default);
    Task<WorkScheduleDto> UpdateAsync(UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<decimal> CalculateExpectedHoursAsync(
        int scheduleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<int> GetWorkingDaysCountAsync(
        int scheduleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for conflicting project-level schedules when enabling client-level schedule.
    /// </summary>
    Task<ScheduleConflictResult> CheckClientScheduleConflictsAsync(
        int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for conflicting client-level schedule when enabling project-level schedule.
    /// </summary>
    Task<ScheduleConflictResult> CheckProjectScheduleConflictsAsync(
        int projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all project-level schedules for a client's projects.
    /// </summary>
    Task DeleteProjectSchedulesForClientAsync(int clientId, CancellationToken cancellationToken = default);
}
