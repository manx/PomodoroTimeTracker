using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application;

/// <summary>
/// Unit tests for WorkScheduleService.
/// Tests XOR validation, CRUD operations, and expected hours calculations.
/// </summary>
public class WorkScheduleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IWorkScheduleRepository> _workScheduleRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IClientRepository> _clientRepoMock;
    private readonly Mock<IPublicHolidayService> _publicHolidayServiceMock;
    private readonly WorkScheduleService _service;

    public WorkScheduleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _workScheduleRepoMock = new Mock<IWorkScheduleRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _clientRepoMock = new Mock<IClientRepository>();
        _publicHolidayServiceMock = new Mock<IPublicHolidayService>();

        _unitOfWorkMock.Setup(u => u.WorkSchedules).Returns(_workScheduleRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Clients).Returns(_clientRepoMock.Object);

        _service = new WorkScheduleService(_unitOfWorkMock.Object, _publicHolidayServiceMock.Object);
    }

    #region XOR Validation Tests

    [Fact]
    public async Task CreateAsync_WithBothClientAndProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateWorkScheduleDto
        {
            ClientId = 1,
            ProjectId = 1, // Both set - invalid
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = WorkDaysFlags.Monday | WorkDaysFlags.Tuesday,
            CountryCode = "US"
        };

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*either a Client or a Project*");
    }

    [Fact]
    public async Task CreateAsync_WithNeitherClientNorProject_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateWorkScheduleDto
        {
            ClientId = null,
            ProjectId = null, // Neither set - invalid
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = WorkDaysFlags.Monday,
            CountryCode = "US"
        };

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*either a Client or a Project*");
    }

    [Fact]
    public async Task CreateAsync_WithOnlyClientId_Succeeds()
    {
        // Arrange
        var dto = new CreateWorkScheduleDto
        {
            ClientId = 1,
            ProjectId = null,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = WorkDaysFlags.Monday | WorkDaysFlags.Tuesday,
            CountryCode = "US"
        };

        // Mock: no existing schedule for client
        _workScheduleRepoMock.Setup(r => r.ClientHasScheduleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _workScheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<WorkSchedule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock: reload after save
        var savedSchedule = new WorkSchedule
        {
            Id = 10,
            ClientId = 1,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday),
            CountryCode = "US"
        };
        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedSchedule);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(1);
        result.ProjectId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithOnlyProjectId_Succeeds()
    {
        // Arrange
        var dto = new CreateWorkScheduleDto
        {
            ClientId = null,
            ProjectId = 2,
            WorkPercentage = 80,
            BaseHoursPerDay = 6,
            WorkDays = WorkDaysFlags.Monday | WorkDaysFlags.Wednesday | WorkDaysFlags.Friday,
            CountryCode = "DE"
        };

        // Mock: no existing schedule for project
        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _workScheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<WorkSchedule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock: reload after save
        var savedSchedule = new WorkSchedule
        {
            Id = 20,
            ProjectId = 2,
            WorkPercentage = 80,
            BaseHoursPerDay = 6,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Wednesday | WorkDaysFlags.Friday),
            CountryCode = "DE"
        };
        _workScheduleRepoMock.Setup(r => r.GetByProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedSchedule);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ProjectId.Should().Be(2);
        result.ClientId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenClientAlreadyHasSchedule_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateWorkScheduleDto
        {
            ClientId = 1,
            ProjectId = null,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = WorkDaysFlags.Monday,
            CountryCode = "US"
        };

        // Mock: client already has schedule
        _workScheduleRepoMock.Setup(r => r.ClientHasScheduleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has a work schedule*");
    }

    #endregion

    #region Get By Client/Project Tests

    [Fact]
    public async Task GetByClientIdAsync_ReturnsScheduleForClient()
    {
        // Arrange
        var schedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 5,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday),
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await _service.GetByClientIdAsync(5);

        // Assert
        result.Should().NotBeNull();
        result!.ClientId.Should().Be(5);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsScheduleForProject()
    {
        // Arrange
        var schedule = new WorkSchedule
        {
            Id = 2,
            ProjectId = 10,
            WorkPercentage = 50,
            BaseHoursPerDay = 4,
            WorkDays = (int)WorkDaysFlags.Friday,
            CountryCode = "UK"
        };

        _workScheduleRepoMock.Setup(r => r.GetByProjectIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Act
        var result = await _service.GetByProjectIdAsync(10);

        // Assert
        result.Should().NotBeNull();
        result!.ProjectId.Should().Be(10);
        result.WorkPercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetByClientIdAsync_WhenNoSchedule_ReturnsNull()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        var result = await _service.GetByClientIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Expected Hours Calculation Tests

    [Fact]
    public async Task CalculateExpectedHoursAsync_ForSingleDay_ReturnsCorrectHours()
    {
        // Arrange - Monday, 100%, 8hrs/day
        var schedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday | WorkDaysFlags.Wednesday |
                            WorkDaysFlags.Thursday | WorkDaysFlags.Friday),
            IncludePublicHolidays = true,
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Monday - a working day
        var monday = new DateTime(2025, 1, 6); // This is a Monday

        // Act
        var result = await _service.CalculateExpectedHoursAsync(1, monday, monday);

        // Assert
        result.Should().Be(8m); // 100% * 8 hours * 1 working day
    }

    [Fact]
    public async Task CalculateExpectedHoursAsync_ForWeekend_ReturnsZero()
    {
        // Arrange - only weekdays
        var schedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday | WorkDaysFlags.Wednesday |
                            WorkDaysFlags.Thursday | WorkDaysFlags.Friday),
            IncludePublicHolidays = true,
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Saturday - not a working day
        var saturday = new DateTime(2025, 1, 4);

        // Act
        var result = await _service.CalculateExpectedHoursAsync(1, saturday, saturday);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateExpectedHoursAsync_WithPartTimePercentage_ReturnsReducedHours()
    {
        // Arrange - 50% time
        var schedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 50,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday | WorkDaysFlags.Wednesday |
                            WorkDaysFlags.Thursday | WorkDaysFlags.Friday),
            IncludePublicHolidays = true,
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Monday
        var monday = new DateTime(2025, 1, 6);

        // Act
        var result = await _service.CalculateExpectedHoursAsync(1, monday, monday);

        // Assert
        result.Should().Be(4m); // 50% * 8 hours = 4 hours
    }

    [Fact]
    public async Task CalculateExpectedHoursAsync_WithHoliday_ExcludesHolidayFromExpected()
    {
        // Arrange
        var schedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday | WorkDaysFlags.Wednesday |
                            WorkDaysFlags.Thursday | WorkDaysFlags.Friday),
            IncludePublicHolidays = false, // Holidays reduce expected hours
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        // Monday that's a holiday
        var monday = new DateTime(2025, 1, 6);

        // Service uses GetHolidaysForDateRangeAsync, not IsHolidayAsync
        _publicHolidayServiceMock.Setup(s => s.GetHolidaysForDateRangeAsync(
                "US", monday, monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PublicHolidayDto>
            {
                new() { Date = monday, Name = "Test Holiday", CountryCode = "US" }
            });

        // Act
        var result = await _service.CalculateExpectedHoursAsync(1, monday, monday);

        // Assert
        result.Should().Be(0m); // Holiday - no expected hours
    }

    [Fact]
    public async Task CalculateExpectedHoursAsync_WhenNoSchedule_ThrowsInvalidOperationException()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        var act = () => _service.CalculateExpectedHoursAsync(999, DateTime.Today, DateTime.Today);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_UpdatesScheduleProperties()
    {
        // Arrange
        var existingSchedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 100,
            BaseHoursPerDay = 8,
            WorkDays = (int)WorkDaysFlags.Monday,
            CountryCode = "US"
        };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSchedule);

        // Mock: reload after save (service reloads with relations)
        var updatedSchedule = new WorkSchedule
        {
            Id = 1,
            ClientId = 1,
            WorkPercentage = 75,
            BaseHoursPerDay = 6,
            WorkDays = (int)(WorkDaysFlags.Monday | WorkDaysFlags.Tuesday),
            IncludePublicHolidays = true,
            CountryCode = "DE"
        };
        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSchedule);

        var updateDto = new UpdateWorkScheduleDto
        {
            Id = 1,
            WorkPercentage = 75,
            BaseHoursPerDay = 6,
            WorkDays = WorkDaysFlags.Monday | WorkDaysFlags.Tuesday,
            IncludePublicHolidays = true,
            CountryCode = "DE"
        };

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.WorkPercentage.Should().Be(75);
        result.BaseHoursPerDay.Should().Be(6);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        var updateDto = new UpdateWorkScheduleDto
        {
            Id = 999,
            WorkPercentage = 75,
            BaseHoursPerDay = 6,
            WorkDays = WorkDaysFlags.Monday,
            CountryCode = "US"
        };

        // Act
        var act = () => _service.UpdateAsync(updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_RemovesSchedule()
    {
        // Arrange
        var existingSchedule = new WorkSchedule { Id = 1, ClientId = 1 };

        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSchedule);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _workScheduleRepoMock.Verify(r => r.Delete(existingSchedule), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _workScheduleRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        var act = () => _service.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public async Task CheckClientScheduleConflictsAsync_WhenNoProjectSchedules_ReturnsNoConflict()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ClientId = 1 },
            new() { Id = 2, Name = "Project B", ClientId = 1 }
        };

        _projectRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CheckClientScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeFalse();
        result.ConflictType.Should().Be(ScheduleConflictType.None);
    }

    [Fact]
    public async Task CheckClientScheduleConflictsAsync_WhenProjectsHaveSchedules_ReturnsConflict()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ClientId = 1 },
            new() { Id = 2, Name = "Project B", ClientId = 1 }
        };

        _projectRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CheckClientScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.ConflictType.Should().Be(ScheduleConflictType.ProjectSchedulesExist);
        result.ConflictingProjectNames.Should().Contain("Project A");
        result.ConflictCount.Should().Be(1);
    }

    [Fact]
    public async Task CheckClientScheduleConflictsAsync_WithMultipleConflicts_ReturnsAllConflictingProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ClientId = 1 },
            new() { Id = 2, Name = "Project B", ClientId = 1 },
            new() { Id = 3, Name = "Project C", ClientId = 1 }
        };

        _projectRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _workScheduleRepoMock.Setup(r => r.ProjectHasScheduleAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CheckClientScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.ConflictingProjectNames.Should().HaveCount(2);
        result.ConflictingProjectNames.Should().Contain("Project A");
        result.ConflictingProjectNames.Should().Contain("Project B");
        result.ConflictCount.Should().Be(2);
    }

    [Fact]
    public async Task CheckProjectScheduleConflictsAsync_WhenProjectHasNoClient_ReturnsNoConflict()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Standalone Project", ClientId = null };

        _projectRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _service.CheckProjectScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeFalse();
    }

    [Fact]
    public async Task CheckProjectScheduleConflictsAsync_WhenClientHasNoSchedule_ReturnsNoConflict()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Project A", ClientId = 5 };

        _projectRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        var result = await _service.CheckProjectScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeFalse();
    }

    [Fact]
    public async Task CheckProjectScheduleConflictsAsync_WhenClientHasSchedule_ReturnsConflict()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Project A", ClientId = 5 };
        var clientSchedule = new WorkSchedule { Id = 10, ClientId = 5 };
        var client = new Client { Id = 5, Name = "Test Client" };

        _projectRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _workScheduleRepoMock.Setup(r => r.GetByClientIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientSchedule);

        _clientRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        var result = await _service.CheckProjectScheduleConflictsAsync(1);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.ConflictType.Should().Be(ScheduleConflictType.ClientScheduleExists);
        result.ConflictingClientName.Should().Be("Test Client");
        result.ConflictCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteProjectSchedulesForClientAsync_DeletesAllProjectSchedules()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ClientId = 1 },
            new() { Id = 2, Name = "Project B", ClientId = 1 }
        };

        var schedule1 = new WorkSchedule { Id = 10, ProjectId = 1 };
        var schedule2 = new WorkSchedule { Id = 20, ProjectId = 2 };

        _projectRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _workScheduleRepoMock.Setup(r => r.GetByProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule1);
        _workScheduleRepoMock.Setup(r => r.GetByProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule2);

        // Act
        await _service.DeleteProjectSchedulesForClientAsync(1);

        // Assert
        _workScheduleRepoMock.Verify(r => r.Delete(schedule1), Times.Once);
        _workScheduleRepoMock.Verify(r => r.Delete(schedule2), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectSchedulesForClientAsync_WhenNoSchedules_DoesNothing()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ClientId = 1 }
        };

        _projectRepoMock.Setup(r => r.GetByClientIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        _workScheduleRepoMock.Setup(r => r.GetByProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        await _service.DeleteProjectSchedulesForClientAsync(1);

        // Assert
        _workScheduleRepoMock.Verify(r => r.Delete(It.IsAny<WorkSchedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
