using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IPomodoroSessionService
{
    Task<IEnumerable<PomodoroSessionDto>> GetAllSessionsAsync();
    Task<PomodoroSessionDto?> GetSessionByIdAsync(int id);
    Task<IEnumerable<PomodoroSessionDto>> GetSessionsByProjectIdAsync(int projectId);
    Task<PomodoroSessionDto?> GetActiveSessionAsync();
    Task<PomodoroSessionDto> CreateSessionAsync(CreatePomodoroSessionDto dto);
    Task UpdateSessionAsync(UpdatePomodoroSessionDto dto);
    Task CompleteSessionAsync(int id);
    Task DeleteSessionAsync(int id);
}
