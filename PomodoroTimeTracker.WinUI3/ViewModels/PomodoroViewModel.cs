using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

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
public partial class PomodoroViewModel : ViewModelBase
{
    private readonly IPomodoroSessionService _sessionService;
    private readonly IPomodoroSettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IProjectService _projectService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _timer;

    private PomodoroState _state = PomodoroState.Setup;
    private int _remainingSeconds;
    private int _totalSeconds;
    private int _pomodoroCount = 0; // Tracks which pomodoro we're on (0-3)
    private int _workDurationSeconds; // The original work duration (without grace period)
    private PomodoroSessionDto? _currentSession;
    private PomodoroSettingsDto? _settings;

    // Setup screen properties
    private ObservableCollection<ClientDto> _clients = new();
    private ObservableCollection<ProjectDto> _projects = new();
    private ClientDto? _selectedClient;
    private ProjectDto? _selectedProject;
    private string _objective = string.Empty;
    private int _durationMinutes;

    // Timer display properties
    private string _timerDisplay = "00:00";
    private double _progressPercentage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PomodoroViewModel"/> class.
    /// </summary>
    /// <param name="sessionService">Service for managing Pomodoro sessions.</param>
    /// <param name="settingsService">Service for managing Pomodoro settings.</param>
    /// <param name="clientService">Service for managing clients.</param>
    /// <param name="projectService">Service for managing projects.</param>
    public PomodoroViewModel(
        IPomodoroSessionService sessionService,
        IPomodoroSettingsService settingsService,
        IClientService clientService,
        IProjectService projectService)
    {
        _sessionService = sessionService;
        _settingsService = settingsService;
        _clientService = clientService;
        _projectService = projectService;

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        StartPomodoroCommand = new AsyncRelayCommand(StartPomodoroAsync, CanStartPomodoro);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogAsync, CanStop);
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

    /// <summary>
    /// Gets a value indicating whether the timer is in Setup state.
    /// </summary>
    public bool IsSetupState => State == PomodoroState.Setup;

    /// <summary>
    /// Gets a value indicating whether the timer is actively running.
    /// </summary>
    public bool IsRunningState => State == PomodoroState.Running;

    /// <summary>
    /// Gets a value indicating whether the timer is paused.
    /// </summary>
    public bool IsPausedState => State == PomodoroState.Paused;

