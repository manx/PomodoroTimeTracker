using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class TimeEntryListPage : Page
{
    public TimeEntryListViewModel ViewModel { get; }

    public TimeEntryListPage()
    {
        ViewModel = App.Services.GetService(typeof(TimeEntryListViewModel)) as TimeEntryListViewModel
                    ?? throw new InvalidOperationException("TimeEntryListViewModel not registered");

        this.InitializeComponent();

        // Set up dialog callback
        ViewModel.ShowStartEntryDialog = ShowStartEntryDialogAsync;
    }

    private async Task<string?> ShowStartEntryDialogAsync()
    {
        var textBox = new TextBox
        {
            PlaceholderText = "What are you working on?",
            AcceptsReturn = false,
            MaxLength = 500
        };

        var dialog = new ContentDialog
        {
            Title = "Start Time Entry",
            Content = textBox,
            PrimaryButtonText = "Start",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }
}
