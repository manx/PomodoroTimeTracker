using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.WinUI3.Services;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for the project list view.
/// </summary>
internal partial class ProjectListViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private ObservableCollection<ProjectDto> _projects = new();
    private ProjectDto? _selectedProject;
    private bool _isLoading;

    public ProjectListViewModel(
        IProjectService projectService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _projectService = projectService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
        AddProjectCommand = new RelayCommand(AddProject);
        EditProjectCommand = new RelayCommand(EditProject, CanEditOrDelete);
        DeleteProjectCommand = new AsyncRelayCommand(DeleteProjectAsync, CanEditOrDelete);
        RefreshCommand = new AsyncRelayCommand(LoadProjectsAsync);

        _ = LoadProjectsAsync();
    }

    public ObservableCollection<ProjectDto> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public ProjectDto? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                ((RelayCommand)EditProjectCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)DeleteProjectCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoadProjectsCommand { get; }
    public ICommand AddProjectCommand { get; }
    public ICommand EditProjectCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand RefreshCommand { get; }

    private async Task LoadProjectsAsync()
    {
        try
        {
            IsLoading = true;
            var projects = await _projectService.GetAllProjectsAsync();
            Projects = new ObservableCollection<ProjectDto>(projects);

            // Select project if one was just saved (created or updated)
            if (_navigationService.ProjectIdToSelect.HasValue)
            {
                var projectToSelect = Projects.FirstOrDefault(p => p.Id == _navigationService.ProjectIdToSelect.Value);
                if (projectToSelect != null)
                {
                    SelectedProject = projectToSelect;
                }
                _navigationService.ProjectIdToSelect = null; // Clear the flag
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load projects: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddProject()
    {
        _navigationService.NavigateTo(typeof(Views.ProjectDetailPage), null);
    }

    private void EditProject()
    {
        if (SelectedProject != null)
        {
            _navigationService.NavigateTo(typeof(Views.ProjectDetailPage), SelectedProject.Id);
        }
    }

    private async Task DeleteProjectAsync()
    {
        if (SelectedProject == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete project '{SelectedProject.Name}'?\n\nThis will also remove all associated time entries and Pomodoro sessions.",
            "Confirm Delete");

        if (confirmed)
        {
            try
            {
                await _projectService.DeleteProjectAsync(SelectedProject.Id);
                await _dialogService.ShowInformationAsync($"Project '{SelectedProject.Name}' deleted successfully.", "Success");
                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"Failed to delete project: {ex.Message}", "Error");
            }
        }
    }

    private bool CanEditOrDelete()
    {
        return SelectedProject != null;
    }
}
