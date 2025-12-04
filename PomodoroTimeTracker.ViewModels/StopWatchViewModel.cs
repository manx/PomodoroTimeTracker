using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Represents the current state of the Stopwatch timer.
/// </summary>
public enum StopWatchState
{
    /// <summary>
    /// Configuring the session before starting (initial state).
    /// </summary>
    Setup,

    /// <summary>
    /// Timer is actively counting up.
    /// </summary>
    Running,

    /// <summary>
    /// Timer is paused and can be resumed.
    /// </summary>
    Paused
}

/// <summary>
/// ViewModel for the Stopwatch page.
/// Simple timer that counts up indefinitely with no duration limit, wrap-up period, or sounds.
/// Sessions are saved with SessionType.StopWatch.
/// </summary>
public sealed partial class StopWatchViewModel : TimerViewModelBase
{
    /// <summary>
    /// Maximum length for the description text field.
    /// </summary>
    public const int DescriptionMaxLength = 90;

    private StopWatchState _state = StopWatchState.Setup;
    private int _elapsedSeconds;
    private string _description = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StopWatchViewModel"/> class.
    /// </summary>
    public StopWatchViewModel(
        IPomodoroSessionService sessionService,
        IClientService clientService,
        IProjectService projectService,
        IActiveTimerService activeTimerService,
        IPomodoroStateService pomodoroStateService,
        IDispatcherTimer timer)
        : base(sessionService, clientService, projectService, activeTimerService, pomodoroStateService, timer)
    {
        Timer.Tick += Timer_Tick;

        StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, CanStartTimer);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogInternalAsync, CanStop);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the current state of the Stopwatch timer.
    /// </summary>
    public StopWatchState State
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
                ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
                ((RelayCommand)PauseResumeCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <inheritdoc/>
    public override bool IsSetupState => State == StopWatchState.Setup;

    /// <inheritdoc/>
    public override bool IsRunningState => State == StopWatchState.Running;

    /// <inheritdoc/>
    public override bool IsPausedState => State == StopWatchState.Paused;

    /// <inheritdoc/>
    /// <remarks>
    /// StopWatch has no wrap up state, so this always returns false.
    /// </remarks>
    public override bool IsWrapUpState => false;

    /// <summary>
    /// Gets a value indicating whether the timer is active (running or paused).
    /// </summary>
    public bool IsTimerActive => State == StopWatchState.Running || State == StopWatchState.Paused;

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
    /// <remarks>
    /// Stopwatch has no progress to track, so meter is not shown.
    /// </remarks>
    public override bool ShowProgressMeter => false;

    /// <inheritdoc/>
    /// <remarks>
    /// Stopwatch always returns 0 - no progress to track.
    /// </remarks>
    public override double ProgressPercentage => 0;

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

    private bool CanStartTimer() => !string.IsNullOrWhiteSpace(Description) && State == StopWatchState.Setup;
    private bool CanPauseResume() => State == StopWatchState.Running || State == StopWatchState.Paused;
    private bool CanStop() => State == StopWatchState.Running || State == StopWatchState.Paused;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads initial data for the Stopwatch page.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            // Load clients
            await LoadClientsAsync();

            // Refresh warning state
            OnPropertyChanged(nameof(ShowCycleWarning));

            // Try to load last session's client/project
            await LoadLastSessionDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading stopwatch data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For stopwatch, this doesn't make much sense (already counting up indefinitely),
    /// but included for interface compatibility. Does nothing for stopwatch.
    /// </remarks>
    public override void AddMinutes(int minutes)
    {
        // Stopwatch counts up indefinitely - adding time doesn't make sense
        // Method exists only for ITimerWindowViewModel interface compatibility
    }

    /// <inheritdoc/>
    public override int ElapsedSeconds => _elapsedSeconds;

    #endregion

    #region Abstract Implementations

    /// <inheritdoc/>
    protected override ActiveTimerType TimerType => ActiveTimerType.StopWatch;

    /// <inheritdoc/>
    protected override SessionType SessionTypeValue => SessionType.StopWatch;

    /// <inheritdoc/>
    protected override string GetStopNotes()
    {
        return $"Stopped at {TimerDisplay}";
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Stopwatch is always considered completed when stopped.
    /// </remarks>
    protected override bool IsCompletedWhenStopped() => true;

    /// <inheritdoc/>
    protected override void PauseResumeCore()
    {
        if (State == StopWatchState.Running)
        {
            Timer.Stop();
            State = StopWatchState.Paused;
        }
        else if (State == StopWatchState.Paused)
        {
            Timer.Start();
            State = StopWatchState.Running;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    /// <inheritdoc/>
    protected override void ResetToSetupCore()
    {
        // Clear active timer when resetting to setup
        ClearActiveTimer();

        State = StopWatchState.Setup;
        Description = string.Empty;
        TimerDisplay = "00:00";
        _elapsedSeconds = 0;
        CurrentSession = null;

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
            // Reset Pomodoro cycle when starting Stopwatch
            PomodoroStateService.ResetCycle();
            OnPropertyChanged(nameof(ShowCycleWarning));

            // Create session with SessionType.StopWatch
            // Duration is 0 for stopwatch (no predefined duration)
            var createDto = new CreatePomodoroSessionDto
            {
                ProjectId = SelectedProject?.Id,
                DurationMinutes = 0, // Stopwatch has no duration limit
                SessionType = SessionType.StopWatch,
                Objective = Description  // Using Description for the Objective field
            };

            CurrentSession = await SessionService.CreateSessionAsync(createDto);

            // Initialize timer
            _elapsedSeconds = 0;

            UpdateTimerDisplay();

            State = StopWatchState.Running;
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


    private void Timer_Tick(object? sender, EventArgs e)
    {
        _elapsedSeconds++;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        // Stopwatch counts UP (shows elapsed time)
        // Format: HH:MM:SS for times over 1 hour, MM:SS otherwise
        int hours = _elapsedSeconds / 3600;
        int minutes = (_elapsedSeconds % 3600) / 60;
        int seconds = _elapsedSeconds % 60;

        if (hours > 0)
        {
            TimerDisplay = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        else
        {
            TimerDisplay = $"{minutes:D2}:{seconds:D2}";
        }
    }

    #endregion
}
