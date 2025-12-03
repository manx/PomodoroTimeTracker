using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// ViewModel for adding or editing a client.
/// </summary>
public sealed partial class ClientDetailViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private int? _clientId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isSaving;

    public ClientDetailViewModel(
        IClientService clientService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _clientService = clientService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsEditMode => _clientId.HasValue;

    public string PageTitle => IsEditMode ? "Edit Client" : "Add New Client";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Initialize the ViewModel for adding a new client
    /// </summary>
    public void InitializeForAdd()
    {
        _clientId = null;
        Name = string.Empty;
        Description = string.Empty;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    /// <summary>
    /// Initialize the ViewModel for editing an existing client
    /// </summary>
    public async Task InitializeForEditAsync(int clientId)
    {
        _clientId = clientId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        try
        {
            var client = await _clientService.GetClientByIdAsync(clientId);
            if (client != null)
            {
                Name = client.Name;
                Description = client.Description ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load client: {ex.Message}", "Error");
            Cancel();
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (IsEditMode)
            {
                // Update existing client
                var updateDto = new UpdateClientDto
                {
                    Id = _clientId!.Value,
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description
                };

                await _clientService.UpdateClientAsync(updateDto);
                // Store the ID so the list can select it
                _navigationService.ClientIdToSelect = _clientId.Value;
            }
            else
            {
                // Create new client
                var createDto = new CreateClientDto
                {
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description
                };

                var createdClient = await _clientService.CreateClientAsync(createDto);
                // Store the ID so the list can select it
                _navigationService.ClientIdToSelect = createdClient.Id;
            }

            // Navigate back to list
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save client: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) && !IsSaving;
    }

    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
