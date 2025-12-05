namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Represents general application preferences and configuration.
/// Singleton entity - only one settings record should exist per user.
/// Separate from PomodoroSettings which handles timer-specific configuration.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for settings.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the last opened settings tab to restore user's context.
    /// Default is "General".
    /// </summary>
    public string LastOpenedSettingsTab { get; set; } = "General";

    /// <summary>
    /// Gets or sets the language override code (e.g., "en-US", "es-ES").
    /// Null indicates the system language should be used.
    /// </summary>
    public string? LanguageOverride { get; set; }

    /// <summary>
    /// Gets or sets the date format override (e.g., "MM/dd/yyyy", "dd/MM/yyyy").
    /// Null indicates the system date format should be used.
    /// </summary>
    public string? DateFormatOverride { get; set; }

    /// <summary>
    /// Gets or sets the first day of the week for calendar displays.
    /// Default is Sunday (US standard). Common alternative is Monday (ISO 8601).
    /// </summary>
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    /// Gets or sets the standard used for week numbering and week year assignment.
    /// Default is ISO 8601 (week containing first Thursday).
    /// </summary>
    public WeekYearStandard WeekYearStandard { get; set; } = WeekYearStandard.Iso8601;

    /// <summary>
    /// Gets or sets when the settings were last modified (UTC).
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
