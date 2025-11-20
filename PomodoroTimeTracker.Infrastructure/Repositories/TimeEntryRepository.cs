using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class TimeEntryRepository(ApplicationDbContext context) : Repository<TimeEntry>(context), ITimeEntryRepository
{
    public async Task<IEnumerable<TimeEntry>> GetAllWithProjectAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TimeEntry>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(te => te.ProjectId == projectId)
            .Include(te => te.Project)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetActiveTimeEntryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .Where(te => te.EndTime == null)
            .OrderByDescending(te => te.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(te => te.StartTime >= startDate && te.StartTime < endDate)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }
}
