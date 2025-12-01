using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PomodoroTimeTracker.Application.Interfaces;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace PomodoroTimeTracker.WinUI3.Services;

/// <summary>
/// Implementation of audio service for playing Windows system sounds.
/// </summary>
internal class AudioService : IAudioService, IDisposable
{
    private readonly ILogger<AudioService> _logger;
    private MediaPlayer? _mediaPlayer;
    private bool _disposed;

    // Windows system sound paths
    private const string WINDOWS_MEDIA_PATH = @"C:\Windows\Media";
    private const string WRAP_UP_SOUND = "Windows Notify.wav";
    private const string ALARM_SOUND = "Alarm01.wav";

    public AudioService(ILogger<AudioService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Plays the wrap-up notification sound (gentle sound).
    /// </summary>
    public async Task PlayWrapUpNotificationAsync(int volume)
    {
        await PlaySoundAsync(WRAP_UP_SOUND, volume);
    }

    /// <summary>
    /// Plays the main alarm sound (prominent sound).
    /// </summary>
    public async Task PlayAlarmAsync(int volume)
    {
        await PlaySoundAsync(ALARM_SOUND, volume);
    }

    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    public Task StopAsync()
    {
        try
        {
            _mediaPlayer?.Pause();
            _logger.LogDebug("Audio playback stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio playback");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Plays a sound file from Windows Media directory.
    /// </summary>
    private async Task PlaySoundAsync(string soundFileName, int volume)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot play sound - AudioService is disposed");
            return;
        }

        try
        {
            // Validate volume range
            if (volume < 0 || volume > 100)
            {
                _logger.LogWarning("Invalid volume {Volume}, clamping to 0-100 range", volume);
                volume = Math.Clamp(volume, 0, 100);
            }

            // Build full path to sound file
            var soundPath = Path.Combine(WINDOWS_MEDIA_PATH, soundFileName);

            // Check if file exists
            if (!File.Exists(soundPath))
            {
                _logger.LogWarning("Sound file not found: {SoundPath}, trying fallback", soundPath);

                // Try fallback sounds
                var fallbackSound = soundFileName == WRAP_UP_SOUND
                    ? "Windows Notify System Generic.wav"
                    : "Windows Critical Stop.wav";

                soundPath = Path.Combine(WINDOWS_MEDIA_PATH, fallbackSound);

                if (!File.Exists(soundPath))
                {
                    _logger.LogError("Fallback sound file not found: {SoundPath}", soundPath);
                    return;
                }
            }

            // Initialize MediaPlayer on first use
            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer();
                _logger.LogDebug("MediaPlayer initialized");
            }

            // Stop any currently playing sound
            _mediaPlayer.Pause();

            // Set volume (convert from 0-100 to 0.0-1.0)
            _mediaPlayer.Volume = volume / 100.0;

            // Load and play the sound
            _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(soundPath));
            _mediaPlayer.Play();

            _logger.LogInformation("Playing sound: {SoundFile} at volume {Volume}%", soundFileName, volume);

            // Wait a bit to ensure playback starts
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing sound: {SoundFile}", soundFileName);
        }
    }

    /// <summary>
    /// Disposes the MediaPlayer resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _mediaPlayer?.Pause();
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
            _logger.LogDebug("AudioService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing AudioService");
        }

        _disposed = true;
    }
}
