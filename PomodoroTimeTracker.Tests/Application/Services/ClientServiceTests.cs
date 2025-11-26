using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for ClientService.
/// Tests CRUD operations and business rules like unique client names.
/// </summary>
public class ClientServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<ILogger<ClientService>> _loggerMock;
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clientRepositoryMock = new Mock<IClientRepository>();
        _loggerMock = new Mock<ILogger<ClientService>>();

        _unitOfWorkMock.Setup(u => u.Clients).Returns(_clientRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new ClientService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region GetAllClientsAsync Tests

    [Fact]
    public async Task GetAllClientsAsync_WithNoClients_ReturnsEmptyCollection()
    {
        // Arrange
        _clientRepositoryMock.Setup(r => r.GetAllWithProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Client>());

        // Act
        var result = await _service.GetAllClientsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllClientsAsync_WithMultipleClients_ReturnsAllClients()
    {
        // Arrange
        var clients = new List<Client>
        {
            new()
            {
                Id = 1,
                Name = "Client A",
                Description = "Description A",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Client B",
                Description = "Description B",
                CreatedAt = DateTime.UtcNow
            }
        };

        _clientRepositoryMock.Setup(r => r.GetAllWithProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        // Act
        var result = await _service.GetAllClientsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Client A");
        result.Should().Contain(c => c.Name == "Client B");
    }

    [Fact]
    public async Task GetAllClientsAsync_WithProjects_MapsProjectsCorrectly()
    {
        // Arrange
        var client = new Client
        {
            Id = 1,
            Name = "Test Client",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            Projects = new List<Project>
            {
                new() { Id = 1, Name = "Project 1", ClientId = 1, CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Name = "Project 2", ClientId = 1, CreatedAt = DateTime.UtcNow }
            }
        };

        _clientRepositoryMock.Setup(r => r.GetAllWithProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Client> { client });

        // Act
        var result = await _service.GetAllClientsAsync();

        // Assert
        var clientDto = result.First();
        clientDto.Projects.Should().HaveCount(2);
        clientDto.Projects.Should().Contain(p => p.Name == "Project 1");
        clientDto.Projects.Should().Contain(p => p.Name == "Project 2");
    }

    #endregion

    #region GetClientByIdAsync Tests

    [Fact]
    public async Task GetClientByIdAsync_WithExistingId_ReturnsClient()
    {
        // Arrange
        var client = new Client
        {
            Id = 1,
            Name = "Test Client",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };

        _clientRepositoryMock.Setup(r => r.GetByIdWithProjectsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        var result = await _service.GetClientByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Client");
        result.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task GetClientByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _clientRepositoryMock.Setup(r => r.GetByIdWithProjectsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        var result = await _service.GetClientByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateClientAsync Tests

    [Fact]
    public async Task CreateClientAsync_WithValidData_CreatesAndReturnsClient()
    {
        // Arrange
        var createDto = new CreateClientDto
        {
            Name = "New Client",
            Description = "Client Description"
        };

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Client? capturedClient = null;
        _clientRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((c, ct) =>
            {
                c.Id = 1;
                capturedClient = c;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateClientAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("New Client");
        result.Description.Should().Be("Client Description");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        capturedClient.Should().NotBeNull();
        capturedClient!.Name.Should().Be("New Client");

        _clientRepositoryMock.Verify(r => r.ExistsWithNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()), Times.Once);
        _clientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateClientAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateClientDto
        {
            Name = "Existing Client",
            Description = "Description"
        };

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateClientAsync(createDto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A client with the name 'Existing Client' already exists");

        _clientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateClientAsync_WithNullDescription_CreatesClient()
    {
        // Arrange
        var createDto = new CreateClientDto
        {
            Name = "Client Without Description",
            Description = null
        };

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((c, ct) => c.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateClientAsync(createDto);

        // Assert
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateClientAsync_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var createDto = new CreateClientDto
        {
            Name = "Time Test Client",
            Description = "Test"
        };

        var beforeCreation = DateTime.UtcNow;

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync(createDto.Name, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((c, ct) => c.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateClientAsync(createDto);

        // Assert
        var afterCreation = DateTime.UtcNow;
        result.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        result.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    #endregion

    #region UpdateClientAsync Tests

    [Fact]
    public async Task UpdateClientAsync_WithValidData_UpdatesClient()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = 1,
            Name = "Old Name",
            Description = "Old Description",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var updateDto = new UpdateClientDto
        {
            Id = 1,
            Name = "Updated Name",
            Description = "Updated Description"
        };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync(updateDto.Name, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.UpdateClientAsync(updateDto);

        // Assert
        existingClient.Name.Should().Be("Updated Name");
        existingClient.Description.Should().Be("Updated Description");

        _clientRepositoryMock.Verify(r => r.Update(existingClient), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClientAsync_WithNonExistingClient_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateClientDto
        {
            Id = 999,
            Name = "Non-existing",
            Description = "Description"
        };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateClientAsync(updateDto))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Client with ID 999 not found");

        _clientRepositoryMock.Verify(r => r.Update(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClientAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingClient = new Client
        {
            Id = 1,
            Name = "Client A",
            Description = "Description",
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateClientDto
        {
            Id = 1,
            Name = "Client B", // Already exists for another client
            Description = "Updated Description"
        };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync("Client B", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateClientAsync(updateDto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A client with the name 'Client B' already exists");

        _clientRepositoryMock.Verify(r => r.Update(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClientAsync_WithSameName_AllowsUpdate()
    {
        // Arrange - Client keeping the same name should be allowed
        var existingClient = new Client
        {
            Id = 1,
            Name = "Same Name",
            Description = "Old Description",
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateClientDto
        {
            Id = 1,
            Name = "Same Name",
            Description = "New Description"
        };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync("Same Name", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Not duplicate because it's the same client

        // Act
        await _service.UpdateClientAsync(updateDto);

        // Assert
        existingClient.Description.Should().Be("New Description");
        _clientRepositoryMock.Verify(r => r.Update(existingClient), Times.Once);
    }

    #endregion

    #region DeleteClientAsync Tests

    [Fact]
    public async Task DeleteClientAsync_WithExistingClient_DeletesSuccessfully()
    {
        // Arrange
        var client = new Client
        {
            Id = 1,
            Name = "Client to Delete",
            Description = "Description",
            CreatedAt = DateTime.UtcNow
        };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        await _service.DeleteClientAsync(1);

        // Assert
        _clientRepositoryMock.Verify(r => r.Delete(client), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteClientAsync_WithNonExistingClient_ThrowsKeyNotFoundException()
    {
        // Arrange
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _service.DeleteClientAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Client with ID 999 not found");

        _clientRepositoryMock.Verify(r => r.Delete(It.IsAny<Client>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Business Rules Tests

    [Fact]
    public async Task CreateClientAsync_EnforcesUniqueNameConstraint()
    {
        // Arrange
        var createDto = new CreateClientDto
        {
            Name = "Duplicate Name",
            Description = "Test"
        };

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync("Duplicate Name", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateClientAsync(createDto))
            .Should().ThrowAsync<InvalidOperationException>();

        _clientRepositoryMock.Verify(r => r.ExistsWithNameAsync("Duplicate Name", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClientAsync_EnforcesUniqueNameConstraint_ExcludingCurrentClient()
    {
        // Arrange
        var existingClient = new Client { Id = 1, Name = "Client A", CreatedAt = DateTime.UtcNow };
        var updateDto = new UpdateClientDto { Id = 1, Name = "Client B" };

        _clientRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        _clientRepositoryMock.Setup(r => r.ExistsWithNameAsync("Client B", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateClientAsync(updateDto))
            .Should().ThrowAsync<InvalidOperationException>();

        // Verify that the check excluded the current client (ID 1)
        _clientRepositoryMock.Verify(r => r.ExistsWithNameAsync("Client B", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
