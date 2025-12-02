using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.Services;
using PomodoroTimeTracker.WinUI3.ViewModels;
using WinRT.Interop;

namespace PomodoroTimeTracker.WinUI3
{
    internal sealed partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private AppWindow? _appWindow;
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

            // Get AppWindow and hook Closing event
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null)
            {
                _appWindow.Closing += AppWindow_Closing;
            }
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            // Get the PomodoroViewModel to check if there's an active session
            var pomodoroViewModel = App.Services.GetRequiredService<PomodoroViewModel>();

            // If there's an active work session (not during break), show the stop dialog
            if (pomodoroViewModel.State == PomodoroState.Running ||
                pomodoroViewModel.State == PomodoroState.Paused ||
                pomodoroViewModel.State == PomodoroState.WrapUp)
            {
                // Cancel the close for now
                args.Cancel = true;

                // Pause the timer if running
                if (pomodoroViewModel.State == PomodoroState.Running)
                {
                    pomodoroViewModel.PauseResumeCommand.Execute(null);
                }

                // Show the stop dialog
                var result = await ShowStopConfirmationDialogAsync(pomodoroViewModel);

                switch (result)
                {
                    case StopDialogResult.Resume:
                        // User wants to continue, just resume if paused
                        if (pomodoroViewModel.IsPausedState)
                        {
                            pomodoroViewModel.PauseResumeCommand.Execute(null);
                        }
                        break;

                    case StopDialogResult.Save:
                        // Save the session and close
                        await pomodoroViewModel.SaveAndStopAsync();
                        // Unsubscribe from the event to avoid recursion
                        if (_appWindow != null)
                        {
                            _appWindow.Closing -= AppWindow_Closing;
                        }
                        this.Close();
                        break;

                    case StopDialogResult.Discard:
                        // Discard the session and close
                        await pomodoroViewModel.DiscardAndStopAsync();
                        // Unsubscribe from the event to avoid recursion
                        if (_appWindow != null)
                        {
                            _appWindow.Closing -= AppWindow_Closing;
                        }
                        this.Close();
                        break;
                }
            }
        }

        private async Task<StopDialogResult> ShowStopConfirmationDialogAsync(PomodoroViewModel viewModel)
        {
            var dialog = new ContentDialog
            {
                Title = "Pomodoro In Progress",
                Content = $"You have a Pomodoro session running.\n\nElapsed time: {viewModel.TimerDisplay}\n\nWhat would you like to do?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Discard",
                CloseButtonText = "Resume",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            return result switch
            {
                ContentDialogResult.Primary => StopDialogResult.Save,
                ContentDialogResult.Secondary => StopDialogResult.Discard,
                _ => StopDialogResult.Resume
            };
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag?.ToString();
                NavigateToPage(tag);
            }
        }

        private async void NavigateToPage(string? tag)
        {
            Type? pageType = tag switch
            {
                "Dashboard" => null,
                "Pomodoro" => typeof(Views.PomodoroPage),
                "TimeEntry" => typeof(Views.TimeEntryListPage),
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
