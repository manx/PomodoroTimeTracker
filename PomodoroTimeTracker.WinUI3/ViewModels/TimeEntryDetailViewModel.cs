using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.WinUI3.Services;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for adding or editing a time entry.
/// </summary>
internal partial class TimeEntryDetailViewModel : ViewModelBase
{
    private readonly ITimeEntryService _timeEntryService;
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private int? _timeEntryId;
    private string _description = string.Empty;
    private int? _selectedProjectId;
    private ObservableCollection<ProjectDto> _projects = new();
    private DateTimeOffset _startDate = DateTimeOffset.Now;
    private TimeSpan _startTime = DateTime.Now.TimeOfDay;
    private DateTimeOffset? _endDate;
    private TimeSpan? _endTime;
    private bool _isSaving;
    private bool _isLoading;

    public TimeEntryDetailViewModel(
        ITimeEntryService timeEntryService,
        IProjectService projectService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _timeEntryService = timeEntryService;
        _projectService = projectService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public int? SelectedProjectId
    {
        get => _selectedProjectId;
        set => SetProperty(ref _selectedProjectId, value);
    }

    public ObservableCollection<ProjectDto> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public DateTimeOffset StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
            {
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
    }

    public DateTimeOffset? EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public TimeSpan? EndTime
    {
        get => _endTime;
        set
        {
            if (SetProperty(ref _endTime, value))
            {
                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public string DurationDisplay
    {
        get
        {
            if (!EndDate.HasValue || !EndTime.HasValue)
                return "Running...";

            var startDateTime = StartDate.Date.Add(StartTime);
            var endDateTime = EndDate.Value.Date.Add(EndTime.Value);

            if (endDateTime <= startDateTime)
                return "Invalid (end before start)";

            var duration = endDateTime - startDateTime;
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;

            if (hours > 0)
                return $"{hours}h {minutes}m";
            else
                return $"{minutes}m";
        }
    }

    public bool IsRunning => !EndDate.HasValue || !EndTime.HasValue;

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public bool IsEditMode => _timeEntryId.HasValue;

    // Non-nullable wrappers for DatePicker/TimePicker binding
    public DateTimeOffset EndDateValue
    {
        get => _endDate ?? DateTimeOffset.Now;
        set
        {
            EndDate = value;
        }
    }

    public TimeSpan EndTimeValue
    {
        get => _endTime ?? DateTime.Now.TimeOfDay;
        set
        {
            EndTime = value;
        }
    }

    public string PageTitle => IsEditMode ? "Edit Time Entry" : "Add Time Entry";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public async Task InitializeForAddAsync()
    {
        _timeEntryId = null;
        Description = string.Empty;
        SelectedProjectId = null;
        StartDate = DateTimeOffset.Now;
        StartTime = DateTime.Now.TimeOfDay;
        EndDate = DateTimeOffset.Now;
        EndTime = DateTime.Now.TimeOfDay;

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DurationDisplay));
        OnPropertyChanged(nameof(IsRunning));

        await LoadProjectsAsync();
    }

    public async Task InitializeForEditAsync(int timeEntryId)
    {
        _timeEntryId = timeEntryId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        await LoadProjectsAsync();

        try
        {
            IsLoading = true;
            var entry = await _timeEntryService.GetTimeEntryByIdAsync(timeEntryId);
            if (entry == null)
            {
                await _dialogService.ShowErrorAsync("Failed to load time entry. Entry not found.", "Error");
                Cancel();
                return;
            }

            Description = entry.Description;
            SelectedProjectId = entry.ProjectId;
            StartDate = new DateTimeOffset(entry.StartTime);
            StartTime = entry.StartTime.TimeOfDay;

            if (entry.EndTime.HasValue)
            {
                EndDate = new DateTimeOffset(entry.EndTime.Value);
                EndTime = entry.EndTime.Value.TimeOfDay;
            }
            else
            {
                EndDate = null;
                EndTime = null;
            }

            OnPropertyChanged(nameof(DurationDisplay));
            OnPropertyChanged(nameof(IsRunning));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load time entry: {ex.Message}", "Error");
            Cancel();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects = await _projectService.GetAllProjectsAsync();
            Projects = new ObservableCollection<ProjectDto>(projects);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load projects: {ex.Message}", "Error");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            var startDateTime = StartDate.Date.Add(StartTime);
            DateTime? endDateTime = null;
            int? durationMinutes = null;

            // Validate that end is after start (if end is specified)
            if (EndDate.HasValue && EndTime.HasValue)
            {
                endDateTime = EndDate.Value.Date.Add(EndTime.Value);

                if (endDateTime.Value <= startDateTime)
                {
                    await _dialogService.ShowErrorAsync(
                        "End time must be after start time.",
                        "Invalid Time Range");
                    return;
                }

                durationMinutes = (int)(endDateTime.Value - startDateTime).TotalMinutes;
            }

            if (IsEditMode)
            {
                var updateDto = new UpdateTimeEntryDto
                {
                    Id = _timeEntryId!.Value,
                    Description = Description,
                    ProjectId = SelectedProjectId,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    DurationMinutes = durationMinutes
                };

                await _timeEntryService.UpdateTimeEntryAsync(updateDto);
                _navigationService.TimeEntryIdToSelect = _timeEntryId.Value;
            }
            else
            {
                var createDto = new CreateTimeEntryDto
                {
                    Description = Description,
                    ProjectId = SelectedProjectId
                };

                var createdEntry = await _timeEntryService.CreateTimeEntryAsync(createDto, startDateTime, endDateTime);
                _navigationService.TimeEntryIdToSelect = createdEntry.Id;
            }

            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save time entry: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Description) && !IsSaving;
    }

    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
