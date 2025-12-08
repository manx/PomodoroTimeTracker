using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class PublicHolidayService(
    IUnitOfWork unitOfWork,
    INagerDateApiClient apiClient) : IPublicHolidayService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INagerDateApiClient _apiClient = apiClient;

    public async Task<IEnumerable<PublicHolidayDto>> GetHolidaysAsync(
        string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var hasCachedData = await _unitOfWork.PublicHolidays
            .HasCachedDataForYearAsync(countryCode, year, cancellationToken);

        if (!hasCachedData)
        {
            await RefreshHolidaysAsync(countryCode, year, cancellationToken);
        }

        var holidays = await _unitOfWork.PublicHolidays
            .GetByCountryAndYearAsync(countryCode, year, cancellationToken);

        return holidays.Select(MapToDto);
    }

    public async Task<IEnumerable<PublicHolidayDto>> GetHolidaysForDateRangeAsync(
        string countryCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Ensure we have data for all years in the range
        for (int year = startDate.Year; year <= endDate.Year; year++)
        {
            var hasCachedData = await _unitOfWork.PublicHolidays
                .HasCachedDataForYearAsync(countryCode, year, cancellationToken);

            if (!hasCachedData)
            {
                await RefreshHolidaysAsync(countryCode, year, cancellationToken);
            }
        }

        var holidays = await _unitOfWork.PublicHolidays
            .GetByCountryAndDateRangeAsync(countryCode, startDate, endDate, cancellationToken);

        return holidays.Select(MapToDto);
    }

    public async Task RefreshHolidaysAsync(
        string countryCode, int year, CancellationToken cancellationToken = default)
    {
        var apiHolidays = await _apiClient.GetPublicHolidaysAsync(year, countryCode, cancellationToken);

        if (!apiHolidays.Any())
        {
            return;
        }

        // Delete existing data for this country and year
        await _unitOfWork.PublicHolidays.DeleteByCountryAndYearAsync(countryCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var apiHoliday in apiHolidays)
        {
            var holiday = new PublicHoliday
            {
                Date = apiHoliday.Date,
                LocalName = apiHoliday.LocalName,
                Name = apiHoliday.Name,
                CountryCode = apiHoliday.CountryCode,
                IsGlobal = apiHoliday.Global,
                FetchedAt = now
            };

            await _unitOfWork.PublicHolidays.AddAsync(holiday, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<CountryDto>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAvailableCountriesAsync(cancellationToken);
    }

    public async Task<bool> IsHolidayAsync(
        string countryCode, DateTime date, CancellationToken cancellationToken = default)
    {
        var holidays = await GetHolidaysAsync(countryCode, date.Year, cancellationToken);
        return holidays.Any(h => h.Date.Date == date.Date);
    }

    private static PublicHolidayDto MapToDto(PublicHoliday entity)
    {
        return new PublicHolidayDto
        {
            Id = entity.Id,
            Date = entity.Date,
            LocalName = entity.LocalName,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            IsGlobal = entity.IsGlobal,
            FetchedAt = entity.FetchedAt
        };
    }
}
