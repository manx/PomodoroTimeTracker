using Microsoft.EntityFrameworkCore;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;
using PomodoroTimeTracker.Infrastructure.Data;

namespace PomodoroTimeTracker.Infrastructure.Repositories;

public class PublicHolidayRepository(ApplicationDbContext context) : Repository<PublicHoliday>(context), IPublicHolidayRepository
{
    public async Task<IEnumerable<PublicHoliday>> GetByCountryAndYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        return await _dbSet
            .Where(ph => ph.CountryCode == countryCode &&
                         ph.Date >= startDate &&
                         ph.Date <= endDate)
            .OrderBy(ph => ph.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PublicHoliday>> GetByCountryAndDateRangeAsync(
        string countryCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ph => ph.CountryCode == countryCode &&
                         ph.Date >= startDate &&
                         ph.Date <= endDate)
            .OrderBy(ph => ph.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasCachedDataForYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        return await _dbSet.AnyAsync(
            ph => ph.CountryCode == countryCode &&
                  ph.Date >= startDate &&
                  ph.Date <= endDate,
            cancellationToken);
    }

    public async Task DeleteByCountryAndYearAsync(
        string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        var holidays = await _dbSet
            .Where(ph => ph.CountryCode == countryCode &&
                         ph.Date >= startDate &&
                         ph.Date <= endDate)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(holidays);
    }
}
