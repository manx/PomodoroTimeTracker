using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using PomodoroTimeTracker.WinUI3.Helpers;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;
using Windows.Foundation;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class RegularTimerPage : Page
{
    public RegularTimerViewModel ViewModel { get; }
    private readonly ILogger<RegularTimerPage> _logger;
    private readonly IDialogService _dialogService;
    private TimerWindow? _timerWindow;

    public RegularTimerPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<RegularTimerPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.GetService<RegularTimerViewModel>();

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();

        // Set up the dialog callback after XAML initialization
        ViewModel.ShowStopDialog = ShowStopConfirmationDialogAsync;

        // Subscribe to state changes for progress arc updates and timer window
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
        // Update the progress arc when progress percentage changes
        else if (e.PropertyName == nameof(ViewModel.ProgressPercentage))
        {
            UpdateProgressArc();
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
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing RegularTimerPage");
            await ViewModel.LoadAsync();
            _logger.LogInformation("RegularTimerPage initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing RegularTimerPage");
            await _dialogService.ShowErrorAsync("Unable to load regular timer. Please try again.");
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

    private void UpdateProgressArc()
    {
        // Time Timer style: arc decreases clockwise from top (12 o'clock position)
        // At 100% (start), full circle is shown
        // At 0% (end), no arc is shown

        var percentage = ViewModel.ProgressPercentage;
        var angle = (percentage / 100.0) * 360.0; // Convert to degrees

        // Build the geometry programmatically using WinUI 3 API
        var geometry = BuildArcGeometry(180, 180, 160, angle);
        ProgressPath.Data = geometry;
    }

    private PathGeometry BuildArcGeometry(double centerX, double centerY, double radius, double angleDegrees)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();

        // If angle is 0 or less, show nothing (empty path)
        if (angleDegrees <= 0)
        {
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        // Convert to radians (start from top, -90 degrees offset)
        var startAngleRad = -Math.PI / 2; // -90 degrees (12 o'clock)
        var endAngleRad = startAngleRad + (angleDegrees * Math.PI / 180.0);

        // Calculate start and end points
        var startX = centerX + radius * Math.Cos(startAngleRad);
        var startY = centerY + radius * Math.Sin(startAngleRad);
        var endX = centerX + radius * Math.Cos(endAngleRad);
        var endY = centerY + radius * Math.Sin(endAngleRad);

        // Start the path at the start point
        pathFigure.StartPoint = new Point(startX, startY);

        // Add arc segment
        var arcSegment = new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = angleDegrees > 180
        };
        pathFigure.Segments.Add(arcSegment);

        // Add line to center
        var lineSegment = new LineSegment { Point = new Point(centerX, centerY) };
        pathFigure.Segments.Add(lineSegment);

        // Close the path
        pathFigure.IsClosed = true;

        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }
}
