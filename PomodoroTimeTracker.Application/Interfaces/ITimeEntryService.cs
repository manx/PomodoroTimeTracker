using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface ITimeEntryService
{
    // Generic CRUD
    Task<IEnumerable<TimeEntryDto>> GetAllEntriesAsync();
    Task<TimeEntryDto?> GetEntryByIdAsync(int id);
    Task<IEnumerable<TimeEntryDto>> GetEntriesByProjectIdAsync(int projectId);
    Task<IEnumerable<TimeEntryDto>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<TimeEntryDto> CreateEntryAsync(CreateTimeEntryDto dto, DateTime? startTime = null, DateTime? endTime = null);
    Task UpdateEntryAsync(UpdateTimeEntryDto dto);
    Task DeleteEntryAsync(int id);

    // Timer-specific operations (previously in IPomodoroSessionService)
    Task<TimeEntryDto?> GetActiveEntryAsync();
    Task<TimeEntryDto?> GetActiveEntryByTypeAsync(int sessionTypeId);
    Task<TimeEntryDto> StartTimerEntryAsync(CreateTimeEntryDto dto);
    Task StopEntryAsync(int id);
    Task CompleteEntryAsync(int id, string? notes = null);

    // Type-specific queries
    Task<IEnumerable<TimeEntryDto>> GetEntriesBySessionTypeAsync(int sessionTypeId);
    Task<IEnumerable<TimeEntryDto>> GetEntriesByDateRangeAndTypeAsync(DateTime startDate, DateTime endDate, int? sessionTypeId = null);
}
