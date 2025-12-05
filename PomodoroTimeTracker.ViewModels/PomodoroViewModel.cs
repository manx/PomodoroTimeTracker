using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Represents the current state of the Pomodoro timer.
/// </summary>
public enum PomodoroState
{
    /// <summary>
    /// Configuring the session before starting (initial state).
    /// </summary>
    Setup,

    /// <summary>
    /// Timer is actively counting down.
    /// </summary>
    Running,

    /// <summary>
    /// Timer is paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// In wrap up period - work time has ended but user can continue to finish up.
    /// </summary>
    WrapUp,

    /// <summary>
    /// In an automatic break period (short or long).
    /// </summary>
    Break
}

/// <summary>
/// Represents the user's choice when stopping a session early.
/// </summary>
public enum StopDialogResult
{
    /// <summary>
    /// Resume the session as if it was only paused.
    /// </summary>
    Resume,

    /// <summary>
    /// Save the partial session with current progress.
    /// </summary>
    Save,

    /// <summary>
    /// Discard the session entirely without saving.
    /// </summary>
    Discard
}

/// <summary>
/// ViewModel for the Pomodoro timer page.
/// Manages timer state, break cycles, and session tracking.
/// Implements the complete Pomodoro workflow: Work → Short Break → ... → Long Break → repeat.
/// </summary>
public sealed partial class PomodoroViewModel : TimerViewModelBase
{
    /// <summary>
    /// Maximum length for the objective text field.
    /// TODO: Move to configuration/settings in the future.
    /// </summary>
    public const int ObjectiveMaxLength = 90;

    private readonly IPomodoroSettingsService _settingsService;
    private readonly IAudioService _audioService;

    private PomodoroState _state = PomodoroState.Setup;
    private int _remainingSeconds;
    private int _totalSeconds;
    private int _pomodoroCount = 0; // Tracks which pomodoro we're on (0-3)

    private int _workDurationSeconds; // The original work duration (without grace period)
    private PomodoroSettingsDto? _settings;

    // Setup screen properties
    private string _objective = string.Empty;
    private int _durationMinutes;

    // Timer display properties
    private double _progressPercentage;
    private bool _isBreakBillable;

