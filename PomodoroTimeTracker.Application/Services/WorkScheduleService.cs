using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class WorkScheduleService(
    IUnitOfWork unitOfWork,
    IPublicHolidayService publicHolidayService) : IWorkScheduleService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPublicHolidayService _publicHolidayService = publicHolidayService;

    public async Task<WorkScheduleDto?> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByClientIdAsync(clientId, cancellationToken);
        return schedule is null ? null : MapToDto(schedule);
    }

    public async Task<WorkScheduleDto?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByProjectIdAsync(projectId, cancellationToken);
        return schedule is null ? null : MapToDto(schedule);
    }

    public async Task<IEnumerable<WorkScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var schedules = await _unitOfWork.WorkSchedules.GetAllWithRelationsAsync(cancellationToken);
        return schedules.Select(MapToDto);
    }

    public async Task<WorkScheduleDto> CreateAsync(CreateWorkScheduleDto dto, CancellationToken cancellationToken = default)
    {
        ValidateXorConstraint(dto.ClientId, dto.ProjectId);

        // Check for existing schedule
        if (dto.ClientId.HasValue)
        {
            var exists = await _unitOfWork.WorkSchedules.ClientHasScheduleAsync(dto.ClientId.Value, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Client {dto.ClientId} already has a work schedule.");
            }
        }
        else if (dto.ProjectId.HasValue)
        {
            var exists = await _unitOfWork.WorkSchedules.ProjectHasScheduleAsync(dto.ProjectId.Value, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Project {dto.ProjectId} already has a work schedule.");
            }
        }

        var schedule = new WorkSchedule
        {
            ClientId = dto.ClientId,
            ProjectId = dto.ProjectId,
            BaseHoursPerDay = dto.BaseHoursPerDay,
            WorkDays = (int)dto.WorkDays,
            IncludePublicHolidays = dto.IncludePublicHolidays,
            CountryCode = dto.CountryCode,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        await _unitOfWork.WorkSchedules.AddAsync(schedule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with relations
        if (dto.ClientId.HasValue)
        {
            schedule = await _unitOfWork.WorkSchedules.GetByClientIdAsync(dto.ClientId.Value, cancellationToken);
        }
        else
        {
            schedule = await _unitOfWork.WorkSchedules.GetByProjectIdAsync(dto.ProjectId!.Value, cancellationToken);
        }

        return MapToDto(schedule!);
    }

    public async Task<WorkScheduleDto> UpdateAsync(UpdateWorkScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Work schedule {dto.Id} not found.");

        schedule.BaseHoursPerDay = dto.BaseHoursPerDay;
        schedule.WorkDays = (int)dto.WorkDays;
        schedule.IncludePublicHolidays = dto.IncludePublicHolidays;
        schedule.CountryCode = dto.CountryCode;
        schedule.LastModified = DateTime.UtcNow;

        _unitOfWork.WorkSchedules.Update(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with relations
        if (schedule.ClientId.HasValue)
        {
            schedule = await _unitOfWork.WorkSchedules.GetByClientIdAsync(schedule.ClientId.Value, cancellationToken);
        }
        else
        {
            schedule = await _unitOfWork.WorkSchedules.GetByProjectIdAsync(schedule.ProjectId!.Value, cancellationToken);
        }

        return MapToDto(schedule!);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Work schedule {id} not found.");

        _unitOfWork.WorkSchedules.Delete(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> CalculateExpectedHoursAsync(
        int scheduleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Work schedule {scheduleId} not found.");

        var workingDays = await GetWorkingDaysCountInternalAsync(schedule, startDate, endDate, cancellationToken);

        return schedule.BaseHoursPerDay * workingDays;
    }

    public async Task<int> GetWorkingDaysCountAsync(
        int scheduleId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var schedule = await _unitOfWork.WorkSchedules.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new InvalidOperationException($"Work schedule {scheduleId} not found.");

        return await GetWorkingDaysCountInternalAsync(schedule, startDate, endDate, cancellationToken);
    }

    private async Task<int> GetWorkingDaysCountInternalAsync(
        WorkSchedule schedule, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        int workingDays = 0;
        var holidays = new HashSet<DateTime>();

        // Get holidays if needed
        if (!schedule.IncludePublicHolidays && !string.IsNullOrEmpty(schedule.CountryCode))
        {
            var holidayList = await _publicHolidayService.GetHolidaysForDateRangeAsync(
                schedule.CountryCode, startDate, endDate, cancellationToken);
            holidays = holidayList.Select(h => h.Date.Date).ToHashSet();
        }

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            // Check if this day is a work day according to the schedule
            if (!schedule.IsWorkDay(date.DayOfWeek))
            {
                continue;
            }

            // Check if this is a holiday (and holidays should be excluded)
            if (!schedule.IncludePublicHolidays && holidays.Contains(date))
            {
                continue;
            }

            workingDays++;
        }

        return workingDays;
    }

    public async Task<ScheduleConflictResult> CheckClientScheduleConflictsAsync(
        int clientId, CancellationToken cancellationToken = default)
    {
        // Get all projects for this client
        var projects = await _unitOfWork.Projects.GetByClientIdAsync(clientId, cancellationToken);
        var projectsWithSchedules = new List<string>();

        foreach (var project in projects)
        {
            var hasSchedule = await _unitOfWork.WorkSchedules.ProjectHasScheduleAsync(project.Id, cancellationToken);
            if (hasSchedule)
            {
                projectsWithSchedules.Add(project.Name);
            }
        }

        if (projectsWithSchedules.Count == 0)
        {
            return ScheduleConflictResult.NoConflict;
        }

        return new ScheduleConflictResult
        {
            HasConflicts = true,
            ConflictType = ScheduleConflictType.ProjectSchedulesExist,
            ConflictingProjectNames = projectsWithSchedules,
            ConflictCount = projectsWithSchedules.Count
        };
    }

    public async Task<ScheduleConflictResult> CheckProjectScheduleConflictsAsync(
        int projectId, CancellationToken cancellationToken = default)
    {
        // Get the project to find its client
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
        if (project?.ClientId is null)
        {
            return ScheduleConflictResult.NoConflict;
        }

        // Check if the client has a schedule
        var clientSchedule = await _unitOfWork.WorkSchedules.GetByClientIdAsync(project.ClientId.Value, cancellationToken);
        if (clientSchedule is null)
        {
            return ScheduleConflictResult.NoConflict;
        }

        // Get client name
        var client = await _unitOfWork.Clients.GetByIdAsync(project.ClientId.Value, cancellationToken);

        return new ScheduleConflictResult
        {
            HasConflicts = true,
            ConflictType = ScheduleConflictType.ClientScheduleExists,
            ConflictingClientName = client?.Name ?? "Unknown",
            ConflictCount = 1
        };
    }

    public async Task DeleteProjectSchedulesForClientAsync(int clientId, CancellationToken cancellationToken = default)
    {
        // Get all projects for this client
        var projects = await _unitOfWork.Projects.GetByClientIdAsync(clientId, cancellationToken);

        foreach (var project in projects)
        {
            var schedule = await _unitOfWork.WorkSchedules.GetByProjectIdAsync(project.Id, cancellationToken);
            if (schedule is not null)
            {
                _unitOfWork.WorkSchedules.Delete(schedule);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateXorConstraint(int? clientId, int? projectId)
    {
        var hasClient = clientId.HasValue;
        var hasProject = projectId.HasValue;

        if (hasClient == hasProject)
        {
            throw new InvalidOperationException(
                "Work schedule must be associated with either a Client or a Project, but not both.");
        }
    }

    private static WorkScheduleDto MapToDto(WorkSchedule entity)
    {
        return new WorkScheduleDto
        {
            Id = entity.Id,
            ClientId = entity.ClientId,
            ClientName = entity.Client?.Name,
            ProjectId = entity.ProjectId,
            ProjectName = entity.Project?.Name,
            BaseHoursPerDay = entity.BaseHoursPerDay,
            WorkDays = (WorkDaysFlags)entity.WorkDays,
            IncludePublicHolidays = entity.IncludePublicHolidays,
            CountryCode = entity.CountryCode,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified
        };
    }
}
