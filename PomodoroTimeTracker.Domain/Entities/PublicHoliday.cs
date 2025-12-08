namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Cached public holiday data from Nager.Date API.
/// </summary>
public class PublicHoliday
{
    public int Id { get; set; }

    /// <summary>
    /// The date of the holiday.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Holiday name in local language.
    /// </summary>
    public string LocalName { get; set; } = string.Empty;

    /// <summary>
    /// Holiday name in English.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "SE", "US").
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a nationwide holiday (vs regional).
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// When this record was fetched from the API.
    /// </summary>
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
