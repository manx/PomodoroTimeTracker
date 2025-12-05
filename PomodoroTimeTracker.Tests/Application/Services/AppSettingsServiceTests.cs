using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for AppSettingsService.
/// Tests settings retrieval, updates, tab management, and week calculations.
/// </summary>
public class AppSettingsServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock;
    private readonly AppSettingsService _service;

    public AppSettingsServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _settingsRepositoryMock = new Mock<IAppSettingsRepository>();

        _unitOfWorkMock.Setup(u => u.AppSettings).Returns(_settingsRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new AppSettingsService(_unitOfWorkMock.Object);
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.LastOpenedSettingsTab.Should().Be("General");
        result.LanguageOverride.Should().BeNull();
        result.DateFormatOverride.Should().BeNull();
        result.WeekStartDay.Should().Be(DayOfWeek.Sunday);
        result.WeekYearStandard.Should().Be(0); // ISO 8601 enum value

        _settingsRepositoryMock.Verify(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsCustomSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "Pomodoro",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.LastOpenedSettingsTab.Should().Be("Pomodoro");
        result.LanguageOverride.Should().Be("en-US");
        result.DateFormatOverride.Should().Be("MM/dd/yyyy");
        result.WeekStartDay.Should().Be(DayOfWeek.Monday);
        result.WeekYearStandard.Should().Be(1); // US Standard enum value
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_WithNewValues_UpdatesAllSettings()
    {
        // Arrange
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow.AddDays(-1)
        };

        var updateDto = new UpdateAppSettingsDto
        {
            LastOpenedSettingsTab = "Pomodoro",
            LanguageOverride = "en-GB",
            DateFormatOverride = "dd/MM/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = 1 // US Standard
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        var afterUpdate = DateTime.UtcNow;

        result.Should().NotBeNull();
        result.LastOpenedSettingsTab.Should().Be("Pomodoro");
        result.LanguageOverride.Should().Be("en-GB");
        result.DateFormatOverride.Should().Be("dd/MM/yyyy");
        result.WeekStartDay.Should().Be(DayOfWeek.Monday);
        result.WeekYearStandard.Should().Be(1);

        result.LastModified.Should().BeOnOrAfter(beforeUpdate);
        result.LastModified.Should().BeOnOrBefore(afterUpdate);

        _settingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesLastModifiedToUtcNow()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddDays(-5);
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = originalTime
        };

        var updateDto = new UpdateAppSettingsDto
        {
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        result.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.LastModified.Should().NotBe(originalTime);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithNullOverrides_UpdatesCorrectly()
    {
        // Arrange
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "Pomodoro",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow
        };

        var updateDto = new UpdateAppSettingsDto
        {
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        // Act
        var result = await _service.UpdateSettingsAsync(updateDto);

        // Assert
        result.LanguageOverride.Should().BeNull();
        result.DateFormatOverride.Should().BeNull();
    }

    #endregion

    #region UpdateLastOpenedTabAsync Tests

    [Fact]
    public async Task UpdateLastOpenedTabAsync_UpdatesOnlyTab()
    {
        // Arrange
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow.AddDays(-1)
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        // Act
        await _service.UpdateLastOpenedTabAsync("Pomodoro");

        // Assert
        existingSettings.LastOpenedSettingsTab.Should().Be("Pomodoro");
        existingSettings.LanguageOverride.Should().Be("en-US"); // Unchanged
        existingSettings.DateFormatOverride.Should().Be("MM/dd/yyyy"); // Unchanged
        existingSettings.WeekStartDay.Should().Be(DayOfWeek.Monday); // Unchanged

        _settingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLastOpenedTabAsync_UpdatesLastModified()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddDays(-5);
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = originalTime
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        // Act
        await _service.UpdateLastOpenedTabAsync("Pomodoro");

        // Assert
        existingSettings.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        existingSettings.LastModified.Should().NotBe(originalTime);
    }

    #endregion

    #region GetWeekStartDay Tests

    [Fact]
    public async Task GetWeekStartDay_ReturnsConfiguredDay()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act - Load settings first to cache them
        await _service.GetSettingsAsync();
        var result = _service.GetWeekStartDay();

        // Assert
        result.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void GetWeekStartDay_WithNoCache_ReturnsDefaultSunday()
    {
        // Arrange - Service without cached settings

        // Act
        var result = _service.GetWeekStartDay();

        // Assert
        result.Should().Be(DayOfWeek.Sunday);
    }

    #endregion

    #region GetWeekStart Tests

    [Fact]
    public async Task GetWeekStart_ReturnsCorrectDate_ForSundayStart()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await _service.GetSettingsAsync(); // Cache settings

        // Wednesday, December 4, 2024
        var date = new DateTime(2024, 12, 4);

        // Act
        var result = _service.GetWeekStart(date);

        // Assert
        // Week should start on Sunday, December 1, 2024
        result.Should().Be(new DateTime(2024, 12, 1));
        result.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task GetWeekStart_ReturnsCorrectDate_ForMondayStart()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await _service.GetSettingsAsync(); // Cache settings

        // Wednesday, December 4, 2024
        var date = new DateTime(2024, 12, 4);

        // Act
        var result = _service.GetWeekStart(date);

        // Assert
        // Week should start on Monday, December 2, 2024
        result.Should().Be(new DateTime(2024, 12, 2));
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetWeekStart_WithDateOnWeekStart_ReturnsSameDate()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await _service.GetSettingsAsync(); // Cache settings

        // Sunday, December 1, 2024
        var date = new DateTime(2024, 12, 1);

        // Act
        var result = _service.GetWeekStart(date);

        // Assert
        result.Should().Be(date);
    }

    #endregion

    #region GetWeekEnd Tests

    [Fact]
    public async Task GetWeekEnd_ReturnsCorrectDate()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        await _service.GetSettingsAsync(); // Cache settings

        // Wednesday, December 4, 2024
        var date = new DateTime(2024, 12, 4);

        // Act
        var result = _service.GetWeekEnd(date);

        // Assert
        // Week ends on Saturday, December 7, 2024 (week starts Sunday Dec 1, +6 days)
        result.Should().Be(new DateTime(2024, 12, 7));
        result.DayOfWeek.Should().Be(DayOfWeek.Saturday);
    }

    #endregion

    #region Settings Singleton Behavior Tests

    [Fact]
    public async Task UpdateSettingsAsync_ModifiesExistingSettings_NotCreatesNew()
    {
        // Arrange
        var existingSettings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.Iso8601,
            LastModified = DateTime.UtcNow
        };

        var updateDto = new UpdateAppSettingsDto
        {
            LastOpenedSettingsTab = "Pomodoro",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = 1
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        // Act
        await _service.UpdateSettingsAsync(updateDto);

        // Assert
        existingSettings.Id.Should().Be(1); // Same entity, not new
        existingSettings.LastOpenedSettingsTab.Should().Be("Pomodoro");

        _settingsRepositoryMock.Verify(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _settingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
    }

    #endregion
}
