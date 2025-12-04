using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Represents the current state of the Regular timer.
/// Simplified version of PomodoroState - no Break state.
/// </summary>
public enum RegularTimerState
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
    /// Timer has completed.
    /// </summary>
    Completed
}

/// <summary>
/// ViewModel for the Regular Timer page.
/// Simplified timer without Pomodoro cycle tracking.
/// Sessions are saved with SessionType.Regular.
/// </summary>
public sealed partial class RegularTimerViewModel : TimerViewModelBase
{
    /// <summary>
    /// Maximum length for the description text field.
    /// </summary>
    public const int DescriptionMaxLength = 90;

    private readonly IPomodoroSettingsService _settingsService;
    private readonly IAudioService _audioService;

    private RegularTimerState _state = RegularTimerState.Setup;
    private int _elapsedSeconds;
    private int _remainingSeconds;
    private int _totalSeconds;
    private int _workDurationSeconds;
    private PomodoroSettingsDto? _settings;

    // Setup screen properties
    private string _description = string.Empty;
    private int _durationMinutes;

    // Timer display properties
    private double _progressPercentage;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegularTimerViewModel"/> class.
    /// </summary>
    /// <param name="sessionService">Service for managing Pomodoro sessions.</param>
    /// <param name="settingsService">Service for managing Pomodoro settings.</param>
    /// <param name="clientService">Service for managing clients.</param>
    /// <param name="projectService">Service for managing projects.</param>
    /// <param name="audioService">Service for playing audio notifications.</param>
    /// <param name="activeTimerService">Service for coordinating active timer state.</param>
    /// <param name="pomodoroStateService">Service for managing Pomodoro cycle state.</param>
    /// <param name="timer">Timer for updating UI every second.</param>
    public RegularTimerViewModel(
        IPomodoroSessionService sessionService,
        IPomodoroSettingsService settingsService,
        IClientService clientService,
        IProjectService projectService,
        IAudioService audioService,
        IActiveTimerService activeTimerService,
        IPomodoroStateService pomodoroStateService,
        IDispatcherTimer timer)
        : base(sessionService, clientService, projectService, activeTimerService, pomodoroStateService, timer)
    {
        _settingsService = settingsService;
        _audioService = audioService;

        Timer.Tick += Timer_Tick;

        StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, CanStartTimer);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogInternalAsync, CanStop);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the current state of the Regular timer.
    /// </summary>
    public RegularTimerState State
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
                OnPropertyChanged(nameof(IsTimerActive));
                OnPropertyChanged(nameof(PauseResumeText));
                OnPropertyChanged(nameof(SessionTypeLabel));
                ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
                ((RelayCommand)PauseResumeCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <inheritdoc/>
    public override bool IsSetupState => State == RegularTimerState.Setup;

    /// <inheritdoc/>
    public override bool IsRunningState => State == RegularTimerState.Running;

    /// <inheritdoc/>
    public override bool IsPausedState => State == RegularTimerState.Paused;

    /// <inheritdoc/>
    public override bool IsWrapUpState => State == RegularTimerState.WrapUp;

    /// <summary>
    /// Gets a value indicating whether the timer is active (running, paused, or wrap up).
    /// </summary>
    public bool IsTimerActive => State == RegularTimerState.Running ||
                                  State == RegularTimerState.Paused ||
                                  State == RegularTimerState.WrapUp;

    /// <summary>
    /// Gets a value indicating whether there's an active Pomodoro cycle.
    /// Used to show warning to user.
    /// </summary>
    public bool ShowCycleWarning => PomodoroStateService.CurrentPomodoroCount > 0;

    /// <inheritdoc/>
    protected override void OnSelectedProjectChanged()
    {
        ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Gets or sets the description for this timer session.
    /// Required field - Start button is disabled until this has a value.
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(DescriptionCharacterCount));
            }
        }
    }

    /// <summary>
    /// Gets the character count display for the description field.
    /// </summary>
    public string DescriptionCharacterCount => $"{Description.Length}/{DescriptionMaxLength}";

    /// <inheritdoc/>
    public override string SessionDescription => Description;

    /// <inheritdoc/>
    public override bool CountsUp => true;

    /// <inheritdoc/>
    public override bool ShowProgressMeter => true;

    /// <summary>
    /// Gets or sets the session duration in minutes.
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
    /// </summary>
    public string SessionTypeLabel
    {
        get
        {
            if (IsWrapUpState)
                return "Wrap Up Period";
            return "Regular Timer";
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to start a new timer session.
    /// </summary>
    public ICommand StartTimerCommand { get; }

    /// <inheritdoc/>
    public override ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets the command to stop the current session early.
    /// </summary>
    public ICommand StopCommand { get; }

    private bool CanStartTimer() => !string.IsNullOrWhiteSpace(Description) && State == RegularTimerState.Setup;
    private bool CanPauseResume() => State == RegularTimerState.Running ||
                                      State == RegularTimerState.Paused ||
                                      State == RegularTimerState.WrapUp;
    private bool CanStop() => State == RegularTimerState.Running ||
                               State == RegularTimerState.Paused ||
                               State == RegularTimerState.WrapUp;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads initial data for the Regular Timer page.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            // Load settings (using same settings as Pomodoro for WrapUp period)
            _settings = await _settingsService.GetSettingsAsync();
            // Default to 60 minutes for Regular Timer (TODO: add separate settings later)
            DurationMinutes = 60;

            // Load clients
            await LoadClientsAsync();

            // Refresh warning state
            OnPropertyChanged(nameof(ShowCycleWarning));

            // Try to load last session's client/project
            await LoadLastSessionDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading regular timer data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public override void AddMinutes(int minutes)
    {
        if (State != RegularTimerState.Running &&
            State != RegularTimerState.Paused &&
            State != RegularTimerState.WrapUp)
        {
            return;
        }

        var additionalSeconds = minutes * 60;
        _remainingSeconds += additionalSeconds;
        _workDurationSeconds += additionalSeconds;
        UpdateTimerDisplay();
    }

    /// <inheritdoc/>
    public override int ElapsedSeconds => _elapsedSeconds;

    #endregion

    #region Abstract Member Implementations

    /// <inheritdoc/>
    protected override ActiveTimerType TimerType => ActiveTimerType.RegularTimer;

    /// <inheritdoc/>
    protected override SessionType SessionTypeValue => SessionType.Regular;

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
        if (State == RegularTimerState.Running || State == RegularTimerState.WrapUp)
        {
            Timer.Stop();
            State = RegularTimerState.Paused;
        }
        else if (State == RegularTimerState.Paused)
        {
            Timer.Start();
            State = _remainingSeconds > 0 ? RegularTimerState.Running : RegularTimerState.WrapUp;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    /// <inheritdoc/>
    protected override void ResetToSetupCore()
    {
        // Clear active timer when resetting to setup
        ClearActiveTimer();

        State = RegularTimerState.Setup;
        Description = string.Empty;
        DurationMinutes = 60; // Default for Regular Timer (TODO: add separate settings later)
        TimerDisplay = "00:00";
        SetProgressPercentage(0);
        _elapsedSeconds = 0;
        CurrentSession = null;

        OnPropertyChanged(nameof(SessionTypeLabel));
        OnPropertyChanged(nameof(ShowCycleWarning));
    }

    #endregion

    #region Private Methods

    private async Task StartTimerAsync()
    {
        // Try to set this as the active timer
        if (!TrySetActiveTimer())
        {
            return; // Another timer is active
        }

        try
        {
            // Reset Pomodoro cycle when starting Regular Timer
            PomodoroStateService.ResetCycle();
            OnPropertyChanged(nameof(ShowCycleWarning));

            // Create session with SessionType.Regular
            var createDto = new CreatePomodoroSessionDto
            {
                ProjectId = SelectedProject?.Id,
                DurationMinutes = DurationMinutes,
                SessionType = SessionType.Regular,
                Objective = Description  // Using Description for the Objective field
            };

            CurrentSession = await SessionService.CreateSessionAsync(createDto);

            // Initialize timer
            _workDurationSeconds = DurationMinutes * 60;
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            _totalSeconds = _workDurationSeconds + wrapUpPeriodSeconds;
            _remainingSeconds = _workDurationSeconds;
            _elapsedSeconds = 0;

            UpdateTimerDisplay();

            State = RegularTimerState.Running;
            Timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting timer: {ex.Message}");
        }
    }

    private void PauseResume()
    {
        PauseResumeCore();
    }

    private void Timer_Tick(object? sender, object e)
    {
        _remainingSeconds--;
        _elapsedSeconds++;

        // Check if work period has ended and entering wrap up period
        if (_remainingSeconds == 0 && State == RegularTimerState.Running)
        {
            TriggerWrapUpNotification();
            State = RegularTimerState.WrapUp;
            _remainingSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            UpdateTimerDisplay();
            return;
        }

        // Check if wrap up period has ended
        if (_remainingSeconds <= 0 && State == RegularTimerState.WrapUp)
        {
            OnTimerComplete();
            return;
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        // Regular Timer counts UP (shows elapsed time)
        int minutes = _elapsedSeconds / 60;
        int seconds = _elapsedSeconds % 60;
        TimerDisplay = $"{minutes:D2}:{seconds:D2}";

        // Progress still moves in the same direction (fills up as time passes)
        if (State == RegularTimerState.WrapUp)
        {
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            SetProgressPercentage(wrapUpPeriodSeconds > 0
                ? ((double)(wrapUpPeriodSeconds - _remainingSeconds) / wrapUpPeriodSeconds) * 100
                : 100);
        }
        else
        {
            SetProgressPercentage(_workDurationSeconds > 0
                ? ((double)(_workDurationSeconds - _remainingSeconds) / _workDurationSeconds) * 100
                : 0);
        }
    }

    private async void TriggerWrapUpNotification()
    {
        System.Diagnostics.Debug.WriteLine("Regular Timer: Wrap up notification triggered!");

        if (_settings?.PlaySound == true)
        {
            await _audioService.PlayWrapUpNotificationAsync(_settings.WrapUpNotificationVolume, _settings.WrapUpNotificationSound);
        }
    }

    private async void OnTimerComplete()
    {
        Timer.Stop();

        System.Diagnostics.Debug.WriteLine("Regular Timer: Timer complete!");

        if (_settings?.PlaySound == true && _settings.UseAlarm)
        {
            await _audioService.PlayAlarmAsync(_settings.AlarmVolume, _settings.AlarmSound);
        }

        // Complete the session
        if (CurrentSession != null)
        {
            await SessionService.CompleteSessionAsync(CurrentSession.Id);
        }

        // Return to setup (no break cycle like Pomodoro)
        ResetToSetupCore();
    }

    #endregion
}
