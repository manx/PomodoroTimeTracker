using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

public sealed partial class ProjectDetailPage : Page
{
    public ProjectDetailViewModel ViewModel { get; }
    private readonly ILogger<ProjectDetailPage> _logger;
    private readonly IDialogService _dialogService;

    public ProjectDetailPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<ProjectDetailPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetService(typeof(ProjectDetailViewModel)) as ProjectDetailViewModel
                    ?? throw new InvalidOperationException("ProjectDetailViewModel not registered");

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
            _logger.LogInformation("Initializing ProjectDetailPage with parameter {Parameter}", parameter);

            if (parameter is int projectId)
            {
                await ViewModel.InitializeForEditAsync(projectId);
                _logger.LogInformation("ProjectDetailPage initialized for editing project {ProjectId}", projectId);
            }
            else
            {
                await ViewModel.InitializeForAddAsync();
                _logger.LogInformation("ProjectDetailPage initialized for adding new project");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ProjectDetailPage with parameter {Parameter}", parameter);
            await _dialogService.ShowErrorAsync("Unable to load project. Please try again.");
        }
    }
}
