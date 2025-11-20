using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _unitOfWork.Projects.GetAllWithClientAsync();
        return projects.Select(MapToDto);
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdWithDetailsAsync(id);
        return project == null ? null : MapToDto(project);
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsByClientIdAsync(int clientId)
    {
        var projects = await _unitOfWork.Projects.GetByClientIdAsync(clientId);
        return projects.Select(MapToDto);
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        // Business rule: Project name must be unique within a client
        if (await _unitOfWork.Projects.ExistsWithNameForClientAsync(dto.Name, dto.ClientId))
        {
            var clientName = dto.ClientId.HasValue
                ? (await _unitOfWork.Clients.GetByIdAsync(dto.ClientId.Value))?.Name ?? "this client"
                : "projects without a client";
            throw new InvalidOperationException($"A project with the name '{dto.Name}' already exists for {clientName}");
        }

        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            ClientId = dto.ClientId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(project);
    }

    public async Task UpdateProjectAsync(UpdateProjectDto dto)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(dto.Id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {dto.Id} not found");

        // Business rule: Project name must be unique within a client (exclude current project)
        if (await _unitOfWork.Projects.ExistsWithNameForClientAsync(dto.Name, dto.ClientId, dto.Id))
        {
            var clientName = dto.ClientId.HasValue
                ? (await _unitOfWork.Clients.GetByIdAsync(dto.ClientId.Value))?.Name ?? "this client"
                : "projects without a client";
            throw new InvalidOperationException($"A project with the name '{dto.Name}' already exists for {clientName}");
        }

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.ClientId = dto.ClientId;

        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            throw new KeyNotFoundException($"Project with ID {id} not found");

        _unitOfWork.Projects.Delete(project);
        await _unitOfWork.SaveChangesAsync();
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            ClientId = project.ClientId,
            ClientName = project.Client?.Name,
            CreatedAt = project.CreatedAt
        };
    }
}
