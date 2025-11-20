using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IPomodoroSessionRepository : IRepository<PomodoroSession>
{
    Task<IEnumerable<PomodoroSession>> GetAllWithProjectAsync(CancellationToken cancellationToken = default);
    Task<PomodoroSession?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PomodoroSession>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<PomodoroSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PomodoroSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
