namespace PomodoroTimeTracker.Application.DTOs;

/// <summary>
/// DTO for public holiday data from Nager.Date API
/// </summary>
public class PublicHolidayApiDto
{
    public DateTime Date { get; set; }
    public string LocalName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool Global { get; set; }
}

/// <summary>
/// DTO for public holiday data stored in the application
/// </summary>
public class PublicHolidayDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string LocalName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public DateTime FetchedAt { get; set; }
}
