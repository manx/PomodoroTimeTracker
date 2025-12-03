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
public sealed partial class StopWatchViewModel : ViewModelBase, ITimerWindowViewModel
{
    /// <summary>
    /// Maximum length for the description text field.
    /// </summary>
    public const int DescriptionMaxLength = 90;

    private readonly IPomodoroSessionService _sessionService;
    private readonly IClientService _clientService;
    private readonly IProjectService _projectService;
    private readonly IActiveTimerService _activeTimerService;
    private readonly IPomodoroStateService _pomodoroStateService;
    private readonly IDispatcherTimer _timer;

    private StopWatchState _state = StopWatchState.Setup;
    private int _elapsedSeconds;
    private PomodoroSessionDto? _currentSession;

    // Setup screen properties
    private ObservableCollection<ClientDto> _clients = new();
    private ObservableCollection<ProjectDto> _projects = new();
    private ClientDto? _selectedClient;
    private ProjectDto? _selectedProject;
    private string _description = string.Empty;

    // Timer display properties
    private string _timerDisplay = "00:00";

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
    {
        _sessionService = sessionService;
        _clientService = clientService;
        _projectService = projectService;
        _activeTimerService = activeTimerService;
        _pomodoroStateService = pomodoroStateService;
        _timer = timer;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;

        StartTimerCommand = new AsyncRelayCommand(StartTimerAsync, CanStartTimer);
        PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
        StopCommand = new AsyncRelayCommand(ShowStopDialogAsync, CanStop);
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
                OnPropertyChanged(nameof(IsTimerActive));
                OnPropertyChanged(nameof(PauseResumeText));
                ((AsyncRelayCommand)StartTimerCommand).NotifyCanExecuteChanged();
                ((RelayCommand)PauseResumeCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the timer is in Setup state.
    /// </summary>
    public bool IsSetupState => State == StopWatchState.Setup;

    /// <summary>
    /// Gets a value indicating whether the timer is actively running.
    /// </summary>
    public bool IsRunningState => State == StopWatchState.Running;

    /// <summary>
    /// Gets a value indicating whether the timer is paused.
    /// </summary>
    public bool IsPausedState => State == StopWatchState.Paused;

    /// <summary>
    /// Gets a value indicating whether the timer is active (running or paused).
    /// </summary>
    public bool IsTimerActive => State == StopWatchState.Running || State == StopWatchState.Paused;

    /// <summary>
    /// Gets a value indicating whether there's an active Pomodoro cycle.
    /// Used to show warning to user.
    /// </summary>
    public bool ShowCycleWarning => _pomodoroStateService.CurrentPomodoroCount > 0;

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
    /// Gets a value indicating whether the timer counts up. Stopwatch always counts up.
    /// </summary>
    public bool CountsUp => true;

    /// <inheritdoc/>
    /// <remarks>
    /// Stopwatch has no progress to track, so meter is not shown.
    /// </remarks>
    public bool ShowProgressMeter => false;

    /// <summary>
    /// Gets or sets the timer display string in HH:MM:SS or MM:SS format.
    /// </summary>
    public string TimerDisplay
    {
        get => _timerDisplay;
        set => SetProperty(ref _timerDisplay, value);
    }

    /// <summary>
    /// Gets the progress percentage (always 0 for stopwatch - no progress to track).
    /// </summary>
    public double ProgressPercentage => 0;

    /// <summary>
    /// Gets the icon glyph for the pause/resume button.
    /// </summary>
    public string PauseResumeIcon => State == StopWatchState.Running ? "\uE769" : "\uE768";

    /// <summary>
    /// Gets the text for the pause/resume button.
    /// </summary>
    public string PauseResumeText => IsPausedState ? "Resume" : "Pause";

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
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);

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

    /// <summary>
    /// Adds time to the current timer session.
    /// For stopwatch, this doesn't make much sense (already counting up indefinitely),
    /// but included for interface compatibility. Does nothing for stopwatch.
    /// </summary>
    /// <param name="minutes">Number of minutes to add (ignored for stopwatch)</param>
    public void AddMinutes(int minutes)
    {
        // Stopwatch counts up indefinitely - adding time doesn't make sense
        // Method exists only for ITimerWindowViewModel interface compatibility
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
                .Where(s => s.SessionType == SessionType.StopWatch)
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
        if (!_activeTimerService.TrySetActiveTimer(ActiveTimerType.StopWatch))
        {
            return; // Another timer is active
        }

        try
        {
            // Reset Pomodoro cycle when starting Stopwatch
            _pomodoroStateService.ResetCycle();
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

            _currentSession = await _sessionService.CreateSessionAsync(createDto);

            // Initialize timer
            _elapsedSeconds = 0;

            UpdateTimerDisplay();

            State = StopWatchState.Running;
            _timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting timer: {ex.Message}");
        }
    }

    private void PauseResume()
    {
        if (State == StopWatchState.Running)
        {
            _timer.Stop();
            State = StopWatchState.Paused;
        }
        else if (State == StopWatchState.Paused)
        {
            _timer.Start();
            State = StopWatchState.Running;
        }

        OnPropertyChanged(nameof(PauseResumeIcon));
    }

    private async Task ShowStopDialogAsync()
    {
        bool wasRunning = State == StopWatchState.Running;
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
    /// Saves the current session and returns to setup.
    /// </summary>
    public async Task SaveAndStopAsync()
    {
        _timer.Stop();

        if (_currentSession != null)
        {
            var updateDto = new UpdatePomodoroSessionDto
            {
                Id = _currentSession.Id,
                ProjectId = _currentSession.ProjectId,
                StartTime = _currentSession.StartTime,
                EndTime = DateTime.UtcNow,
                DurationMinutes = _currentSession.DurationMinutes,
                IsCompleted = true, // Stopwatch is always considered completed when stopped
                SessionType = _currentSession.SessionType,
                Objective = _currentSession.Objective,
                Notes = $"Stopped at {_timerDisplay}"
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

    private void ResetToSetup()
    {
        // Clear active timer when resetting to setup
        _activeTimerService.ClearActiveTimer();

        State = StopWatchState.Setup;
        Description = string.Empty;
        TimerDisplay = "00:00";
        _elapsedSeconds = 0;
        _currentSession = null;

        OnPropertyChanged(nameof(ShowCycleWarning));
    }

    #endregion
}
