using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.WinUI3.Services;
using IDispatcherTimer = PomodoroTimeTracker.Application.Interfaces.IDispatcherTimer;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// Display item for a single time entry in the list.
/// </summary>
internal class TimeEntryDisplayItem
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ProjectDisplay { get; set; } = string.Empty;
    public string TimeRangeDisplay { get; set; } = string.Empty;
    public string DurationDisplay { get; set; } = string.Empty;
}

/// <summary>
/// Helper DTO for grouping time entries by date.
/// </summary>
internal class TimeEntryGroupDto
{
    public string DateHeader { get; set; } = string.Empty; // "Today", "Yesterday", "2025-01-20"
    public DateTime Date { get; set; }
    public ObservableCollection<TimeEntryDisplayItem> Entries { get; set; } = new();
}

/// <summary>
/// ViewModel for the time entry list view.
/// </summary>
internal partial class TimeEntryListViewModel : ViewModelBase
{
    private readonly ITimeEntryService _timeEntryService;
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IDispatcherTimer _timer;

    private ObservableCollection<TimeEntryGroupDto> _groupedEntries = new();
    private TimeEntryDto? _selectedEntry;
    private TimeEntryDto? _activeTimeEntry;
    private bool _isLoading;
    private string _activeTimerDisplay = string.Empty;

    public TimeEntryListViewModel(
        ITimeEntryService timeEntryService,
        IProjectService projectService,
        IDialogService dialogService,
        INavigationService navigationService,
        IDispatcherTimer timer)
    {
        _timeEntryService = timeEntryService;
        _projectService = projectService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _timer = timer;

        // Set up timer for active entry display
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        StartNewEntryCommand = new AsyncRelayCommand(StartNewEntryAsync);
        StopActiveEntryCommand = new AsyncRelayCommand(StopActiveEntryAsync, () => HasActiveEntry);
        AddManualEntryCommand = new RelayCommand(AddManualEntry);
        EditEntryCommand = new RelayCommand(EditEntry, CanEditOrDelete);
        DeleteEntryCommand = new AsyncRelayCommand(DeleteEntryAsync, CanEditOrDelete);
        RefreshCommand = new AsyncRelayCommand(LoadEntriesAsync);

        _ = LoadEntriesAsync();
    }

    public ObservableCollection<TimeEntryGroupDto> GroupedEntries
    {
        get => _groupedEntries;
        set => SetProperty(ref _groupedEntries, value);
    }

    public TimeEntryDto? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                ((RelayCommand)EditEntryCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)DeleteEntryCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public TimeEntryDto? ActiveTimeEntry
    {
        get => _activeTimeEntry;
        set
        {
            if (SetProperty(ref _activeTimeEntry, value))
            {
                OnPropertyChanged(nameof(HasActiveEntry));
                OnPropertyChanged(nameof(ActiveEntryDescription));
                OnPropertyChanged(nameof(ActiveEntryProjectName));
                ((AsyncRelayCommand)StopActiveEntryCommand).NotifyCanExecuteChanged();

                // Start or stop timer based on active entry
                if (value != null)
                {
                    UpdateActiveTimerDisplay();
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                    ActiveTimerDisplay = string.Empty;
                }
            }
        }
    }

    public string ActiveTimerDisplay
    {
        get => _activeTimerDisplay;
        set => SetProperty(ref _activeTimerDisplay, value);
    }

    public bool HasActiveEntry => ActiveTimeEntry != null;

    public bool HasActiveEntryProject => !string.IsNullOrEmpty(ActiveTimeEntry?.ProjectName);

    public string ActiveEntryDescription => ActiveTimeEntry?.Description ?? string.Empty;

    public string ActiveEntryProjectName => ActiveTimeEntry?.ProjectName ?? string.Empty;

