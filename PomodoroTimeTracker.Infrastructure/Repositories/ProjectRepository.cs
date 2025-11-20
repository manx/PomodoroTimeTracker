using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class ProjectRepository(ApplicationDbContext context) : Repository<Project>(context), IProjectRepository
{
    public async Task<Project?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Client)
            .Include(p => p.PomodoroSessions)
            .Include(p => p.TimeEntries)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetAllWithClientAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Client)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ClientId == clientId)
            .Include(p => p.Client)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameForClientAsync(string name, int? clientId, int? excludeProjectId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p =>
            p.Name.ToLower() == name.ToLower() &&
            p.ClientId == clientId);

        if (excludeProjectId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProjectId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
