using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of dialog service for WinUI 3
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly AppNotificationManager? _notificationManager;

    public DialogService()
    {
        try
        {
            _notificationManager = AppNotificationManager.Default;
        }
        catch
        {
            // Notifications not supported
        }
    }

    private static XamlRoot? GetXamlRoot()
    {
        // Get the main window's content XamlRoot
        var mainWindow = (Microsoft.UI.Xaml.Application.Current as App)?._window;
        return mainWindow?.Content?.XamlRoot;
    }

    public async Task ShowInformationAsync(string message, string title = "Information")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task ShowWarningAsync(string message, string title = "Warning")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task ShowErrorAsync(string message, string title = "Error")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<string?> ShowInputAsync(string message, string title = "Input", string defaultValue = "")
    {
        var textBox = new TextBox
        {
            Text = defaultValue,
            AcceptsReturn = false
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 12) },
                    textBox
                }
            },
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }

    public void ShowToast(string message, string title = "")
    {
        if (_notificationManager == null)
            return;

        try
        {
            var builder = new AppNotificationBuilder();

            if (!string.IsNullOrEmpty(title))
            {
                builder.AddText(title);
            }

            builder.AddText(message);

            var notification = builder.BuildNotification();
            _notificationManager.Show(notification);
        }
        catch
        {
            // Silently fail if notifications don't work
        }
    }
}
