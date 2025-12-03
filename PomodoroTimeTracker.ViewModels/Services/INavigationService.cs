using Microsoft.UI.Xaml.Controls;

namespace PomodoroTimeTracker.ViewModels.Services;

/// <summary>
/// Known page names for navigation
/// </summary>
public static class PageNames
{
    public const string ClientList = "ClientListPage";
    public const string ClientDetail = "ClientDetailPage";
    public const string ProjectList = "ProjectListPage";
    public const string ProjectDetail = "ProjectDetailPage";
    public const string TimeEntryList = "TimeEntryListPage";
    public const string TimeEntryDetail = "TimeEntryDetailPage";
    public const string Pomodoro = "PomodoroPage";
    public const string RegularTimer = "RegularTimerPage";
    public const string StopWatch = "StopWatchPage";
    public const string Settings = "PomodoroSettingsPage";
}

/// <summary>
/// Service for handling navigation between views
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets or sets the current navigation frame
    /// </summary>
    Frame? NavigationFrame { get; set; }

    /// <summary>
    /// Navigate to a view by type
    /// </summary>
    void NavigateTo(Type pageType);

    /// <summary>
    /// Navigate to a view by type with parameter
    /// </summary>
    void NavigateTo(Type pageType, object? parameter);

    /// <summary>
    /// Navigate to a view by page name
    /// </summary>
    void NavigateTo(string pageName);

    /// <summary>
    /// Navigate to a view by page name with parameter
    /// </summary>
    void NavigateTo(string pageName, object? parameter);

    /// <summary>
    /// Go back to previous view
    /// </summary>
    bool GoBack();

    /// <summary>
    /// Check if can go back
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Store the ID of a client to select after navigation (after create or update)
    /// </summary>
    int? ClientIdToSelect { get; set; }

    /// <summary>
    /// Store the ID of a project to select after navigation (after create or update)
    /// </summary>
    int? ProjectIdToSelect { get; set; }

    /// <summary>
    /// Store the ID of a time entry to select after navigation (after create or update)
    /// </summary>
    int? TimeEntryIdToSelect { get; set; }
}
