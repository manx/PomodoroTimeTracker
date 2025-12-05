using Microsoft.EntityFrameworkCore.Storage;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;
    private IDbContextTransaction? _transaction;

    public IClientRepository Clients { get; } = new ClientRepository(context);
    public IProjectRepository Projects { get; } = new ProjectRepository(context);
    public ITimeEntryRepository TimeEntries { get; } = new TimeEntryRepository(context);
    public IPomodoroSettingsRepository PomodoroSettings { get; } = new PomodoroSettingsRepository(context);
    public IAppSettingsRepository AppSettings { get; } = new AppSettingsRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        // Only dispose resources we created (the transaction)
        // DO NOT dispose _context - it's owned by the DI container
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}
