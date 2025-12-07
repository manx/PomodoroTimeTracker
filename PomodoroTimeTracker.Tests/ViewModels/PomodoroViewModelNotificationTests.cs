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
/// Unit tests for PomodoroViewModel notification integration.
/// Tests toast notification behavior during Pomodoro timer state transitions.
/// </summary>
public class PomodoroViewModelNotificationTests
{
    private readonly Mock<ITimeEntryService> _entryServiceMock;
    private readonly Mock<IPomodoroSettingsService> _settingsServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IActiveTimerService> _activeTimerServiceMock;
    private readonly Mock<IPomodoroStateService> _pomodoroStateServiceMock;
    private readonly Mock<IDispatcherTimer> _timerMock;

    // Notification tracking
    private int _workCompleteCallCount;
    private int _breakStartingCallCount;
    private int _breakCompleteCallCount;
    private string? _lastWorkCompleteObjective;
    private string? _lastBreakStartingObjective;
    private bool? _lastBreakStartingIsLong;

    public PomodoroViewModelNotificationTests()
    {
        _entryServiceMock = new Mock<ITimeEntryService>();
        _settingsServiceMock = new Mock<IPomodoroSettingsService>();
        _clientServiceMock = new Mock<IClientService>();
        _projectServiceMock = new Mock<IProjectService>();
        _audioServiceMock = new Mock<IAudioService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _navigationServiceMock = new Mock<INavigationService>();
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
                WrapUpNotificationVolume = 50,
                ShowNotification = true
            });
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
        _entryServiceMock.Setup(s => s.GetEntriesBySessionTypeAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TimeEntryDto>());
        _activeTimerServiceMock.Setup(a => a.TrySetActiveTimer(It.IsAny<ActiveTimerType>()))
            .Returns(true);
        _notificationServiceMock.Setup(n => n.IsSupported)
            .Returns(true);

        // Set up notification tracking with callbacks
        _notificationServiceMock.Setup(n => n.ShowWorkCompleteAsync(It.IsAny<string>()))
            .Callback<string>(obj =>
            {
                _workCompleteCallCount++;
                _lastWorkCompleteObjective = obj;
            })
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.ShowBreakStartingAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, bool>((obj, isLong) =>
            {
                _breakStartingCallCount++;
                _lastBreakStartingObjective = obj;
                _lastBreakStartingIsLong = isLong;
            })
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.ShowBreakCompleteAsync())
            .Callback(() => _breakCompleteCallCount++)
            .Returns(Task.CompletedTask);
    }

    private void ResetNotificationTracking()
    {
        _workCompleteCallCount = 0;
        _breakStartingCallCount = 0;
        _breakCompleteCallCount = 0;
        _lastWorkCompleteObjective = null;
        _lastBreakStartingObjective = null;
        _lastBreakStartingIsLong = null;
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
            _navigationServiceMock.Object,
            _activeTimerServiceMock.Object,
            _pomodoroStateServiceMock.Object,
            _timerMock.Object);
    }

    #region ShowWorkComplete Notification Tests

    [Fact]
    public async Task TriggerWrapUpNotification_WhenShowNotificationEnabled_CallsShowWorkCompleteAsync()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync("Write unit tests");
        ResetNotificationTracking();

        // Act - Simulate work period completion
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        // Assert
        _workCompleteCallCount.Should().Be(1);
        _lastWorkCompleteObjective.Should().Be("Write unit tests");
    }

    [Fact]
    public async Task TriggerWrapUpNotification_WhenShowNotificationDisabled_DoesNotCallShowWorkCompleteAsync()
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
                PlaySound = true,
                ShowNotification = false  // Disabled
            });

        var viewModel = await StartPomodoroAsync("Write unit tests");
        ResetNotificationTracking();

        // Act - Simulate work period completion
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        // Assert
        _workCompleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task TriggerWrapUpNotification_WhenIsNotSupported_DoesNotCallShowWorkCompleteAsync()
    {
        // Arrange
        _notificationServiceMock.Setup(n => n.IsSupported)
            .Returns(false);

        var viewModel = await StartPomodoroAsync("Write unit tests");
        ResetNotificationTracking();

        // Act - Simulate work period completion
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        // Assert
        _workCompleteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task TriggerWrapUpNotification_WithEmptyObjective_PassesEmptyStringToNotification()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync("");
        ResetNotificationTracking();

        // Act - Simulate work period completion
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        // Assert
        _workCompleteCallCount.Should().Be(1);
        _lastWorkCompleteObjective.Should().Be("");
    }

    [Fact]
    public async Task TriggerWrapUpNotification_WithSpecialCharactersInObjective_PassesCorrectString()
    {
        // Arrange
        var objectiveWithSpecialChars = "Review & update C# code: \"important\" task!";
        var viewModel = await StartPomodoroAsync(objectiveWithSpecialChars);
        ResetNotificationTracking();

        // Act - Simulate work period completion
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        // Assert
        _workCompleteCallCount.Should().Be(1);
        _lastWorkCompleteObjective.Should().Be(objectiveWithSpecialChars);
    }

    #endregion

    #region ShowBreakStarting Notification Tests

    [Fact]
    public async Task OnTimerComplete_WhenWrapUpEndsAndShowNotificationEnabled_CallsShowBreakStartingAsync()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync("Complete feature");

        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        SimulateTimerToWrapUp(viewModel);
        ResetNotificationTracking();

        // Act - Simulate wrap up period completion (should start break)
        SimulateWrapUpComplete(viewModel);
        await Task.Delay(100);

        // Assert - Should show short break notification (first pomodoro)
        _breakStartingCallCount.Should().Be(1);
        _lastBreakStartingObjective.Should().Be("Complete feature");
        _lastBreakStartingIsLong.Should().BeFalse();
    }

    [Fact]
    public async Task OnTimerComplete_WhenLongBreakStarting_CallsShowBreakStartingWithTrue()
    {
        // Arrange
        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Manually set pomodoro count to 3 (so next break will be long break)
        var viewModel = await StartPomodoroAsync("Pomodoro 4");

        var pomodoroCountField = typeof(PomodoroViewModel).GetField("_pomodoroCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        pomodoroCountField!.SetValue(viewModel, 3);  // Set to 3, so when incremented it becomes 4

        SimulateTimerToWrapUp(viewModel);
        ResetNotificationTracking();

        // Act - Complete wrap up (should trigger long break)
        SimulateWrapUpComplete(viewModel);
        await Task.Delay(100);

        // Assert - Should show long break notification
        _breakStartingCallCount.Should().Be(1);
        _lastBreakStartingIsLong.Should().BeTrue();
    }

    [Fact]
    public async Task OnTimerComplete_WhenShowNotificationDisabled_DoesNotCallShowBreakStartingAsync()
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
                PlaySound = true,
                ShowNotification = false  // Disabled
            });

        var viewModel = await StartPomodoroAsync("Test task");

        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        SimulateTimerToWrapUp(viewModel);
        ResetNotificationTracking();

        // Act - Complete wrap up
        SimulateWrapUpComplete(viewModel);
        await Task.Delay(100);

        // Assert
        _breakStartingCallCount.Should().Be(0);
    }

    [Fact]
    public async Task OnTimerComplete_WhenIsNotSupported_DoesNotCallShowBreakStartingAsync()
    {
        // Arrange
        _notificationServiceMock.Setup(n => n.IsSupported)
            .Returns(false);

        var viewModel = await StartPomodoroAsync("Test task");

        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        SimulateTimerToWrapUp(viewModel);
        ResetNotificationTracking();

        // Act - Complete wrap up
        SimulateWrapUpComplete(viewModel);
        await Task.Delay(100);

        // Assert
        _breakStartingCallCount.Should().Be(0);
    }

    #endregion

    #region ShowBreakComplete Notification Tests

    // NOTE: Break completion notifications are not currently implemented in PomodoroViewModel.
    // Timer_Tick does not have a check for break period ending (_remainingSeconds <= 0 && State == Break).
    // These tests are skipped until that functionality is added to the ViewModel.

    #endregion

    #region Notification Sequence Tests

    [Fact]
    public async Task FullPomodoroSequence_WhenNotificationsEnabled_CallsWorkCompleteAndBreakStartingNotifications()
    {
        // Arrange
        var viewModel = await StartPomodoroAsync("Complete cycle");

        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        ResetNotificationTracking();

        // Act - Complete: Work -> WrapUp -> Break
        SimulateTimerToWrapUp(viewModel);
        await Task.Delay(100);

        SimulateWrapUpComplete(viewModel);
        await Task.Delay(100);

        // Assert - Work complete and break starting notifications should be called
        // (Break complete is not implemented in Timer_Tick yet)
        _workCompleteCallCount.Should().Be(1);
        _breakStartingCallCount.Should().Be(1);
        _lastWorkCompleteObjective.Should().Be("Complete cycle");
        _lastBreakStartingObjective.Should().Be("Complete cycle");
        _lastBreakStartingIsLong.Should().BeFalse();
    }

    [Fact]
    public async Task MultiplePomodorosCycle_WhenNotificationsEnabled_CallsCorrectNotificationCounts()
    {
        // Arrange
        _entryServiceMock.Setup(e => e.CompleteEntryAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        ResetNotificationTracking();

        // Simulate 2 complete pomodoro cycles
        for (int cycle = 0; cycle < 2; cycle++)
        {
            var viewModel = await StartPomodoroAsync($"Cycle {cycle + 1}");

            // Complete work -> wrap up -> break starting
            SimulateTimerToWrapUp(viewModel);
            await Task.Delay(50);

            SimulateWrapUpComplete(viewModel);
            await Task.Delay(50);
        }

        // Assert - Should have 2 work complete and 2 break starting notifications
        // (Break complete is not implemented in Timer_Tick yet)
        _workCompleteCallCount.Should().Be(2);
        _breakStartingCallCount.Should().Be(2);
    }

    #endregion

    #region Helper Methods

    private async Task<PomodoroViewModel> StartPomodoroAsync(string objective)
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
        viewModel.Objective = objective;
        await ((AsyncRelayCommand)viewModel.StartPomodoroCommand).ExecuteAsync(null);

        return viewModel;
    }

    private void SimulateTimerToWrapUp(PomodoroViewModel viewModel)
    {
        // Use reflection to access private Timer_Tick method and simulate work period completion
        var timerTickMethod = typeof(PomodoroViewModel).GetMethod("Timer_Tick",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Set remaining seconds to 1 so next tick transitions to wrap up
        var remainingSecondsField = typeof(PomodoroViewModel).GetField("_remainingSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        remainingSecondsField!.SetValue(viewModel, 1);

        // Trigger tick - should transition to WrapUp
        timerTickMethod!.Invoke(viewModel, new object?[] { null, EventArgs.Empty });
    }

    private void SimulateWrapUpComplete(PomodoroViewModel viewModel)
    {
        // Simulate wrap up period completion (transitions to break)
        var timerTickMethod = typeof(PomodoroViewModel).GetMethod("Timer_Tick",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var remainingSecondsField = typeof(PomodoroViewModel).GetField("_remainingSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        remainingSecondsField!.SetValue(viewModel, 1);

        // Setup break entry creation
        var breakEntry = new TimeEntryDto
        {
            Id = 2,
            StartTime = DateTime.UtcNow,
            SessionTypeId = SessionType.Ids.ShortBreak,
            SessionTypeName = "Short Break",
            DurationMinutes = 5
        };
        _entryServiceMock.Setup(s => s.StartTimerEntryAsync(
            It.Is<CreateTimeEntryDto>(dto => dto.SessionTypeId == SessionType.Ids.ShortBreak || dto.SessionTypeId == SessionType.Ids.LongBreak)))
            .ReturnsAsync(breakEntry);

        // Trigger tick - should complete wrap up and start break
        timerTickMethod!.Invoke(viewModel, new object?[] { null, EventArgs.Empty });
    }

    #endregion
}
