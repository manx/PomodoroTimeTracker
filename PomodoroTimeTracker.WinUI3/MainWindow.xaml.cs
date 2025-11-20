using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            // Get services from DI
            _navigationService = App.Services.GetRequiredService<INavigationService>();
            _dialogService = App.Services.GetRequiredService<IDialogService>();
            ViewModel = App.Services.GetRequiredService<MainWindowViewModel>();

            // Set the navigation frame
            _navigationService.NavigationFrame = ContentFrame;

            // Set window title
            Title = "Pomodoro Time Tracker";

            // Navigate to default page
            ContentFrame.Navigate(typeof(Views.ClientListPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag?.ToString();
                NavigateToPage(tag);
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private async void NavigateToPage(string? tag)
        {
            Type? pageType = tag switch
            {
                "Dashboard" => null,
                "Pomodoro" => typeof(Views.PomodoroPage),
                "TimeEntry" => null,
                "Clients" => typeof(Views.ClientListPage),
                "Projects" => typeof(Views.ProjectListPage),
                "Statistics" => null,
                "Settings" => typeof(Views.PomodoroSettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
            else
            {
                await _dialogService.ShowInformationAsync($"{tag} view not yet implemented", "Coming Soon");
            }
        }
    }
}
