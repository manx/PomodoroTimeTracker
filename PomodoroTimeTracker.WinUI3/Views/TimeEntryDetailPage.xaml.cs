using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class TimeEntryDetailPage : Page
{
    public TimeEntryDetailViewModel ViewModel { get; }
    private readonly ILogger<TimeEntryDetailPage> _logger;
    private readonly IDialogService _dialogService;

    public TimeEntryDetailPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<TimeEntryDetailPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetService(typeof(TimeEntryDetailViewModel)) as TimeEntryDetailViewModel
                    ?? throw new InvalidOperationException("TimeEntryDetailViewModel not registered");

        // Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = InitializeAsync(e.Parameter);
    }

    private async Task InitializeAsync(object? parameter)
    {
        try
        {
            _logger.LogInformation("Initializing TimeEntryDetailPage with parameter {Parameter}", parameter);

            if (parameter is int timeEntryId)
            {
                await ViewModel.InitializeForEditAsync(timeEntryId);
                _logger.LogInformation("TimeEntryDetailPage initialized for editing time entry {TimeEntryId}", timeEntryId);
            }
            else
            {
                await ViewModel.InitializeForAddAsync();
                _logger.LogInformation("TimeEntryDetailPage initialized for adding new time entry");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing TimeEntryDetailPage with parameter {Parameter}", parameter);
            await _dialogService.ShowErrorAsync("Unable to load time entry. Please try again.");
        }
    }
}
