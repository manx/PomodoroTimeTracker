using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class PomodoroSettingsTab : UserControl
{
    public PomodoroSettingsViewModel ViewModel { get; }
    private readonly ILogger<PomodoroSettingsTab> _logger;
    private readonly IDialogService _dialogService;

    public PomodoroSettingsTab()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<PomodoroSettingsTab>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetRequiredService<PomodoroSettingsViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();

        // Load data asynchronously
        this.Loaded += PomodoroSettingsTab_Loaded;
    }

    private async void PomodoroSettingsTab_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Loading PomodoroSettingsTab");
            await ViewModel.LoadAsync();
            _logger.LogInformation("PomodoroSettingsTab loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading PomodoroSettingsTab");
            await _dialogService.ShowErrorAsync("Unable to load Pomodoro settings. Please try again.");
        }
    }

    private void WrapUpVolumeSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _ = ViewModel.TestWrapUpSoundCommand.ExecuteAsync(null);
    }

    private void AlarmVolumeSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _ = ViewModel.TestAlarmSoundCommand.ExecuteAsync(null);
    }
}
