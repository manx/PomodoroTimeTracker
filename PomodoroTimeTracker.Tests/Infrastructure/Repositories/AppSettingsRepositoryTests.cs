using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Infrastructure.Data;
using PomodoroTimeTracker.Infrastructure.Repositories;

namespace PomodoroTimeTracker.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for AppSettingsRepository using InMemory DbContext.
/// Tests singleton settings pattern and default initialization.
/// </summary>
public class AppSettingsRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AppSettingsRepository _repository;

    public AppSettingsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AppSettingsRepository(_context);
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
        result.LastOpenedSettingsTab.Should().Be("General");
        result.LanguageOverride.Should().BeNull();
        result.DateFormatOverride.Should().BeNull();
        result.WeekStartDay.Should().Be(DayOfWeek.Sunday);
        result.WeekYearStandard.Should().Be(WeekYearStandard.Iso8601);
        result.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetSettingsAsync_WithExistingSettings_ReturnsExistingSettings()
    {
        // Arrange - Create settings first
        var existingSettings = new AppSettings
        {
            LastOpenedSettingsTab = "Pomodoro",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = WeekYearStandard.UsStandard,
            LastModified = DateTime.UtcNow.AddDays(-1)
        };

        await _context.AppSettings.AddAsync(existingSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingSettings.Id);
        result.LastOpenedSettingsTab.Should().Be("Pomodoro");
        result.LanguageOverride.Should().Be("en-US");
        result.DateFormatOverride.Should().Be("MM/dd/yyyy");
        result.WeekStartDay.Should().Be(DayOfWeek.Monday);
        result.WeekYearStandard.Should().Be(WeekYearStandard.UsStandard);
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
        var allSettings = await _context.AppSettings.ToListAsync();
        allSettings.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSettingsAsync_AutomaticallyCreatesDefaults_AndSavesToDatabase()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - Verify it was actually saved
        _context.ChangeTracker.Clear(); // Clear tracking to force fresh load

        var savedSettings = await _context.AppSettings.FirstOrDefaultAsync();
        savedSettings.Should().NotBeNull();
        savedSettings!.Id.Should().Be(result.Id);
        savedSettings.LastOpenedSettingsTab.Should().Be("General");
        savedSettings.WeekStartDay.Should().Be(DayOfWeek.Sunday);
        savedSettings.WeekYearStandard.Should().Be(WeekYearStandard.Iso8601);
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
        var count = await _context.AppSettings.CountAsync();

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
        settings.LastOpenedSettingsTab = "Pomodoro";
        settings.LanguageOverride = "en-GB";
        settings.DateFormatOverride = "dd/MM/yyyy";
        settings.WeekStartDay = DayOfWeek.Monday;
        settings.WeekYearStandard = WeekYearStandard.UsStandard;
        settings.LastModified = DateTime.UtcNow;

        _repository.Update(settings);
        await _context.SaveChangesAsync();

        // Assert
        _context.ChangeTracker.Clear();

        var updated = await _repository.GetSettingsAsync();
        updated.Id.Should().Be(originalId); // Same record, not new
        updated.LastOpenedSettingsTab.Should().Be("Pomodoro");
        updated.LanguageOverride.Should().Be("en-GB");
        updated.DateFormatOverride.Should().Be("dd/MM/yyyy");
        updated.WeekStartDay.Should().Be(DayOfWeek.Monday);
        updated.WeekYearStandard.Should().Be(WeekYearStandard.UsStandard);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public async Task GetSettingsAsync_CreatesDefaultsWithCorrectValues()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - Verify default values
        result.LastOpenedSettingsTab.Should().Be("General");
        result.LanguageOverride.Should().BeNull();
        result.DateFormatOverride.Should().BeNull();
        result.WeekStartDay.Should().Be(DayOfWeek.Sunday);
        result.WeekYearStandard.Should().Be(WeekYearStandard.Iso8601);
    }

    [Fact]
    public async Task GetSettingsAsync_DefaultsIncludeAllRequiredProperties()
    {
        // Act
        var result = await _repository.GetSettingsAsync();

        // Assert - All properties should have values
        result.LastOpenedSettingsTab.Should().NotBeNullOrEmpty();
        result.WeekStartDay.Should().BeDefined();
        result.WeekYearStandard.Should().BeDefined();
        result.LastModified.Should().NotBe(default(DateTime));
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
