using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface ITimeEntryService
{
    Task<IEnumerable<TimeEntryDto>> GetAllTimeEntriesAsync();
    Task<TimeEntryDto?> GetTimeEntryByIdAsync(int id);
    Task<IEnumerable<TimeEntryDto>> GetTimeEntriesByProjectIdAsync(int projectId);
    Task<TimeEntryDto?> GetActiveTimeEntryAsync();
    Task<TimeEntryDto> StartTimeEntryAsync(CreateTimeEntryDto dto);
    Task StopTimeEntryAsync(int id);
    Task<TimeEntryDto> CreateTimeEntryAsync(CreateTimeEntryDto dto, DateTime? startTime = null, DateTime? endTime = null);
    Task UpdateTimeEntryAsync(UpdateTimeEntryDto dto);
    Task DeleteTimeEntryAsync(int id);
}
