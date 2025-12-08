using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page showing work goals comparison.
/// </summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IGoalService _goalService;
    private readonly IClientService _clientService;
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;

    private bool _isLoading;
    private bool _hasSchedule;
    private ClientDto? _selectedClient;
    private ProjectDto? _selectedProject;
    private ObservableCollection<ClientDto> _clients = [];
    private ObservableCollection<ProjectDto> _projects = [];

    // Daily goal
    private double _dailyProgress;
    private decimal _dailyActualHours;
    private decimal _dailyExpectedHours;
    private decimal _dailyDifferenceHours;
    private bool _dailyIsAhead;

    // Weekly goal
    private double _weeklyProgress;
    private decimal _weeklyActualHours;
    private decimal _weeklyExpectedHours;
    private decimal _weeklyDifferenceHours;
    private bool _weeklyIsAhead;

    // Monthly goal
    private double _monthlyProgress;
    private decimal _monthlyActualHours;
    private decimal _monthlyExpectedHours;
    private decimal _monthlyDifferenceHours;
    private bool _monthlyIsAhead;

    public DashboardViewModel(
        IGoalService goalService,
        IClientService clientService,
        IProjectService projectService,
        IDialogService dialogService)
    {
        _goalService = goalService;
        _clientService = clientService;
        _projectService = projectService;
        _dialogService = dialogService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasSchedule
    {
        get => _hasSchedule;
        set
        {
            if (SetProperty(ref _hasSchedule, value))
            {
                OnPropertyChanged(nameof(ShowNoScheduleMessage));
            }
        }
    }

    public bool ShowNoScheduleMessage => !HasSchedule && !IsLoading;

    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    public ObservableCollection<ProjectDto> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public ClientDto? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                // When client changes, clear project and reload projects
                SelectedProject = null;
                _ = LoadProjectsForClientAsync();
                _ = RefreshAsync();
            }
        }
    }

    public ProjectDto? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    // Daily goal properties
    public double DailyProgress
    {
        get => _dailyProgress;
        set => SetProperty(ref _dailyProgress, value);
    }

    public decimal DailyActualHours
    {
        get => _dailyActualHours;
        set => SetProperty(ref _dailyActualHours, value);
    }

    public decimal DailyExpectedHours
    {
        get => _dailyExpectedHours;
        set => SetProperty(ref _dailyExpectedHours, value);
    }

    public decimal DailyDifferenceHours
    {
        get => _dailyDifferenceHours;
        set => SetProperty(ref _dailyDifferenceHours, value);
    }

    public bool DailyIsAhead
    {
        get => _dailyIsAhead;
        set => SetProperty(ref _dailyIsAhead, value);
    }

    // Weekly goal properties
    public double WeeklyProgress
    {
        get => _weeklyProgress;
        set => SetProperty(ref _weeklyProgress, value);
    }

    public decimal WeeklyActualHours
    {
        get => _weeklyActualHours;
        set => SetProperty(ref _weeklyActualHours, value);
    }

    public decimal WeeklyExpectedHours
    {
        get => _weeklyExpectedHours;
        set => SetProperty(ref _weeklyExpectedHours, value);
    }

    public decimal WeeklyDifferenceHours
    {
        get => _weeklyDifferenceHours;
        set => SetProperty(ref _weeklyDifferenceHours, value);
    }

    public bool WeeklyIsAhead
    {
        get => _weeklyIsAhead;
        set => SetProperty(ref _weeklyIsAhead, value);
    }

    // Monthly goal properties
    public double MonthlyProgress
    {
        get => _monthlyProgress;
        set => SetProperty(ref _monthlyProgress, value);
    }

    public decimal MonthlyActualHours
    {
        get => _monthlyActualHours;
        set => SetProperty(ref _monthlyActualHours, value);
    }

    public decimal MonthlyExpectedHours
    {
        get => _monthlyExpectedHours;
        set => SetProperty(ref _monthlyExpectedHours, value);
    }

    public decimal MonthlyDifferenceHours
    {
        get => _monthlyDifferenceHours;
        set => SetProperty(ref _monthlyDifferenceHours, value);
    }

    public bool MonthlyIsAhead
    {
        get => _monthlyIsAhead;
        set => SetProperty(ref _monthlyIsAhead, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadClientsAsync();
        await RefreshAsync();
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);
        }
        catch
        {
            // Silently fail - clients dropdown will be empty
        }
    }

    private async Task LoadProjectsForClientAsync()
    {
        try
        {
            if (SelectedClient is null)
            {
                // Load all projects if no client selected
                var allProjects = await _projectService.GetAllProjectsAsync();
                Projects = new ObservableCollection<ProjectDto>(allProjects);
            }
            else
            {
                var projects = await _projectService.GetProjectsByClientIdAsync(SelectedClient.Id);
                Projects = new ObservableCollection<ProjectDto>(projects);
            }
        }
        catch
        {
            // Silently fail - projects dropdown will be empty
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;

            var clientId = SelectedClient?.Id;
            var projectId = SelectedProject?.Id;
            var today = DateTime.Today;

            // Load all comparisons in parallel
            var dailyTask = _goalService.GetDailyComparisonAsync(clientId, projectId, today);
            var weeklyTask = _goalService.GetWeeklyComparisonAsync(clientId, projectId, today);
            var monthlyTask = _goalService.GetMonthlyComparisonAsync(clientId, projectId, today);

            await Task.WhenAll(dailyTask, weeklyTask, monthlyTask);

            var daily = await dailyTask;
            var weekly = await weeklyTask;
            var monthly = await monthlyTask;

            // Check if we have a schedule (null result means no schedule configured)
            HasSchedule = daily is not null || weekly is not null || monthly is not null;

            // Update daily (use defaults if no schedule)
            DailyProgress = daily is not null ? (double)daily.ProgressPercentage : 0;
            DailyActualHours = daily?.ActualHours ?? 0;
            DailyExpectedHours = daily?.ExpectedHours ?? 0;
            DailyDifferenceHours = daily?.DifferenceHours ?? 0;
            DailyIsAhead = (daily?.DifferenceHours ?? 0) >= 0;

            // Update weekly (use defaults if no schedule)
            WeeklyProgress = weekly is not null ? (double)weekly.ProgressPercentage : 0;
            WeeklyActualHours = weekly?.ActualHours ?? 0;
            WeeklyExpectedHours = weekly?.ExpectedHours ?? 0;
            WeeklyDifferenceHours = weekly?.DifferenceHours ?? 0;
            WeeklyIsAhead = (weekly?.DifferenceHours ?? 0) >= 0;

            // Update monthly (use defaults if no schedule)
            MonthlyProgress = monthly is not null ? (double)monthly.ProgressPercentage : 0;
            MonthlyActualHours = monthly?.ActualHours ?? 0;
            MonthlyExpectedHours = monthly?.ExpectedHours ?? 0;
            MonthlyDifferenceHours = monthly?.DifferenceHours ?? 0;
            MonthlyIsAhead = (monthly?.DifferenceHours ?? 0) >= 0;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load dashboard: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
