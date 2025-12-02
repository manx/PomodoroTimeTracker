using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for the Pomodoro settings configuration page.
/// Manages user preferences for timer durations, alarms, and notifications.
/// </summary>
internal sealed partial class PomodoroSettingsViewModel : ViewModelBase
{
    private readonly IPomodoroSettingsService _settingsService;
    private readonly IAudioService _audioService;
    private PomodoroSettingsDto? _settings;

    private int _workDurationMinutes;
    private int _shortBreakDurationMinutes;
    private int _longBreakDurationMinutes;
    private int _longBreakInterval;
    private bool _showNotification;
    private bool _playSound;
    private bool _flashWindow;
    private int _wrapUpPeriodMinutes;
    private int _wrapUpNotificationVolume;
    private string _wrapUpNotificationSound = "Windows Notify.wav";
    private bool _useAlarm;
    private int _alarmVolume;
    private string _alarmSound = "Alarm01.wav";
    private bool _isSaving;

    public PomodoroSettingsViewModel(IPomodoroSettingsService settingsService, IAudioService audioService)
    {
        _settingsService = settingsService;
        _audioService = audioService;

        AvailableWrapUpSounds = _audioService.GetAvailableWrapUpSounds();
        AvailableAlarmSounds = _audioService.GetAvailableAlarmSounds();

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsSaving);
        ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);
        CalculateDefaultBreaksCommand = new AsyncRelayCommand(CalculateDefaultBreaksAsync);
        TestWrapUpSoundCommand = new AsyncRelayCommand(TestWrapUpSoundAsync);
        TestAlarmSoundCommand = new AsyncRelayCommand(TestAlarmSoundAsync);
    }

    public int WorkDurationMinutes
    {
        get => _workDurationMinutes;
        set => SetProperty(ref _workDurationMinutes, value);
    }

    public int ShortBreakDurationMinutes
    {
        get => _shortBreakDurationMinutes;
        set => SetProperty(ref _shortBreakDurationMinutes, value);
    }

    public int LongBreakDurationMinutes
    {
        get => _longBreakDurationMinutes;
        set => SetProperty(ref _longBreakDurationMinutes, value);
    }

    public int LongBreakInterval
    {
        get => _longBreakInterval;
        set => SetProperty(ref _longBreakInterval, value);
    }

    public bool ShowNotification
    {
        get => _showNotification;
        set => SetProperty(ref _showNotification, value);
    }

    public bool PlaySound
    {
        get => _playSound;
        set => SetProperty(ref _playSound, value);
    }

    public bool FlashWindow
    {
        get => _flashWindow;
        set => SetProperty(ref _flashWindow, value);
    }

    public int WrapUpPeriodMinutes
    {
        get => _wrapUpPeriodMinutes;
        set => SetProperty(ref _wrapUpPeriodMinutes, value);
    }

    public int WrapUpNotificationVolume
    {
        get => _wrapUpNotificationVolume;
        set => SetProperty(ref _wrapUpNotificationVolume, value);
    }

    public bool UseAlarm
    {
        get => _useAlarm;
        set => SetProperty(ref _useAlarm, value);
    }

    public int AlarmVolume
    {
        get => _alarmVolume;
        set => SetProperty(ref _alarmVolume, value);
    }

    public IReadOnlyList<string> AvailableWrapUpSounds { get; }

    public string WrapUpNotificationSound
    {
        get => _wrapUpNotificationSound;
        set => SetProperty(ref _wrapUpNotificationSound, value);
    }

    public IReadOnlyList<string> AvailableAlarmSounds { get; }

    public string AlarmSound
    {
        get => _alarmSound;
        set => SetProperty(ref _alarmSound, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            SetProperty(ref _isSaving, value);
            OnPropertyChanged(nameof(IsNotSaving));
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }

    public bool IsNotSaving => !IsSaving;

    public ICommand SaveCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand CalculateDefaultBreaksCommand { get; }
    public IAsyncRelayCommand TestWrapUpSoundCommand { get; }
    public IAsyncRelayCommand TestAlarmSoundCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            _settings = await _settingsService.GetSettingsAsync();
            WorkDurationMinutes = _settings.WorkDurationMinutes;
            ShortBreakDurationMinutes = _settings.ShortBreakDurationMinutes;
            LongBreakDurationMinutes = _settings.LongBreakDurationMinutes;
            LongBreakInterval = _settings.LongBreakInterval;
            ShowNotification = _settings.ShowNotification;
            PlaySound = _settings.PlaySound;
            FlashWindow = _settings.FlashWindow;
            WrapUpPeriodMinutes = _settings.WrapUpPeriodMinutes;
            WrapUpNotificationVolume = _settings.WrapUpNotificationVolume;
            WrapUpNotificationSound = _settings.WrapUpNotificationSound;
            UseAlarm = _settings.UseAlarm;
            AlarmVolume = _settings.AlarmVolume;
            AlarmSound = _settings.AlarmSound;
        }
        catch (Exception ex)
        {
            // TODO: Handle error properly
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var dto = new UpdatePomodoroSettingsDto
            {
                WorkDurationMinutes = WorkDurationMinutes,
                ShortBreakDurationMinutes = ShortBreakDurationMinutes,
                LongBreakDurationMinutes = LongBreakDurationMinutes,
                LongBreakInterval = LongBreakInterval,
                ShowNotification = ShowNotification,
                PlaySound = PlaySound,
                FlashWindow = FlashWindow,
                WrapUpPeriodMinutes = WrapUpPeriodMinutes,
                WrapUpNotificationVolume = WrapUpNotificationVolume,
                WrapUpNotificationSound = WrapUpNotificationSound,
                UseAlarm = UseAlarm,
                AlarmVolume = AlarmVolume,
                AlarmSound = AlarmSound
            };

            _settings = await _settingsService.UpdateSettingsAsync(dto);

            // TODO: Show success message
        }
        catch (Exception ex)
        {
            // TODO: Handle error properly
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetToDefaults()
    {
        WorkDurationMinutes = 25;
        ShortBreakDurationMinutes = 5;
        LongBreakDurationMinutes = 15;
        LongBreakInterval = 4;
        ShowNotification = true;
        PlaySound = true;
        FlashWindow = false;
        WrapUpPeriodMinutes = 3;
        WrapUpNotificationVolume = 50;
        WrapUpNotificationSound = "Windows Notify.wav";
        UseAlarm = true;
        AlarmVolume = 50;
        AlarmSound = "Alarm01.wav";
    }

    private async Task CalculateDefaultBreaksAsync()
    {
        try
        {
            ShortBreakDurationMinutes = await _settingsService.CalculateDefaultShortBreak(WorkDurationMinutes);
            LongBreakDurationMinutes = await _settingsService.CalculateDefaultLongBreak(WorkDurationMinutes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculating default breaks: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays the selected wrap-up notification sound at current volume for preview.
    /// </summary>
    public async Task TestWrapUpSoundAsync()
    {
        try
        {
            await _audioService.PlayWrapUpNotificationAsync(WrapUpNotificationVolume, WrapUpNotificationSound);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing wrap-up sound: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays the selected alarm sound at current volume for preview.
    /// </summary>
    public async Task TestAlarmSoundAsync()
    {
        try
        {
            await _audioService.PlayAlarmAsync(AlarmVolume, AlarmSound);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing alarm sound: {ex.Message}");
        }
    }
}
