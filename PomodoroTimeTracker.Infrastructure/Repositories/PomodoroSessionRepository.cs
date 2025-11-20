using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class PomodoroSessionRepository(ApplicationDbContext context) : Repository<PomodoroSession>(context), IPomodoroSessionRepository
{
    public async Task<IEnumerable<PomodoroSession>> GetAllWithProjectAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ps => ps.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(ps => ps.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PomodoroSession?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ps => ps.Project)
                .ThenInclude(p => p!.Client)
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ps => ps.ProjectId == projectId)
            .Include(ps => ps.Project)
            .OrderByDescending(ps => ps.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PomodoroSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ps => ps.Project)
                .ThenInclude(p => p!.Client)
            .Where(ps => !ps.IsCompleted && ps.EndTime == null)
            .OrderByDescending(ps => ps.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ps => ps.StartTime >= startDate && ps.StartTime < endDate)
            .Include(ps => ps.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(ps => ps.StartTime)
            .ToListAsync(cancellationToken);
    }
}
