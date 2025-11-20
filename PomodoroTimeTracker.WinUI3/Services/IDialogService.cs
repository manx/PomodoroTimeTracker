namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Service for displaying dialogs and messages
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Show an information message
    /// </summary>
    Task ShowInformationAsync(string message, string title = "Information");

    /// <summary>
    /// Show a warning message
    /// </summary>
    Task ShowWarningAsync(string message, string title = "Warning");

    /// <summary>
    /// Show an error message
    /// </summary>
    Task ShowErrorAsync(string message, string title = "Error");

    /// <summary>
    /// Show a confirmation dialog and return user's choice
    /// </summary>
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");

    /// <summary>
    /// Show an input dialog and return user's input
    /// </summary>
    Task<string?> ShowInputAsync(string message, string title = "Input", string defaultValue = "");
}
