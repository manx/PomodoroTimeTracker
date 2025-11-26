using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for PomodoroSessionService using mocked repositories.
/// Tests business logic without depending on actual database operations.
/// </summary>
public class PomodoroSessionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPomodoroSessionRepository> _sessionRepositoryMock;
    private readonly PomodoroSessionService _service;

    public PomodoroSessionServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sessionRepositoryMock = new Mock<IPomodoroSessionRepository>();

        _unitOfWorkMock.Setup(u => u.PomodoroSessions).Returns(_sessionRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new PomodoroSessionService(_unitOfWorkMock.Object);
    }

    #region GetAllSessionsAsync Tests

    [Fact]
    public async Task GetAllSessionsAsync_WithNoSessions_ReturnsEmptyCollection()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PomodoroSession>());

        // Act
        var result = await _service.GetAllSessionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _sessionRepositoryMock.Verify(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllSessionsAsync_WithMultipleSessions_ReturnsAllSessions()
    {
        // Arrange
        var sessions = new List<PomodoroSession>
        {
            new()
            {
                Id = 1,
                Objective = "Task 1",
                StartTime = DateTime.UtcNow.AddHours(-2),
                DurationMinutes = 25,
                SessionType = SessionType.Work,
                IsCompleted = true
            },
            new()
            {
                Id = 2,
                Objective = "Task 2",
                StartTime = DateTime.UtcNow.AddHours(-1),
                DurationMinutes = 5,
                SessionType = SessionType.ShortBreak,
                IsCompleted = true
            }
        };

        _sessionRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetAllSessionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Objective == "Task 1");
        result.Should().Contain(s => s.Objective == "Task 2");
    }

    [Fact]
    public async Task GetAllSessionsAsync_WithProjectAndClient_MapsRelationshipsCorrectly()
    {
        // Arrange
        var client = new Client { Id = 1, Name = "Test Client" };
        var project = new Project { Id = 1, Name = "Test Project", Client = client, ClientId = 1 };
        var session = new PomodoroSession
        {
            Id = 1,
            Objective = "Work on feature",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Project = project,
            ProjectId = 1
        };

        _sessionRepositoryMock.Setup(r => r.GetAllWithProjectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PomodoroSession> { session });

        // Act
        var result = await _service.GetAllSessionsAsync();

        // Assert
        var dto = result.First();
        dto.ProjectName.Should().Be("Test Project");
        dto.ClientName.Should().Be("Test Client");
    }

    #endregion

    #region GetSessionByIdAsync Tests

    [Fact]
    public async Task GetSessionByIdAsync_WithExistingId_ReturnsSession()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Id = 1,
            Objective = "Test task",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetSessionByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Objective.Should().Be("Test task");
    }

    [Fact]
    public async Task GetSessionByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetByIdWithProjectAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PomodoroSession?)null);

        // Act
        var result = await _service.GetSessionByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSessionsByProjectIdAsync Tests

    [Fact]
    public async Task GetSessionsByProjectIdAsync_WithMatchingSessions_ReturnsFilteredSessions()
    {
        // Arrange
        var sessions = new List<PomodoroSession>
        {
            new()
            {
                Id = 1,
                ProjectId = 5,
                Objective = "Task 1",
                StartTime = DateTime.UtcNow,
                DurationMinutes = 25,
                SessionType = SessionType.Work
            },
            new()
            {
                Id = 2,
                ProjectId = 5,
                Objective = "Task 2",
                StartTime = DateTime.UtcNow,
                DurationMinutes = 25,
                SessionType = SessionType.Work
            }
        };

        _sessionRepositoryMock.Setup(r => r.GetByProjectIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetSessionsByProjectIdAsync(5);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.ProjectId == 5);
    }

    [Fact]
    public async Task GetSessionsByProjectIdAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetByProjectIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PomodoroSession>());

        // Act
        var result = await _service.GetSessionsByProjectIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetActiveSessionAsync Tests

    [Fact]
    public async Task GetActiveSessionAsync_WithActiveSession_ReturnsSession()
    {
        // Arrange
        var activeSession = new PomodoroSession
        {
            Id = 1,
            Objective = "Active work",
            StartTime = DateTime.UtcNow,
            EndTime = null,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            IsCompleted = false
        };

        _sessionRepositoryMock.Setup(r => r.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSession);

        // Act
        var result = await _service.GetActiveSessionAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.IsCompleted.Should().BeFalse();
        result.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithNoActiveSession_ReturnsNull()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((PomodoroSession?)null);

        // Act
        var result = await _service.GetActiveSessionAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidData_CreatesAndReturnsSession()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            ProjectId = 1,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Objective = "Write unit tests",
            Notes = "Focus on service layer"
        };

        PomodoroSession? capturedSession = null;
        _sessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()))
            .Callback<PomodoroSession, CancellationToken>((s, ct) =>
            {
                s.Id = 1; // Simulate database ID generation
                capturedSession = s;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Objective.Should().Be("Write unit tests");
        result.Notes.Should().Be("Focus on service layer");
        result.DurationMinutes.Should().Be(25);
        result.SessionType.Should().Be(SessionType.Work);
        result.IsCompleted.Should().BeFalse();
        result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        capturedSession.Should().NotBeNull();
        capturedSession!.ProjectId.Should().Be(1);

        _sessionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullProjectId_CreatesSessionWithoutProject()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            ProjectId = null,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Objective = "Quick task"
        };

        _sessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()))
            .Callback<PomodoroSession, CancellationToken>((s, ct) => s.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        result.ProjectId.Should().BeNull();
        result.Objective.Should().Be("Quick task");
    }

    [Fact]
    public async Task CreateSessionAsync_WithShortBreakType_CreatesBreakSession()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            DurationMinutes = 5,
            SessionType = SessionType.ShortBreak,
            Objective = "Short break"
        };

        _sessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()))
            .Callback<PomodoroSession, CancellationToken>((s, ct) => s.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        result.SessionType.Should().Be(SessionType.ShortBreak);
        result.DurationMinutes.Should().Be(5);
    }

    #endregion

    #region UpdateSessionAsync Tests

    [Fact]
    public async Task UpdateSessionAsync_WithExistingSession_UpdatesSuccessfully()
    {
        // Arrange
        var existingSession = new PomodoroSession
        {
            Id = 1,
            Objective = "Old objective",
            StartTime = DateTime.UtcNow.AddHours(-1),
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            IsCompleted = false
        };

        var updateDto = new UpdatePomodoroSessionDto
        {
            Id = 1,
            ProjectId = 2,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            DurationMinutes = 25,
            IsCompleted = true,
            SessionType = SessionType.Work,
            Objective = "Updated objective",
            Notes = "Completed successfully"
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        // Act
        await _service.UpdateSessionAsync(updateDto);

        // Assert
        existingSession.Objective.Should().Be("Updated objective");
        existingSession.Notes.Should().Be("Completed successfully");
        existingSession.ProjectId.Should().Be(2);
        existingSession.IsCompleted.Should().BeTrue();
        existingSession.EndTime.Should().NotBeNull();

        _sessionRepositoryMock.Verify(r => r.Update(existingSession), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNonExistingSession_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdatePomodoroSessionDto
        {
            Id = 999,
            Objective = "Non-existing",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PomodoroSession?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateSessionAsync(updateDto))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Session with ID 999 not found");

        _sessionRepositoryMock.Verify(r => r.Update(It.IsAny<PomodoroSession>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CompleteSessionAsync Tests

    [Fact]
    public async Task CompleteSessionAsync_WithActiveSession_MarksAsCompleted()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Id = 1,
            Objective = "Work session",
            StartTime = DateTime.UtcNow.AddMinutes(-25),
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            IsCompleted = false,
            EndTime = null
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.CompleteSessionAsync(1);

        // Assert
        session.IsCompleted.Should().BeTrue();
        session.EndTime.Should().NotBeNull();
        session.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _sessionRepositoryMock.Verify(r => r.Update(session), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteSessionAsync_WithNonExistingSession_ThrowsKeyNotFoundException()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PomodoroSession?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.CompleteSessionAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Session with ID 999 not found");

        _sessionRepositoryMock.Verify(r => r.Update(It.IsAny<PomodoroSession>()), Times.Never);
    }

    #endregion

    #region DeleteSessionAsync Tests

    [Fact]
    public async Task DeleteSessionAsync_WithExistingSession_DeletesSuccessfully()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Id = 1,
            Objective = "Session to delete",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.DeleteSessionAsync(1);

        // Assert
        _sessionRepositoryMock.Verify(r => r.Delete(session), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNonExistingSession_ThrowsKeyNotFoundException()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PomodoroSession?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.DeleteSessionAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Session with ID 999 not found");

        _sessionRepositoryMock.Verify(r => r.Delete(It.IsAny<PomodoroSession>()), Times.Never);
    }

    #endregion

    #region Edge Cases and Business Rules

    [Fact]
    public async Task CreateSessionAsync_SetsIsCompletedToFalse()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Objective = "New session"
        };

        _sessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()))
            .Callback<PomodoroSession, CancellationToken>((s, ct) => s.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSessionAsync_SetsStartTimeToUtcNow()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Objective = "Time check"
        };

        var beforeCreation = DateTime.UtcNow;

        _sessionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PomodoroSession>(), It.IsAny<CancellationToken>()))
            .Callback<PomodoroSession, CancellationToken>((s, ct) => s.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        var afterCreation = DateTime.UtcNow;
        result.StartTime.Should().BeOnOrAfter(beforeCreation);
        result.StartTime.Should().BeOnOrBefore(afterCreation);
    }

    #endregion
}
