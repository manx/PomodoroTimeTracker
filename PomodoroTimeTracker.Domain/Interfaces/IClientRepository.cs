using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByIdWithProjectsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Client>> GetAllWithProjectsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameAsync(string name, int? excludeClientId = null, CancellationToken cancellationToken = default);
}
