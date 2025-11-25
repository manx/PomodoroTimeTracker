using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for ClientRepository using InMemory DbContext.
/// Tests client CRUD operations and unique name constraint validation.
/// </summary>
public class ClientRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ClientRepository _repository;

    public ClientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ClientRepository(_context);
    }

    #region GetByIdWithProjectsAsync Tests

    [Fact]
    public async Task GetByIdWithProjectsAsync_WithExistingId_ReturnsClientWithProjects()
    {
        // Arrange
        var client = new Client
        {
            Name = "Test Client",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            Projects = new List<Project>
            {
                new() { Name = "Project 1", CreatedAt = DateTime.UtcNow },
                new() { Name = "Project 2", CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithProjectsAsync(client.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(client.Id);
        result.Name.Should().Be("Test Client");
        result.Projects.Should().HaveCount(2);
        result.Projects.Should().Contain(p => p.Name == "Project 1");
        result.Projects.Should().Contain(p => p.Name == "Project 2");
    }

    [Fact]
    public async Task GetByIdWithProjectsAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdWithProjectsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdWithProjectsAsync_WithNoProjects_ReturnsClientWithEmptyCollection()
    {
        // Arrange
        var client = new Client
        {
            Name = "Client Without Projects",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdWithProjectsAsync(client.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Projects.Should().BeEmpty();
    }

    #endregion

    #region GetAllWithProjectsAsync Tests

    [Fact]
    public async Task GetAllWithProjectsAsync_WithNoClients_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllWithProjectsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithProjectsAsync_WithMultipleClients_ReturnsAllClients()
    {
        // Arrange
        var client1 = new Client { Name = "Client A", CreatedAt = DateTime.UtcNow };
        var client2 = new Client { Name = "Client B", CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddRangeAsync(client1, client2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllWithProjectsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Client A");
        result.Should().Contain(c => c.Name == "Client B");
    }

    [Fact]
    public async Task GetAllWithProjectsAsync_IncludesProjects()
    {
        // Arrange
        var client = new Client
        {
            Name = "Client With Projects",
            CreatedAt = DateTime.UtcNow,
            Projects = new List<Project>
            {
                new() { Name = "Project 1", CreatedAt = DateTime.UtcNow },
                new() { Name = "Project 2", CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllWithProjectsAsync();

        // Assert
        var loadedClient = result.First();
        loadedClient.Projects.Should().HaveCount(2);
    }

    #endregion

    #region ExistsWithNameAsync Tests

    [Fact]
    public async Task ExistsWithNameAsync_WithExistingName_ReturnsTrue()
    {
        // Arrange
        var client = new Client { Name = "Existing Client", CreatedAt = DateTime.UtcNow };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsWithNameAsync("Existing Client");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithNonExistingName_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsWithNameAsync("Non-Existing Client");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_IsCaseInsensitive()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act & Assert
        (await _repository.ExistsWithNameAsync("test client")).Should().BeTrue();
        (await _repository.ExistsWithNameAsync("TEST CLIENT")).Should().BeTrue();
        (await _repository.ExistsWithNameAsync("TeSt ClIeNt")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithExcludeClientId_ExcludesSpecifiedClient()
    {
        // Arrange
        var client = new Client { Name = "Test Client", CreatedAt = DateTime.UtcNow };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act - Exclude the client itself
        var result = await _repository.ExistsWithNameAsync("Test Client", client.Id);

        // Assert - Should return false because we excluded this client
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithExcludeClientId_StillDetectsOtherClients()
    {
        // Arrange
        var client1 = new Client { Name = "Duplicate Name", CreatedAt = DateTime.UtcNow };
        var client2 = new Client { Name = "Duplicate Name", CreatedAt = DateTime.UtcNow };

        await _context.Clients.AddRangeAsync(client1, client2);
        await _context.SaveChangesAsync();

        // Act - Exclude client1, but client2 still has the same name
        var result = await _repository.ExistsWithNameAsync("Duplicate Name", client1.Id);

        // Assert - Should return true because client2 exists with same name
        result.Should().BeTrue();
    }

    #endregion

    #region CRUD Operations Tests

    [Fact]
    public async Task AddAsync_AddsClientToDatabase()
    {
        // Arrange
        var client = new Client
        {
            Name = "New Client",
            Description = "Description",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(client);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Clients.FindAsync(client.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Client");
    }

    [Fact]
    public async Task Update_ModifiesExistingClient()
    {
        // Arrange
        var client = new Client
        {
            Name = "Original Name",
            Description = "Original Description",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        // Act
        client.Name = "Updated Name";
        client.Description = "Updated Description";
        _repository.Update(client);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Clients.FindAsync(client.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Delete_RemovesClientFromDatabase()
    {
        // Arrange
        var client = new Client { Name = "To Delete", CreatedAt = DateTime.UtcNow };
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();
        var clientId = client.Id;

        // Act
        _repository.Delete(client);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Clients.FindAsync(clientId);
        deleted.Should().BeNull();
    }

    #endregion

    #region Cascade Behavior Tests

    [Fact]
    public async Task Delete_Client_SetsProjectsClientIdToNull()
    {
        // Arrange
        var client = new Client
        {
            Name = "Client",
            CreatedAt = DateTime.UtcNow,
            Projects = new List<Project>
            {
                new() { Name = "Project 1", CreatedAt = DateTime.UtcNow },
                new() { Name = "Project 2", CreatedAt = DateTime.UtcNow }
            }
        };

        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();

        var projectIds = client.Projects.Select(p => p.Id).ToList();

        // Act
        _repository.Delete(client);
        await _context.SaveChangesAsync();

        // Assert - Projects should still exist but with null ClientId
        var projects = await _context.Projects.Where(p => projectIds.Contains(p.Id)).ToListAsync();
        projects.Should().HaveCount(2);
        projects.Should().OnlyContain(p => p.ClientId == null);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
