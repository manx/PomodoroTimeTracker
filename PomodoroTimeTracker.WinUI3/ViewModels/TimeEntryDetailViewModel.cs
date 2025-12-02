using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
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
    private DateTimeOffset? _startDate = DateTimeOffset.Now;
    private TimeSpan _startTime = DateTime.Now.TimeOfDay;
    private int _startHour = DateTime.Now.Hour;
    private int _startMinute = DateTime.Now.Minute;
    private DateTimeOffset? _endDate;
    private TimeSpan? _endTime;
    private int _endHour = DateTime.Now.Hour;
    private int _endMinute = DateTime.Now.Minute;
    private bool _isSaving;
    private bool _isLoading;

    private static readonly IReadOnlyList<int> HoursList = Enumerable.Range(0, 24).ToList();
    private static readonly IReadOnlyList<int> MinutesList = Enumerable.Range(0, 60).ToList();

    /// <summary>Hours 0-23 for ComboBox binding.</summary>
    public IReadOnlyList<int> Hours => HoursList;

    /// <summary>Minutes 0-59 for ComboBox binding.</summary>
    public IReadOnlyList<int> Minutes => MinutesList;

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

    public DateTimeOffset? StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(IsEndDateInvalid));
                OnPropertyChanged(nameof(EndDateForeground));
                OnPropertyChanged(nameof(IsEndTimeInvalid));
                OnPropertyChanged(nameof(EndTimeForeground));
            }
        }
    }

    /// <summary>
    /// True when End Date is before Start Date.
    /// </summary>
    public bool IsEndDateInvalid =>
        StartDate.HasValue &&
        EndDate.HasValue &&
        EndDate.Value.Date < StartDate.Value.Date;

    /// <summary>
    /// Returns red brush when end date is invalid, otherwise default foreground.
    /// </summary>
    public SolidColorBrush EndDateForeground =>
        IsEndDateInvalid
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.White);

    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
            {
                // Sync StartHour/StartMinute fields
                _startHour = value.Hours;
                _startMinute = value.Minutes;
                OnPropertyChanged(nameof(StartHour));
                OnPropertyChanged(nameof(StartMinute));

                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(IsEndTimeInvalid));
                OnPropertyChanged(nameof(EndTimeForeground));
            }
        }
    }

    /// <summary>
    /// Start hour (0-23) for ComboBox binding. Updates StartTime when changed.
    /// </summary>
    public int StartHour
    {
        get => _startHour;
        set
        {
            if (SetProperty(ref _startHour, value))
            {
                StartTime = new TimeSpan(_startHour, _startMinute, 0);
            }
        }
    }

    /// <summary>
    /// Start minute (0-59) for ComboBox binding. Updates StartTime when changed.
    /// </summary>
    public int StartMinute
    {
        get => _startMinute;
        set
        {
            if (SetProperty(ref _startMinute, value))
            {
                StartTime = new TimeSpan(_startHour, _startMinute, 0);
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
                OnPropertyChanged(nameof(IsEndDateInvalid));
                OnPropertyChanged(nameof(EndDateForeground));
                OnPropertyChanged(nameof(IsEndTimeInvalid));
                OnPropertyChanged(nameof(EndTimeForeground));
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
                // Sync EndHour/EndMinute fields (without triggering their setters)
                if (value.HasValue)
                {
                    _endHour = value.Value.Hours;
                    _endMinute = value.Value.Minutes;
                    OnPropertyChanged(nameof(EndHour));
                    OnPropertyChanged(nameof(EndMinute));
                }

                OnPropertyChanged(nameof(DurationDisplay));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsEndTimeInvalid));
                OnPropertyChanged(nameof(EndTimeForeground));
            }
        }
    }

    public string DurationDisplay
    {
        get
        {
            if (!StartDate.HasValue || !EndDate.HasValue || !EndTime.HasValue)
                return "Running...";

            var startDateTime = StartDate.Value.Date.Add(StartTime);
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

    /// <summary>
    /// True when End Time is before or equal to Start Time on the same day.
    /// </summary>
    public bool IsEndTimeInvalid =>
        StartDate.HasValue &&
        EndDate.HasValue &&
        EndTime.HasValue &&
        StartDate.Value.Date == EndDate.Value.Date &&
        EndTime.Value <= StartTime;

    /// <summary>
    /// Returns red brush when end time is invalid, otherwise default foreground.
    /// </summary>
    public SolidColorBrush EndTimeForeground =>
        IsEndTimeInvalid
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.White);

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

    // Non-nullable wrapper for TimePicker binding (TimePicker doesn't support nullable)
    public TimeSpan EndTimeValue
    {
        get => _endTime ?? DateTime.Now.TimeOfDay;
        set
        {
            EndTime = value;
        }
    }

    /// <summary>
    /// End hour (0-23) for ComboBox binding. Updates EndTime when changed.
    /// </summary>
    public int EndHour
    {
        get => _endHour;
        set
        {
            if (SetProperty(ref _endHour, value))
            {
                // Update EndTime from hour/minute
                EndTime = new TimeSpan(_endHour, _endMinute, 0);
            }
        }
    }

    /// <summary>
    /// End minute (0-59) for ComboBox binding. Updates EndTime when changed.
    /// </summary>
    public int EndMinute
    {
        get => _endMinute;
        set
        {
            if (SetProperty(ref _endMinute, value))
            {
                // Update EndTime from hour/minute
                EndTime = new TimeSpan(_endHour, _endMinute, 0);
            }
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

            if (!StartDate.HasValue)
            {
                await _dialogService.ShowErrorAsync("Start date is required.", "Missing Date");
                return;
            }

            var startDateTime = StartDate.Value.Date.Add(StartTime);
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
