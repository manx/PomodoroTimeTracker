using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class TimeEntryService : ITimeEntryService
{
    private readonly IUnitOfWork _unitOfWork;

    public TimeEntryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Map entity to DTO (include SessionTypeName from navigation property)
    private static TimeEntryDto MapToDto(TimeEntry entry) => new()
    {
        Id = entry.Id,
        ProjectId = entry.ProjectId,
        ProjectName = entry.Project?.Name,
        ClientName = entry.Project?.Client?.Name,
        SessionTypeId = entry.SessionTypeId,
        SessionTypeName = entry.SessionType?.Name ?? string.Empty,
        Description = entry.Description,
        StartTime = entry.StartTime,
        EndTime = entry.EndTime,
        DurationMinutes = entry.DurationMinutes,
        IsCompleted = entry.IsCompleted,
        Notes = entry.Notes,
        CreatedAt = entry.CreatedAt
    };

    public async Task<IEnumerable<TimeEntryDto>> GetAllEntriesAsync()
    {
        var entries = await _unitOfWork.TimeEntries.GetAllWithProjectAsync();
        return entries.Select(MapToDto);
    }

    public async Task<TimeEntryDto?> GetEntryByIdAsync(int id)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdWithProjectAsync(id);
        return entry == null ? null : MapToDto(entry);
    }

    public async Task<IEnumerable<TimeEntryDto>> GetEntriesByProjectIdAsync(int projectId)
    {
        var entries = await _unitOfWork.TimeEntries.GetByProjectIdAsync(projectId);
        return entries.Select(MapToDto);
    }

    public async Task<IEnumerable<TimeEntryDto>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var entries = await _unitOfWork.TimeEntries.GetByDateRangeAsync(startDate, endDate);
        return entries.Select(MapToDto);
    }

    public async Task<TimeEntryDto?> GetActiveEntryAsync()
    {
        var entry = await _unitOfWork.TimeEntries.GetActiveEntryAsync();
        return entry == null ? null : MapToDto(entry);
    }

    public async Task<TimeEntryDto?> GetActiveEntryByTypeAsync(int sessionTypeId)
    {
        var entry = await _unitOfWork.TimeEntries.GetActiveEntryByTypeAsync(sessionTypeId);
        return entry == null ? null : MapToDto(entry);
    }

    public async Task<TimeEntryDto> CreateEntryAsync(CreateTimeEntryDto dto, DateTime? startTime = null, DateTime? endTime = null)
    {
        var entry = new TimeEntry
        {
            ProjectId = dto.ProjectId,
            SessionTypeId = dto.SessionTypeId,
            Description = dto.Description,
            StartTime = startTime ?? DateTime.UtcNow,
            EndTime = endTime,
            DurationMinutes = endTime.HasValue && startTime.HasValue
                ? (int)(endTime.Value - startTime.Value).TotalMinutes
                : dto.PlannedDurationMinutes,
            IsCompleted = dto.SessionTypeId == SessionType.Ids.Manual ? null : false,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TimeEntries.AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigation properties, or return the created entry if reload fails
        var saved = await _unitOfWork.TimeEntries.GetByIdWithProjectAsync(entry.Id);
        return MapToDto(saved ?? entry);
    }

    public async Task<TimeEntryDto> StartTimerEntryAsync(CreateTimeEntryDto dto)
    {
        var entry = new TimeEntry
        {
            ProjectId = dto.ProjectId,
            SessionTypeId = dto.SessionTypeId,
            Description = dto.Description,
            StartTime = DateTime.UtcNow,
            EndTime = null,
            DurationMinutes = dto.PlannedDurationMinutes,
            IsCompleted = false,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TimeEntries.AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigation properties, or return the created entry if reload fails
        var saved = await _unitOfWork.TimeEntries.GetByIdWithProjectAsync(entry.Id);
        return MapToDto(saved ?? entry);
    }

    public async Task StopEntryAsync(int id)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with id {id} not found.");

        if (entry.EndTime.HasValue)
            throw new InvalidOperationException("Time entry is already stopped.");

        entry.EndTime = DateTime.UtcNow;
        entry.DurationMinutes = (int)(entry.EndTime.Value - entry.StartTime).TotalMinutes;

        _unitOfWork.TimeEntries.Update(entry);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CompleteEntryAsync(int id, string? notes = null)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with id {id} not found.");

        entry.EndTime = DateTime.UtcNow;
        entry.DurationMinutes = (int)(entry.EndTime.Value - entry.StartTime).TotalMinutes;
        entry.IsCompleted = true;
        if (notes != null)
            entry.Notes = notes;

        _unitOfWork.TimeEntries.Update(entry);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateEntryAsync(UpdateTimeEntryDto dto)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(dto.Id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with id {dto.Id} not found.");

        entry.ProjectId = dto.ProjectId;
        entry.SessionTypeId = dto.SessionTypeId;
        entry.Description = dto.Description;
        entry.StartTime = dto.StartTime;
        entry.EndTime = dto.EndTime;
        entry.DurationMinutes = dto.DurationMinutes ?? (dto.EndTime.HasValue
            ? (int)(dto.EndTime.Value - dto.StartTime).TotalMinutes
            : null);
        entry.IsCompleted = dto.IsCompleted;
        entry.Notes = dto.Notes;

        _unitOfWork.TimeEntries.Update(entry);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        var entry = await _unitOfWork.TimeEntries.GetByIdAsync(id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with id {id} not found.");

        _unitOfWork.TimeEntries.Delete(entry);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<TimeEntryDto>> GetEntriesBySessionTypeAsync(int sessionTypeId)
    {
        var entries = await _unitOfWork.TimeEntries.GetBySessionTypeAsync(sessionTypeId);
        return entries.Select(MapToDto);
    }

    public async Task<IEnumerable<TimeEntryDto>> GetEntriesByDateRangeAndTypeAsync(DateTime startDate, DateTime endDate, int? sessionTypeId = null)
    {
        var entries = await _unitOfWork.TimeEntries.GetByDateRangeAndTypeAsync(startDate, endDate, sessionTypeId);
        return entries.Select(MapToDto);
    }
}
