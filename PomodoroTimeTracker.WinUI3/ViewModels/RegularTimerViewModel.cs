using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.WinUI3.Services;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// Represents the current state of the Regular timer.
/// Simplified version of PomodoroState - no Break state.
/// </summary>
internal enum RegularTimerState
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
internal sealed partial class RegularTimerViewModel : ViewModelBase, ITimerWindowViewModel
{
    /// <summary>
    /// Maximum length for the description text field.
    /// </summary>
    public const int DescriptionMaxLength = 90;

    private readonly IPomodoroSessionService _sessionService;
    private readonly IPomodoroSettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IProjectService _projectService;
    private readonly IAudioService _audioService;
    private readonly IActiveTimerService _activeTimerService;
    private readonly IDispatcherTimer _timer;

    private RegularTimerState _state = RegularTimerState.Setup;
    private int _elapsedSeconds;
    private int _remainingSeconds;
    private int _totalSeconds;
    private int _workDurationSeconds;
    private PomodoroSessionDto? _currentSession;
    private PomodoroSettingsDto? _settings;

    // Setup screen properties
    private ObservableCollection<ClientDto> _clients = new();
    private ObservableCollection<ProjectDto> _projects = new();
    private ClientDto? _selectedClient;
    private ProjectDto? _selectedProject;
    private string _description = string.Empty;
    private int _durationMinutes;

