using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class ClientDetailPage : Page
{
    public ClientDetailViewModel ViewModel { get; }
    private readonly ILogger<ClientDetailPage> _logger;
    private readonly IDialogService _dialogService;

    public ClientDetailPage()
    {
        // Resolve dependencies first
        _logger = App.GetService<ILogger<ClientDetailPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetService(typeof(ClientDetailViewModel)) as ClientDetailViewModel
                    ?? throw new InvalidOperationException("ClientDetailViewModel not registered");

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
            _logger.LogInformation("Initializing ClientDetailPage with parameter {Parameter}", parameter);

            // Check if we're editing an existing client (parameter is client ID)
            // or adding a new one (parameter is null)
            if (parameter is int clientId)
            {
                await ViewModel.InitializeForEditAsync(clientId);
                _logger.LogInformation("ClientDetailPage initialized for editing client {ClientId}", clientId);
            }
            else
            {
                ViewModel.InitializeForAdd();
                _logger.LogInformation("ClientDetailPage initialized for adding new client");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ClientDetailPage with parameter {Parameter}", parameter);
            await _dialogService.ShowErrorAsync("Unable to load client. Please try again.");
        }
    }
}
