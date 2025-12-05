using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for TimeEntryService using mocked repositories.
/// Tests business logic without depending on actual database operations.
/// </summary>
public class TimeEntryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITimeEntryRepository> _timeEntryRepositoryMock;
    private readonly TimeEntryService _service;

    public TimeEntryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeEntryRepositoryMock = new Mock<ITimeEntryRepository>();

        _unitOfWorkMock.Setup(u => u.TimeEntries).Returns(_timeEntryRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new TimeEntryService(_unitOfWorkMock.Object);
    }

    #region GetAllEntriesAsync Tests

    [Fact]
    public async Task GetAllEntriesAsync_WithNoEntries_ReturnsEmptyCollection()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>());

        // Act
        var result = await _service.GetAllEntriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _timeEntryRepositoryMock.Verify(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllEntriesAsync_WithMultipleEntries_ReturnsAllEntries()
    {
        // Arrange
        var entries = new List<TimeEntry>
        {
            new()
            {
                Id = 1,
                Description = "Task 1",
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                DurationMinutes = 60,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = 2,
                Description = "Task 2",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null,
                DurationMinutes = null,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _timeEntryRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _service.GetAllEntriesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Description == "Task 1");
        result.Should().Contain(e => e.Description == "Task 2");
    }

    [Fact]
    public async Task GetAllEntriesAsync_WithProjectAndClient_MapsRelationshipsCorrectly()
    {
        // Arrange
        var client = new Client { Id = 1, Name = "Test Client" };
        var project = new Project { Id = 1, Name = "Test Project", Client = client, ClientId = 1 };
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Work on feature",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            DurationMinutes = 60,
            Project = project,
            ProjectId = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry> { entry });

        // Act
        var result = await _service.GetAllEntriesAsync();

        // Assert
        var dto = result.First();
        dto.ProjectName.Should().Be("Test Project");
        dto.ClientName.Should().Be("Test Client");
        dto.ProjectId.Should().Be(1);
    }

    #endregion

    #region GetEntryByIdAsync Tests

    [Fact]
    public async Task GetEntryByIdAsync_WithExistingId_ReturnsEntry()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Test entry",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            DurationMinutes = 60,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.GetEntryByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Description.Should().Be("Test entry");
        result.DurationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task GetEntryByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetByIdWithProjectAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeEntry?)null);

        // Act
        var result = await _service.GetEntryByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetEntriesByProjectIdAsync Tests

    [Fact]
    public async Task GetEntriesByProjectIdAsync_WithMatchingEntries_ReturnsFilteredEntries()
    {
        // Arrange
        var entries = new List<TimeEntry>
        {
            new()
            {
                Id = 1,
                ProjectId = 5,
                Description = "Task 1",
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                ProjectId = 5,
                Description = "Task 2",
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByProjectIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _service.GetEntriesByProjectIdAsync(5);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.ProjectId == 5);
    }

    [Fact]
    public async Task GetEntriesByProjectIdAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetByProjectIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>());

        // Act
        var result = await _service.GetEntriesByProjectIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetActiveEntryAsync Tests

    [Fact]
    public async Task GetActiveEntryAsync_WithActiveEntry_ReturnsEntry()
    {
        // Arrange
        var activeEntry = new TimeEntry
        {
            Id = 1,
            Description = "Active work",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = null,
            DurationMinutes = null,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetActiveEntryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEntry);

        // Act
        var result = await _service.GetActiveEntryAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Description.Should().Be("Active work");
        result.EndTime.Should().BeNull();
        result.DurationMinutes.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveEntryAsync_WithNoActiveEntry_ReturnsNull()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetActiveEntryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeEntry?)null);

        // Act
        var result = await _service.GetActiveEntryAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region StartTimerEntryAsync Tests

    [Fact]
    public async Task StartTimerEntryAsync_WithValidData_CreatesActiveEntry()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "New work task"
        };

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1; // Simulate database ID generation
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        var beforeStart = DateTime.UtcNow;

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Description.Should().Be("New work task");
        result.ProjectId.Should().Be(1);
        result.EndTime.Should().BeNull();
        result.DurationMinutes.Should().BeNull();
        result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.StartTime.Should().BeOnOrAfter(beforeStart);

        capturedEntry.Should().NotBeNull();
        capturedEntry!.ProjectId.Should().Be(1);
        capturedEntry.EndTime.Should().BeNull();
        capturedEntry.DurationMinutes.Should().BeNull();

        _timeEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartTimerEntryAsync_WithNullProjectId_CreatesEntryWithoutProject()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = null,
            Description = "Quick task"
        };

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        result.ProjectId.Should().BeNull();
        result.Description.Should().Be("Quick task");
        result.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task StartTimerEntryAsync_WithEmptyDescription_CreatesEntry()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = string.Empty
        };

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        result.Description.Should().BeEmpty();
        result.EndTime.Should().BeNull();
    }

    #endregion

    #region StopEntryAsync Tests

    [Fact]
    public async Task StopEntryAsync_WithActiveEntry_SetsEndTimeAndDuration()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Active work",
            StartTime = startTime,
            EndTime = null,
            DurationMinutes = null,
            CreatedAt = startTime
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        await _service.StopEntryAsync(1);

        // Assert
        entry.EndTime.Should().NotBeNull();
        entry.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entry.DurationMinutes.Should().BeGreaterThan(0);
        entry.DurationMinutes.Should().BeInRange(28, 32); // ~30 minutes +/- 2

        _timeEntryRepositoryMock.Verify(r => r.Update(entry), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopEntryAsync_WithNonExistingEntry_ThrowsKeyNotFoundException()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeEntry?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.StopEntryAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");

        _timeEntryRepositoryMock.Verify(r => r.Update(It.IsAny<TimeEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopEntryAsync_WithAlreadyStoppedEntry_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Completed work",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(-30), // Already stopped
            DurationMinutes = 30,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act & Assert
        await FluentActions.Invoking(() => _service.StopEntryAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already stopped*");

        _timeEntryRepositoryMock.Verify(r => r.Update(It.IsAny<TimeEntry>()), Times.Never);
    }

    [Fact]
    public async Task StopEntryAsync_CalculatesDurationCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2).AddMinutes(-15); // 2h 15min ago
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Long task",
            StartTime = startTime,
            EndTime = null,
            CreatedAt = startTime
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        await _service.StopEntryAsync(1);

        // Assert
        entry.DurationMinutes.Should().BeGreaterThan(120); // More than 2 hours
        entry.DurationMinutes.Should().BeLessThan(150); // Less than 2.5 hours
    }

    #endregion

    #region CreateEntryAsync Tests

    [Fact]
    public async Task CreateEntryAsync_WithOnlyDto_CreatesEntryWithCurrentStartTime()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Manual entry"
        };

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _service.CreateEntryAsync(createDto);

        // Assert
        result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.StartTime.Should().BeOnOrAfter(beforeCreate);
        result.EndTime.Should().BeNull();
        result.DurationMinutes.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntryAsync_WithStartTimeOnly_CreatesActiveEntry()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Custom start time"
        };
        var customStartTime = DateTime.UtcNow.AddHours(-2);

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateEntryAsync(createDto, customStartTime);

        // Assert
        result.StartTime.Should().Be(customStartTime);
        result.EndTime.Should().BeNull();
        result.DurationMinutes.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntryAsync_WithBothTimes_CreatesCompletedEntryWithDuration()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Complete entry"
        };
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1;
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateEntryAsync(createDto, startTime, endTime);

        // Assert
        result.StartTime.Should().Be(startTime);
        result.EndTime.Should().Be(endTime);
        result.DurationMinutes.Should().Be(120); // 2 hours

        capturedEntry.Should().NotBeNull();
        capturedEntry!.DurationMinutes.Should().Be(120);
    }

    [Fact]
    public async Task CreateEntryAsync_WithVeryShortDuration_CalculatesCorrectly()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Quick task"
        };
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var endTime = DateTime.UtcNow;

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateEntryAsync(createDto, startTime, endTime);

        // Assert
        result.DurationMinutes.Should().Be(5);
    }

    #endregion

    #region UpdateEntryAsync Tests

    [Fact]
    public async Task UpdateEntryAsync_WithExistingEntry_UpdatesSuccessfully()
    {
        // Arrange
        var existingEntry = new TimeEntry
        {
            Id = 1,
            Description = "Old description",
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = null,
            ProjectId = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var updateDto = new UpdateTimeEntryDto
        {
            Id = 1,
            ProjectId = 2,
            Description = "Updated description",
            StartTime = DateTime.UtcNow.AddHours(-3),
            EndTime = DateTime.UtcNow,
            DurationMinutes = null
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        // Act
        await _service.UpdateEntryAsync(updateDto);

        // Assert
        existingEntry.Description.Should().Be("Updated description");
        existingEntry.ProjectId.Should().Be(2);
        existingEntry.StartTime.Should().Be(updateDto.StartTime);
        existingEntry.EndTime.Should().NotBeNull();
        existingEntry.DurationMinutes.Should().Be(180); // 3 hours

        _timeEntryRepositoryMock.Verify(r => r.Update(existingEntry), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEntryAsync_WithNullEndTime_UsesProvidedDuration()
    {
        // Arrange
        var existingEntry = new TimeEntry
        {
            Id = 1,
            Description = "Active task",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null,
            DurationMinutes = null,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var updateDto = new UpdateTimeEntryDto
        {
            Id = 1,
            ProjectId = 1,
            Description = "Updated active task",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null,
            DurationMinutes = 45
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        // Act
        await _service.UpdateEntryAsync(updateDto);

        // Assert
        existingEntry.DurationMinutes.Should().Be(45);
        existingEntry.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEntryAsync_WithEndTime_CalculatesDurationAutomatically()
    {
        // Arrange
        var existingEntry = new TimeEntry
        {
            Id = 1,
            Description = "Task",
            StartTime = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var startTime = DateTime.UtcNow.AddHours(-3);
        var endTime = DateTime.UtcNow.AddHours(-1);

        var updateDto = new UpdateTimeEntryDto
        {
            Id = 1,
            ProjectId = 1,
            Description = "Task",
            StartTime = startTime,
            EndTime = endTime,
            DurationMinutes = null // This should be ignored when EndTime is present
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        // Act
        await _service.UpdateEntryAsync(updateDto);

        // Assert
        existingEntry.DurationMinutes.Should().Be(120); // 2 hours calculated from start/end times
    }

    [Fact]
    public async Task UpdateEntryAsync_WithNonExistingEntry_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateTimeEntryDto
        {
            Id = 999,
            Description = "Non-existing",
            StartTime = DateTime.UtcNow,
            EndTime = null
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeEntry?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateEntryAsync(updateDto))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");

        _timeEntryRepositoryMock.Verify(r => r.Update(It.IsAny<TimeEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEntryAsync_RemovingProject_SetsProjectIdToNull()
    {
        // Arrange
        var existingEntry = new TimeEntry
        {
            Id = 1,
            Description = "Task",
            StartTime = DateTime.UtcNow.AddHours(-1),
            ProjectId = 5,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var updateDto = new UpdateTimeEntryDto
        {
            Id = 1,
            ProjectId = null, // Remove project association
            Description = "Task",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        // Act
        await _service.UpdateEntryAsync(updateDto);

        // Assert
        existingEntry.ProjectId.Should().BeNull();
    }

    #endregion

    #region DeleteEntryAsync Tests

    [Fact]
    public async Task DeleteEntryAsync_WithExistingEntry_DeletesSuccessfully()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Entry to delete",
            StartTime = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        await _service.DeleteEntryAsync(1);

        // Assert
        _timeEntryRepositoryMock.Verify(r => r.Delete(entry), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEntryAsync_WithNonExistingEntry_ThrowsKeyNotFoundException()
    {
        // Arrange
        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeEntry?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.DeleteEntryAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");

        _timeEntryRepositoryMock.Verify(r => r.Delete(It.IsAny<TimeEntry>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEntryAsync_WithActiveEntry_DeletesSuccessfully()
    {
        // Arrange - Active entry with no EndTime
        var activeEntry = new TimeEntry
        {
            Id = 1,
            Description = "Active entry to delete",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = null,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEntry);

        // Act
        await _service.DeleteEntryAsync(1);

        // Assert - Should allow deleting active entries
        _timeEntryRepositoryMock.Verify(r => r.Delete(activeEntry), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IsBillable Tests

    [Fact]
    public async Task StartTimerEntryAsync_WithBillableTrue_CreatesBillableEntry()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Break session",
            IsBillable = true
        };

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1;
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.IsBillable.Should().BeTrue();
        result.IsBillable.Should().BeTrue();
    }

    [Fact]
    public async Task StartTimerEntryAsync_WithBillableFalse_CreatesNonBillableEntry()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Break session",
            IsBillable = false
        };

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1;
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.IsBillable.Should().BeFalse();
        result.IsBillable.Should().BeFalse();
    }

    [Fact]
    public async Task StartTimerEntryAsync_WithBillableNull_CreatesEntryWithNullBillable()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Work session",
            IsBillable = null
        };

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1;
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.IsBillable.Should().BeNull();
        result.IsBillable.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntryAsync_WithBillable_SetsIsBillable()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = 1,
            Description = "Billable work",
            IsBillable = true
        };

        TimeEntry? capturedEntry = null;
        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) =>
            {
                e.Id = 1;
                capturedEntry = e;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateEntryAsync(createDto);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.IsBillable.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEntryAsync_UpdatesIsBillable()
    {
        // Arrange
        var existingEntry = new TimeEntry
        {
            Id = 1,
            Description = "Break",
            StartTime = DateTime.UtcNow.AddHours(-1),
            IsBillable = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var updateDto = new UpdateTimeEntryDto
        {
            Id = 1,
            ProjectId = 1,
            Description = "Break",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null,
            IsBillable = true
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        // Act
        await _service.UpdateEntryAsync(updateDto);

        // Assert
        existingEntry.IsBillable.Should().BeTrue();
        _timeEntryRepositoryMock.Verify(r => r.Update(existingEntry), Times.Once);
    }

    [Fact]
    public async Task GetAllEntriesAsync_MapsIsBillableCorrectly()
    {
        // Arrange
        var entries = new List<TimeEntry>
        {
            new()
            {
                Id = 1,
                Description = "Billable task",
                StartTime = DateTime.UtcNow.AddHours(-2),
                IsBillable = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = 2,
                Description = "Non-billable task",
                StartTime = DateTime.UtcNow.AddHours(-1),
                IsBillable = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _timeEntryRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _service.GetAllEntriesAsync();

        // Assert
        var resultList = result.ToList();
        resultList[0].IsBillable.Should().BeTrue();
        resultList[1].IsBillable.Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Business Rules

    [Fact]
    public async Task StartTimerEntryAsync_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            Description = "Time check entry"
        };

        var beforeCreation = DateTime.UtcNow;

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartTimerEntryAsync(createDto);

        // Assert
        var afterCreation = DateTime.UtcNow;
        result.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        result.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public async Task CreateEntryAsync_WithZeroDuration_HandlesCorrectly()
    {
        // Arrange
        var createDto = new CreateTimeEntryDto
        {
            Description = "Instant task"
        };
        var time = DateTime.UtcNow;

        _timeEntryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TimeEntry>(), It.IsAny<CancellationToken>()))
            .Callback<TimeEntry, CancellationToken>((e, ct) => e.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateEntryAsync(createDto, time, time);

        // Assert
        result.DurationMinutes.Should().Be(0);
    }

    [Fact]
    public async Task StopEntryAsync_WithLongRunningEntry_CalculatesDurationCorrectly()
    {
        // Arrange - Entry running for over 8 hours
        var startTime = DateTime.UtcNow.AddHours(-10);
        var entry = new TimeEntry
        {
            Id = 1,
            Description = "Long running task",
            StartTime = startTime,
            EndTime = null,
            CreatedAt = startTime
        };

        _timeEntryRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        await _service.StopEntryAsync(1);

        // Assert
        entry.DurationMinutes.Should().BeGreaterThan(590); // More than 9h 50min
        entry.DurationMinutes.Should().BeLessThan(610); // Less than 10h 10min
    }

    #endregion
}
