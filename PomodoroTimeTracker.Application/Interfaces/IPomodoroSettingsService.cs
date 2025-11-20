using PomodoroTimeTracker.Application.DTOs;

namespace PomodoroTimeTracker.Application.Interfaces;

public interface IPomodoroSettingsService
{
    Task<PomodoroSettingsDto> GetSettingsAsync();
    Task<PomodoroSettingsDto> UpdateSettingsAsync(UpdatePomodoroSettingsDto dto);
    Task<int> CalculateDefaultShortBreak(int workDurationMinutes);
    Task<int> CalculateDefaultLongBreak(int workDurationMinutes);
}
