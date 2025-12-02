using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of dialog service for WinUI 3
/// </summary>
internal sealed class DialogService : IDialogService
{
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
}
