using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.Infrastructure.Services;

public class NagerDateApiClient : INagerDateApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NagerDateApiClient> _logger;
    private const string BaseUrl = "https://date.nager.at/api/v3";

    public NagerDateApiClient(HttpClient httpClient, ILogger<NagerDateApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _logger = logger;
    }

    public async Task<IEnumerable<PublicHolidayApiDto>> GetPublicHolidaysAsync(
        int year, string countryCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v3/publicholidays/{year}/{countryCode}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch holidays for {CountryCode} {Year}: {StatusCode}",
                    countryCode, year, response.StatusCode);
                return [];
            }

            var holidays = await response.Content.ReadFromJsonAsync<List<PublicHolidayApiDto>>(
                cancellationToken: cancellationToken);

            return holidays ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching holidays for {CountryCode} {Year}", countryCode, year);
            return [];
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching holidays for {CountryCode} {Year}", countryCode, year);
            return [];
        }
    }

    public async Task<IEnumerable<CountryDto>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "/api/v3/AvailableCountries",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch available countries: {StatusCode}", response.StatusCode);
                return [];
            }

            var countries = await response.Content.ReadFromJsonAsync<List<CountryDto>>(
                cancellationToken: cancellationToken);

            return countries ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching available countries");
            return [];
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available countries");
            return [];
        }
    }
}
