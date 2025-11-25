using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for PomodoroSessionRepository using InMemory DbContext.
/// Tests actual EF Core queries and database operations.
/// </summary>
public class PomodoroSessionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PomodoroSessionRepository _repository;

    public PomodoroSessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PomodoroSessionRepository(_context);
    }

    #region GetAllWithProjectAsync Tests

    [Fact]
    public async Task GetAllWithProjectAsync_WithNoSessions_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllWithProjectAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithProjectAsync_WithMultipleSessions_ReturnsOrderedByStartTimeDescending()
    {
        // Arrange
        var session1 = new PomodoroSession
        {
            Objective = "First",
            StartTime = DateTime.UtcNow.AddHours(-3),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };
        var session2 = new PomodoroSession
        {
            Objective = "Second",
            StartTime = DateTime.UtcNow.AddHours(-2),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };
        var session3 = new PomodoroSession
        {
            Objective = "Third",
            StartTime = DateTime.UtcNow.AddHours(-1),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddRangeAsync(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithProjectAsync();

        // Assert
        result.Should().HaveCount(3);
        result.First().Objective.Should().Be("Third"); // Most recent first
        result.Last().Objective.Should().Be("First");
    }

    [Fact]
    public async Task GetAllWithProjectAsync_IncludesProjectAndClient()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Test Project", Client = client, CreatedAt = DateTime.UtcNow };
        var session = new PomodoroSession
        {
            Objective = "Test Session",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Project = project
        };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Detach to ensure fresh load
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllWithProjectAsync();

        // Assert
        var loadedSession = result.First();
        loadedSession.Project.Should().NotBeNull();
        loadedSession.Project!.Name.Should().Be("Test Project");
        loadedSession.Project.Client.Should().NotBeNull();
        loadedSession.Project.Client!.Name.Should().Be("Test Client");
    }

    #endregion

    #region GetByIdWithProjectAsync Tests

    [Fact]
    public async Task GetByIdWithProjectAsync_WithExistingId_ReturnsSession()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Objective = "Test",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithProjectAsync(session.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.Objective.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdWithProjectAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdWithProjectAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithProjectAsync_IncludesProjectAndClient()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Project", Client = client, CreatedAt = DateTime.UtcNow };
        var session = new PomodoroSession
        {
            Objective = "Session",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            Project = project
        };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithProjectAsync(session.Id);

        // Assert
        result!.Project.Should().NotBeNull();
        result.Project!.Client.Should().NotBeNull();
    }

    #endregion

    #region GetByProjectIdAsync Tests

    [Fact]
    public async Task GetByProjectIdAsync_WithMatchingSessions_ReturnsFilteredSessions()
    {
        // Arrange
        var project1 = new Project { Name = "Project 1", CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Name = "Project 2", CreatedAt = DateTime.UtcNow };

        var session1 = new PomodoroSession
        {
            Objective = "Session 1",
            Project = project1,
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };
        var session2 = new PomodoroSession
        {
            Objective = "Session 2",
            Project = project1,
            StartTime = DateTime.UtcNow.AddHours(-1),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };
        var session3 = new PomodoroSession
        {
            Objective = "Session 3",
            Project = project2,
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.PomodoroSessions.AddRangeAsync(session1, session2, session3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(project1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.ProjectId == project1.Id);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByProjectIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsOrderedByStartTimeDescending()
    {
        // Arrange
        var project = new Project { Name = "Project", CreatedAt = DateTime.UtcNow };

        var session1 = new PomodoroSession
        {
            Objective = "Old",
            Project = project,
            StartTime = DateTime.UtcNow.AddHours(-2),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };
        var session2 = new PomodoroSession
        {
            Objective = "New",
            Project = project,
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.Projects.AddAsync(project);
        await _context.PomodoroSessions.AddRangeAsync(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProjectIdAsync(project.Id);

        // Assert
        result.First().Objective.Should().Be("New");
    }

    #endregion

    #region GetActiveSessionAsync Tests

    [Fact]
    public async Task GetActiveSessionAsync_WithActiveSession_ReturnsSession()
    {
        // Arrange
        var activeSession = new PomodoroSession
        {
            Objective = "Active",
            StartTime = DateTime.UtcNow,
            EndTime = null,
            IsCompleted = false,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        var completedSession = new PomodoroSession
        {
            Objective = "Completed",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(-35),
            IsCompleted = true,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddRangeAsync(activeSession, completedSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSessionAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(activeSession.Id);
        result.IsCompleted.Should().BeFalse();
        result.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithNoActiveSession_ReturnsNull()
    {
        // Arrange
        var completedSession = new PomodoroSession
        {
            Objective = "Completed",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddMinutes(-35),
            IsCompleted = true,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddAsync(completedSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSessionAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithMultipleActiveSessions_ReturnsMostRecent()
    {
        // Arrange - edge case: multiple active sessions
        var older = new PomodoroSession
        {
            Objective = "Older",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null,
            IsCompleted = false,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        var newer = new PomodoroSession
        {
            Objective = "Newer",
            StartTime = DateTime.UtcNow,
            EndTime = null,
            IsCompleted = false,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddRangeAsync(older, newer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSessionAsync();

        // Assert
        result!.Objective.Should().Be("Newer");
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_WithSessionsInRange_ReturnsMatchingSessions()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(1);

        var inRange1 = new PomodoroSession
        {
            Objective = "In Range 1",
            StartTime = startDate.AddHours(10),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        var inRange2 = new PomodoroSession
        {
            Objective = "In Range 2",
            StartTime = startDate.AddHours(14),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        var outOfRange = new PomodoroSession
        {
            Objective = "Out of Range",
            StartTime = startDate.AddDays(-1),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddRangeAsync(inRange1, inRange2, outOfRange);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Objective == "In Range 1");
        result.Should().Contain(s => s.Objective == "In Range 2");
        result.Should().NotContain(s => s.Objective == "Out of Range");
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithNoSessionsInRange_ReturnsEmptyList()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(1);

        var session = new PomodoroSession
        {
            Objective = "Out of Range",
            StartTime = startDate.AddDays(-2),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsOrderedByStartTimeDescending()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(1);

        var session1 = new PomodoroSession
        {
            Objective = "Morning",
            StartTime = startDate.AddHours(9),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        var session2 = new PomodoroSession
        {
            Objective = "Afternoon",
            StartTime = startDate.AddHours(14),
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddRangeAsync(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.First().Objective.Should().Be("Afternoon"); // Most recent first
    }

    #endregion

    #region Base Repository Methods Tests

    [Fact]
    public async Task AddAsync_AddsSessionToDatabase()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Objective = "New Session",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        // Act
        await _repository.AddAsync(session);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.PomodoroSessions.FindAsync(session.Id);
        saved.Should().NotBeNull();
        saved!.Objective.Should().Be("New Session");
    }

    [Fact]
    public async Task Update_ModifiesExistingSession()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Objective = "Original",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work,
            IsCompleted = false
        };

        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        session.Objective = "Updated";
        session.IsCompleted = true;
        _repository.Update(session);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.PomodoroSessions.FindAsync(session.Id);
        updated!.Objective.Should().Be("Updated");
        updated.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_RemovesSessionFromDatabase()
    {
        // Arrange
        var session = new PomodoroSession
        {
            Objective = "To Delete",
            StartTime = DateTime.UtcNow,
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddAsync(session);
        await _context.SaveChangesAsync();
        var sessionId = session.Id;

        // Act
        _repository.Delete(session);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.PomodoroSessions.FindAsync(sessionId);
        deleted.Should().BeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
