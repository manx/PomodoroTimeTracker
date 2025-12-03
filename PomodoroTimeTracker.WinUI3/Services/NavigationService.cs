using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.ViewModels.Services;
using PomodoroTimeTracker.WinUI3.Views;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of navigation service for WinUI 3
/// </summary>
internal sealed class NavigationService : INavigationService
{
    private static readonly Dictionary<string, Type> PageMap = new()
    {
        { PageNames.ClientList, typeof(ClientListPage) },
        { PageNames.ClientDetail, typeof(ClientDetailPage) },
        { PageNames.ProjectList, typeof(ProjectListPage) },
        { PageNames.ProjectDetail, typeof(ProjectDetailPage) },
        { PageNames.TimeEntryList, typeof(TimeEntryListPage) },
        { PageNames.TimeEntryDetail, typeof(TimeEntryDetailPage) },
        { PageNames.Pomodoro, typeof(PomodoroPage) },
        { PageNames.RegularTimer, typeof(RegularTimerPage) },
        { PageNames.StopWatch, typeof(StopWatchPage) },
        { PageNames.Settings, typeof(PomodoroSettingsPage) },
    };

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

    public void NavigateTo(string pageName)
    {
        NavigateTo(pageName, null);
    }

    public void NavigateTo(string pageName, object? parameter)
    {
        if (!PageMap.TryGetValue(pageName, out var pageType))
        {
            throw new ArgumentException($"Unknown page name: {pageName}", nameof(pageName));
        }

        NavigateTo(pageType, parameter);
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
