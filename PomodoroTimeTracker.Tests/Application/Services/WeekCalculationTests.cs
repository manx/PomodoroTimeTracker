using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.Services;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Tests.Application.Services;

/// <summary>
/// Unit tests for week number calculations in AppSettingsService.
/// Tests ISO 8601 and US Standard week numbering edge cases.
/// </summary>
public class WeekCalculationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAppSettingsRepository> _settingsRepositoryMock;

    public WeekCalculationTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _settingsRepositoryMock = new Mock<IAppSettingsRepository>();

        _unitOfWorkMock.Setup(u => u.AppSettings).Returns(_settingsRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AppSettingsService CreateService()
    {
        return new AppSettingsService(_unitOfWorkMock.Object);
    }

    #region ISO 8601 Week Number Tests

    [Theory]
    [InlineData(2024, 1, 1, 1)]  // Jan 1, 2024 (Monday) - Week 1
    [InlineData(2024, 1, 7, 1)]  // Jan 7, 2024 (Sunday) - Week 1
    [InlineData(2025, 1, 1, 1)]  // Jan 1, 2025 (Wednesday) - Week 1
    [InlineData(2025, 1, 5, 1)]  // Jan 5, 2025 (Sunday) - Week 1
    [InlineData(2026, 1, 1, 1)]  // Jan 1, 2026 (Thursday) - Week 1
    [InlineData(2026, 1, 4, 1)]  // Jan 4, 2026 (Sunday) - Week 1
    [InlineData(2026, 1, 5, 2)]  // Jan 5, 2026 (Monday) - Week 2
    public async Task GetWeekNumber_Iso8601_FirstWeekOfYear(int year, int month, int day, int expectedWeek)
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

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        var date = new DateTime(year, month, day);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(expectedWeek);
    }

    [Theory]
    [InlineData(2024, 12, 29, 52)] // Dec 29, 2024 (Sunday) - Week 52
    [InlineData(2024, 12, 30, 1)]  // Dec 30, 2024 (Monday) - Week 1 of 2025
    [InlineData(2024, 12, 31, 1)]  // Dec 31, 2024 (Tuesday) - Week 1 of 2025
    [InlineData(2025, 12, 28, 52)] // Dec 28, 2025 (Sunday) - Week 52
    [InlineData(2025, 12, 29, 1)]  // Dec 29, 2025 (Monday) - Week 1 of 2026
    [InlineData(2025, 12, 31, 1)]  // Dec 31, 2025 (Wednesday) - Week 1 of 2026
    public async Task GetWeekNumber_Iso8601_LastWeekOfYear(int year, int month, int day, int expectedWeek)
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

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        var date = new DateTime(year, month, day);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(expectedWeek);
    }

    [Fact]
    public async Task GetWeekNumber_Iso8601_MidYearDate()
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

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        // July 4, 2024 (Thursday) - Should be week 27
        var date = new DateTime(2024, 7, 4);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(27);
    }

    #endregion

    #region US Standard Week Number Tests

    [Theory]
    [InlineData(2024, 1, 1, 1)]  // Jan 1, 2024 (Monday) - Week 1
    [InlineData(2024, 1, 6, 1)]  // Jan 6, 2024 (Saturday) - Week 1
    [InlineData(2024, 1, 7, 2)]  // Jan 7, 2024 (Sunday) - Week 2 (Sunday start)
    [InlineData(2025, 1, 1, 1)]  // Jan 1, 2025 (Wednesday) - Week 1
    [InlineData(2025, 1, 4, 1)]  // Jan 4, 2025 (Saturday) - Week 1
    [InlineData(2025, 1, 5, 2)]  // Jan 5, 2025 (Sunday) - Week 2 (Sunday start)
    public async Task GetWeekNumber_UsStandard_FirstWeekOfYear(int year, int month, int day, int expectedWeek)
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        var date = new DateTime(year, month, day);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(expectedWeek);
    }

    [Theory]
    [InlineData(2024, 12, 28, 52)] // Dec 28, 2024 (Saturday) - Week 52
    [InlineData(2024, 12, 29, 53)] // Dec 29, 2024 (Sunday) - Week 53
    [InlineData(2024, 12, 31, 53)] // Dec 31, 2024 (Tuesday) - Week 53
    [InlineData(2025, 12, 27, 52)] // Dec 27, 2025 (Saturday) - Week 52
    [InlineData(2025, 12, 28, 53)] // Dec 28, 2025 (Sunday) - Week 53
    [InlineData(2025, 12, 31, 53)] // Dec 31, 2025 (Wednesday) - Week 53
    public async Task GetWeekNumber_UsStandard_LastWeekOfYear(int year, int month, int day, int expectedWeek)
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        var date = new DateTime(year, month, day);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(expectedWeek);
    }

    [Fact]
    public async Task GetWeekNumber_UsStandard_WithMondayStart()
    {
        // Arrange
        var settings = new AppSettings
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow
        };

        _settingsRepositoryMock.Setup(r => r.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        // Jan 7, 2024 (Sunday)
        var date = new DateTime(2024, 1, 7);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        // With Monday start, Jan 1-7 contains only 6 days of week 1
        // Jan 7 (Sunday) is still in week 1
        result.Should().Be(1);
    }

    #endregion

    #region Default Behavior Tests

    [Fact]
    public void GetWeekNumber_WithNoCache_UsesIso8601Default()
    {
        // Arrange
        var service = CreateService();
        // Don't call GetSettingsAsync, so no cached settings

        // Dec 30, 2024 (Monday) - Week 1 of 2025 in ISO 8601
        var date = new DateTime(2024, 12, 30);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().Be(1); // ISO 8601 behavior
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData(2020, 12, 31)] // Thursday - Leap year end
    [InlineData(2021, 1, 1)]   // Friday - After leap year
    [InlineData(2023, 12, 31)] // Sunday - Regular year end
    [InlineData(2024, 1, 1)]   // Monday - Leap year start
    public async Task GetWeekNumber_HandlesLeapYearTransitions(int year, int month, int day)
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

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        var date = new DateTime(year, month, day);

        // Act
        var result = service.GetWeekNumber(date);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThanOrEqualTo(53);
    }

    [Fact]
    public async Task GetWeekNumber_ConsecutiveDates_IncrementsProperly()
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

        var service = CreateService();
        await service.GetSettingsAsync(); // Cache settings

        // Saturday Dec 30, 2023 to Sunday Dec 31, 2023
        var saturday = new DateTime(2023, 12, 30);
        var sunday = new DateTime(2023, 12, 31);

        // Act
        var saturdayWeek = service.GetWeekNumber(saturday);
        var sundayWeek = service.GetWeekNumber(sunday);

        // Assert
        // Both should be week 52 (same week in ISO 8601)
        saturdayWeek.Should().Be(52);
        sundayWeek.Should().Be(52);
    }

    #endregion
}
