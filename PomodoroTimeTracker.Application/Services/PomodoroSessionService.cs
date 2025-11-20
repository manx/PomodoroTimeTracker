using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

/// <summary>
/// Service for managing Pomodoro work sessions and break periods.
/// Implements business logic for session CRUD operations and tracking.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PomodoroSessionService"/> class.
/// </remarks>
/// <param name="unitOfWork">Unit of work for database operations.</param>
public class PomodoroSessionService(IUnitOfWork unitOfWork) : IPomodoroSessionService
{

    /// <summary>
    /// Retrieves all Pomodoro sessions with associated project and client information.
    /// </summary>
    /// <returns>Collection of all sessions.</returns>
    public async Task<IEnumerable<PomodoroSessionDto>> GetAllSessionsAsync()
    {
        var sessions = await unitOfWork.PomodoroSessions.GetAllWithProjectAsync();
        return sessions.Select(MapToDto);
    }

    /// <summary>
    /// Retrieves a specific Pomodoro session by ID.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <returns>The session if found; otherwise null.</returns>
    public async Task<PomodoroSessionDto?> GetSessionByIdAsync(int id)
    {
        var session = await unitOfWork.PomodoroSessions.GetByIdWithProjectAsync(id);
        return session == null ? null : MapToDto(session);
    }

    public async Task<IEnumerable<PomodoroSessionDto>> GetSessionsByProjectIdAsync(int projectId)
    {
        var sessions = await unitOfWork.PomodoroSessions.GetByProjectIdAsync(projectId);
        return sessions.Select(MapToDto);
    }

    public async Task<PomodoroSessionDto?> GetActiveSessionAsync()
    {
        var session = await unitOfWork.PomodoroSessions.GetActiveSessionAsync();
        return session == null ? null : MapToDto(session);
    }

    /// <summary>
    /// Creates a new Pomodoro session.
    /// </summary>
    /// <param name="dto">The data for creating the session.</param>
    /// <returns>The created session.</returns>
    public async Task<PomodoroSessionDto> CreateSessionAsync(CreatePomodoroSessionDto dto)
    {
        var session = new PomodoroSession
        {
            ProjectId = dto.ProjectId,
            StartTime = DateTime.UtcNow,
            DurationMinutes = dto.DurationMinutes,
            SessionType = dto.SessionType,
            Objective = dto.Objective,
            Notes = dto.Notes,
            IsCompleted = false
        };

        await unitOfWork.PomodoroSessions.AddAsync(session);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(session);
    }

    public async Task UpdateSessionAsync(UpdatePomodoroSessionDto dto)
    {
        var session = await unitOfWork.PomodoroSessions.GetByIdAsync(dto.Id);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {dto.Id} not found");

        session.ProjectId = dto.ProjectId;
        session.StartTime = dto.StartTime;
        session.EndTime = dto.EndTime;
        session.DurationMinutes = dto.DurationMinutes;
        session.IsCompleted = dto.IsCompleted;
        session.SessionType = dto.SessionType;
        session.Objective = dto.Objective;
        session.Notes = dto.Notes;

        unitOfWork.PomodoroSessions.Update(session);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Marks a session as completed with the current timestamp.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <exception cref="KeyNotFoundException">Thrown when session is not found.</exception>
    public async Task CompleteSessionAsync(int id)
    {
        var session = await unitOfWork.PomodoroSessions.GetByIdAsync(id);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {id} not found");

        session.EndTime = DateTime.UtcNow;
        session.IsCompleted = true;

        unitOfWork.PomodoroSessions.Update(session);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync(int id)
    {
        var session = await unitOfWork.PomodoroSessions.GetByIdAsync(id);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {id} not found");

        unitOfWork.PomodoroSessions.Delete(session);
        await unitOfWork.SaveChangesAsync();
    }

    private static PomodoroSessionDto MapToDto(PomodoroSession session)
    {
        return new PomodoroSessionDto
        {
            Id = session.Id,
            ProjectId = session.ProjectId,
            ProjectName = session.Project?.Name,
            ClientName = session.Project?.Client?.Name,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            DurationMinutes = session.DurationMinutes,
            IsCompleted = session.IsCompleted,
            SessionType = session.SessionType,
            Objective = session.Objective,
            Notes = session.Notes
        };
    }
}
