using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for StopWatchViewModel.
/// Tests timer states, elapsed time counting, and save/discard operations.
/// </summary>
public class StopWatchViewModelTests
{
    private readonly Mock<IPomodoroSessionService> _sessionServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IActiveTimerService> _activeTimerServiceMock;
    private readonly Mock<IDispatcherTimer> _timerMock;

    public StopWatchViewModelTests()
    {
        _sessionServiceMock = new Mock<IPomodoroSessionService>();
        _clientServiceMock = new Mock<IClientService>();
        _projectServiceMock = new Mock<IProjectService>();
        _activeTimerServiceMock = new Mock<IActiveTimerService>();
        _timerMock = new Mock<IDispatcherTimer>();

        // Default setup
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
        _sessionServiceMock.Setup(s => s.GetAllSessionsAsync())
            .ReturnsAsync(new List<PomodoroSessionDto>());
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(It.IsAny<ActiveTimerType>()))
            .Returns(true);
    }

    private StopWatchViewModel CreateViewModel()
    {
        return new StopWatchViewModel(
            _sessionServiceMock.Object,
            _clientServiceMock.Object,
            _projectServiceMock.Object,
            _activeTimerServiceMock.Object,
            _timerMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitialState_IsSetup()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.State.Should().Be(StopWatchState.Setup);
        viewModel.IsSetupState.Should().BeTrue();
        viewModel.IsRunningState.Should().BeFalse();
        viewModel.IsPausedState.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.StartTimerCommand.Should().NotBeNull();
        viewModel.PauseResumeCommand.Should().NotBeNull();
        viewModel.StopCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_CountsUp_ReturnsTrue()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CountsUp.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShowProgressMeter_ReturnsFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert - StopWatch doesn't show progress
        viewModel.ShowProgressMeter.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ProgressPercentage_AlwaysZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void Constructor_TimerDisplay_StartsAtZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.TimerDisplay.Should().Be("00:00");
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsClients()
    {
        // Arrange
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Client A" }
        };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.Clients.Should().HaveCount(1);
    }

    #endregion

    #region StartTimerCommand Tests

    [Fact]
    public void StartTimerCommand_WhenDescriptionEmpty_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Description = "";

        // Act
        var canExecute = viewModel.StartTimerCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void StartTimerCommand_WhenDescriptionProvided_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Description = "Test Task";

        // Act
        var canExecute = viewModel.StartTimerCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public async Task StartTimerCommand_CreatesSessionAndStartsTimer()
    {
        // Arrange
        var createdSession = new PomodoroSessionDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionType = SessionType.StopWatch
        };
        _sessionServiceMock.Setup(s => s.CreateSessionAsync(
            It.IsAny<CreatePomodoroSessionDto>()))
            .ReturnsAsync(createdSession);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Description = "Test Task";

        // Act
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(StopWatchState.Running);
        _sessionServiceMock.Verify(s => s.CreateSessionAsync(
            It.Is<CreatePomodoroSessionDto>(dto =>
                dto.SessionType == SessionType.StopWatch &&
                dto.DurationMinutes == 0)), Times.Once);
        _timerMock.Verify(t => t.Start(), Times.Once);
    }

    [Fact]
    public async Task StartTimerCommand_WhenAnotherTimerActive_DoesNotStart()
    {
        // Arrange
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(ActiveTimerType.StopWatch))
            .Returns(false);

        var viewModel = CreateViewModel();
        viewModel.Description = "Test Task";

        // Act
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(StopWatchState.Setup);
        _timerMock.Verify(t => t.Start(), Times.Never);
    }

    #endregion

    #region PauseResumeCommand Tests

    [Fact]
    public void PauseResumeCommand_WhenSetup_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.PauseResumeCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task PauseResumeCommand_WhenRunning_PausesTimer()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        viewModel.State.Should().Be(StopWatchState.Paused);
        _timerMock.Verify(t => t.Stop(), Times.Once);
    }

    [Fact]
    public async Task PauseResumeCommand_WhenPaused_ResumesTimer()
    {
        // Arrange
        var viewModel = await StartTimerAsync();
        viewModel.PauseResumeCommand.Execute(null); // Pause first

        // Act
        viewModel.PauseResumeCommand.Execute(null); // Resume

        // Assert
        viewModel.State.Should().Be(StopWatchState.Running);
        _timerMock.Verify(t => t.Start(), Times.Exactly(2));
    }

    [Fact]
    public async Task PauseResumeText_WhenPaused_ReturnsResume()
    {
        // Arrange
        var viewModel = await StartTimerAsync();
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        viewModel.PauseResumeText.Should().Be("Resume");
    }

    [Fact]
    public async Task PauseResumeText_WhenRunning_ReturnsPause()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Assert
        viewModel.PauseResumeText.Should().Be("Pause");
    }

    #endregion

    #region StopCommand Tests

    [Fact]
    public void StopCommand_WhenSetup_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task StopCommand_WhenRunning_CanExecute()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    #endregion

    #region SaveAndStopAsync Tests

    [Fact]
    public async Task SaveAndStopAsync_StopsTimerAndSavesSession()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        _timerMock.Verify(t => t.Stop(), Times.Once);
        _sessionServiceMock.Verify(s => s.UpdateSessionAsync(
            It.Is<UpdatePomodoroSessionDto>(dto => dto.IsCompleted == true)), Times.Once);
        viewModel.State.Should().Be(StopWatchState.Setup);
    }

    [Fact]
    public async Task SaveAndStopAsync_ClearsActiveTimer()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        _activeTimerServiceMock.Verify(a => a.ClearActiveTimer(), Times.Once);
    }

    [Fact]
    public async Task SaveAndStopAsync_ResetsViewModelState()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        viewModel.State.Should().Be(StopWatchState.Setup);
        viewModel.Description.Should().BeEmpty();
        viewModel.TimerDisplay.Should().Be("00:00");
        viewModel.ElapsedSeconds.Should().Be(0);
    }

    #endregion

    #region DiscardAndStopAsync Tests

    [Fact]
    public async Task DiscardAndStopAsync_StopsTimerAndDeletesSession()
    {
        // Arrange
        var viewModel = await StartTimerAsync();

        // Act
        await viewModel.DiscardAndStopAsync();

        // Assert
        _timerMock.Verify(t => t.Stop(), Times.Once);
        _sessionServiceMock.Verify(s => s.DeleteSessionAsync(
            It.IsAny<int>()), Times.Once);
        viewModel.State.Should().Be(StopWatchState.Setup);
    }

    #endregion

    #region AddMinutes Tests

    [Fact]
    public void AddMinutes_DoesNothing()
    {
        // Arrange - StopWatch's AddMinutes is a no-op (interface compatibility)
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddMinutes(5);

        // Assert - no exception, no change
        viewModel.TimerDisplay.Should().Be("00:00");
    }

    #endregion

    #region State Property Tests

    [Fact]
    public async Task State_WhenChanged_NotifiesAllDependentProperties()
    {
        // Arrange
        var viewModel = await StartTimerAsync();
        var propertiesChanged = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

        // Act
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        propertiesChanged.Should().Contain(nameof(viewModel.IsSetupState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsRunningState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsPausedState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsTimerActive));
        propertiesChanged.Should().Contain(nameof(viewModel.PauseResumeText));
    }

    [Fact]
    public void IsTimerActive_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Set state directly for testing
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, StopWatchState.Running);

        // Assert
        viewModel.IsTimerActive.Should().BeTrue();
    }

    [Fact]
    public void IsTimerActive_WhenPaused_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, StopWatchState.Paused);

        // Assert
        viewModel.IsTimerActive.Should().BeTrue();
    }

    #endregion

    #region Description Property Tests

    [Fact]
    public void Description_WhenChanged_NotifiesCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChanged = false;
        viewModel.StartTimerCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        // Act
        viewModel.Description = "Test";

        // Assert
        canExecuteChanged.Should().BeTrue();
    }

    [Fact]
    public void DescriptionCharacterCount_ReturnsCorrectFormat()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Description = "Test";

        // Assert
        viewModel.DescriptionCharacterCount.Should().Be($"4/{StopWatchViewModel.DescriptionMaxLength}");
    }

    [Fact]
    public void SessionDescription_ReturnsDescription()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Description = "My Task";

        // Assert
        viewModel.SessionDescription.Should().Be("My Task");
    }

    #endregion

    #region Client Selection Tests

    [Fact]
    public async Task SelectedClient_WhenChanged_LoadsProjects()
    {
        // Arrange
        var client = new ClientDto { Id = 1, Name = "Client" };
        var projects = new List<ProjectDto>
        {
            new() { Id = 1, Name = "Project 1", ClientId = 1 }
        };
        _projectServiceMock.Setup(s => s.GetProjectsByClientIdAsync(1))
            .ReturnsAsync(projects);

        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedClient = client;
        await Task.Delay(50);

        // Assert
        _projectServiceMock.Verify(s => s.GetProjectsByClientIdAsync(1), Times.Once);
    }

    [Fact]
    public void IsClientSelected_WhenClientSelected_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = new ClientDto { Id = 1, Name = "Client" };

        // Assert
        viewModel.IsClientSelected.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<StopWatchViewModel> StartTimerAsync()
    {
        var createdSession = new PomodoroSessionDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionType = SessionType.StopWatch,
            DurationMinutes = 0
        };
        _sessionServiceMock.Setup(s => s.CreateSessionAsync(
            It.IsAny<CreatePomodoroSessionDto>()))
            .ReturnsAsync(createdSession);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Description = "Test Task";
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        return viewModel;
    }

    #endregion
}
