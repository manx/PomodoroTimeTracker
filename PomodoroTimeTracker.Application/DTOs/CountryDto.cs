namespace PomodoroTimeTracker.Application.DTOs;

/// <summary>
/// DTO for country data from Nager.Date API
/// </summary>
public class CountryDto
{
    public string CountryCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
