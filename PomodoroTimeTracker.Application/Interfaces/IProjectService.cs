using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
    Task<ProjectDto?> GetProjectByIdAsync(int id);
    Task<IEnumerable<ProjectDto>> GetProjectsByClientIdAsync(int clientId);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);
    Task UpdateProjectAsync(UpdateProjectDto dto);
    Task DeleteProjectAsync(int id);
}
