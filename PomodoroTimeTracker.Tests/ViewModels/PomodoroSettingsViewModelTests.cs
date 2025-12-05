using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for PomodoroSettingsViewModel.
/// Tests settings loading, saving, and property management.
/// </summary>
public class PomodoroSettingsViewModelTests
{
    private readonly Mock<IPomodoroSettingsService> _settingsServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;

    public PomodoroSettingsViewModelTests()
    {
        _settingsServiceMock = new Mock<IPomodoroSettingsService>();
        _audioServiceMock = new Mock<IAudioService>();

        // Default setup for audio service
        _audioServiceMock.Setup(a => a.GetAvailableWrapUpSounds())
            .Returns(new List<string> { "Windows Notify.wav" });
        _audioServiceMock.Setup(a => a.GetAvailableAlarmSounds())
            .Returns(new List<string> { "Alarm01.wav" });
    }

    private PomodoroSettingsViewModel CreateViewModel()
    {
        return new PomodoroSettingsViewModel(
            _settingsServiceMock.Object,
            _audioServiceMock.Object);
    }

    #region Billable Breaks Property Tests

    [Fact]
    public void ShortBreaksAreBillable_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ShortBreaksAreBillable))
                propertyChanged = true;
        };

        // Act
        viewModel.ShortBreaksAreBillable = true;

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.ShortBreaksAreBillable.Should().BeTrue();
    }

    [Fact]
    public void LongBreaksAreBillable_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.LongBreaksAreBillable))
                propertyChanged = true;
        };

        // Act
        viewModel.LongBreaksAreBillable = true;

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.LongBreaksAreBillable.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_LoadsBillableBreakSettings()
    {
        // Arrange
        var settings = new PomodoroSettingsDto
        {
            Id = 1,
            WorkDurationMinutes = 25,
            ShortBreakDurationMinutes = 5,
            LongBreakDurationMinutes = 15,
            LongBreakInterval = 4,
            ShowNotification = true,
            PlaySound = true,
            FlashWindow = false,
            WrapUpPeriodMinutes = 3,
            WrapUpNotificationVolume = 50,
            WrapUpNotificationSound = "Windows Notify.wav",
            UseAlarm = true,
            AlarmVolume = 50,
            AlarmSound = "Alarm01.wav",
            ShortBreaksAreBillable = true,
            LongBreaksAreBillable = true
        };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.ShortBreaksAreBillable.Should().BeTrue();
        viewModel.LongBreaksAreBillable.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WithDefaultBillableSettings_LoadsFalseValues()
    {
        // Arrange
        var settings = new PomodoroSettingsDto
        {
            Id = 1,
            WorkDurationMinutes = 25,
            ShortBreakDurationMinutes = 5,
            LongBreakDurationMinutes = 15,
            LongBreakInterval = 4,
            ShowNotification = true,
            PlaySound = true,
            FlashWindow = false,
            WrapUpPeriodMinutes = 3,
            WrapUpNotificationVolume = 50,
            WrapUpNotificationSound = "Windows Notify.wav",
            UseAlarm = true,
            AlarmVolume = 50,
            AlarmSound = "Alarm01.wav",
            ShortBreaksAreBillable = false,
            LongBreaksAreBillable = false
        };

        _settingsServiceMock.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.ShortBreaksAreBillable.Should().BeFalse();
        viewModel.LongBreaksAreBillable.Should().BeFalse();
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.ResetToDefaultsCommand.Should().NotBeNull();
        viewModel.CalculateDefaultBreaksCommand.Should().NotBeNull();
        viewModel.TestWrapUpSoundCommand.Should().NotBeNull();
        viewModel.TestAlarmSoundCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesAvailableSounds()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.AvailableWrapUpSounds.Should().Contain("Windows Notify.wav");
        viewModel.AvailableAlarmSounds.Should().Contain("Alarm01.wav");
    }

    [Fact]
    public void Constructor_DefaultBillableBreaksAreFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ShortBreaksAreBillable.Should().BeFalse();
        viewModel.LongBreaksAreBillable.Should().BeFalse();
    }

    #endregion

    #region Reset to Defaults Tests

    [Fact]
    public void ResetToDefaults_SetsBillableBreaksToFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ShortBreaksAreBillable = true;
        viewModel.LongBreaksAreBillable = true;

        // Act
        viewModel.ResetToDefaultsCommand.Execute(null);

        // Assert
        viewModel.ShortBreaksAreBillable.Should().BeFalse();
        viewModel.LongBreaksAreBillable.Should().BeFalse();
    }

    [Fact]
    public void ResetToDefaults_SetsAllDefaultValues()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.WorkDurationMinutes = 50;
        viewModel.ShortBreakDurationMinutes = 10;
        viewModel.LongBreakDurationMinutes = 30;
        viewModel.LongBreakInterval = 2;

        // Act
        viewModel.ResetToDefaultsCommand.Execute(null);

        // Assert
        viewModel.WorkDurationMinutes.Should().Be(25);
        viewModel.ShortBreakDurationMinutes.Should().Be(5);
        viewModel.LongBreakDurationMinutes.Should().Be(15);
        viewModel.LongBreakInterval.Should().Be(4);
        viewModel.WrapUpPeriodMinutes.Should().Be(3);
    }

    #endregion

    #region IsSaving Property Tests

    [Fact]
    public void IsSaving_WhenSet_UpdatesIsNotSaving()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.IsSaving = true;
        viewModel.IsNotSaving.Should().BeFalse();

        viewModel.IsSaving = false;
        viewModel.IsNotSaving.Should().BeTrue();
    }

    #endregion
}