    /// <summary>
    /// Initializes a new instance of the <see cref="PomodoroViewModel"/> class.
    /// </summary>
    /// <param name="entryService">Service for managing time entries.</param>
    /// <param name="settingsService">Service for managing Pomodoro settings.</param>
    /// <param name="clientService">Service for managing clients.</param>
    /// <param name="projectService">Service for managing projects.</param>
    /// <param name="audioService">Service for playing audio notifications.</param>
    /// <param name="activeTimerService">Service for coordinating active timer state.</param>
    /// <param name="pomodoroStateService">Service for managing Pomodoro cycle state.</param>
    /// <param name="timer">Timer for updating UI every second.</param>
    public PomodoroViewModel(
        ITimeEntryService entryService,
        IPomodoroSettingsService settingsService,
        IClientService clientService,
        IProjectService projectService,
        IAudioService audioService,
        IActiveTimerService activeTimerService,
        IPomodoroStateService pomodoroStateService,
        IDispatcherTimer timer)
        : base(entryService, clientService, projectService, activeTimerService, pomodoroStateService, timer)
    {
        _settingsService = settingsService;
        _audioService = audioService;

        Timer.Tick += Timer_Tick;

        StartPomodoroCommand = new AsyncRelayCommand(StartPomodoroAsync, CanStartPomodoro);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogInternalAsync, CanStop);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the current state of the Pomodoro timer.
    /// Triggers notifications for all dependent UI properties when changed.
    /// </summary>
    public PomodoroState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsSetupState));
                OnPropertyChanged(nameof(IsRunningState));
                OnPropertyChanged(nameof(IsPausedState));
                OnPropertyChanged(nameof(IsWrapUpState));
                OnPropertyChanged(nameof(IsBreakState));
                OnPropertyChanged(nameof(IsNotBreakState));
                OnPropertyChanged(nameof(IsTimerActive));
                OnPropertyChanged(nameof(PauseResumeText));
                OnPropertyChanged(nameof(SessionTypeLabel));
                ((AsyncRelayCommand)StartPomodoroCommand).NotifyCanExecuteChanged();
                ((RelayCommand)PauseResumeCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <inheritdoc/>
    public override bool IsSetupState => State == PomodoroState.Setup;

    /// <inheritdoc/>
    public override bool IsRunningState => State == PomodoroState.Running;

    /// <inheritdoc/>
    public override bool IsPausedState => State == PomodoroState.Paused;

    /// <inheritdoc/>
    public override bool IsWrapUpState => State == PomodoroState.WrapUp;

    /// <summary>
    /// Gets a value indicating whether the timer is in a break period.
    /// </summary>
    public bool IsBreakState => State == PomodoroState.Break;

    /// <summary>
    /// Gets a value indicating whether the timer is active (running, paused, wrap up, or in break).
    /// Used to show/hide the timer display.
    /// </summary>
    public bool IsTimerActive => State == PomodoroState.Running || State == PomodoroState.Paused || State == PomodoroState.WrapUp || State == PomodoroState.Break;

    /// <summary>
    /// Gets a value indicating whether NOT in a break state.
    /// Used to show/hide pause/stop buttons during breaks.
    /// </summary>
    public bool IsNotBreakState => !IsBreakState;

    /// <summary>
    /// Gets or sets whether the current break session is billable.
    /// Allows user to toggle during breaks. Defaults to setting value.
    /// </summary>
    public bool IsBreakBillable
    {
        get => _isBreakBillable;
        set
        {
            if (SetProperty(ref _isBreakBillable, value))
            {
                // Update the break entry in the database when toggled during a break
                _ = UpdateBreakEntryBillableAsync();
            }
        }
    }

    private async Task UpdateBreakEntryBillableAsync()
    {
        if (CurrentEntry != null && IsBreakState)
        {
            try
            {
                var updateDto = new UpdateTimeEntryDto
                {
                    Id = CurrentEntry.Id,
                    ProjectId = CurrentEntry.ProjectId,
                    SessionTypeId = CurrentEntry.SessionTypeId,
                    Description = CurrentEntry.Description,
                    StartTime = CurrentEntry.StartTime,
                    EndTime = CurrentEntry.EndTime,
                    DurationMinutes = CurrentEntry.DurationMinutes,
                    IsCompleted = CurrentEntry.IsCompleted,
                    IsBillable = IsBreakBillable,
                    Notes = CurrentEntry.Notes
                };
                await EntryService.UpdateEntryAsync(updateDto);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating break billable status: {ex.Message}");
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectedProjectChanged()
    {
        ((AsyncRelayCommand)StartPomodoroCommand).NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Gets or sets the objective/goal for this Pomodoro session.
    /// Required field - Start button is disabled until this has a value.
    /// </summary>
    public string Objective
    {
        get => _objective;
        set
        {
            if (SetProperty(ref _objective, value))
            {
                ((AsyncRelayCommand)StartPomodoroCommand).NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ObjectiveCharacterCount));
            }
        }
    }

    /// <summary>
    /// Gets the character count display for the objective field (e.g., "25/120").
    /// </summary>
    public string ObjectiveCharacterCount => $"{Objective.Length}/{ObjectiveMaxLength}";

    /// <inheritdoc/>
    public override string SessionDescription => Objective;

    /// <inheritdoc/>
    public override bool CountsUp => false;

    /// <inheritdoc/>
    public override bool ShowProgressMeter => true;

    /// <summary>
    /// Gets or sets the session duration in minutes.
    /// Defaults to WorkDurationMinutes from settings.
    /// </summary>
    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, value);
    }

    /// <inheritdoc/>
    public override double ProgressPercentage => _progressPercentage;

    /// <summary>
    /// Sets the progress percentage. Used internally by UpdateTimerDisplay.
    /// </summary>
    private void SetProgressPercentage(double value)
    {
        if (_progressPercentage != value)
        {
            _progressPercentage = value;
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    /// <summary>
    /// Gets the label describing the current session type.
    /// Shows "Pomodoro X/4" during work, "Wrap Up Period" during wrap up, "Short Break" or "Long Break" during breaks.
    /// </summary>
    public string SessionTypeLabel
    {
        get
        {
            if (IsBreakState)
                return _pomodoroCount == 0 ? "Long Break" : "Short Break";
            if (IsWrapUpState)
                return "Wrap Up Period";
            return $"Pomodoro {_pomodoroCount + 1}/4";
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to start a new Pomodoro session.
    /// Disabled when objective is empty or state is not Setup.
    /// </summary>
    public ICommand StartPomodoroCommand { get; }

    /// <inheritdoc/>
    public override ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets the command to stop the current session early.
    /// Shows a dialog with save, discard, or resume options.
    /// </summary>
    public ICommand StopCommand { get; }

    private bool CanStartPomodoro() => !string.IsNullOrWhiteSpace(Objective) && State == PomodoroState.Setup;
    private bool CanPauseResume() => State == PomodoroState.Running || State == PomodoroState.Paused || State == PomodoroState.WrapUp;
    private bool CanStop() => State == PomodoroState.Running || State == PomodoroState.Paused || State == PomodoroState.WrapUp;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads initial data for the Pomodoro page.
    /// Fetches settings, clients, and attempts to restore last session's client/project.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            // Load settings
            _settings = await _settingsService.GetSettingsAsync();
            DurationMinutes = _settings.WorkDurationMinutes;

            // Load clients
            await LoadClientsAsync();

            // Try to load last session's client/project
            await LoadLastSessionDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading pomodoro data: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds time to the current pomodoro session.
    /// Can be called during Running, Paused, or WrapUp states.
    /// During WrapUp, extends the wrap up period instead of the work period.
    /// </summary>
    /// <param name="minutes">Number of minutes to add</param>
    public override void AddMinutes(int minutes)
    {
        // Only allow adding time during active work sessions and wrap up period (not during breaks)
        if (State != PomodoroState.Running &&
            State != PomodoroState.Paused &&
            State != PomodoroState.WrapUp)
        {
            return;
        }

        // Add the minutes (convert to seconds)
        _remainingSeconds += minutes * 60;

        // Update the display
        UpdateTimerDisplay();
    }

    /// <inheritdoc/>
    public override int ElapsedSeconds
    {
        get
        {
            // During work period (Running/Paused), elapsed = total work duration - remaining
            // During WrapUp, the work period is complete, so use the original work duration
            if (State == PomodoroState.Running || State == PomodoroState.Paused)
            {
                return _workDurationSeconds - _remainingSeconds;
            }
            else if (State == PomodoroState.WrapUp)
            {
                return _workDurationSeconds;
            }
            return 0;
        }
    }

    #endregion

    #region Abstract Member Implementations

    /// <inheritdoc/>
    protected override ActiveTimerType TimerType => ActiveTimerType.Pomodoro;

    /// <inheritdoc/>
    protected override int SessionTypeId => SessionType.Ids.Work;

    /// <inheritdoc/>
    protected override string GetStopNotes()
    {
        return IsWrapUpState
            ? $"Stopped during wrap up period with {TimerDisplay} remaining"
            : $"Stopped early at {TimerDisplay}";
    }

    /// <inheritdoc/>
    protected override bool IsCompletedWhenStopped() => IsWrapUpState;

    /// <inheritdoc/>
    protected override void PauseResumeCore()
    {
        if (State == PomodoroState.Running || State == PomodoroState.WrapUp)
        {
            Timer.Stop();
            State = PomodoroState.Paused;
        }
        else if (State == PomodoroState.Paused)
        {
            Timer.Start();
            // Return to appropriate state based on remaining time
            State = _remainingSeconds > 0 ? PomodoroState.Running : PomodoroState.WrapUp;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    /// <inheritdoc/>
    protected override void ResetToSetupCore()
    {
        // Clear active timer when fully resetting to setup
        ClearActiveTimer();

        State = PomodoroState.Setup;
        Objective = string.Empty;
        DurationMinutes = _settings?.WorkDurationMinutes ?? 25;
        TimerDisplay = "00:00";
        SetProgressPercentage(0);
        CurrentEntry = null;

        OnPropertyChanged(nameof(SessionTypeLabel));
    }

    #endregion

    #region Private Methods

    private async Task StartPomodoroAsync()
    {
        // Try to set this as the active timer
        if (!TrySetActiveTimer())
        {
            return; // Another timer is active
        }

        try
        {
            // Create time entry
            var createDto = new CreateTimeEntryDto
            {
                ProjectId = SelectedProject?.Id,
                SessionTypeId = SessionType.Ids.Work,
                Description = Objective,
                PlannedDurationMinutes = DurationMinutes
            };

            CurrentEntry = await EntryService.StartTimerEntryAsync(createDto);

            // Initialize timer
            // Total time = work duration + wrap up period
            _workDurationSeconds = DurationMinutes * 60;
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            _totalSeconds = _workDurationSeconds + wrapUpPeriodSeconds;
            _remainingSeconds = _workDurationSeconds; // Start counting from work duration

            UpdateTimerDisplay();

            State = PomodoroState.Running;
            Timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting pomodoro: {ex.Message}");
        }
    }

    private void PauseResume()
    {
        PauseResumeCore();
    }

    private void Timer_Tick(object? sender, object e)
    {
        _remainingSeconds--;

        // Check if work period has ended and entering wrap up period
        if (_remainingSeconds == 0 && State == PomodoroState.Running)
        {
            TriggerWrapUpNotification();
            State = PomodoroState.WrapUp;
            // Now counting wrap up period - set remaining to wrap up period duration
            _remainingSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            UpdateTimerDisplay();
            return;
        }

        // Check if wrap up period has ended
        if (_remainingSeconds <= 0 && State == PomodoroState.WrapUp)
        {
            OnTimerComplete();
            return;
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = _remainingSeconds / 60;
        int seconds = _remainingSeconds % 60;
        TimerDisplay = $"{minutes:D2}:{seconds:D2}";

        // Progress calculation depends on state
        if (State == PomodoroState.WrapUp)
        {
            // During wrap up, show progress as counting down wrap up period
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            SetProgressPercentage(wrapUpPeriodSeconds > 0
                ? ((double)(wrapUpPeriodSeconds - _remainingSeconds) / wrapUpPeriodSeconds) * 100
                : 100);
        }
        else
        {
            // During work, show progress towards work duration
            SetProgressPercentage(_workDurationSeconds > 0
                ? ((double)(_workDurationSeconds - _remainingSeconds) / _workDurationSeconds) * 100
                : 0);
        }
    }

    private async void TriggerWrapUpNotification()
    {
        System.Diagnostics.Debug.WriteLine("Wrap up notification triggered - work period complete, wrap up period starting!");

        if (_settings?.PlaySound == true)
        {
            await _audioService.PlayWrapUpNotificationAsync(_settings.WrapUpNotificationVolume, _settings.WrapUpNotificationSound);
        }
    }

    private async void OnTimerComplete()
    {
        Timer.Stop();

        System.Diagnostics.Debug.WriteLine("Main alarm - Timer complete!");

        if (_settings?.PlaySound == true && _settings.UseAlarm)
        {
            await _audioService.PlayAlarmAsync(_settings.AlarmVolume, _settings.AlarmSound);
        }

        // Complete current entry if it's a work session (including wrap up) or break
        if (CurrentEntry != null)
        {
            await EntryService.CompleteEntryAsync(CurrentEntry.Id);
        }

        if (State == PomodoroState.Break)
        {
            // Break finished, return to setup for next pomodoro
            ResetToSetupCore();
        }
        else
        {
            // Pomodoro finished (wrap up period ended), start appropriate break
            _pomodoroCount++;
            PomodoroStateService.CurrentPomodoroCount = _pomodoroCount;
            await StartBreakAsync();
        }
    }

    private async Task StartBreakAsync()
    {
        if (_settings == null)
            return;

        bool isLongBreak = _pomodoroCount >= 4;
        int breakDuration = isLongBreak
            ? _settings.LongBreakDurationMinutes
            : _settings.ShortBreakDurationMinutes;

        // Set billable default based on settings
        IsBreakBillable = isLongBreak
            ? _settings.LongBreaksAreBillable
            : _settings.ShortBreaksAreBillable;

        if (isLongBreak)
        {
            _pomodoroCount = 0; // Reset cycle
            PomodoroStateService.ResetCycle();
        }

        // Create a time entry for the break session
        var breakSessionType = isLongBreak ? SessionType.Ids.LongBreak : SessionType.Ids.ShortBreak;
        var createDto = new CreateTimeEntryDto
        {
            ProjectId = SelectedProject?.Id,
            SessionTypeId = breakSessionType,
            Description = isLongBreak ? "Long Break" : "Short Break",
            PlannedDurationMinutes = breakDuration,
            IsBillable = IsBreakBillable
        };

        CurrentEntry = await EntryService.StartTimerEntryAsync(createDto);

        _totalSeconds = breakDuration * 60;
        _remainingSeconds = _totalSeconds;

        UpdateTimerDisplay();
        OnPropertyChanged(nameof(SessionTypeLabel));

        State = PomodoroState.Break;
        Timer.Start();
    }

    #endregion
}
