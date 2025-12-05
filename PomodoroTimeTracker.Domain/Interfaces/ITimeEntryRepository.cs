using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<IEnumerable<TimeEntry>> GetAllWithProjectAsync(CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdWithProjectAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntry>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetActiveEntryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // New methods for type filtering
    Task<IEnumerable<TimeEntry>> GetBySessionTypeAsync(int sessionTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeEntry>> GetByDateRangeAndTypeAsync(DateTime startDate, DateTime endDate, int? sessionTypeId = null, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetActiveEntryByTypeAsync(int sessionTypeId, CancellationToken cancellationToken = default);
}
