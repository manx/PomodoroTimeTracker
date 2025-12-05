using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for ProjectRepository using InMemory DbContext.
/// Tests project CRUD operations and unique name per client constraint validation.
/// </summary>
public class ProjectRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProjectRepository(_context);
    }

    #region GetByIdWithDetailsAsync Tests

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithExistingId_ReturnsProjectWithAllRelations()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        var project = new Project
        {
            Name = "Test Project",
            Description = "Description",
            Client = client,
            CreatedAt = DateTime.UtcNow,
            TimeEntries = new List<TimeEntry>
            {
                new() { Description = "Session 1", StartTime = DateTime.UtcNow, SessionTypeId = SessionType.Ids.Work, DurationMinutes = 25, CreatedAt = DateTime.UtcNow },
                new() { Description = "Entry 1", StartTime = DateTime.UtcNow, SessionTypeId = SessionType.Ids.Manual, CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
        result.Name.Should().Be("Test Project");
        result.Client.Should().NotBeNull();
        result.Client!.Name.Should().Be("Test Client");
        result.TimeEntries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdWithDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WithNoClient_ReturnsProjectWithNullClient()
    {
        // Arrange
        var project = new Project
        {
            Name = "Standalone Project",
            ClientId = null,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Client.Should().BeNull();
    }

    #endregion

    #region GetAllWithClientAsync Tests

    [Fact]
    public async Task GetAllWithClientAsync_WithNoProjects_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllWithClientAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithClientAsync_WithMultipleProjects_ReturnsAllProjects()
    {
        // Arrange
        var project1 = new Project { Name = "Project A", CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Name = "Project B", CreatedAt = DateTime.UtcNow };

        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithClientAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Name == "Project A");
        result.Should().Contain(p => p.Name == "Project B");
    }

    [Fact]
    public async Task GetAllWithClientAsync_IncludesClientInformation()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        var project = new Project
        {
            Name = "Project with Client",
            Client = client,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllWithClientAsync();

        // Assert
        var loadedProject = result.First();
        loadedProject.Client.Should().NotBeNull();
        loadedProject.Client!.Name.Should().Be("Test Client");
    }

    #endregion

    #region GetByClientIdAsync Tests

    [Fact]
    public async Task GetByClientIdAsync_WithMatchingProjects_ReturnsFilteredProjects()
    {
        // Arrange
        var client1 = new Client { Name = "Client 1", CreatedAt = DateTime.UtcNow };
        var client2 = new Client { Name = "Client 2", CreatedAt = DateTime.UtcNow };

        var project1 = new Project { Name = "Project 1", Client = client1, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Name = "Project 2", Client = client1, CreatedAt = DateTime.UtcNow };
        var project3 = new Project { Name = "Project 3", Client = client2, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddRangeAsync(client1, client2);
        await _context.Projects.AddRangeAsync(project1, project2, project3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByClientIdAsync(client1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.ClientId == client1.Id);
        result.Should().Contain(p => p.Name == "Project 1");
        result.Should().Contain(p => p.Name == "Project 2");
    }

    [Fact]
    public async Task GetByClientIdAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByClientIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByClientIdAsync_IncludesClientInformation()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Project", Client = client, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByClientIdAsync(client.Id);

        // Assert
        result.First().Client.Should().NotBeNull();
    }

    #endregion

    #region ExistsWithNameForClientAsync Tests

    [Fact]
    public async Task ExistsWithNameForClientAsync_WithExistingNameForClient_ReturnsTrue()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Existing Project", Client = client, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsWithNameForClientAsync("Existing Project", client.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_WithNonExistingName_ReturnsFalse()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsWithNameForClientAsync("Non-Existing", client.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_IsCaseInsensitive()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Test Project", Client = client, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act & Assert
        (await _repository.ExistsWithNameForClientAsync("test project", client.Id)).Should().BeTrue();
        (await _repository.ExistsWithNameForClientAsync("TEST PROJECT", client.Id)).Should().BeTrue();
        (await _repository.ExistsWithNameForClientAsync("TeSt PrOjEcT", client.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_SameNameDifferentClients_ReturnsFalse()
    {
        // Arrange - Same project name for different clients should be allowed
        var client1 = new Client { Name = "Client 1", CreatedAt = DateTime.UtcNow };
        var client2 = new Client { Name = "Client 2", CreatedAt = DateTime.UtcNow };

        var project1 = new Project { Name = "Website", Client = client1, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddRangeAsync(client1, client2);
        await _context.Projects.AddAsync(project1);
        await _context.SaveChangesAsync();

        // Act - Check if "Website" exists for client2
        var result = await _repository.ExistsWithNameForClientAsync("Website", client2.Id);

        // Assert - Should be false because it only exists for client1
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_WithExcludeProjectId_ExcludesSpecifiedProject()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        var project = new Project { Name = "Project", Client = client, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act - Exclude the project itself
        var result = await _repository.ExistsWithNameForClientAsync("Project", client.Id, project.Id);

        // Assert - Should return false because we excluded this project
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_WithExcludeProjectId_StillDetectsOtherProjects()
    {
        // Arrange
        var client = new Client { Name = "Client", CreatedAt = DateTime.UtcNow };
        var project1 = new Project { Name = "Duplicate", Client = client, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Name = "Duplicate", Client = client, CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddAsync(client);
        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        // Act - Exclude project1, but project2 still has the same name
        var result = await _repository.ExistsWithNameForClientAsync("Duplicate", client.Id, project1.Id);

        // Assert - Should return true because project2 exists with same name
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithNameForClientAsync_ForNullClient_ChecksProjectsWithoutClient()
    {
        // Arrange
        var project1 = new Project { Name = "Standalone", ClientId = null, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Name = "Another", ClientId = null, CreatedAt = DateTime.UtcNow };

        await _context.Projects.AddRangeAsync(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsWithNameForClientAsync("Standalone", null);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CRUD Operations Tests

    [Fact]
    public async Task AddAsync_AddsProjectToDatabase()
    {
        // Arrange
        var project = new Project
        {
            Name = "New Project",
            Description = "Description",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(project);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Projects.FindAsync(project.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Project");
    }

    [Fact]
    public async Task Update_ModifiesExistingProject()
    {
        // Arrange
        var project = new Project
        {
            Name = "Original Name",
            Description = "Original Description",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Act
        project.Name = "Updated Name";
        project.Description = "Updated Description";
        _repository.Update(project);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Projects.FindAsync(project.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Delete_RemovesProjectFromDatabase()
    {
        // Arrange
        var project = new Project { Name = "To Delete", CreatedAt = DateTime.UtcNow };
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        var projectId = project.Id;

        // Act
        _repository.Delete(project);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Projects.FindAsync(projectId);
        deleted.Should().BeNull();
    }

    #endregion

    #region Cascade Behavior Tests

    [Fact]
    public async Task Delete_Project_SetsSessionsProjectIdToNull()
    {
        // Arrange
        var project = new Project
        {
            Name = "Project",
            CreatedAt = DateTime.UtcNow,
            TimeEntries = new List<TimeEntry>
            {
                new() { Description = "Session 1", StartTime = DateTime.UtcNow, SessionTypeId = SessionType.Ids.Work, DurationMinutes = 25, CreatedAt = DateTime.UtcNow },
                new() { Description = "Session 2", StartTime = DateTime.UtcNow, SessionTypeId = SessionType.Ids.Work, DurationMinutes = 25, CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var entryIds = project.TimeEntries.Select(e => e.Id).ToList();

        // Act
        _repository.Delete(project);
        await _context.SaveChangesAsync();

        // Assert - Entries should still exist but with null ProjectId
        var entries = await _context.TimeEntries.Where(e => entryIds.Contains(e.Id)).ToListAsync();
        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.ProjectId == null);
    }

    [Fact]
    public async Task Delete_Project_SetsTimeEntriesProjectIdToNull()
    {
        // Arrange
        var project = new Project
        {
            Name = "Project",
            CreatedAt = DateTime.UtcNow,
            TimeEntries = new List<TimeEntry>
            {
                new() { Description = "Entry 1", StartTime = DateTime.UtcNow, SessionTypeId = SessionType.Ids.Manual, CreatedAt = DateTime.UtcNow },
                new() { Description = "Entry 2", StartTime = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        var entryIds = project.TimeEntries.Select(e => e.Id).ToList();

        // Act
        _repository.Delete(project);
        await _context.SaveChangesAsync();

        // Assert - TimeEntries should still exist but with null ProjectId
        var entries = await _context.TimeEntries.Where(e => entryIds.Contains(e.Id)).ToListAsync();
        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.ProjectId == null);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
