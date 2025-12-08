using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IPublicHolidayService
{
    Task<IEnumerable<PublicHolidayDto>> GetHolidaysAsync(
        string countryCode, int year, CancellationToken cancellationToken = default);

    Task<IEnumerable<PublicHolidayDto>> GetHolidaysForDateRangeAsync(
        string countryCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task RefreshHolidaysAsync(
        string countryCode, int year, CancellationToken cancellationToken = default);

    Task<IEnumerable<CountryDto>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsHolidayAsync(
        string countryCode, DateTime date, CancellationToken cancellationToken = default);
}
