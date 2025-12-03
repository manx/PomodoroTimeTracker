using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;
using PomodoroTimeTracker.WinUI3.Helpers;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class StopWatchPage : Page
{
    public StopWatchViewModel ViewModel { get; }
    private readonly ILogger<StopWatchPage> _logger;
    private readonly IDialogService _dialogService;
    private TimerWindow? _timerWindow;

    public StopWatchPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<StopWatchPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.GetService<StopWatchViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();

        // Set up the dialog callback after XAML initialization
        ViewModel.ShowStopDialog = ShowStopConfirmationDialogAsync;

        // Subscribe to state changes for timer window
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Set up context menu for timer area
        TimerGrid.ContextFlyout = TimerContextMenuHelper.CreateTimerContextMenu(ViewModel);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Show timer window when timer becomes active, hide when it stops
        if (e.PropertyName == nameof(ViewModel.IsTimerActive))
        {
            if (ViewModel.IsTimerActive)
            {
                ShowTimerWindow();
            }
            else
            {
                HideTimerWindow();
            }
        }
    }

    private void ShowTimerWindow()
    {
        if (_timerWindow == null)
        {
            _timerWindow = new TimerWindow(ViewModel);
            _timerWindow.Closed += (s, e) => _timerWindow = null;
        }
        _timerWindow.Activate();
    }

    private void HideTimerWindow()
    {
        if (_timerWindow != null)
        {
            _timerWindow.Close();
            _timerWindow = null;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Only initialize if we're in setup state (not returning to an active timer)
        if (ViewModel.IsSetupState)
        {
            _ = InitializeAsync();
        }
        else
        {
            // Timer is active, show the timer window
            ShowTimerWindow();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing StopWatchPage");
            await ViewModel.LoadAsync();
            _logger.LogInformation("StopWatchPage initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing StopWatchPage");
            await _dialogService.ShowErrorAsync("Unable to load stopwatch. Please try again.");
        }
    }

    private async Task<StopDialogResult> ShowStopConfirmationDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Stop Timer?",
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
