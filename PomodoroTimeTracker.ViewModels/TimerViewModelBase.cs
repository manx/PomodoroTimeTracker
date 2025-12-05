using System.Collections.ObjectModel;
using System.Windows.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Abstract base class for timer ViewModels.
/// Provides common functionality for client/project selection, session management, and stop dialog handling.
/// </summary>
public abstract class TimerViewModelBase : ViewModelBase, ITimerWindowViewModel
{
    #region Protected Services

    /// <summary>
    /// Service for managing time entries.
    /// </summary>
    protected readonly ITimeEntryService EntryService;

    /// <summary>
    /// Service for managing clients.
    /// </summary>
    protected readonly IClientService ClientService;

    /// <summary>
    /// Service for managing projects.
    /// </summary>
    protected readonly IProjectService ProjectService;

    /// <summary>
    /// Service for coordinating active timer state.
    /// </summary>
    protected readonly IActiveTimerService ActiveTimerService;

    /// <summary>
    /// Service for managing Pomodoro cycle state.
    /// </summary>
    protected readonly IPomodoroStateService PomodoroStateService;

    /// <summary>
    /// Timer for updating UI every second.
    /// </summary>
    protected readonly IDispatcherTimer Timer;

    /// <summary>
    /// The current time entry being tracked.
    /// </summary>
    protected TimeEntryDto? CurrentEntry;

    #endregion

    #region Private Fields

    private ObservableCollection<ClientDto> _clients = new();
    private ObservableCollection<ProjectDto> _projects = new();
    private ClientDto? _selectedClient;
    private ProjectDto? _selectedProject;
    private string _timerDisplay = "00:00";

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerViewModelBase"/> class.
    /// </summary>
    protected TimerViewModelBase(
        ITimeEntryService entryService,
        IClientService clientService,
        IProjectService projectService,
        IActiveTimerService activeTimerService,
        IPomodoroStateService pomodoroStateService,
        IDispatcherTimer timer)
    {
        EntryService = entryService;
        ClientService = clientService;
        ProjectService = projectService;
        ActiveTimerService = activeTimerService;
        PomodoroStateService = pomodoroStateService;
        Timer = timer;

        Timer.Interval = TimeSpan.FromSeconds(1);
    }

    #region Abstract Members - Children Must Implement

    /// <summary>
    /// Gets the timer type for active timer coordination.
    /// </summary>
    protected abstract ActiveTimerType TimerType { get; }

    /// <summary>
    /// Gets the session type ID for loading last session data.
    /// Use SessionType.Ids.Work, SessionType.Ids.Regular, SessionType.Ids.StopWatch, etc.
    /// </summary>
    protected abstract int SessionTypeId { get; }

    /// <summary>
    /// Gets the session description for TimerWindow display.
    /// </summary>
    public abstract string SessionDescription { get; }

    /// <summary>
    /// Gets the elapsed time in seconds for the current session.
    /// </summary>
    public abstract int ElapsedSeconds { get; }

    /// <summary>
    /// Gets the command to pause or resume the timer.
    /// </summary>
    public abstract ICommand PauseResumeCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is in Setup state.
    /// </summary>
    public abstract bool IsSetupState { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is actively running.
    /// </summary>
    public abstract bool IsRunningState { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is paused.
    /// </summary>
    public abstract bool IsPausedState { get; }

    /// <summary>
    /// Gets a value indicating whether the timer is in wrap up period.
    /// Override in children that don't support wrap up to return false.
    /// </summary>
    public abstract bool IsWrapUpState { get; }

    /// <summary>
    /// Gets the progress percentage (0-100) for the progress ring.
    /// </summary>
    public abstract double ProgressPercentage { get; }

    /// <summary>
    /// Pauses or resumes the timer. Called internally.
    /// </summary>
    protected abstract void PauseResumeCore();

    /// <summary>
    /// Resets the timer to setup state. Called after save/discard.
    /// </summary>
    protected abstract void ResetToSetupCore();

    /// <summary>
    /// Gets the notes to save when stopping early.
    /// </summary>
    protected abstract string GetStopNotes();

    /// <summary>
    /// Gets whether the session should be marked as completed when stopped.
    /// </summary>
    protected abstract bool IsCompletedWhenStopped();

    #endregion

    #region Virtual Members - Children Can Override

    /// <summary>
    /// Gets a value indicating whether the timer counts up (true) or down (false).
    /// Default is false (countdown).
    /// </summary>
    public virtual bool CountsUp => false;

    /// <summary>
    /// Gets a value indicating whether the progress meter should be shown.
    /// Default is true. StopWatch overrides to false.
    /// </summary>
    public virtual bool ShowProgressMeter => true;

    /// <summary>
    /// Adds time to the current timer session.
    /// Default implementation does nothing. Override in children that support adding time.
    /// </summary>
    /// <param name="minutes">Number of minutes to add</param>
    public virtual void AddMinutes(int minutes)
    {
        // Default: do nothing. Override in children that support adding time.
    }

    #endregion

    #region Common Properties

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
                OnSelectedProjectChanged();
            }
        }
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
    /// Gets the icon glyph for the pause/resume button.
    /// Returns pause icon when running, play icon when paused.
    /// </summary>
    public string PauseResumeIcon => IsRunningState ? "\uE769" : "\uE768"; // Pause : Play

