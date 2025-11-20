using Microsoft.UI.Xaml.Controls;

namespace PomodoroTimeTracker.WinUI3.Services;

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
    /// Go back to previous view
    /// </summary>
    bool GoBack();

    /// <summary>
    /// Check if can go back
    /// </summary>
    bool CanGoBack { get; }
}
