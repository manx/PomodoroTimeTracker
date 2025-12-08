using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface INagerDateApiClient
{
    Task<IEnumerable<PublicHolidayApiDto>> GetPublicHolidaysAsync(
        int year, string countryCode, CancellationToken cancellationToken = default);

    Task<IEnumerable<CountryDto>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default);
}
