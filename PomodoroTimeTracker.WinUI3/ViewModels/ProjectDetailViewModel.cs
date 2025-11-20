using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.WinUI3.Services;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for adding or editing a project.
/// </summary>
public partial class ProjectDetailViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IClientService _clientService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private int? _projectId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private ClientDto? _selectedClient;
    private ObservableCollection<ClientDto> _clients = new();
    private bool _isSaving;
    private bool _isLoading;

    public ProjectDetailViewModel(
        IProjectService projectService,
        IClientService clientService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _projectService = projectService;
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

    public ClientDto? SelectedClient
    {
        get => _selectedClient;
        set => SetProperty(ref _selectedClient, value);
    }

    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditMode => _projectId.HasValue;

    public string PageTitle => IsEditMode ? "Edit Project" : "Add New Project";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeForAddAsync()
    {
        _projectId = null;
        Name = string.Empty;
        Description = string.Empty;
        SelectedClient = null;

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        await LoadClientsAsync();
    }

    public async Task InitializeForEditAsync(int projectId)
    {
        _projectId = projectId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        await LoadClientsAsync();

        try
        {
            IsLoading = true;
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project != null)
            {
                Name = project.Name;
                Description = project.Description ?? string.Empty;

                if (project.ClientId.HasValue)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == project.ClientId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load project: {ex.Message}", "Error");
            Cancel();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load clients: {ex.Message}", "Error");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (IsEditMode)
            {
                var updateDto = new UpdateProjectDto
                {
                    Id = _projectId!.Value,
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                    ClientId = SelectedClient?.Id
                };

                await _projectService.UpdateProjectAsync(updateDto);
                await _dialogService.ShowInformationAsync("Project updated successfully.", "Success");
            }
            else
            {
                var createDto = new CreateProjectDto
                {
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                    ClientId = SelectedClient?.Id
                };

                await _projectService.CreateProjectAsync(createDto);
                await _dialogService.ShowInformationAsync("Project created successfully.", "Success");
            }

            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save project: {ex.Message}", "Error");
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
