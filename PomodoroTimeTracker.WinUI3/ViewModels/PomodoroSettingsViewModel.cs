using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.WinUI3.ViewModels;

/// <summary>
/// ViewModel for the Pomodoro settings configuration page.
/// Manages user preferences for timer durations, alarms, and notifications.
/// </summary>
public partial class PomodoroSettingsViewModel : ViewModelBase
{
    private readonly IPomodoroSettingsService _settingsService;
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
    private bool _useAlarm;
    private int _alarmVolume;
    private bool _isSaving;

    public PomodoroSettingsViewModel(IPomodoroSettingsService settingsService)
    {
        _settingsService = settingsService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsSaving);
        ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);
        CalculateDefaultBreaksCommand = new AsyncRelayCommand(CalculateDefaultBreaksAsync);
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
            UseAlarm = _settings.UseAlarm;
            AlarmVolume = _settings.AlarmVolume;
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
                UseAlarm = UseAlarm,
                AlarmVolume = AlarmVolume
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
        UseAlarm = true;
        AlarmVolume = 50;
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
}
