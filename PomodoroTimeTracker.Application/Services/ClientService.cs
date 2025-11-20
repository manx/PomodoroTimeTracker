using Microsoft.Extensions.Logging;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class ClientService(IUnitOfWork unitOfWork, ILogger<ClientService> logger) : IClientService
{
    public async Task<IEnumerable<ClientDto>> GetAllClientsAsync()
    {
        try
        {
            logger.LogInformation("Retrieving all clients");
            var clients = await unitOfWork.Clients.GetAllWithProjectsAsync();
            logger.LogInformation("Retrieved {Count} clients", clients.Count());
            return clients.Select(MapToDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all clients");
            throw;
        }
    }

    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
        var client = await unitOfWork.Clients.GetByIdWithProjectsAsync(id);
        return client == null ? null : MapToDto(client);
    }

    public async Task<ClientDto> CreateClientAsync(CreateClientDto dto)
    {
        try
        {
            logger.LogInformation("Creating new client: {ClientName}", dto.Name);

            // Business rule: Client name must be unique
            if (await unitOfWork.Clients.ExistsWithNameAsync(dto.Name))
            {
                logger.LogWarning("Attempt to create client with duplicate name: {ClientName}", dto.Name);
                throw new InvalidOperationException($"A client with the name '{dto.Name}' already exists");
            }

            var client = new Client
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Clients.AddAsync(client);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Successfully created client {ClientId}: {ClientName}", client.Id, client.Name);
            return MapToDto(client);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Error creating client: {ClientName}", dto.Name);
            throw;
        }
    }

    public async Task UpdateClientAsync(UpdateClientDto dto)
    {
        try
        {
            logger.LogInformation("Updating client {ClientId}: {ClientName}", dto.Id, dto.Name);

            var client = await unitOfWork.Clients.GetByIdAsync(dto.Id);
            if (client == null)
            {
                logger.LogWarning("Attempt to update non-existent client {ClientId}", dto.Id);
                throw new KeyNotFoundException($"Client with ID {dto.Id} not found");
            }

            // Business rule: Client name must be unique (exclude current client)
            if (await unitOfWork.Clients.ExistsWithNameAsync(dto.Name, dto.Id))
            {
                logger.LogWarning("Attempt to update client {ClientId} with duplicate name: {ClientName}", dto.Id, dto.Name);
                throw new InvalidOperationException($"A client with the name '{dto.Name}' already exists");
            }

            client.Name = dto.Name;
            client.Description = dto.Description;

            unitOfWork.Clients.Update(client);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Successfully updated client {ClientId}", dto.Id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            logger.LogError(ex, "Error updating client {ClientId}", dto.Id);
            throw;
        }
    }

    public async Task DeleteClientAsync(int id)
    {
        try
        {
            logger.LogInformation("Deleting client {ClientId}", id);

            var client = await unitOfWork.Clients.GetByIdAsync(id);
            if (client == null)
            {
                logger.LogWarning("Attempt to delete non-existent client {ClientId}", id);
                throw new KeyNotFoundException($"Client with ID {id} not found");
            }

            unitOfWork.Clients.Delete(client);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Successfully deleted client {ClientId}", id);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "Error deleting client {ClientId}", id);
            throw;
        }
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Description = client.Description,
            CreatedAt = client.CreatedAt,
            Projects = client.Projects?.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ClientId = p.ClientId,
                CreatedAt = p.CreatedAt
            }).ToList()
        };
    }
}