    // Timer display properties
    private string _timerDisplay = "00:00";
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
    /// <param name="timer">Timer for updating UI every second.</param>
    public RegularTimerViewModel(
        IPomodoroSessionService sessionService,
        IPomodoroSettingsService settingsService,
        IClientService clientService,
        IProjectService projectService,
        IAudioService audioService,
        IActiveTimerService activeTimerService,
        IDispatcherTimer timer)
    {
        _sessionService = sessionService;
        _settingsService = settingsService;
        _clientService = clientService;
        _projectService = projectService;
        _audioService = audioService;
        _activeTimerService = activeTimerService;
        _timer = timer;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, CanStartTimer);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogAsync, CanStop);
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

    /// <summary>
    /// Gets a value indicating whether the timer is in Setup state.
    /// </summary>
    public bool IsSetupState => State == RegularTimerState.Setup;

    /// <summary>
    /// Gets a value indicating whether the timer is actively running.
    /// </summary>
    public bool IsRunningState => State == RegularTimerState.Running;

    /// <summary>
    /// Gets a value indicating whether the timer is paused.
    /// </summary>
    public bool IsPausedState => State == RegularTimerState.Paused;

    /// <summary>
    /// Gets a value indicating whether the timer is in wrap up period.
    /// </summary>
    public bool IsWrapUpState => State == RegularTimerState.WrapUp;

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
    public bool ShowCycleWarning => PomodoroViewModel.CurrentPomodoroCount > 0;

    /// <summary>
    /// Gets or sets the collection of available clients.
    /// </summary>
    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    /// <summary>
    /// Gets or sets the collection of projects for the selected client.
    /// </summary>
    public ObservableCollection<ProjectDto> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    /// <summary>
    /// Gets or sets the selected client.
    /// </summary>
    public ClientDto? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                OnPropertyChanged(nameof(IsClientSelected));
                _ = LoadProjectsForClientAsync();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a client is currently selected.
    /// </summary>
    public bool IsClientSelected => SelectedClient != null;

    /// <summary>
    /// Gets or sets the selected project for this session.
    /// </summary>
    public ProjectDto? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
            }
        }
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

    /// <summary>
    /// Gets the session description for TimerWindow display.
    /// </summary>
    public string SessionDescription => Description;

    /// <summary>
    /// Gets a value indicating whether the timer counts up. Regular Timer counts up.
    /// </summary>
    public bool CountsUp => true;

    /// <inheritdoc/>
    public bool ShowProgressMeter => true;

    /// <summary>
    /// Gets or sets the session duration in minutes.
    /// </summary>
    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, value);
    }

    /// <summary>
    /// Gets or sets the timer display string in MM:SS format.
    /// </summary>
    public string TimerDisplay
    {
        get => _timerDisplay;
        set => SetProperty(ref _timerDisplay, value);
    }

    /// <summary>
    /// Gets or sets the progress percentage (0-100) for the progress ring.
    /// </summary>
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    /// <summary>
    /// Gets the icon glyph for the pause/resume button.
    /// </summary>
    public string PauseResumeIcon => State == RegularTimerState.Running ? "\uE769" : "\uE768";

    /// <summary>
    /// Gets the text for the pause/resume button.
    /// </summary>
    public string PauseResumeText => IsPausedState ? "Resume" : "Pause";

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

    /// <summary>
    /// Gets the command to pause or resume the timer.
    /// </summary>
    public ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets the command to stop the current session early.
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// Gets or sets the callback function to show the stop confirmation dialog.
    /// </summary>
    public Func<Task<StopDialogResult>>? ShowStopDialog { get; set; }

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
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);

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

    /// <summary>
    /// Adds time to the current timer session.
    /// </summary>
    /// <param name="minutes">Number of minutes to add</param>
    public void AddMinutes(int minutes)
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

    /// <summary>
    /// Gets the elapsed time in seconds for the current session.
    /// </summary>
    public int ElapsedSeconds => _elapsedSeconds;

    #endregion

    #region Private Methods

    private async Task LoadLastSessionDataAsync()
    {
        try
        {
            var sessions = await _sessionService.GetAllSessionsAsync();
            var lastSession = sessions
                .Where(s => s.SessionType == SessionType.Regular)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefault();

            if (lastSession?.ProjectId != null)
            {
                var project = await _projectService.GetProjectByIdAsync(lastSession.ProjectId.Value);
                if (project != null)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == project.ClientId);
                    await Task.Delay(100);
                    SelectedProject = Projects.FirstOrDefault(p => p.Id == project.Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading last session data: {ex.Message}");
        }
    }

    private async Task LoadProjectsForClientAsync()
    {
        if (SelectedClient == null)
        {
            Projects.Clear();
            SelectedProject = null;
            return;
        }

        try
        {
            var projects = await _projectService.GetProjectsByClientIdAsync(SelectedClient.Id);
            Projects = new ObservableCollection<ProjectDto>(projects);

            if (SelectedProject != null && !Projects.Any(p => p.Id == SelectedProject.Id))
            {
                SelectedProject = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
        }
    }

    private async Task StartTimerAsync()
    {
        // Try to set this as the active timer
        if (!_activeTimerService.TrySetActiveTimer(ActiveTimerType.RegularTimer))
        {
            return; // Another timer is active
        }

        try
        {
            // Reset Pomodoro cycle when starting Regular Timer
            PomodoroViewModel.CurrentPomodoroCount = 0;
            OnPropertyChanged(nameof(ShowCycleWarning));

            // Create session with SessionType.Regular
            var createDto = new CreatePomodoroSessionDto
            {
                ProjectId = SelectedProject?.Id,
                DurationMinutes = DurationMinutes,
                SessionType = SessionType.Regular,
                Objective = Description  // Using Description for the Objective field
            };

            _currentSession = await _sessionService.CreateSessionAsync(createDto);

            // Initialize timer
            _workDurationSeconds = DurationMinutes * 60;
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            _totalSeconds = _workDurationSeconds + wrapUpPeriodSeconds;
            _remainingSeconds = _workDurationSeconds;
            _elapsedSeconds = 0;

            UpdateTimerDisplay();

            State = RegularTimerState.Running;
            _timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting timer: {ex.Message}");
        }
    }

    private void PauseResume()
    {
        if (State == RegularTimerState.Running || State == RegularTimerState.WrapUp)
        {
            _timer.Stop();
            State = RegularTimerState.Paused;
        }
        else if (State == RegularTimerState.Paused)
        {
            _timer.Start();
            State = _remainingSeconds > 0 ? RegularTimerState.Running : RegularTimerState.WrapUp;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    private async Task ShowStopDialogAsync()
    {
        bool wasRunning = State == RegularTimerState.Running;
        if (wasRunning)
        {
            PauseResume();
        }

        if (ShowStopDialog != null)
        {
            var result = await ShowStopDialog();

            switch (result)
            {
                case StopDialogResult.Resume:
                    if (wasRunning)
                    {
                        PauseResume();
                    }
                    break;

                case StopDialogResult.Save:
                    await SaveAndStopAsync();
                    break;

                case StopDialogResult.Discard:
                    await DiscardAndStopAsync();
                    break;
            }
        }
    }

    /// <summary>
    /// Saves the current session as a partial completion and returns to setup.
    /// </summary>
    public async Task SaveAndStopAsync()
    {
        _timer.Stop();

        if (_currentSession != null)
        {
            string notes = IsWrapUpState
                ? $"Stopped during wrap up period with {_timerDisplay} remaining"
                : $"Stopped early at {_timerDisplay}";

            var updateDto = new UpdatePomodoroSessionDto
            {
                Id = _currentSession.Id,
                ProjectId = _currentSession.ProjectId,
                StartTime = _currentSession.StartTime,
                EndTime = DateTime.UtcNow,
                DurationMinutes = _currentSession.DurationMinutes,
                IsCompleted = IsWrapUpState,
                SessionType = _currentSession.SessionType,
                Objective = _currentSession.Objective,
                Notes = notes
            };

            await _sessionService.UpdateSessionAsync(updateDto);
        }

        ResetToSetup();
    }

    /// <summary>
    /// Deletes the current session and returns to setup.
    /// </summary>
    public async Task DiscardAndStopAsync()
    {
        _timer.Stop();

        if (_currentSession != null)
        {
            await _sessionService.DeleteSessionAsync(_currentSession.Id);
        }

        ResetToSetup();
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
            ProgressPercentage = wrapUpPeriodSeconds > 0
                ? ((double)(wrapUpPeriodSeconds - _remainingSeconds) / wrapUpPeriodSeconds) * 100
                : 100;
        }
        else
        {
            ProgressPercentage = _workDurationSeconds > 0
                ? ((double)(_workDurationSeconds - _remainingSeconds) / _workDurationSeconds) * 100
                : 0;
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
        _timer.Stop();

        System.Diagnostics.Debug.WriteLine("Regular Timer: Timer complete!");

        if (_settings?.PlaySound == true && _settings.UseAlarm)
        {
            await _audioService.PlayAlarmAsync(_settings.AlarmVolume, _settings.AlarmSound);
        }

        // Complete the session
        if (_currentSession != null)
        {
            await _sessionService.CompleteSessionAsync(_currentSession.Id);
        }

        // Return to setup (no break cycle like Pomodoro)
        ResetToSetup();
    }

    private void ResetToSetup()
    {
        // Clear active timer when resetting to setup
        _activeTimerService.ClearActiveTimer();

        State = RegularTimerState.Setup;
        Description = string.Empty;
        DurationMinutes = 60; // Default for Regular Timer (TODO: add separate settings later)
        TimerDisplay = "00:00";
        ProgressPercentage = 0;
        _elapsedSeconds = 0;
        _currentSession = null;

        OnPropertyChanged(nameof(SessionTypeLabel));
        OnPropertyChanged(nameof(ShowCycleWarning));
    }

    #endregion
}
