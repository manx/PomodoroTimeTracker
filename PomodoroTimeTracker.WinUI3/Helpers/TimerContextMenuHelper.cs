using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Helpers;

/// <summary>
/// Helper class that creates a reusable context menu for timer controls.
/// Used by TimerWindow, PomodoroPage, and RegularTimerPage.
/// </summary>
internal static class TimerContextMenuHelper
{
    /// <summary>
    /// Creates a configured MenuFlyout for timer right-click actions.
    /// </summary>
    /// <param name="viewModel">The timer ViewModel implementing ITimerWindowViewModel.</param>
    /// <returns>A configured MenuFlyout ready to be attached to a control.</returns>
    public static MenuFlyout CreateTimerContextMenu(ITimerWindowViewModel viewModel)
    {
        var menuFlyout = new MenuFlyout();

        // Pause/Resume item
        var pauseMenuItem = new MenuFlyoutItem { Text = "Pause" };
        pauseMenuItem.Click += (s, e) =>
        {
            if (viewModel.PauseResumeCommand.CanExecute(null))
            {
                viewModel.PauseResumeCommand.Execute(null);
            }
        };

        // Update text when menu opens
        menuFlyout.Opening += (s, e) =>
        {
            pauseMenuItem.Text = viewModel.IsPausedState ? "Resume" : "Pause";
        };

        menuFlyout.Items.Add(pauseMenuItem);

        // Stop item
        var stopMenuItem = new MenuFlyoutItem { Text = "Stop" };
        stopMenuItem.Click += (s, e) =>
        {
            // Pause first if running
            if (viewModel.IsRunningState && viewModel.PauseResumeCommand.CanExecute(null))
            {
                viewModel.PauseResumeCommand.Execute(null);
            }

            // Show stop submenu with Save/Discard/Resume options
            ShowStopSubmenu(viewModel, (s as FrameworkElement)!);
        };
        menuFlyout.Items.Add(stopMenuItem);

        // Separator
        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // Add time items
        menuFlyout.Items.Add(CreateAddTimeItem(viewModel, 1));
        menuFlyout.Items.Add(CreateAddTimeItem(viewModel, 2));
        menuFlyout.Items.Add(CreateAddTimeItem(viewModel, 5));

        return menuFlyout;
    }

    private static MenuFlyoutItem CreateAddTimeItem(ITimerWindowViewModel viewModel, int minutes)
    {
        var item = new MenuFlyoutItem { Text = $"+ {minutes} minute{(minutes > 1 ? "s" : "")}" };
        item.Click += (s, e) => viewModel.AddMinutes(minutes);
        return item;
    }

    private static void ShowStopSubmenu(ITimerWindowViewModel viewModel, FrameworkElement target)
    {
        var elapsedSeconds = viewModel.ElapsedSeconds;
        var minutes = elapsedSeconds / 60;
        var seconds = elapsedSeconds % 60;

        var submenu = new MenuFlyout();

        var saveItem = new MenuFlyoutItem { Text = $"Save {minutes:D2}:{seconds:D2} work item" };
        saveItem.Click += async (s, e) => await viewModel.SaveAndStopAsync();
        submenu.Items.Add(saveItem);

        var discardItem = new MenuFlyoutItem { Text = "Discard work item" };
        discardItem.Click += async (s, e) => await viewModel.DiscardAndStopAsync();
        submenu.Items.Add(discardItem);

        var resumeItem = new MenuFlyoutItem { Text = "Resume" };
        resumeItem.Click += (s, e) =>
        {
            if (viewModel.IsPausedState && viewModel.PauseResumeCommand.CanExecute(null))
            {
                viewModel.PauseResumeCommand.Execute(null);
            }
        };
        submenu.Items.Add(resumeItem);

        // Show at the target element
        submenu.ShowAt(target);
    }
}
