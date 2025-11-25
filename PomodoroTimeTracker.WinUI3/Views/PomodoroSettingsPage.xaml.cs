using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class PomodoroSettingsPage : Page
{
    public PomodoroSettingsViewModel ViewModel { get; }
    private readonly ILogger<PomodoroSettingsPage> _logger;
    private readonly IDialogService _dialogService;

    public PomodoroSettingsPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<PomodoroSettingsPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetRequiredService<PomodoroSettingsViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing PomodoroSettingsPage");
            await ViewModel.LoadAsync();
            _logger.LogInformation("PomodoroSettingsPage initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PomodoroSettingsPage");
            await _dialogService.ShowErrorAsync("Unable to load settings. Please try again.");
        }
    }
}
