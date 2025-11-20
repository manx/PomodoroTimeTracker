using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class TimeEntryService(IUnitOfWork unitOfWork) : ITimeEntryService
{
    public async Task<IEnumerable<TimeEntryDto>> GetAllTimeEntriesAsync()
    {
        var entries = await unitOfWork.TimeEntries.GetAllWithProjectAsync();
        return entries.Select(MapToDto);
    }

    public async Task<TimeEntryDto?> GetTimeEntryByIdAsync(int id)
    {
        var entry = await unitOfWork.TimeEntries.GetByIdWithProjectAsync(id);
        return entry == null ? null : MapToDto(entry);
    }

    public async Task<IEnumerable<TimeEntryDto>> GetTimeEntriesByProjectIdAsync(int projectId)
    {
        var entries = await unitOfWork.TimeEntries.GetByProjectIdAsync(projectId);
        return entries.Select(MapToDto);
    }

    public async Task<TimeEntryDto?> GetActiveTimeEntryAsync()
    {
        var entry = await unitOfWork.TimeEntries.GetActiveTimeEntryAsync();
        return entry == null ? null : MapToDto(entry);
    }

    public async Task<TimeEntryDto> StartTimeEntryAsync(CreateTimeEntryDto dto)
    {
        var entry = new TimeEntry
        {
            ProjectId = dto.ProjectId,
            Description = dto.Description,
            StartTime = DateTime.UtcNow,
            EndTime = null,
            DurationMinutes = null,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.TimeEntries.AddAsync(entry);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(entry);
    }

    public async Task StopTimeEntryAsync(int id)
    {
        var entry = await unitOfWork.TimeEntries.GetByIdAsync(id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with ID {id} not found");

        if (entry.EndTime != null)
            throw new InvalidOperationException("Time entry already stopped");

        entry.EndTime = DateTime.UtcNow;
        entry.DurationMinutes = (int)(entry.EndTime.Value - entry.StartTime).TotalMinutes;

        unitOfWork.TimeEntries.Update(entry);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<TimeEntryDto> CreateTimeEntryAsync(CreateTimeEntryDto dto, DateTime? startTime = null, DateTime? endTime = null)
    {
        var entry = new TimeEntry
        {
            ProjectId = dto.ProjectId,
            Description = dto.Description,
            StartTime = startTime ?? DateTime.UtcNow,
            EndTime = endTime,
            CreatedAt = DateTime.UtcNow
        };

        if (entry.EndTime != null)
        {
            entry.DurationMinutes = (int)(entry.EndTime.Value - entry.StartTime).TotalMinutes;
        }

        await unitOfWork.TimeEntries.AddAsync(entry);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(entry);
    }

    public async Task UpdateTimeEntryAsync(UpdateTimeEntryDto dto)
    {
        var entry = await unitOfWork.TimeEntries.GetByIdAsync(dto.Id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with ID {dto.Id} not found");

        entry.ProjectId = dto.ProjectId;
        entry.Description = dto.Description;
        entry.StartTime = dto.StartTime;
        entry.EndTime = dto.EndTime;

        if (entry.EndTime != null)
        {
            entry.DurationMinutes = (int)(entry.EndTime.Value - entry.StartTime).TotalMinutes;
        }
        else
        {
            entry.DurationMinutes = dto.DurationMinutes;
        }

        unitOfWork.TimeEntries.Update(entry);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteTimeEntryAsync(int id)
    {
        var entry = await unitOfWork.TimeEntries.GetByIdAsync(id);
        if (entry == null)
            throw new KeyNotFoundException($"Time entry with ID {id} not found");

        unitOfWork.TimeEntries.Delete(entry);
        await unitOfWork.SaveChangesAsync();
    }

    private static TimeEntryDto MapToDto(TimeEntry entry)
    {
        return new TimeEntryDto
        {
            Id = entry.Id,
            ProjectId = entry.ProjectId,
            ProjectName = entry.Project?.Name,
            ClientName = entry.Project?.Client?.Name,
            Description = entry.Description,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            DurationMinutes = entry.DurationMinutes,
            CreatedAt = entry.CreatedAt
        };
    }
}
