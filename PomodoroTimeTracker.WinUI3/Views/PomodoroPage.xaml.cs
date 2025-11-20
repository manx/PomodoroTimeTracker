using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

public sealed partial class PomodoroPage : Page
{
    public PomodoroViewModel ViewModel { get; }
    private readonly ILogger<PomodoroPage> _logger;
    private readonly IDialogService _dialogService;

    public PomodoroPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<PomodoroPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.GetService<PomodoroViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();

        // Set up the dialog callback after XAML initialization
        ViewModel.ShowStopDialog = ShowStopConfirmationDialogAsync;
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
            _logger.LogInformation("Initializing PomodoroPage");
            await ViewModel.LoadAsync();
            _logger.LogInformation("PomodoroPage initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PomodoroPage");
            await _dialogService.ShowErrorAsync("Unable to load pomodoro timer. Please try again.");
        }
    }

    private async Task<StopDialogResult> ShowStopConfirmationDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Stop Pomodoro?",
            Content = "What would you like to do with this session?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Discard",
            CloseButtonText = "Resume",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        return result switch
        {
            ContentDialogResult.Primary => StopDialogResult.Save,
            ContentDialogResult.Secondary => StopDialogResult.Discard,
            _ => StopDialogResult.Resume
        };
    }
}
