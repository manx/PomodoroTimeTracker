using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Display option for language selection.
/// </summary>
public sealed class LanguageOption
{
    public string? Code { get; }
    public string DisplayName { get; }

    public LanguageOption(string? code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }

    public static LanguageOption SystemDefault { get; } = new(null, "System Default");
}

/// <summary>
/// Display option for date format selection.
/// </summary>
public sealed class DateFormatOption
{
    public string? Format { get; }
    public string DisplayName { get; }
    public string Example { get; }

    public DateFormatOption(string? format, string displayName, string example)
    {
        Format = format;
        DisplayName = displayName;
        Example = example;
    }

    public static DateFormatOption SystemDefault { get; } = new(null, "System Default", DateTime.Now.ToShortDateString());
}

/// <summary>
/// Display option for week start day selection.
/// </summary>
public sealed class WeekStartDayOption
{
    public DayOfWeek DayOfWeek { get; }
    public string DisplayName { get; }

    public WeekStartDayOption(DayOfWeek dayOfWeek)
    {
        DayOfWeek = dayOfWeek;
        DisplayName = dayOfWeek.ToString();
    }
}

/// <summary>
/// Display option for week/year numbering standard selection.
/// </summary>
public sealed class WeekYearStandardOption
{
    public WeekYearStandard Standard { get; }
    public string DisplayName { get; }

    public WeekYearStandardOption(WeekYearStandard standard, string displayName)
    {
        Standard = standard;
        DisplayName = displayName;
    }
}

