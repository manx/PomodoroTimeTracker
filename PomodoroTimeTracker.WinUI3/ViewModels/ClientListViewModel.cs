using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.WinUI3.Services;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for the client list view.
/// </summary>
internal partial class ClientListViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ClientListViewModel> _logger;

    private ObservableCollection<ClientDto> _clients = new();
    private ClientDto? _selectedClient;
    private bool _isLoading;

    public ClientListViewModel(
        IClientService clientService,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger<ClientListViewModel> logger)
    {
        _clientService = clientService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;

        // Initialize commands
        LoadClientsCommand = new AsyncRelayCommand(LoadClientsAsync);
        AddClientCommand = new RelayCommand(AddClient);
        EditClientCommand = new RelayCommand(EditClient, CanEditOrDelete);
        DeleteClientCommand = new AsyncRelayCommand(DeleteClientAsync, CanEditOrDelete);
        RefreshCommand = new AsyncRelayCommand(LoadClientsAsync);

        // Load clients on initialization
        _ = LoadClientsAsync();
    }

    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    public ClientDto? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                // Notify commands that CanExecute may have changed
                ((RelayCommand)EditClientCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)DeleteClientCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // Commands
    public ICommand LoadClientsCommand { get; }
    public ICommand AddClientCommand { get; }
    public ICommand EditClientCommand { get; }
    public ICommand DeleteClientCommand { get; }
    public ICommand RefreshCommand { get; }

    private async Task LoadClientsAsync()
    {
        try
        {
            _logger.LogInformation("Loading clients in UI");
            IsLoading = true;
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);
            _logger.LogInformation("Successfully loaded {Count} clients in UI", Clients.Count);

            // Select client if one was just saved (created or updated)
            if (_navigationService.ClientIdToSelect.HasValue)
            {
                var clientToSelect = Clients.FirstOrDefault(c => c.Id == _navigationService.ClientIdToSelect.Value);
                if (clientToSelect != null)
                {
                    SelectedClient = clientToSelect;
                    _logger.LogInformation("Selected client {ClientId}", clientToSelect.Id);
                }
                _navigationService.ClientIdToSelect = null; // Clear the flag
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients in UI");
            await _dialogService.ShowErrorAsync(
                "Unable to load clients. Please try again or contact support if the problem persists.",
                "Error Loading Clients");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddClient()
    {
        // Navigate to client detail view in "add" mode
        _navigationService.NavigateTo(typeof(Views.ClientDetailPage), null);
    }

    private void EditClient()
    {
        if (SelectedClient != null)
        {
            // Navigate to client detail view in "edit" mode
            _navigationService.NavigateTo(typeof(Views.ClientDetailPage), SelectedClient.Id);
        }
    }

    private async Task DeleteClientAsync()
    {
        if (SelectedClient == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete client '{SelectedClient.Name}'?\n\nThis will also remove all associated projects.",
            "Confirm Delete");

        if (confirmed)
        {
            try
            {
                _logger.LogInformation("User confirmed deletion of client {ClientId}: {ClientName}", SelectedClient.Id, SelectedClient.Name);
                await _clientService.DeleteClientAsync(SelectedClient.Id);
                await _dialogService.ShowInformationAsync($"Client '{SelectedClient.Name}' deleted successfully.", "Success");
                await LoadClientsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client {ClientId} in UI", SelectedClient.Id);
                await _dialogService.ShowErrorAsync(
                    "Unable to delete the client. Please try again or contact support if the problem persists.",
                    "Error Deleting Client");
            }
        }
        else
        {
            _logger.LogInformation("User cancelled deletion of client {ClientId}", SelectedClient.Id);
        }
    }

    private bool CanEditOrDelete()
    {
        return SelectedClient != null;
    }
}
