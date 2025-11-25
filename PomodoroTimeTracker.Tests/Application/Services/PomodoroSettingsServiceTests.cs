using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for PomodoroSettingsService.
/// Tests settings retrieval, updates, and break time calculations.
/// </summary>
public class PomodoroSettingsServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPomodoroSettingsRepository> _settingsRepositoryMock;
    private readonly PomodoroSettingsService _service;

    public PomodoroSettingsServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _settingsRepositoryMock = new Mock<IPomodoroSettingsRepository>();

        _unitOfWorkMock.Setup(u => u.PomodoroSettings).Returns(_settingsRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        _service = new PomodoroSettingsService(_unitOfWorkMock.Object);
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings()
    {
        // Arrange
        var settings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.WorkDurationMinutes.Should().Be(25);
        result.ShortBreakDurationMinutes.Should().Be(5);
        result.LongBreakDurationMinutes.Should().Be(15);
        result.LongBreakInterval.Should().Be(4);
        result.ShowNotification.Should().BeTrue();
        result.PlaySound.Should().BeTrue();
        result.FlashWindow.Should().BeFalse();
        result.WrapUpPeriodMinutes.Should().Be(3);
        result.WrapUpNotificationVolume.Should().Be(50);
        result.UseAlarm.Should().BeTrue();
        result.AlarmVolume.Should().Be(50);

        _settingsRepositoryMock.Verify(r => r.GetSettingsAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsCustomSettings()
    {
        // Arrange
        var settings = new PomodoroSettings
        {
            Id = 1,
            WorkDurationMinutes = 50,
            ShortBreakDurationMinutes = 10,
            LongBreakDurationMinutes = 30,
            LongBreakInterval = 3,
            ShowNotification = false,
            PlaySound = false,
            FlashWindow = true,
            WrapUpPeriodMinutes = 5,
            WrapUpNotificationVolume = 75,
            UseAlarm = false,
            AlarmVolume = 25,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.WorkDurationMinutes.Should().Be(50);
        result.ShortBreakDurationMinutes.Should().Be(10);
        result.LongBreakDurationMinutes.Should().Be(30);
        result.LongBreakInterval.Should().Be(3);
        result.ShowNotification.Should().BeFalse();
        result.PlaySound.Should().BeFalse();
        result.FlashWindow.Should().BeTrue();
        result.WrapUpPeriodMinutes.Should().Be(5);
        result.WrapUpNotificationVolume.Should().Be(75);
        result.UseAlarm.Should().BeFalse();
        result.AlarmVolume.Should().Be(25);
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_WithNewValues_UpdatesAllSettings()
    {
        // Arrange
        var existingSettings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = DateTime.UtcNow.AddDays(-1)
        };

        var updateDto = new UpdatePomodoroSettingsDto
        {
            WorkDurationMinutes = 30,
            ShortBreakDurationMinutes = 6,
            LongBreakDurationMinutes = 18,
            LongBreakInterval = 3,
            ShowNotification = false,
            PlaySound = false,
            FlashWindow = true,
            WrapUpPeriodMinutes = 5,
            WrapUpNotificationVolume = 75,
            UseAlarm = false,
            AlarmVolume = 25
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(existingSettings);

        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        var afterUpdate = DateTime.UtcNow;

        result.Should().NotBeNull();
        result.WorkDurationMinutes.Should().Be(30);
        result.ShortBreakDurationMinutes.Should().Be(6);
        result.LongBreakDurationMinutes.Should().Be(18);
        result.LongBreakInterval.Should().Be(3);
        result.ShowNotification.Should().BeFalse();
        result.PlaySound.Should().BeFalse();
        result.FlashWindow.Should().BeTrue();
        result.WrapUpPeriodMinutes.Should().Be(5);
        result.WrapUpNotificationVolume.Should().Be(75);
        result.UseAlarm.Should().BeFalse();
        result.AlarmVolume.Should().Be(25);

        result.LastModified.Should().BeOnOrAfter(beforeUpdate);
        result.LastModified.Should().BeOnOrBefore(afterUpdate);

        _settingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesLastModifiedToUtcNow()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddDays(-5);
        var existingSettings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = originalTime
        };

        var updateDto = new UpdatePomodoroSettingsDto
        {
            WorkDurationMinutes = 25,
            ShortBreakDurationMinutes = 5,
            LongBreakDurationMinutes = 15,
            LongBreakInterval = 4,
            ShowNotification = true,
            PlaySound = true,
            FlashWindow = false,
            WrapUpPeriodMinutes = 3,
            WrapUpNotificationVolume = 50,
            UseAlarm = true,
            AlarmVolume = 50
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(existingSettings);

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        result.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.LastModified.Should().NotBe(originalTime);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithMinimumValues_UpdatesCorrectly()
    {
        // Arrange
        var existingSettings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = DateTime.UtcNow
        };

        var updateDto = new UpdatePomodoroSettingsDto
        {
            WorkDurationMinutes = 1,
            ShortBreakDurationMinutes = 1,
            LongBreakDurationMinutes = 1,
            LongBreakInterval = 1,
            ShowNotification = false,
            PlaySound = false,
            FlashWindow = false,
            WrapUpPeriodMinutes = 0,
            WrapUpNotificationVolume = 0,
            UseAlarm = false,
            AlarmVolume = 0
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(existingSettings);

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        result.WorkDurationMinutes.Should().Be(1);
        result.ShortBreakDurationMinutes.Should().Be(1);
        result.LongBreakDurationMinutes.Should().Be(1);
        result.LongBreakInterval.Should().Be(1);
        result.WrapUpPeriodMinutes.Should().Be(0);
        result.WrapUpNotificationVolume.Should().Be(0);
        result.AlarmVolume.Should().Be(0);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithMaximumValues_UpdatesCorrectly()
    {
        // Arrange
        var existingSettings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = DateTime.UtcNow
        };

        var updateDto = new UpdatePomodoroSettingsDto
        {
            WorkDurationMinutes = 120,
            ShortBreakDurationMinutes = 60,
            LongBreakDurationMinutes = 60,
            LongBreakInterval = 10,
            ShowNotification = true,
            PlaySound = true,
            FlashWindow = true,
            WrapUpPeriodMinutes = 10,
            WrapUpNotificationVolume = 100,
            UseAlarm = true,
            AlarmVolume = 100
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(existingSettings);

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        result.WorkDurationMinutes.Should().Be(120);
        result.ShortBreakDurationMinutes.Should().Be(60);
        result.LongBreakDurationMinutes.Should().Be(60);
        result.LongBreakInterval.Should().Be(10);
        result.WrapUpPeriodMinutes.Should().Be(10);
        result.WrapUpNotificationVolume.Should().Be(100);
        result.AlarmVolume.Should().Be(100);
    }

    #endregion

    #region CalculateDefaultShortBreak Tests

    [Theory]
    [InlineData(25, 5)]    // Default: 25 ÷ 5 = 5
    [InlineData(50, 10)]   // 50 ÷ 5 = 10
    [InlineData(30, 6)]    // 30 ÷ 5 = 6
    [InlineData(15, 3)]    // 15 ÷ 5 = 3
    [InlineData(60, 12)]   // 60 ÷ 5 = 12
    [InlineData(5, 1)]     // 5 ÷ 5 = 1
    [InlineData(1, 0)]     // 1 ÷ 5 = 0.2, rounds to 0
    public async Task CalculateDefaultShortBreak_WithVariousWorkDurations_ReturnsCorrectValue(
        int workDuration, int expectedShortBreak)
    {
        // Act
        var result = await _service.CalculateDefaultShortBreak(workDuration);

        // Assert
        result.Should().Be(expectedShortBreak);
    }

    #endregion

    #region CalculateDefaultLongBreak Tests

    [Theory]
    [InlineData(25, 15)]   // Default: (25 ÷ 5) × 3 = 15
    [InlineData(50, 30)]   // (50 ÷ 5) × 3 = 30
    [InlineData(30, 18)]   // (30 ÷ 5) × 3 = 18
    [InlineData(15, 9)]    // (15 ÷ 5) × 3 = 9
    [InlineData(60, 36)]   // (60 ÷ 5) × 3 = 36
    [InlineData(5, 3)]     // (5 ÷ 5) × 3 = 3
    [InlineData(1, 1)]     // (1 ÷ 5) × 3 = 0.6, rounds to 1
    public async Task CalculateDefaultLongBreak_WithVariousWorkDurations_ReturnsCorrectValue(
        int workDuration, int expectedLongBreak)
    {
        // Act
        var result = await _service.CalculateDefaultLongBreak(workDuration);

        // Assert
        result.Should().Be(expectedLongBreak);
    }

    #endregion

    #region Break Calculation Consistency Tests

    [Theory]
    [InlineData(25)]
    [InlineData(30)]
    [InlineData(50)]
    public async Task CalculateBreaks_LongBreakIsThreeTimesShortBreak(int workDuration)
    {
        // Act
        var shortBreak = await _service.CalculateDefaultShortBreak(workDuration);
        var longBreak = await _service.CalculateDefaultLongBreak(workDuration);

        // Assert
        longBreak.Should().Be(shortBreak * 3);
    }

    #endregion

    #region Settings Singleton Behavior Tests

    [Fact]
    public async Task UpdateSettingsAsync_ModifiesExistingSettings_NotCreatesNew()
    {
        // Arrange
        var existingSettings = new PomodoroSettings
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
            UseAlarm = true,
            AlarmVolume = 50,
            LastModified = DateTime.UtcNow
        };

        var updateDto = new UpdatePomodoroSettingsDto
        {
            WorkDurationMinutes = 30,
            ShortBreakDurationMinutes = 6,
            LongBreakDurationMinutes = 18,
            LongBreakInterval = 4,
            ShowNotification = true,
            PlaySound = true,
            FlashWindow = false,
            WrapUpPeriodMinutes = 3,
            WrapUpNotificationVolume = 50,
            UseAlarm = true,
            AlarmVolume = 50
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(default))
            .ReturnsAsync(existingSettings);

        // Act
        await _service.UpdateSettingsAsync(updateDto);

        // Assert
        existingSettings.Id.Should().Be(1); // Same entity, not new
        existingSettings.WorkDurationMinutes.Should().Be(30);

        _settingsRepositoryMock.Verify(r => r.GetSettingsAsync(default), Times.Once);
        _settingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
    }

    #endregion
}
