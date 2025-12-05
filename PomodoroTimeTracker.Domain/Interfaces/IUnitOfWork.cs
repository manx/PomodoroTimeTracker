namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IClientRepository Clients { get; }
    IProjectRepository Projects { get; }
    ITimeEntryRepository TimeEntries { get; }
    IPomodoroSettingsRepository PomodoroSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
