using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;
using WinRT.Interop;

namespace PomodoroTimeTracker.WinUI3
{
    internal sealed partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IActiveTimerService _activeTimerService;
        private AppWindow? _appWindow;
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            // Get services from DI
            _navigationService = App.Services.GetRequiredService<INavigationService>();
            _dialogService = App.Services.GetRequiredService<IDialogService>();
            _activeTimerService = App.Services.GetRequiredService<IActiveTimerService>();
            ViewModel = App.Services.GetRequiredService<MainWindowViewModel>();

            // Subscribe to active timer changes to update navigation menu
            _activeTimerService.PropertyChanged += ActiveTimerService_PropertyChanged;

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

        private void ActiveTimerService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update navigation item enabled states when active timer changes
            UpdateNavigationItemStates();
        }

        private void UpdateNavigationItemStates()
        {
            PomodoroNavItem.IsEnabled = _activeTimerService.IsPomodoroEnabled;
            RegularTimerNavItem.IsEnabled = _activeTimerService.IsRegularTimerEnabled;
            StopWatchNavItem.IsEnabled = _activeTimerService.IsStopWatchEnabled;
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            // Check if any timer is active
            if (!_activeTimerService.HasActiveTimer)
            {
                return; // No active timer, allow close
            }

            // Cancel the close for now
            args.Cancel = true;

            // Get the appropriate ViewModel and show dialog based on active timer type
            ITimerWindowViewModel? activeViewModel = _activeTimerService.ActiveTimer switch
            {
                ActiveTimerType.Pomodoro => App.Services.GetRequiredService<PomodoroViewModel>(),
                ActiveTimerType.RegularTimer => App.Services.GetRequiredService<RegularTimerViewModel>(),
                ActiveTimerType.StopWatch => App.Services.GetRequiredService<StopWatchViewModel>(),
                _ => null
            };

            if (activeViewModel == null)
            {
                return;
            }

            // Pause the timer if running
            if (activeViewModel.PauseResumeCommand.CanExecute(null))
            {
                activeViewModel.PauseResumeCommand.Execute(null);
            }

            // Show the stop dialog
            var result = await ShowStopConfirmationDialogAsync(activeViewModel);

            switch (result)
            {
                case StopDialogResult.Resume:
                    // User wants to continue, resume if paused
                    if (activeViewModel.PauseResumeCommand.CanExecute(null))
                    {
                        activeViewModel.PauseResumeCommand.Execute(null);
                    }
                    break;

                case StopDialogResult.Save:
                    // Save the session and close
                    await SaveActiveTimerAsync(activeViewModel);
                    // Unsubscribe from the event to avoid recursion
                    if (_appWindow != null)
                    {
                        _appWindow.Closing -= AppWindow_Closing;
                    }
                    this.Close();
                    break;

                case StopDialogResult.Discard:
                    // Discard the session and close
                    await DiscardActiveTimerAsync(activeViewModel);
                    // Unsubscribe from the event to avoid recursion
                    if (_appWindow != null)
                    {
                        _appWindow.Closing -= AppWindow_Closing;
                    }
                    this.Close();
                    break;
            }
        }

        private static async Task SaveActiveTimerAsync(ITimerWindowViewModel viewModel)
        {
            switch (viewModel)
            {
                case PomodoroViewModel pvm:
                    await pvm.SaveAndStopAsync();
                    break;
                case RegularTimerViewModel rtvm:
                    await rtvm.SaveAndStopAsync();
                    break;
                case StopWatchViewModel swvm:
                    await swvm.SaveAndStopAsync();
                    break;
            }
        }

        private static async Task DiscardActiveTimerAsync(ITimerWindowViewModel viewModel)
        {
            switch (viewModel)
            {
                case PomodoroViewModel pvm:
                    await pvm.DiscardAndStopAsync();
                    break;
                case RegularTimerViewModel rtvm:
                    await rtvm.DiscardAndStopAsync();
                    break;
                case StopWatchViewModel swvm:
                    await swvm.DiscardAndStopAsync();
                    break;
            }
        }

        private async Task<StopDialogResult> ShowStopConfirmationDialogAsync(ITimerWindowViewModel viewModel)
        {
            string timerType = _activeTimerService.ActiveTimer switch
            {
                ActiveTimerType.Pomodoro => "Pomodoro",
                ActiveTimerType.RegularTimer => "Regular Timer",
                ActiveTimerType.StopWatch => "Stop Watch",
                _ => "Timer"
            };

            var dialog = new ContentDialog
            {
                Title = $"{timerType} In Progress",
                Content = $"You have a {timerType} session running.\n\nElapsed time: {viewModel.TimerDisplay}\n\nWhat would you like to do?",
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
                "RegularTimer" => typeof(Views.RegularTimerPage),
                "StopWatch" => typeof(Views.StopWatchPage),
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
