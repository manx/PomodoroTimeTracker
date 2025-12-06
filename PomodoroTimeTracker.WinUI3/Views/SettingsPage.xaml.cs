using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    private readonly ILogger<SettingsPage> _logger;
    private readonly IDialogService _dialogService;

    public SettingsPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<SettingsPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Check if navigation parameter contains a tab index
        if (e.Parameter is int tabIndex)
        {
            ViewModel.InitializeFromNavigation(tabIndex);
        }

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing SettingsPage");
            await ViewModel.LoadAsync();
            _logger.LogInformation("SettingsPage initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SettingsPage");
            await _dialogService.ShowErrorAsync("Unable to load settings. Please try again.");
        }
    }
}
