using System.Globalization;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

/// <summary>
/// Service for managing application-level settings and week calculations.
/// Settings are cached after first load for performance.
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private AppSettings? _cachedSettings;

    public AppSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the current application settings.
    /// </summary>
    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.AppSettings.GetSettingsAsync(cancellationToken);
        _cachedSettings = settings; // Cache for week calculation methods
        return MapToDto(settings);
    }

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    public async Task<AppSettingsDto> UpdateSettingsAsync(UpdateAppSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.AppSettings.GetSettingsAsync(cancellationToken);

        settings.LastOpenedSettingsTab = dto.LastOpenedSettingsTab;
        settings.LanguageOverride = dto.LanguageOverride;
        settings.DateFormatOverride = dto.DateFormatOverride;
        settings.WeekStartDay = dto.WeekStartDay;
        settings.WeekYearStandard = (WeekYearStandard)dto.WeekYearStandard;
        settings.LastModified = DateTime.UtcNow;

        _unitOfWork.AppSettings.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cachedSettings = settings; // Update cache
        return MapToDto(settings);
    }

    /// <summary>
    /// Updates only the last opened settings tab.
    /// </summary>
    public async Task UpdateLastOpenedTabAsync(string tabName, CancellationToken cancellationToken = default)
    {
        var settings = await _unitOfWork.AppSettings.GetSettingsAsync(cancellationToken);
        settings.LastOpenedSettingsTab = tabName;
        settings.LastModified = DateTime.UtcNow;

        _unitOfWork.AppSettings.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cachedSettings = settings; // Update cache
    }

    /// <summary>
    /// Gets the configured week start day from cached settings.
    /// </summary>
    public DayOfWeek GetWeekStartDay()
    {
        return _cachedSettings?.WeekStartDay ?? DayOfWeek.Sunday;
    }

    /// <summary>
    /// Calculates the week number for a given date using the configured standard.
    /// </summary>
    public int GetWeekNumber(DateTime date)
    {
        var standard = _cachedSettings?.WeekYearStandard ?? WeekYearStandard.Iso8601;

        if (standard == WeekYearStandard.Iso8601)
        {
            return GetIso8601WeekNumber(date);
        }
        else
        {
            return GetUsWeekNumber(date);
        }
    }

    /// <summary>
    /// Gets the start date of the week containing the given date.
    /// </summary>
    public DateTime GetWeekStart(DateTime date)
    {
        var weekStartDay = GetWeekStartDay();
        var daysFromWeekStart = ((int)date.DayOfWeek - (int)weekStartDay + 7) % 7;
        return date.Date.AddDays(-daysFromWeekStart);
    }

    /// <summary>
    /// Gets the end date of the week containing the given date.
    /// </summary>
    public DateTime GetWeekEnd(DateTime date)
    {
        return GetWeekStart(date).AddDays(6);
    }

    /// <summary>
    /// Calculates the ISO 8601 week number.
    /// Week 1 contains the first Thursday of the year, weeks start on Monday.
    /// </summary>
    private static int GetIso8601WeekNumber(DateTime date)
    {
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    /// <summary>
    /// Calculates the US week number.
    /// Week 1 contains January 1st, weeks start on the configured day.
    /// </summary>
    private int GetUsWeekNumber(DateTime date)
    {
        var weekStartDay = GetWeekStartDay();
        var jan1 = new DateTime(date.Year, 1, 1);
        var daysOffset = ((int)jan1.DayOfWeek - (int)weekStartDay + 7) % 7;
        var firstWeekStart = jan1.AddDays(-daysOffset);

        var daysDiff = (date.Date - firstWeekStart).Days;
        return (daysDiff / 7) + 1;
    }

    /// <summary>
    /// Maps the domain entity to a DTO.
    /// </summary>
    private static AppSettingsDto MapToDto(AppSettings settings)
    {
        return new AppSettingsDto
        {
            Id = settings.Id,
            LastOpenedSettingsTab = settings.LastOpenedSettingsTab,
            LanguageOverride = settings.LanguageOverride,
            DateFormatOverride = settings.DateFormatOverride,
            WeekStartDay = settings.WeekStartDay,
            WeekYearStandard = (int)settings.WeekYearStandard,
            LastModified = settings.LastModified
        };
    }
}