/// <summary>
/// ViewModel for the General Settings tab.
/// Manages application-level settings including language, date format, and week preferences.
/// </summary>
public sealed partial class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IDialogService _dialogService;
    private AppSettingsDto? _settings;

    private LanguageOption _selectedLanguage;
    private DateFormatOption _selectedDateFormat;
    private WeekStartDayOption _selectedWeekStartDay;
    private WeekYearStandardOption _selectedWeekYearStandard;
    private bool _isSaving;
    private bool _hasChanges;

    public GeneralSettingsViewModel(
        IAppSettingsService appSettingsService,
        IDialogService dialogService)
    {
        _appSettingsService = appSettingsService;
        _dialogService = dialogService;

        // Initialize available options
        AvailableLanguages = new ObservableCollection<LanguageOption>
        {
            LanguageOption.SystemDefault,
            new("en-US", "English (US)"),
            new("en-GB", "English (UK)")
        };

        AvailableDateFormats = new ObservableCollection<DateFormatOption>
        {
            DateFormatOption.SystemDefault,
            new("MM/dd/yyyy", "MM/dd/yyyy", DateTime.Now.ToString("MM/dd/yyyy")),
            new("dd/MM/yyyy", "dd/MM/yyyy", DateTime.Now.ToString("dd/MM/yyyy")),
            new("yyyy-MM-dd", "yyyy-MM-dd", DateTime.Now.ToString("yyyy-MM-dd"))
        };

        AvailableWeekStartDays = new ObservableCollection<WeekStartDayOption>
        {
            new(DayOfWeek.Sunday),
            new(DayOfWeek.Monday),
            new(DayOfWeek.Saturday)
        };

        AvailableWeekYearStandards = new ObservableCollection<WeekYearStandardOption>
        {
            new(WeekYearStandard.Iso8601, "ISO 8601 (International)"),
            new(WeekYearStandard.UsStandard, "US Standard")
        };

        // Set initial selections (will be overwritten on LoadAsync)
        _selectedLanguage = AvailableLanguages[0];
        _selectedDateFormat = AvailableDateFormats[0];
        _selectedWeekStartDay = AvailableWeekStartDays[0];
        _selectedWeekYearStandard = AvailableWeekYearStandards[0];

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsSaving && HasChanges);
        ResetToDefaultsCommand = new AsyncRelayCommand(ResetToDefaultsAsync);
    }

    #region Available Options (Read-Only)

    public ObservableCollection<LanguageOption> AvailableLanguages { get; }
    public ObservableCollection<DateFormatOption> AvailableDateFormats { get; }
    public ObservableCollection<WeekStartDayOption> AvailableWeekStartDays { get; }
    public ObservableCollection<WeekYearStandardOption> AvailableWeekYearStandards { get; }

    #endregion

    #region Observable Properties

    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                OnPropertyChanged(nameof(UseSystemLanguage));
                MarkAsChanged();
            }
        }
    }

    public bool UseSystemLanguage => SelectedLanguage.Code == null;

    public DateFormatOption SelectedDateFormat
    {
        get => _selectedDateFormat;
        set
        {
            if (SetProperty(ref _selectedDateFormat, value))
            {
                OnPropertyChanged(nameof(UseSystemDateFormat));
                MarkAsChanged();
            }
        }
    }

    public bool UseSystemDateFormat => SelectedDateFormat.Format == null;

    public WeekStartDayOption SelectedWeekStartDay
    {
        get => _selectedWeekStartDay;
        set
        {
            if (SetProperty(ref _selectedWeekStartDay, value))
            {
                MarkAsChanged();
            }
        }
    }

    public WeekYearStandardOption SelectedWeekYearStandard
    {
        get => _selectedWeekYearStandard;
        set
        {
            if (SetProperty(ref _selectedWeekYearStandard, value))
            {
                MarkAsChanged();
            }
        }
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

    public bool HasChanges
    {
        get => _hasChanges;
        set
        {
            SetProperty(ref _hasChanges, value);
            ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }

    #endregion

    #region Public Methods

    public async Task LoadAsync()
    {
        try
        {
            _settings = await _appSettingsService.GetSettingsAsync();

            // Load language setting
            var languageCode = _settings.LanguageOverride;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == languageCode)
                ?? LanguageOption.SystemDefault;

            // Load date format setting
            var dateFormat = _settings.DateFormatOverride;
            SelectedDateFormat = AvailableDateFormats.FirstOrDefault(d => d.Format == dateFormat)
                ?? DateFormatOption.SystemDefault;

            // Load week start day
            SelectedWeekStartDay = AvailableWeekStartDays.First(w => w.DayOfWeek == _settings.WeekStartDay);

            // Load week/year standard
            var weekYearStandard = (WeekYearStandard)_settings.WeekYearStandard;
            SelectedWeekYearStandard = AvailableWeekYearStandards.First(w => w.Standard == weekYearStandard);

            // Clear change flag after loading
            HasChanges = false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load settings: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Private Methods

    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var dto = new UpdateAppSettingsDto
            {
                LastOpenedSettingsTab = _settings?.LastOpenedSettingsTab ?? "General",
                LanguageOverride = SelectedLanguage.Code,
                DateFormatOverride = SelectedDateFormat.Format,
                WeekStartDay = SelectedWeekStartDay.DayOfWeek,
                WeekYearStandard = (int)SelectedWeekYearStandard.Standard
            };

            _settings = await _appSettingsService.UpdateSettingsAsync(dto);
            HasChanges = false;

            await _dialogService.ShowInformationAsync("Settings saved successfully.", "Success");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save settings: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Are you sure you want to reset all settings to their default values?",
            "Confirm Reset");

        if (!confirmed)
            return;

        // Reset to defaults
        SelectedLanguage = LanguageOption.SystemDefault;
        SelectedDateFormat = DateFormatOption.SystemDefault;
        SelectedWeekStartDay = AvailableWeekStartDays.First(w => w.DayOfWeek == DayOfWeek.Sunday);
        SelectedWeekYearStandard = AvailableWeekYearStandards.First(w => w.Standard == WeekYearStandard.Iso8601);

        // Changes will be marked automatically by property setters
    }

    private void MarkAsChanged()
    {
        if (_settings != null)
        {
            HasChanges = true;
        }
    }

    #endregion
}
