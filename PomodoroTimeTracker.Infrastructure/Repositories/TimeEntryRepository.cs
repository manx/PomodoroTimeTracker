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
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TimeEntry>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(te => te.ProjectId == projectId)
            .Include(te => te.SessionType)
            .Include(te => te.Project)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetActiveEntryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.SessionType)
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
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TimeEntry>> GetBySessionTypeAsync(int sessionTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(te => te.SessionTypeId == sessionTypeId)
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TimeEntry>> GetByDateRangeAndTypeAsync(DateTime startDate, DateTime endDate, int? sessionTypeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(te => te.StartTime >= startDate && te.StartTime < endDate);

        if (sessionTypeId.HasValue)
        {
            query = query.Where(te => te.SessionTypeId == sessionTypeId.Value);
        }

        return await query
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(te => te.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry?> GetActiveEntryByTypeAsync(int sessionTypeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(te => te.SessionType)
            .Include(te => te.Project)
                .ThenInclude(p => p!.Client)
            .Where(te => te.SessionTypeId == sessionTypeId && te.EndTime == null)
            .OrderByDescending(te => te.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
