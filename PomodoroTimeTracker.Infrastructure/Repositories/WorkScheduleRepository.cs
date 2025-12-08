using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class WorkScheduleRepository(ApplicationDbContext context) : Repository<WorkSchedule>(context), IWorkScheduleRepository
{
    public async Task<WorkSchedule?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ws => ws.Client)
            .FirstOrDefaultAsync(ws => ws.ClientId == clientId, cancellationToken);
    }

    public async Task<WorkSchedule?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ws => ws.Project)
            .FirstOrDefaultAsync(ws => ws.ProjectId == projectId, cancellationToken);
    }

    public async Task<IEnumerable<WorkSchedule>> GetAllWithRelationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ws => ws.Client)
            .Include(ws => ws.Project)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ClientHasScheduleAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(ws => ws.ClientId == clientId, cancellationToken);
    }

    public async Task<bool> ProjectHasScheduleAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(ws => ws.ProjectId == projectId, cancellationToken);
    }
}
