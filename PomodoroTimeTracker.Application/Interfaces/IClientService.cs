using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IClientService
{
    Task<IEnumerable<ClientDto>> GetAllClientsAsync();
    Task<ClientDto?> GetClientByIdAsync(int id);
    Task<ClientDto> CreateClientAsync(CreateClientDto dto);
    Task UpdateClientAsync(UpdateClientDto dto);
    Task DeleteClientAsync(int id);
}
