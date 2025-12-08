using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IWorkScheduleRepository : IRepository<WorkSchedule>
{
    Task<WorkSchedule?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<WorkSchedule?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkSchedule>> GetAllWithRelationsAsync(CancellationToken cancellationToken = default);
    Task<bool> ClientHasScheduleAsync(int clientId, CancellationToken cancellationToken = default);
    Task<bool> ProjectHasScheduleAsync(int projectId, CancellationToken cancellationToken = default);
}
