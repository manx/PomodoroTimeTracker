using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.Domain.Interfaces;

namespace PomodoroTimeTracker.Application.Services;

public class PomodoroSettingsService(IUnitOfWork unitOfWork) : IPomodoroSettingsService
{
    public async Task<PomodoroSettingsDto> GetSettingsAsync()
    {
        var settings = await unitOfWork.PomodoroSettings.GetSettingsAsync();
        return MapToDto(settings);
    }

    public async Task<PomodoroSettingsDto> UpdateSettingsAsync(UpdatePomodoroSettingsDto dto)
    {
        var settings = await unitOfWork.PomodoroSettings.GetSettingsAsync();

        settings.WorkDurationMinutes = dto.WorkDurationMinutes;
        settings.ShortBreakDurationMinutes = dto.ShortBreakDurationMinutes;
        settings.LongBreakDurationMinutes = dto.LongBreakDurationMinutes;
        settings.LongBreakInterval = dto.LongBreakInterval;
        settings.ShowNotification = dto.ShowNotification;
        settings.PlaySound = dto.PlaySound;
        settings.FlashWindow = dto.FlashWindow;
        settings.WrapUpPeriodMinutes = dto.WrapUpPeriodMinutes;
        settings.WrapUpNotificationVolume = dto.WrapUpNotificationVolume;
        settings.WrapUpNotificationSound = dto.WrapUpNotificationSound;
        settings.UseAlarm = dto.UseAlarm;
        settings.AlarmVolume = dto.AlarmVolume;
        settings.AlarmSound = dto.AlarmSound;
        settings.LastModified = DateTime.UtcNow;

        unitOfWork.PomodoroSettings.Update(settings);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(settings);
    }

    public Task<int> CalculateDefaultShortBreak(int workDurationMinutes)
    {
        return Task.FromResult(PomodoroSettings.CalculateDefaultShortBreak(workDurationMinutes));
    }

    public Task<int> CalculateDefaultLongBreak(int workDurationMinutes)
    {
        return Task.FromResult(PomodoroSettings.CalculateDefaultLongBreak(workDurationMinutes));
    }

    private static PomodoroSettingsDto MapToDto(PomodoroSettings settings)
    {
        return new PomodoroSettingsDto
        {
            Id = settings.Id,
            WorkDurationMinutes = settings.WorkDurationMinutes,
            ShortBreakDurationMinutes = settings.ShortBreakDurationMinutes,
            LongBreakDurationMinutes = settings.LongBreakDurationMinutes,
            LongBreakInterval = settings.LongBreakInterval,
            ShowNotification = settings.ShowNotification,
            PlaySound = settings.PlaySound,
            FlashWindow = settings.FlashWindow,
            WrapUpPeriodMinutes = settings.WrapUpPeriodMinutes,
            WrapUpNotificationVolume = settings.WrapUpNotificationVolume,
            WrapUpNotificationSound = settings.WrapUpNotificationSound,
            UseAlarm = settings.UseAlarm,
            AlarmVolume = settings.AlarmVolume,
            AlarmSound = settings.AlarmSound,
            LastModified = settings.LastModified
        };
    }
}
