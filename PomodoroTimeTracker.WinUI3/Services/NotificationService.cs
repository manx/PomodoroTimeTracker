using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of notification service for displaying Windows toast notifications.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly AppNotificationManager? _notificationManager;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;

        // Check if notifications are supported
        try
        {
            _notificationManager = AppNotificationManager.Default;
            IsSupported = _notificationManager != null;
            _logger.LogInformation("NotificationService initialized. Supported: {IsSupported}", IsSupported);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Toast notifications are not supported on this system");
            IsSupported = false;
        }
    }

    /// <summary>
    /// Gets whether toast notifications are supported on this system.
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>
    /// Shows a toast notification when work session completes (entering wrap-up).
    /// </summary>
    /// <param name="objective">The work session objective text.</param>
    public Task ShowWorkCompleteAsync(string objective)
    {
        if (!IsSupported || _notificationManager == null)
        {
            _logger.LogDebug("Notifications not supported, skipping ShowWorkCompleteAsync");
            return Task.CompletedTask;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Work Session Complete!")
                .AddText(objective);

            var notification = builder.BuildNotification();
            _notificationManager.Show(notification);

            _logger.LogInformation("Work complete notification shown: {Objective}", objective);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing work complete notification");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a toast notification when wrap-up period completes (time for break).
    /// </summary>
    /// <param name="objective">The work session objective text.</param>
    /// <param name="isLongBreak">True if next break is a long break.</param>
    public Task ShowBreakStartingAsync(string objective, bool isLongBreak)
    {
        if (!IsSupported || _notificationManager == null)
        {
            _logger.LogDebug("Notifications not supported, skipping ShowBreakStartingAsync");
            return Task.CompletedTask;
        }

        try
        {
            var breakDuration = isLongBreak ? 15 : 5;
            var breakType = isLongBreak ? "long" : "short";

            var builder = new AppNotificationBuilder()
                .AddText("Time for a Break!")
                .AddText($"Take a {breakDuration}-minute {breakType} break.");

            var notification = builder.BuildNotification();
            _notificationManager.Show(notification);

            _logger.LogInformation("Break starting notification shown: {BreakType}, Duration: {Duration} minutes",
                breakType, breakDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing break starting notification");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a toast notification when break period completes.
    /// </summary>
    public Task ShowBreakCompleteAsync()
    {
        if (!IsSupported || _notificationManager == null)
        {
            _logger.LogDebug("Notifications not supported, skipping ShowBreakCompleteAsync");
            return Task.CompletedTask;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Break is Over!")
                .AddText("Ready for your next work session?");

            var notification = builder.BuildNotification();
            _notificationManager.Show(notification);

            _logger.LogInformation("Break complete notification shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing break complete notification");
        }

        return Task.CompletedTask;
    }
}
