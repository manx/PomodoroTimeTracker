using Microsoft.UI.Xaml.Controls;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of navigation service for WinUI 3
/// </summary>
internal class NavigationService : INavigationService
{
    public Frame? NavigationFrame { get; set; }
    public int? ClientIdToSelect { get; set; }
    public int? ProjectIdToSelect { get; set; }
    public int? TimeEntryIdToSelect { get; set; }

    public void NavigateTo(Type pageType)
    {
        NavigateTo(pageType, null);
    }

    public void NavigateTo(Type pageType, object? parameter)
    {
        if (NavigationFrame == null)
        {
            throw new InvalidOperationException("Navigation frame is not set");
        }

        NavigationFrame.Navigate(pageType, parameter);
    }

    public bool GoBack()
    {
        if (NavigationFrame?.CanGoBack == true)
        {
            NavigationFrame.GoBack();
            return true;
        }
        return false;
    }

    public bool CanGoBack => NavigationFrame?.CanGoBack ?? false;
}
