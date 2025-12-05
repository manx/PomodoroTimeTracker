using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Container ViewModel for the Settings TabView.
/// Manages tab selection and persists the last opened tab.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private int _selectedTabIndex;
    private bool _isInitialized;

    public SettingsViewModel(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
    }

    /// <summary>
    /// Currently selected tab index (bound to TabView.SelectedIndex).
    /// Tab indices:
    /// 0 = General
    /// 1 = PomodoroTimer
    /// 2 = RegularTimer
    /// 3 = StopWatch
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value) && _isInitialized)
            {
                // Save the tab selection asynchronously (fire and forget)
                _ = SaveSelectedTabAsync(value);
            }
        }
    }

    /// <summary>
    /// Loads the last opened settings tab from the service.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            var settings = await _appSettingsService.GetSettingsAsync();

            // Map tab name to index
            _selectedTabIndex = settings.LastOpenedSettingsTab switch
            {
                "General" => 0,
                "PomodoroTimer" => 1,
                "RegularTimer" => 2,
                "StopWatch" => 3,
                _ => 0 // Default to General if unknown
            };

            OnPropertyChanged(nameof(SelectedTabIndex));
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            // Log error but don't crash - default to first tab
            System.Diagnostics.Debug.WriteLine($"Error loading last opened tab: {ex.Message}");
            _selectedTabIndex = 0;
            OnPropertyChanged(nameof(SelectedTabIndex));
            _isInitialized = true;
        }
    }

    private async Task SaveSelectedTabAsync(int tabIndex)
    {
        try
        {
            // Map index to tab name
            var tabName = tabIndex switch
            {
                0 => "General",
                1 => "PomodoroTimer",
                2 => "RegularTimer",
                3 => "StopWatch",
                _ => "General"
            };

            await _appSettingsService.UpdateLastOpenedTabAsync(tabName);
        }
        catch (Exception ex)
        {
            // Log error but don't show to user (this is a non-critical operation)
            System.Diagnostics.Debug.WriteLine($"Error saving tab selection: {ex.Message}");
        }
    }
}
