namespace PomodoroTimeTracker.Application.DTOs;

public class PomodoroSettingsDto
{
    public int Id { get; set; }
    public int WorkDurationMinutes { get; set; }
    public int ShortBreakDurationMinutes { get; set; }
    public int LongBreakDurationMinutes { get; set; }
    public int LongBreakInterval { get; set; }
    public bool ShowNotification { get; set; }
    public bool PlaySound { get; set; }
    public bool FlashWindow { get; set; }
    public int WrapUpPeriodMinutes { get; set; }
    public int WrapUpNotificationVolume { get; set; }
    public string WrapUpNotificationSound { get; set; } = "Windows Notify.wav";
    public bool UseAlarm { get; set; }
    public int AlarmVolume { get; set; }
    public string AlarmSound { get; set; } = "Alarm01.wav";
    public DateTime LastModified { get; set; }
}

public class UpdatePomodoroSettingsDto
{
    public int WorkDurationMinutes { get; set; }
    public int ShortBreakDurationMinutes { get; set; }
    public int LongBreakDurationMinutes { get; set; }
    public int LongBreakInterval { get; set; }
    public bool ShowNotification { get; set; }
    public bool PlaySound { get; set; }
    public bool FlashWindow { get; set; }
    public int WrapUpPeriodMinutes { get; set; }
    public int WrapUpNotificationVolume { get; set; }
    public string WrapUpNotificationSound { get; set; } = "Windows Notify.wav";
    public bool UseAlarm { get; set; }
    public int AlarmVolume { get; set; }
    public string AlarmSound { get; set; } = "Alarm01.wav";
}
