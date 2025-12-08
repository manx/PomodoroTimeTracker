using PomodoroTimeTracker.Domain.Entities;

namespace PomodoroTimeTracker.Domain.Interfaces;

public interface IPublicHolidayRepository : IRepository<PublicHoliday>
{
    Task<IEnumerable<PublicHoliday>> GetByCountryAndYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default);

    Task<IEnumerable<PublicHoliday>> GetByCountryAndDateRangeAsync(
        string countryCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<bool> HasCachedDataForYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default);

    Task DeleteByCountryAndYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default);
}