    /// <summary>
    /// Gets a value indicating whether the timer is in wrap up period.
    /// </summary>
    public bool IsWrapUpState => State == PomodoroState.WrapUp;

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
    /// Gets or sets the collection of available clients.
    /// </summary>
    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    /// <summary>
    /// Gets or sets the collection of projects for the selected client.
    /// Filtered automatically when client selection changes.
    /// </summary>
    public ObservableCollection<ProjectDto> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    /// <summary>
    /// Gets or sets the selected client.
    /// When changed, automatically loads projects for that client.
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
    /// Used to enable/disable the project dropdown.
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
                ((AsyncRelayCommand)StartPomodoroCommand).NotifyCanExecuteChanged();
            }
        }
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
            }
        }
    }

    /// <summary>
    /// Gets or sets the session duration in minutes.
    /// Defaults to WorkDurationMinutes from settings.
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
    /// Returns pause icon when running, play icon when paused.
    /// </summary>
    public string PauseResumeIcon => State == PomodoroState.Running ? "\uE769" : "\uE768"; // Pause : Play

    /// <summary>
    /// Gets the text for the pause/resume button.
    /// Returns "Pause" when running, "Resume" when paused.
    /// </summary>
    public string PauseResumeText => IsPausedState ? "Resume" : "Pause";

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

    /// <summary>
    /// Gets the command to pause or resume the timer.
    /// Available only when state is Running or Paused.
    /// </summary>
    public ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets the command to stop the current session early.
    /// Shows a dialog with save, discard, or resume options.
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// Gets or sets the callback function to show the stop confirmation dialog.
    /// Set by the Page to maintain separation between ViewModel and View.
    /// </summary>
    public Func<Task<StopDialogResult>>? ShowStopDialog { get; set; }

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
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);

            // Try to load last session's client/project
            await LoadLastSessionDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading pomodoro data: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadLastSessionDataAsync()
    {
        try
        {
            var sessions = await _sessionService.GetAllSessionsAsync();
            var lastSession = sessions
                .Where(s => s.SessionType == SessionType.Work)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefault();

            if (lastSession?.ProjectId != null)
            {
                var project = await _projectService.GetProjectByIdAsync(lastSession.ProjectId.Value);
                if (project != null)
                {
                    // Set client first
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == project.ClientId);

                    // Projects will be loaded by SelectedClient setter
                    // Wait a bit for async loading
                    await Task.Delay(100);

                    // Then set project
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

            // Clear selection if previous project doesn't belong to new client
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

    private async Task StartPomodoroAsync()
    {
        try
        {
            // Create session
            var createDto = new CreatePomodoroSessionDto
            {
                ProjectId = SelectedProject?.Id,
                DurationMinutes = DurationMinutes,
                SessionType = SessionType.Work,
                Objective = Objective
            };

            _currentSession = await _sessionService.CreateSessionAsync(createDto);

            // Initialize timer
            // Total time = work duration + wrap up period
            _workDurationSeconds = DurationMinutes * 60;
            int wrapUpPeriodSeconds = (_settings?.WrapUpPeriodMinutes ?? 3) * 60;
            _totalSeconds = _workDurationSeconds + wrapUpPeriodSeconds;
            _remainingSeconds = _workDurationSeconds; // Start counting from work duration

            UpdateTimerDisplay();

            State = PomodoroState.Running;
            _timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting pomodoro: {ex.Message}");
        }
    }

    private void PauseResume()
    {
        if (State == PomodoroState.Running || State == PomodoroState.WrapUp)
        {
            _timer.Stop();
            State = PomodoroState.Paused;
        }
        else if (State == PomodoroState.Paused)
        {
            _timer.Start();
            // Return to appropriate state based on remaining time
            State = _remainingSeconds > 0 ? PomodoroState.Running : PomodoroState.WrapUp;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    private async Task ShowStopDialogAsync()
    {
        // Pause timer while dialog is showing
        bool wasRunning = State == PomodoroState.Running;
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
                        PauseResume(); // Resume
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
    /// Called when user chooses "Save" from the stop dialog.
    /// </summary>
    public async Task SaveAndStopAsync()
    {
        _timer.Stop();

        if (_currentSession != null)
        {
            string notes = IsWrapUpState
                ? $"Stopped during wrap up period with {_timerDisplay} remaining"
                : $"Stopped early at {_timerDisplay}";

            // Save the session with current progress
            var updateDto = new UpdatePomodoroSessionDto
            {
                Id = _currentSession.Id,
                ProjectId = _currentSession.ProjectId,
                StartTime = _currentSession.StartTime,
                EndTime = DateTime.UtcNow,
                DurationMinutes = _currentSession.DurationMinutes,
                IsCompleted = IsWrapUpState, // If in wrap up period, work was completed
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
    /// Called when user chooses "Discard" from the stop dialog.
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
            ProgressPercentage = wrapUpPeriodSeconds > 0
                ? ((double)(wrapUpPeriodSeconds - _remainingSeconds) / wrapUpPeriodSeconds) * 100
                : 100;
        }
        else
        {
            // During work, show progress towards work duration
            ProgressPercentage = _workDurationSeconds > 0
                ? ((double)(_workDurationSeconds - _remainingSeconds) / _workDurationSeconds) * 100
                : 0;
        }
    }

    private void TriggerWrapUpNotification()
    {
        // TODO: Play wrap up notification sound at WrapUpNotificationVolume
        System.Diagnostics.Debug.WriteLine("Wrap up notification triggered - work period complete, wrap up period starting!");
    }

    private async void OnTimerComplete()
    {
        _timer.Stop();

        // TODO: Play main alarm at AlarmVolume
        System.Diagnostics.Debug.WriteLine("Main alarm - Timer complete!");

        // Complete current session if it's a work session (including wrap up)
        if (_currentSession != null && (State == PomodoroState.WrapUp || State == PomodoroState.Running))
        {
            await _sessionService.CompleteSessionAsync(_currentSession.Id);
        }

        if (State == PomodoroState.Break)
        {
            // Break finished, return to setup for next pomodoro
            ResetToSetup();
        }
        else
        {
            // Pomodoro finished (wrap up period ended), start appropriate break
            _pomodoroCount++;
            await StartBreakAsync();
        }
    }

    private async Task StartBreakAsync()
    {
        if (_settings == null) return;

        bool isLongBreak = _pomodoroCount >= 4;
        int breakDuration = isLongBreak
            ? _settings.LongBreakDurationMinutes
            : _settings.ShortBreakDurationMinutes;

        if (isLongBreak)
        {
            _pomodoroCount = 0; // Reset cycle
        }

        _totalSeconds = breakDuration * 60;
        _remainingSeconds = _totalSeconds;

        UpdateTimerDisplay();
        OnPropertyChanged(nameof(SessionTypeLabel));

        State = PomodoroState.Break;
        _timer.Start();

        await Task.CompletedTask;
    }

    private void ResetToSetup()
    {
        State = PomodoroState.Setup;
        Objective = string.Empty;
        DurationMinutes = _settings?.WorkDurationMinutes ?? 25;
        TimerDisplay = "00:00";
        ProgressPercentage = 0;
        _currentSession = null;

        OnPropertyChanged(nameof(SessionTypeLabel));
    }

    #endregion
}
