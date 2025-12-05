using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class GeneralSettingsTab : UserControl
{
    public GeneralSettingsViewModel ViewModel { get; }
    private readonly ILogger<GeneralSettingsTab> _logger;
    private readonly IDialogService _dialogService;

    public GeneralSettingsTab()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<GeneralSettingsTab>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetRequiredService<GeneralSettingsViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();

        // Load data asynchronously
        this.Loaded += GeneralSettingsTab_Loaded;
    }

    private async void GeneralSettingsTab_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Loading GeneralSettingsTab");
            await ViewModel.LoadAsync();
            _logger.LogInformation("GeneralSettingsTab loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GeneralSettingsTab");
            await _dialogService.ShowErrorAsync("Unable to load general settings. Please try again.");
        }
    }
}
