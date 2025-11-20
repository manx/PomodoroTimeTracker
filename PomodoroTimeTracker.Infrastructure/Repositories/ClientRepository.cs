using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class ClientRepository(ApplicationDbContext context) : Repository<Client>(context), IClientRepository
{
    public async Task<Client?> GetByIdWithProjectsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Client>> GetAllWithProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Projects)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameAsync(string name, int? excludeClientId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.Name.ToLower() == name.ToLower());

        if (excludeClientId.HasValue)
        {
            query = query.Where(c => c.Id != excludeClientId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
