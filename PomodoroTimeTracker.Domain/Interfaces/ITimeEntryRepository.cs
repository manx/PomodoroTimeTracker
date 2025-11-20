using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<IEnumerable<TimeEntry>> GetAllWithProjectAsync(CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntry>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetActiveTimeEntryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
