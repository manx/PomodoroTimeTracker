namespace PomodoroTimeTracker.Application.DTOs;

/// <summary>
/// Data Transfer Object for application settings.
/// </summary>
public class AppSettingsDto
{
    public int Id { get; set; }
    public string LastOpenedSettingsTab { get; set; } = "General";
    public string? LanguageOverride { get; set; }
    public string? DateFormatOverride { get; set; }
    public DayOfWeek WeekStartDay { get; set; }
    public int WeekYearStandard { get; set; } // 0 = ISO 8601, 1 = US
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Data Transfer Object for updating application settings.
/// </summary>
public class UpdateAppSettingsDto
{
    public string LastOpenedSettingsTab { get; set; } = "General";
    public string? LanguageOverride { get; set; }
    public string? DateFormatOverride { get; set; }
    public DayOfWeek WeekStartDay { get; set; }
    public int WeekYearStandard { get; set; }
}