    /// <summary>
    /// Gets the text for the pause/resume button.
    /// Returns "Resume" when paused, "Pause" otherwise.
    /// </summary>
    public string PauseResumeText => IsPausedState ? "Resume" : "Pause";

    /// <summary>
    /// Gets or sets the callback function to show the stop confirmation dialog.
    /// Set by the Page to maintain separation between ViewModel and View.
    /// </summary>
    public Func<Task<StopDialogResult>>? ShowStopDialog { get; set; }

    #endregion

    #region Protected Methods - For Child Classes

    /// <summary>
    /// Called when SelectedProject changes. Override to notify command state changes.
    /// </summary>
    protected virtual void OnSelectedProjectChanged()
    {
        // Override in children to notify command state changes
    }

    /// <summary>
    /// Loads clients from the database.
    /// </summary>
    protected async Task LoadClientsAsync()
    {
        var clients = await ClientService.GetAllClientsAsync();
        Clients = new ObservableCollection<ClientDto>(clients);
    }

    /// <summary>
    /// Loads projects for the currently selected client.
    /// </summary>
    protected async Task LoadProjectsForClientAsync()
    {
        if (SelectedClient == null)
        {
            Projects.Clear();
            SelectedProject = null;
            return;
        }

        try
        {
            var projects = await ProjectService.GetProjectsByClientIdAsync(SelectedClient.Id);
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

    /// <summary>
    /// Loads the last entry's client/project for the current timer type.
    /// </summary>
    protected async Task LoadLastSessionDataAsync()
    {
        try
        {
            var entries = await EntryService.GetEntriesBySessionTypeAsync(SessionTypeId);
            var lastEntry = entries
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefault();

            if (lastEntry?.ProjectId != null)
            {
                var project = await ProjectService.GetProjectByIdAsync(lastEntry.ProjectId.Value);
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

    /// <summary>
    /// Shows the stop dialog and handles the user's choice.
    /// </summary>
    protected async Task ShowStopDialogInternalAsync()
    {
        // Pause timer while dialog is showing
        bool wasRunning = IsRunningState;
        if (wasRunning)
        {
            PauseResumeCore();
        }

        if (ShowStopDialog != null)
        {
            var result = await ShowStopDialog();

            switch (result)
            {
                case StopDialogResult.Resume:
                    if (wasRunning)
                    {
                        PauseResumeCore(); // Resume
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
    /// Saves the current entry as a partial completion and returns to setup.
    /// </summary>
    public async Task SaveAndStopAsync()
    {
        Timer.Stop();

        if (CurrentEntry != null)
        {
            var updateDto = new UpdateTimeEntryDto
            {
                Id = CurrentEntry.Id,
                ProjectId = CurrentEntry.ProjectId,
                SessionTypeId = CurrentEntry.SessionTypeId,
                Description = CurrentEntry.Description,
                StartTime = CurrentEntry.StartTime,
                EndTime = DateTime.UtcNow,
                DurationMinutes = (int)(DateTime.UtcNow - CurrentEntry.StartTime).TotalMinutes,
                IsCompleted = IsCompletedWhenStopped(),
                Notes = GetStopNotes()
            };

            await EntryService.UpdateEntryAsync(updateDto);
        }

        ResetToSetupCore();
    }

    /// <summary>
    /// Deletes the current entry and returns to setup.
    /// </summary>
    public async Task DiscardAndStopAsync()
    {
        Timer.Stop();

        if (CurrentEntry != null)
        {
            await EntryService.DeleteEntryAsync(CurrentEntry.Id);
        }

        ResetToSetupCore();
    }

    /// <summary>
    /// Clears the active timer when resetting to setup.
    /// Call this at the beginning of ResetToSetupCore().
    /// </summary>
    protected void ClearActiveTimer()
    {
        ActiveTimerService.ClearActiveTimer();
    }

    /// <summary>
    /// Tries to set this timer as the active timer.
    /// Returns false if another timer is active.
    /// </summary>
    protected bool TrySetActiveTimer()
    {
        return ActiveTimerService.TrySetActiveTimer(TimerType);
    }

    #endregion
}
