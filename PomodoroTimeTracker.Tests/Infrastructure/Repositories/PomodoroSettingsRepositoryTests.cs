using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for PomodoroSettingsRepository using InMemory DbContext.
/// Tests singleton settings pattern and default initialization.
/// </summary>
public class PomodoroSettingsRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PomodoroSettingsRepository _repository;

    public PomodoroSettingsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PomodoroSettingsRepository(_context);
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_WithNoExistingSettings_CreatesAndReturnsDefaults()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0); // Should be saved to database
        result.WorkDurationMinutes.Should().Be(25);
        result.ShortBreakDurationMinutes.Should().Be(5); // 25 / 5
        result.LongBreakDurationMinutes.Should().Be(15); // (25 / 5) * 3
        result.LongBreakInterval.Should().Be(4);
        result.ShowNotification.Should().BeTrue();
        result.PlaySound.Should().BeTrue();
        result.FlashWindow.Should().BeFalse();
        result.WrapUpPeriodMinutes.Should().Be(3);
        result.WrapUpNotificationVolume.Should().Be(50);
        result.UseAlarm.Should().BeTrue();
        result.AlarmVolume.Should().Be(50);
        result.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetSettingsAsync_WithExistingSettings_ReturnsExistingSettings()
    {
        // Arrange - Create settings first
        var existingSettings = new PomodoroSettings
        {
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
            LastModified = DateTime.UtcNow.AddDays(-1)
        };

        await _context.PomodoroSettings.AddAsync(existingSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingSettings.Id);
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

    [Fact]
    public async Task GetSettingsAsync_CalledMultipleTimes_ReturnsSameSingletonSettings()
    {
        // Act
        var result1 = await _repository.GetSettingsAsync();
        var result2 = await _repository.GetSettingsAsync();

        // Assert
        result1.Id.Should().Be(result2.Id);

        // Verify only one settings record exists
        var allSettings = await _context.PomodoroSettings.ToListAsync();
        allSettings.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSettingsAsync_AutomaticallyCreatesDefaults_AndSavesToDatabase()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - Verify it was actually saved
        _context.ChangeTracker.Clear(); // Clear tracking to force fresh load

        var savedSettings = await _context.PomodoroSettings.FirstOrDefaultAsync();
        savedSettings.Should().NotBeNull();
        savedSettings!.Id.Should().Be(result.Id);
        savedSettings.WorkDurationMinutes.Should().Be(25);
    }

    #endregion

    #region Singleton Pattern Tests

    [Fact]
    public async Task GetSettingsAsync_EnforcesSingletonPattern()
    {
        // Arrange - Get settings twice
        var first = await _repository.GetSettingsAsync();
        var second = await _repository.GetSettingsAsync();

        // Act - Count total settings records
        var count = await _context.PomodoroSettings.CountAsync();

        // Assert - Should only have one record
        count.Should().Be(1);
        first.Id.Should().Be(second.Id);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ModifiesExistingSettings()
    {
        // Arrange - Get default settings
        var settings = await _repository.GetSettingsAsync();
        var originalId = settings.Id;

        // Act - Modify settings
        settings.WorkDurationMinutes = 30;
        settings.ShortBreakDurationMinutes = 6;
        settings.LongBreakDurationMinutes = 18;
        settings.LongBreakInterval = 3;
        settings.LastModified = DateTime.UtcNow;

        _repository.Update(settings);
        await _context.SaveChangesAsync();

        // Assert
        _context.ChangeTracker.Clear();

        var updated = await _repository.GetSettingsAsync();
        updated.Id.Should().Be(originalId); // Same record, not new
        updated.WorkDurationMinutes.Should().Be(30);
        updated.ShortBreakDurationMinutes.Should().Be(6);
        updated.LongBreakDurationMinutes.Should().Be(18);
        updated.LongBreakInterval.Should().Be(3);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public async Task GetSettingsAsync_CreatesDefaultsWithCorrectCalculations()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - Verify calculation formulas
        result.ShortBreakDurationMinutes.Should().Be(
            PomodoroSettings.CalculateDefaultShortBreak(result.WorkDurationMinutes));

        result.LongBreakDurationMinutes.Should().Be(
            PomodoroSettings.CalculateDefaultLongBreak(result.WorkDurationMinutes));
    }

    [Fact]
    public async Task GetSettingsAsync_DefaultsIncludeAllRequiredProperties()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - All properties should have values
        result.WorkDurationMinutes.Should().BeGreaterThan(0);
        result.ShortBreakDurationMinutes.Should().BeGreaterThan(0);
        result.LongBreakDurationMinutes.Should().BeGreaterThan(0);
        result.LongBreakInterval.Should().BeGreaterThan(0);
        result.WrapUpPeriodMinutes.Should().BeGreaterThanOrEqualTo(0);
        result.WrapUpNotificationVolume.Should().BeInRange(0, 100);
        result.AlarmVolume.Should().BeInRange(0, 100);
        result.LastModified.Should().NotBe(default(DateTime));
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
