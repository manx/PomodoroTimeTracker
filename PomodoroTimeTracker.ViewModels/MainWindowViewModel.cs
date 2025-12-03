using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private string _title = "Pomodoro Time Tracker";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;

        // Initialize commands
        NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
        NavigateToPomodoroCommand = new RelayCommand(NavigateToPomodoro);
        NavigateToTimeEntryCommand = new RelayCommand(NavigateToTimeEntry);
        NavigateToClientsCommand = new RelayCommand(NavigateToClients);
        NavigateToProjectsCommand = new RelayCommand(NavigateToProjects);
        NavigateToStatisticsCommand = new RelayCommand(NavigateToStatistics);
    }

    // Navigation Commands
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToPomodoroCommand { get; }
    public ICommand NavigateToTimeEntryCommand { get; }
    public ICommand NavigateToClientsCommand { get; }
    public ICommand NavigateToProjectsCommand { get; }
    public ICommand NavigateToStatisticsCommand { get; }

    // Navigation methods (will be implemented as we create the views)
    private async void NavigateToDashboard()
    {
        await _dialogService.ShowInformationAsync("Dashboard view not yet implemented", "Coming Soon");
    }

    private async void NavigateToPomodoro()
    {
        await _dialogService.ShowInformationAsync("Pomodoro timer view not yet implemented", "Coming Soon");
    }

    private async void NavigateToTimeEntry()
    {
        await _dialogService.ShowInformationAsync("Time entry view not yet implemented", "Coming Soon");
    }

    private void NavigateToClients()
    {
        _navigationService.NavigateTo(PageNames.ClientList);
    }

    private void NavigateToProjects()
    {
        _navigationService.NavigateTo(PageNames.ProjectList);
    }

    private async void NavigateToStatistics()
    {
        await _dialogService.ShowInformationAsync("Statistics view not yet implemented", "Coming Soon");
    }
}
