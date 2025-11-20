using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IPomodoroSettingsRepository : IRepository<PomodoroSettings>
{
    Task<PomodoroSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
}
