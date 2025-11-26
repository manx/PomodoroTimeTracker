using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for ProjectService.
/// Tests CRUD operations and business rules like unique project names per client.
/// </summary>
public class ProjectServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _clientRepositoryMock = new Mock<IClientRepository>();

        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Clients).Returns(_clientRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new ProjectService(_unitOfWorkMock.Object);
    }

    #region GetAllProjectsAsync Tests

    [Fact]
    public async Task GetAllProjectsAsync_WithNoProjects_ReturnsEmptyCollection()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetAllWithClientAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        // Act
        var result = await _service.GetAllProjectsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllProjectsAsync_WithMultipleProjects_ReturnsAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new()
            {
                Id = 1,
                Name = "Project A",
                Description = "Description A",
                ClientId = 1,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Project B",
                Description = "Description B",
                ClientId = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        _projectRepositoryMock.Setup(r => r.GetAllWithClientAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _service.GetAllProjectsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Name == "Project A");
        result.Should().Contain(p => p.Name == "Project B");
    }

    [Fact]
    public async Task GetAllProjectsAsync_WithClient_MapsClientNameCorrectly()
    {
        // Arrange
        var client = new Client { Id = 1, Name = "Test Client", CreatedAt = DateTime.UtcNow };
        var project = new Project
        {
            Id = 1,
            Name = "Project with Client",
            Description = "Description",
            ClientId = 1,
            Client = client,
            CreatedAt = DateTime.UtcNow
        };

        _projectRepositoryMock.Setup(r => r.GetAllWithClientAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        // Act
        var result = await _service.GetAllProjectsAsync();

        // Assert
        var projectDto = result.First();
        projectDto.ClientName.Should().Be("Test Client");
        projectDto.ClientId.Should().Be(1);
    }

    #endregion

    #region GetProjectByIdAsync Tests

    [Fact]
    public async Task GetProjectByIdAsync_WithExistingId_ReturnsProject()
    {
        // Arrange
        var project = new Project
        {
            Id = 1,
            Name = "Test Project",
            Description = "Test Description",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _projectRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.GetProjectByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Project");
        result.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task GetProjectByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _service.GetProjectByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetProjectsByClientIdAsync Tests

    [Fact]
    public async Task GetProjectsByClientIdAsync_WithMatchingProjects_ReturnsFilteredProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new()
            {
                Id = 1,
                Name = "Project 1",
                ClientId = 5,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Project 2",
                ClientId = 5,
                CreatedAt = DateTime.UtcNow
            }
        };

        _projectRepositoryMock.Setup(r => r.GetByClientIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _service.GetProjectsByClientIdAsync(5);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.ClientId == 5);
    }

    [Fact]
    public async Task GetProjectsByClientIdAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByClientIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        // Act
        var result = await _service.GetProjectsByClientIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateProjectAsync Tests

    [Fact]
    public async Task CreateProjectAsync_WithValidDataAndClient_CreatesProject()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Name = "New Project",
            Description = "Project Description",
            ClientId = 1
        };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync(createDto.Name, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Project? capturedProject = null;
        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) =>
            {
                p.Id = 1;
                capturedProject = p;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProjectAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("New Project");
        result.Description.Should().Be("Project Description");
        result.ClientId.Should().Be(1);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        capturedProject.Should().NotBeNull();
        capturedProject!.ClientId.Should().Be(1);

        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProjectAsync_WithoutClient_CreatesProject()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Name = "Standalone Project",
            Description = "No client",
            ClientId = null
        };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync(createDto.Name, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) => p.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProjectAsync(createDto);

        // Assert
        result.ClientId.Should().BeNull();
        result.Name.Should().Be("Standalone Project");
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicateNameForClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Name = "Duplicate Project",
            Description = "Description",
            ClientId = 1
        };

        var client = new Client { Id = 1, Name = "Test Client", CreatedAt = DateTime.UtcNow };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Duplicate Project", 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateProjectAsync(createDto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A project with the name 'Duplicate Project' already exists for Test Client");

        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicateNameForNoClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Name = "Duplicate Project",
            Description = "Description",
            ClientId = null
        };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Duplicate Project", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateProjectAsync(createDto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A project with the name 'Duplicate Project' already exists for projects without a client");

        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProjectAsync_SameNameDifferentClients_AllowsBothProjects()
    {
        // Arrange - Same project name should be allowed for different clients
        var createDto1 = new CreateProjectDto { Name = "Website", ClientId = 1 };
        var createDto2 = new CreateProjectDto { Name = "Website", ClientId = 2 };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Website", 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Website", 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _projectRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) => p.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _service.CreateProjectAsync(createDto1);
        var result2 = await _service.CreateProjectAsync(createDto2);

        // Assert
        result1.Name.Should().Be("Website");
        result2.Name.Should().Be("Website");
        _projectRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region UpdateProjectAsync Tests

    [Fact]
    public async Task UpdateProjectAsync_WithValidData_UpdatesProject()
    {
        // Arrange
        var existingProject = new Project
        {
            Id = 1,
            Name = "Old Name",
            Description = "Old Description",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var updateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Updated Name",
            Description = "Updated Description",
            ClientId = 2
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Updated Name", 2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.UpdateProjectAsync(updateDto);

        // Assert
        existingProject.Name.Should().Be("Updated Name");
        existingProject.Description.Should().Be("Updated Description");
        existingProject.ClientId.Should().Be(2);

        _projectRepositoryMock.Verify(r => r.Update(existingProject), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithNonExistingProject_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateProjectDto
        {
            Id = 999,
            Name = "Non-existing",
            Description = "Description",
            ClientId = 1
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateProjectAsync(updateDto))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Project with ID 999 not found");

        _projectRepositoryMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithDuplicateNameForClient_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingProject = new Project
        {
            Id = 1,
            Name = "Project A",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Project B", // Already exists for this client
            ClientId = 1
        };

        var client = new Client { Id = 1, Name = "Test Client", CreatedAt = DateTime.UtcNow };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Project B", 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateProjectAsync(updateDto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A project with the name 'Project B' already exists for Test Client");

        _projectRepositoryMock.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithSameName_AllowsUpdate()
    {
        // Arrange - Project keeping the same name should be allowed
        var existingProject = new Project
        {
            Id = 1,
            Name = "Same Name",
            Description = "Old Description",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Same Name",
            Description = "New Description",
            ClientId = 1
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Same Name", 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.UpdateProjectAsync(updateDto);

        // Assert
        existingProject.Description.Should().Be("New Description");
        _projectRepositoryMock.Verify(r => r.Update(existingProject), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectAsync_ChangingClient_ValidatesNameForNewClient()
    {
        // Arrange
        var existingProject = new Project
        {
            Id = 1,
            Name = "Website",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateProjectDto
        {
            Id = 1,
            Name = "Website",
            ClientId = 2 // Moving to different client
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        // Check should be for client 2, not client 1
        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Website", 2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.UpdateProjectAsync(updateDto);

        // Assert
        existingProject.ClientId.Should().Be(2);
        _projectRepositoryMock.Verify(r => r.ExistsWithNameForClientAsync("Website", 2, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteProjectAsync Tests

    [Fact]
    public async Task DeleteProjectAsync_WithExistingProject_DeletesSuccessfully()
    {
        // Arrange
        var project = new Project
        {
            Id = 1,
            Name = "Project to Delete",
            Description = "Description",
            ClientId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        await _service.DeleteProjectAsync(1);

        // Assert
        _projectRepositoryMock.Verify(r => r.Delete(project), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithNonExistingProject_ThrowsKeyNotFoundException()
    {
        // Arrange
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.DeleteProjectAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Project with ID 999 not found");

        _projectRepositoryMock.Verify(r => r.Delete(It.IsAny<Project>()), Times.Never);
    }

    #endregion

    #region Business Rules Tests

    [Fact]
    public async Task CreateProjectAsync_EnforcesUniqueNamePerClient()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Name = "Duplicate",
            ClientId = 1
        };

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Duplicate", 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Client { Id = 1, Name = "Client", CreatedAt = DateTime.UtcNow });

        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateProjectAsync(createDto))
            .Should().ThrowAsync<InvalidOperationException>();

        _projectRepositoryMock.Verify(r => r.ExistsWithNameForClientAsync("Duplicate", 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectAsync_EnforcesUniqueNamePerClient_ExcludingCurrentProject()
    {
        // Arrange
        var existingProject = new Project { Id = 1, Name = "Project A", ClientId = 1, CreatedAt = DateTime.UtcNow };
        var updateDto = new UpdateProjectDto { Id = 1, Name = "Project B", ClientId = 1 };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        _projectRepositoryMock.Setup(r => r.ExistsWithNameForClientAsync("Project B", 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Client { Id = 1, Name = "Client", CreatedAt = DateTime.UtcNow });

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateProjectAsync(updateDto))
            .Should().ThrowAsync<InvalidOperationException>();

        // Verify check excluded current project (ID 1)
        _projectRepositoryMock.Verify(r => r.ExistsWithNameForClientAsync("Project B", 1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
