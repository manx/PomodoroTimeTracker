using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetAllWithClientAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameForClientAsync(string name, int? clientId, int? excludeProjectId = null, CancellationToken cancellationToken = default);
}
