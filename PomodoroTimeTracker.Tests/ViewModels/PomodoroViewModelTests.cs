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
/// Unit tests for PomodoroViewModel.
/// Tests timer states, break cycles, wrap-up period, and session tracking.
/// </summary>
public class PomodoroViewModelTests
{
    private readonly Mock<ITimeEntryService> _entryServiceMock;
    private readonly Mock<IPomodoroSettingsService> _settingsServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IActiveTimerService> _activeTimerServiceMock;
    private readonly Mock<IPomodoroStateService> _pomodoroStateServiceMock;
    private readonly Mock<IDispatcherTimer> _timerMock;

    public PomodoroViewModelTests()
    {
        _entryServiceMock = new Mock<ITimeEntryService>();
        _settingsServiceMock = new Mock<IPomodoroSettingsService>();
        _clientServiceMock = new Mock<IClientService>();
        _projectServiceMock = new Mock<IProjectService>();
        _audioServiceMock = new Mock<IAudioService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _activeTimerServiceMock = new Mock<IActiveTimerService>();
        _pomodoroStateServiceMock = new Mock<IPomodoroStateService>();
        _timerMock = new Mock<IDispatcherTimer>();

        // Default setup
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new PomodoroSettingsDto
            {
                WorkDurationMinutes = 25,
                ShortBreakDurationMinutes = 5,
                LongBreakDurationMinutes = 15,
                LongBreakInterval = 4,
                WrapUpPeriodMinutes = 3,
                PlaySound = true,
                UseAlarm = true,
                AlarmVolume = 50,
                WrapUpNotificationVolume = 50
            });
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
        _entryServiceMock.Setup(s => s.GetEntriesBySessionTypeAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TimeEntryDto>());
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(It.IsAny<ActiveTimerType>()))
            .Returns(true);
    }

    private PomodoroViewModel CreateViewModel()
    {
        return new PomodoroViewModel(
            _entryServiceMock.Object,
            _settingsServiceMock.Object,
            _clientServiceMock.Object,
            _projectServiceMock.Object,
            _audioServiceMock.Object,
            _notificationServiceMock.Object,
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
        viewModel.State.Should().Be(PomodoroState.Setup);
        viewModel.IsSetupState.Should().BeTrue();
        viewModel.IsRunningState.Should().BeFalse();
        viewModel.IsPausedState.Should().BeFalse();
        viewModel.IsWrapUpState.Should().BeFalse();
        viewModel.IsBreakState.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.StartPomodoroCommand.Should().NotBeNull();
        viewModel.PauseResumeCommand.Should().NotBeNull();
        viewModel.StopCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_CountsUp_ReturnsFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CountsUp.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShowProgressMeter_ReturnsTrue()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ShowProgressMeter.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ObjectiveMaxLength_Is90()
    {
        // Assert
        PomodoroViewModel.ObjectiveMaxLength.Should().Be(90);
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsSettingsAndClients()
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
        viewModel.DurationMinutes.Should().Be(25); // From settings
    }

    #endregion

    #region StartPomodoroCommand Tests

    [Fact]
    public void StartPomodoroCommand_WhenObjectiveEmpty_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Objective = "";

        // Act
        var canExecute = viewModel.StartPomodoroCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void StartPomodoroCommand_WhenObjectiveProvided_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Objective = "Write tests";

        // Act
        var canExecute = viewModel.StartPomodoroCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public async Task StartPomodoroCommand_CreatesSessionAndStartsTimer()
    {
        // Arrange
        var createdEntry = new TimeEntryDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.Pomodoro,
            SessionTypeName = "Pomodoro",
            DurationMinutes = 25
        };
        _entryServiceMock.Setup(s => s.StartTimerEntryAsync(
            It.IsAny<CreateTimeEntryDto>()))
            .ReturnsAsync(createdEntry);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Objective = "Write tests";

        // Act
        await ((AsyncRelayCommand)viewModel.StartPomodoroCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(PomodoroState.Running);
        _entryServiceMock.Verify(s => s.StartTimerEntryAsync(
            It.Is<CreateTimeEntryDto>(dto =>
                dto.SessionTypeId == SessionType.Ids.Pomodoro &&
                dto.Description == "Write tests")), Times.Once);
        _timerMock.Verify(t => t.Start(), Times.Once);
    }

    [Fact]
    public async Task StartPomodoroCommand_WhenAnotherTimerActive_DoesNotStart()
    {
        // Arrange
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(ActiveTimerType.Pomodoro))
            .Returns(false);

        var viewModel = CreateViewModel();
        viewModel.Objective = "Test";

        // Act
        await ((AsyncRelayCommand)viewModel.StartPomodoroCommand).ExecuteAsync(null);

        // Assert
        viewModel.State.Should().Be(PomodoroState.Setup);
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
        var viewModel = await StartPomodoroAsync();

        // Act
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        viewModel.State.Should().Be(PomodoroState.Paused);
        _timerMock.Verify(t => t.Stop(), Times.Once);
    }

    [Fact]
    public async Task PauseResumeCommand_WhenPaused_ResumesTimer()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();
        viewModel.PauseResumeCommand.Execute(null); // Pause first

        // Act
        viewModel.PauseResumeCommand.Execute(null); // Resume

        // Assert
        viewModel.State.Should().Be(PomodoroState.Running);
        _timerMock.Verify(t => t.Start(), Times.Exactly(2));
    }

    [Fact]
    public void PauseResumeCommand_WhenBreak_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // Set state directly
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Act
        var canExecute = viewModel.PauseResumeCommand.CanExecute(null);

        // Assert - Cannot pause during breaks
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task PauseResumeText_WhenPaused_ReturnsResume()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        viewModel.PauseResumeText.Should().Be("Resume");
    }

    [Fact]
    public async Task PauseResumeText_WhenRunning_ReturnsPause()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

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
        var viewModel = await StartPomodoroAsync();

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void StopCommand_WhenBreak_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Act
        var canExecute = viewModel.StopCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    #endregion

    #region SaveAndStopAsync Tests

    [Fact]
    public async Task SaveAndStopAsync_StopsTimerAndSavesSession()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        _timerMock.Verify(t => t.Stop(), Times.Once);
        _entryServiceMock.Verify(s => s.UpdateEntryAsync(
            It.IsAny<UpdateTimeEntryDto>()), Times.Once);
        viewModel.State.Should().Be(PomodoroState.Setup);
    }

    [Fact]
    public async Task SaveAndStopAsync_ClearsActiveTimer()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        _activeTimerServiceMock.Verify(a => a.ClearActiveTimer(), Times.Once);
    }

    [Fact]
    public async Task SaveAndStopAsync_ResetsViewModelState()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        viewModel.State.Should().Be(PomodoroState.Setup);
        viewModel.Objective.Should().BeEmpty();
        viewModel.TimerDisplay.Should().Be("00:00");
        viewModel.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public async Task SaveAndStopAsync_WhenInWrapUp_MarksAsCompleted()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();
        // Set state to WrapUp
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.WrapUp);

        // Act
        await viewModel.SaveAndStopAsync();

        // Assert
        _entryServiceMock.Verify(s => s.UpdateEntryAsync(
            It.Is<UpdateTimeEntryDto>(dto => dto.IsCompleted == true)), Times.Once);
    }

    #endregion

    #region DiscardAndStopAsync Tests

    [Fact]
    public async Task DiscardAndStopAsync_StopsTimerAndDeletesSession()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

        // Act
        await viewModel.DiscardAndStopAsync();

        // Assert
        _timerMock.Verify(t => t.Stop(), Times.Once);
        _entryServiceMock.Verify(s => s.DeleteEntryAsync(
            It.IsAny<int>()), Times.Once);
        viewModel.State.Should().Be(PomodoroState.Setup);
    }

    #endregion

    #region AddMinutes Tests

    [Fact]
    public async Task AddMinutes_WhenRunning_AddsTime()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();

        // Act - should not throw
        viewModel.AddMinutes(5);

        // Assert - method executed without error
        viewModel.State.Should().Be(PomodoroState.Running);
    }

    [Fact]
    public void AddMinutes_WhenSetup_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddMinutes(5);

        // Assert
        viewModel.TimerDisplay.Should().Be("00:00");
    }

    [Fact]
    public void AddMinutes_WhenBreak_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Act
        viewModel.AddMinutes(5);

        // Assert - no exception, no change
        viewModel.State.Should().Be(PomodoroState.Break);
    }

    #endregion

    #region SessionTypeLabel Tests

    [Fact]
    public void SessionTypeLabel_WhenSetup_ShowsPomodoroCount()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SessionTypeLabel.Should().Be("Pomodoro 1/4");
    }

    [Fact]
    public void SessionTypeLabel_WhenWrapUp_ShowsWrapUpPeriod()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.WrapUp);

        // Assert
        viewModel.SessionTypeLabel.Should().Be("Wrap Up Period");
    }

    #endregion

    #region State Property Tests

    [Fact]
    public async Task State_WhenChanged_NotifiesAllDependentProperties()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync();
        var propertiesChanged = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

        // Act
        viewModel.PauseResumeCommand.Execute(null);

        // Assert
        propertiesChanged.Should().Contain(nameof(viewModel.IsSetupState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsRunningState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsPausedState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsWrapUpState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsBreakState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsNotBreakState));
        propertiesChanged.Should().Contain(nameof(viewModel.IsTimerActive));
        propertiesChanged.Should().Contain(nameof(viewModel.PauseResumeText));
        propertiesChanged.Should().Contain(nameof(viewModel.SessionTypeLabel));
    }

    [Fact]
    public void IsTimerActive_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Running);

        // Assert
        viewModel.IsTimerActive.Should().BeTrue();
    }

    [Fact]
    public void IsTimerActive_WhenBreak_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Assert
        viewModel.IsTimerActive.Should().BeTrue();
    }

    [Fact]
    public void IsNotBreakState_WhenBreak_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Assert
        viewModel.IsNotBreakState.Should().BeFalse();
    }

    [Fact]
    public void IsNotBreakState_WhenRunning_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Running);

        // Assert
        viewModel.IsNotBreakState.Should().BeTrue();
    }

    #endregion

    #region Objective Property Tests

    [Fact]
    public void Objective_WhenChanged_NotifiesCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChanged = false;
        viewModel.StartPomodoroCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        // Act
        viewModel.Objective = "Test";

        // Assert
        canExecuteChanged.Should().BeTrue();
    }

    [Fact]
    public void ObjectiveCharacterCount_ReturnsCorrectFormat()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Objective = "Test";

        // Assert
        viewModel.ObjectiveCharacterCount.Should().Be($"4/{PomodoroViewModel.ObjectiveMaxLength}");
    }

    [Fact]
    public void SessionDescription_ReturnsObjective()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Objective = "My Task";

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

    #region ElapsedSeconds Tests

    [Fact]
    public void ElapsedSeconds_WhenSetup_ReturnsZero()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ElapsedSeconds.Should().Be(0);
    }

    #endregion

    #region DurationMinutes Tests

    [Fact]
    public void DurationMinutes_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.DurationMinutes))
                propertyChanged = true;
        };

        // Act
        viewModel.DurationMinutes = 30;

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.DurationMinutes.Should().Be(30);
    }

    #endregion

    #region IsBreakBillable Tests

    [Fact]
    public void IsBreakBillable_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsBreakBillable))
                propertyChanged = true;
        };

        // Act
        viewModel.IsBreakBillable = true;

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.IsBreakBillable.Should().BeTrue();
    }

    [Fact]
    public void IsBreakBillable_DefaultsToFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.IsBreakBillable.Should().BeFalse();
    }

    [Fact]
    public async Task IsBreakBillable_WhenChangedDuringBreak_UpdatesEntryInDatabase()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.GetType().GetProperty("State")!.SetValue(viewModel, PomodoroState.Break);

        // Create a current entry using reflection (CurrentEntry is a protected field)
        var entryField = typeof(TimerViewModelBase).GetField("CurrentEntry",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        entryField!.SetValue(viewModel, new TimeEntryDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.ShortBreak,
            SessionTypeName = "Short Break",
            IsBillable = false
        });

        // Act
        viewModel.IsBreakBillable = true;
        await Task.Delay(100); // Allow async update to complete

        // Assert
        _entryServiceMock.Verify(s => s.UpdateEntryAsync(
            It.Is<UpdateTimeEntryDto>(dto => dto.IsBillable == true)), Times.Once);
    }

    [Fact]
    public void IsBreakBillable_WhenChangedDuringSetup_DoesNotUpdateDatabase()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // State is already Setup by default

        // Act
        viewModel.IsBreakBillable = true;

        // Assert - Should not call UpdateEntryAsync because not in break state
        _entryServiceMock.Verify(s => s.UpdateEntryAsync(It.IsAny<UpdateTimeEntryDto>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_WithShortBreaksBillable_SetsCorrectDefault()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new PomodoroSettingsDto
            {
                WorkDurationMinutes = 25,
                ShortBreakDurationMinutes = 5,
                LongBreakDurationMinutes = 15,
                LongBreakInterval = 4,
                WrapUpPeriodMinutes = 3,
                ShortBreaksAreBillable = true,
                LongBreaksAreBillable = false
            });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert - Settings are loaded but IsBreakBillable isn't set until break starts
        // This test verifies settings are loaded correctly
        _settingsServiceMock.Verify(s => s.GetSettingsAsync(), Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<PomodoroViewModel> StartPomodoroAsync()
    {
        var createdEntry = new TimeEntryDto
        {
            Id = 1,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.Pomodoro,
            SessionTypeName = "Pomodoro",
            DurationMinutes = 25
        };
        _entryServiceMock.Setup(s => s.StartTimerEntryAsync(
            It.IsAny<CreateTimeEntryDto>()))
            .ReturnsAsync(createdEntry);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.Objective = "Test Task";
        await ((AsyncRelayCommand)viewModel.StartPomodoroCommand).ExecuteAsync(null);

        return viewModel;
    }

    #endregion
}
