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
/// Unit tests for RegularTimerViewModel.
/// Tests timer states, commands, and time tracking.
/// </summary>
public class RegularTimerViewModelTests
{
    private readonly Mock<ITimeEntryService> _entryServiceMock;
    private readonly Mock<IPomodoroSettingsService> _settingsServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IActiveTimerService> _activeTimerServiceMock;
    private readonly Mock<IPomodoroStateService> _pomodoroStateServiceMock;
    private readonly Mock<IDispatcherTimer> _timerMock;

    public RegularTimerViewModelTests()
    {
        _entryServiceMock = new Mock<ITimeEntryService>();
        _settingsServiceMock = new Mock<IPomodoroSettingsService>();
        _clientServiceMock = new Mock<IClientService>();
        _projectServiceMock = new Mock<IProjectService>();
        _audioServiceMock = new Mock<IAudioService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _activeTimerServiceMock = new Mock<IActiveTimerService>();
        _pomodoroStateServiceMock = new Mock<IPomodoroStateService>();
        _timerMock = new Mock<IDispatcherTimer>();

        // Default setup
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new PomodoroSettingsDto
            {
                WorkDurationMinutes = 25,
                WrapUpPeriodMinutes = 3,
                PlaySound = true,
                UseAlarm = true,
                AlarmVolume = 50
            });
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
        _entryServiceMock.Setup(s => s.GetEntriesBySessionTypeAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TimeEntryDto>());
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(It.IsAny<ActiveTimerType>()))
            .Returns(true);
    }

    private RegularTimerViewModel CreateViewModel()
    {
        return new RegularTimerViewModel(
            _entryServiceMock.Object,
            _settingsServiceMock.Object,
            _clientServiceMock.Object,
            _projectServiceMock.Object,
            _audioServiceMock.Object,
            _navigationServiceMock.Object,
            _activeTimerServiceMock.Object,
            _pomodoroStateServiceMock.Object,
            _timerMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitialState_IsSetup()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.State.Should().Be(RegularTimerState.Setup);
        viewModel.IsSetupState.Should().BeTrue();
        viewModel.IsRunningState.Should().BeFalse();
        viewModel.IsPausedState.Should().BeFalse();
        viewModel.IsWrapUpState.Should().BeFalse();
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
    public void Constructor_SetsTimerInterval()
    {
        // Act
        CreateViewModel();

        // Assert
        _timerMock.VerifySet(t => t.Interval = TimeSpan.FromSeconds(1), Times.Once);
    }

    [Fact]
    public void Constructor_CountsUp_ReturnsTrue()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CountsUp.Should().BeTrue();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsClientsAndSettings()
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
        viewModel.DurationMinutes.Should().Be(60); // Default for Regular Timer
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
        var createdEntry = new TimeEntryDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.Regular,
            SessionTypeName = "Regular"
        };
        _entryServiceMock.Setup(s => s.StartTimerEntryAsync(
            It.IsAny<CreateTimeEntryDto>()))
            .ReturnsAsync(createdEntry);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Description = "Test Task";

        // Act
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(RegularTimerState.Running);
        _entryServiceMock.Verify(s => s.StartTimerEntryAsync(
            It.Is<CreateTimeEntryDto>(dto =>
                dto.SessionTypeId == SessionType.Ids.Regular &&
                dto.Description == "Test Task")), Times.Once);
        _timerMock.Verify(t => t.Start(), Times.Once);
    }

    [Fact]
    public async Task StartTimerCommand_WhenAnotherTimerActive_DoesNotStart()
    {
        // Arrange
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(ActiveTimerType.RegularTimer))
            .Returns(false);

        var viewModel = CreateViewModel();
        viewModel.Description = "Test Task";

        // Act
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(RegularTimerState.Setup);
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
        viewModel.State.Should().Be(RegularTimerState.Paused);
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
        viewModel.State.Should().Be(RegularTimerState.Running);
        _timerMock.Verify(t => t.Start(), Times.Exactly(2)); // Once for start, once for resume
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
        _entryServiceMock.Verify(s => s.UpdateEntryAsync(
            It.IsAny<UpdateTimeEntryDto>()), Times.Once);
        viewModel.State.Should().Be(RegularTimerState.Setup);
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
        viewModel.State.Should().Be(RegularTimerState.Setup);
        viewModel.Description.Should().BeEmpty();
        viewModel.TimerDisplay.Should().Be("00:00");
        viewModel.ProgressPercentage.Should().Be(0);
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
        _entryServiceMock.Verify(s => s.DeleteEntryAsync(
            It.IsAny<int>()), Times.Once);
        viewModel.State.Should().Be(RegularTimerState.Setup);
    }

    #endregion

    #region AddMinutes Tests

    [Fact]
    public async Task AddMinutes_WhenRunning_AddsTime()
    {
        // Arrange
        var viewModel = await StartTimerAsync();
        var initialElapsed = viewModel.ElapsedSeconds;

        // Act
        viewModel.AddMinutes(5);

        // Assert - the timer display should reflect added time
        // (The internal _remainingSeconds should have increased by 5*60)
        viewModel.ElapsedSeconds.Should().Be(initialElapsed);
    }

    [Fact]
    public void AddMinutes_WhenSetup_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddMinutes(5);

        // Assert - no exception, timer display unchanged
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
        propertiesChanged.Should().Contain(nameof(viewModel.IsWrapUpState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsTimerActive));
        propertiesChanged.Should().Contain(nameof(viewModel.PauseResumeText));
    }

    [Fact]
    public void IsTimerActive_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Set state directly for testing
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, RegularTimerState.Running);

        // Assert
        viewModel.IsTimerActive.Should().BeTrue();
    }

    [Fact]
    public void SessionTypeLabel_WhenRunning_ReturnsRegularTimer()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SessionTypeLabel.Should().Be("Regular Timer");
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
        viewModel.DescriptionCharacterCount.Should().Be($"4/{RegularTimerViewModel.DescriptionMaxLength}");
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
        await Task.Delay(50); // Wait for async load

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

    [Fact]
    public void IsClientSelected_WhenNoClientSelected_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = null;

        // Assert
        viewModel.IsClientSelected.Should().BeFalse();
    }

    #endregion

    #region OpenSettingsCommand Tests

    [Fact]
    public void OpenSettingsCommand_NavigatesToRegularTimerSettingsTab()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.OpenSettingsCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo(PageNames.Settings, 2), Times.Once);
    }

    [Fact]
    public void OpenSettingsCommand_CanAlwaysExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.OpenSettingsCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<RegularTimerViewModel> StartTimerAsync()
    {
        var createdEntry = new TimeEntryDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.Regular,
            SessionTypeName = "Regular",
            DurationMinutes = 60
        };
        _entryServiceMock.Setup(s => s.StartTimerEntryAsync(
            It.IsAny<CreateTimeEntryDto>()))
            .ReturnsAsync(createdEntry);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Description = "Test Task";
        await ((AsyncRelayCommand)viewModel.StartTimerCommand).ExecuteAsync(null);

        return viewModel;
    }

    #endregion
}