    public bool IsEmpty => !IsLoading && GroupedEntries.Count == 0 && !HasActiveEntry;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand StartNewEntryCommand { get; }
    public ICommand StopActiveEntryCommand { get; }
    public ICommand AddManualEntryCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand RefreshCommand { get; }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateActiveTimerDisplay();
    }

    private void UpdateActiveTimerDisplay()
    {
        if (ActiveTimeEntry == null)
        {
            ActiveTimerDisplay = string.Empty;
            return;
        }

        var elapsed = DateTime.Now - ActiveTimeEntry.StartTime;
        ActiveTimerDisplay = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private async Task LoadEntriesAsync()
    {
        try
        {
            IsLoading = true;

            // Load all entries and the active entry
            var allEntries = await _timeEntryService.GetAllTimeEntriesAsync();
            var activeEntry = await _timeEntryService.GetActiveTimeEntryAsync();

            ActiveTimeEntry = activeEntry;

            // Group entries by date
            var grouped = allEntries
                .Where(e => e.EndTime.HasValue) // Only include completed entries in the list
                .GroupBy(e => e.StartTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new TimeEntryGroupDto
                {
                    Date = g.Key,
                    DateHeader = GetDateHeader(g.Key),
                    Entries = new ObservableCollection<TimeEntryDisplayItem>(
                        g.OrderByDescending(e => e.StartTime)
                         .Select(e => new TimeEntryDisplayItem
                         {
                             Id = e.Id,
                             Description = e.Description,
                             ProjectDisplay = e.ProjectName ?? "(No project)",
                             TimeRangeDisplay = FormatTimeRange(e.StartTime, e.EndTime),
                             DurationDisplay = FormatDuration(e.DurationMinutes)
                         }))
                })
                .ToList();

            GroupedEntries = new ObservableCollection<TimeEntryGroupDto>(grouped);
            OnPropertyChanged(nameof(IsEmpty));

            // Clear selection flag (selection not used in grouped view)
            _navigationService.TimeEntryIdToSelect = null;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load time entries: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GetDateHeader(DateTime date)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (date.Date == today)
            return "Today";
        if (date.Date == yesterday)
            return "Yesterday";

        return date.ToString("yyyy-MM-dd");
    }

    private static string FormatTimeRange(DateTime start, DateTime? end)
    {
        if (!end.HasValue)
            return $"{start:HH:mm} - running";

        return $"{start:HH:mm} - {end.Value:HH:mm}";
    }

    private static string FormatDuration(int? minutes)
    {
        if (!minutes.HasValue || minutes.Value == 0)
            return "0m";

        var hours = minutes.Value / 60;
        var mins = minutes.Value % 60;

        if (hours > 0)
            return $"{hours}h {mins}m";

        return $"{mins}m";
    }

    public Func<Task<string?>>? ShowStartEntryDialog { get; set; }

    private async Task StartNewEntryAsync()
    {
        try
        {
            // Check if there's already an active entry
            if (ActiveTimeEntry != null)
            {
                await _dialogService.ShowErrorAsync(
                    "You already have an active time entry. Please stop it before starting a new one.",
                    "Active Entry Exists");
                return;
            }

            // Show dialog to get description
            if (ShowStartEntryDialog == null)
            {
                await _dialogService.ShowErrorAsync(
                    "Unable to show start dialog. Please use Add Manual instead.",
                    "Error");
                return;
            }

            var description = await ShowStartEntryDialog();
            if (string.IsNullOrWhiteSpace(description))
                return; // User cancelled

            // Start new entry
            var createDto = new CreateTimeEntryDto { Description = description };
            var newEntry = await _timeEntryService.StartTimeEntryAsync(createDto);

            ActiveTimeEntry = newEntry;
            await LoadEntriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to start time entry: {ex.Message}", "Error");
        }
    }

    private async Task StopActiveEntryAsync()
    {
        if (ActiveTimeEntry == null)
            return;

        try
        {
            await _timeEntryService.StopTimeEntryAsync(ActiveTimeEntry.Id);
            await _dialogService.ShowInformationAsync("Time entry stopped successfully.", "Success");
            await LoadEntriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to stop time entry: {ex.Message}", "Error");
        }
    }

    private void AddManualEntry()
    {
        _navigationService.NavigateTo(typeof(Views.TimeEntryDetailPage), null);
    }

    private void EditEntry()
    {
        if (SelectedEntry != null)
        {
            _navigationService.NavigateTo(typeof(Views.TimeEntryDetailPage), SelectedEntry.Id);
        }
    }

    private async Task DeleteEntryAsync()
    {
        if (SelectedEntry == null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete this time entry?\n\nDescription: {SelectedEntry.Description}\nDuration: {SelectedEntry.DurationMinutes ?? 0} minutes",
            "Confirm Delete");

        if (confirmed)
        {
            try
            {
                await _timeEntryService.DeleteTimeEntryAsync(SelectedEntry.Id);
                await _dialogService.ShowInformationAsync("Time entry deleted successfully.", "Success");
                await LoadEntriesAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"Failed to delete time entry: {ex.Message}", "Error");
            }
        }
    }

    private bool CanEditOrDelete()
    {
        return SelectedEntry != null;
    }
}
