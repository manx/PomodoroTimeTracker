using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PomodoroTimeTracker.Application.Interfaces;

namespace PomodoroTimeTracker.Tests.WinUI3.Services;

/// <summary>
/// Unit tests for IAudioService interface and mockability.
/// These tests verify that the interface can be properly mocked for use in ViewModels
/// and that the contract behavior is testable.
/// </summary>
public class AudioServiceTests
{
    private readonly Mock<IAudioService> _audioServiceMock;
    private const string DefaultWrapUpSound = "Windows Notify.wav";
    private const string DefaultAlarmSound = "Alarm01.wav";

    public AudioServiceTests()
    {
        _audioServiceMock = new Mock<IAudioService>();
    }

    #region Interface Mockability Tests

    [Fact]
    public void IAudioService_CanBeMocked_ForPlayWrapUpNotificationAsync()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var task = _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound), Times.Once);
    }

    [Fact]
    public void IAudioService_CanBeMocked_ForPlayAlarmAsync()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var task = _audioServiceMock.Object.PlayAlarmAsync(75, DefaultAlarmSound);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(75, DefaultAlarmSound), Times.Once);
    }

    [Fact]
    public void IAudioService_CanBeMocked_ForStopAsync()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.StopAsync())
            .Returns(Task.CompletedTask);

        // Act
        var task = _audioServiceMock.Object.StopAsync();

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
        _audioServiceMock.Verify(s => s.StopAsync(), Times.Once);
    }

    [Fact]
    public void IAudioService_CanBeMocked_ForGetAvailableWrapUpSounds()
    {
        // Arrange
        var expectedSounds = new[] { "Windows Notify.wav", "chimes.wav" };
        _audioServiceMock.Setup(s => s.GetAvailableWrapUpSounds())
            .Returns(expectedSounds);

        // Act
        var result = _audioServiceMock.Object.GetAvailableWrapUpSounds();

        // Assert
        result.Should().BeEquivalentTo(expectedSounds);
    }

    [Fact]
    public void IAudioService_CanBeMocked_ForGetAvailableAlarmSounds()
    {
        // Arrange
        var expectedSounds = new[] { "Alarm01.wav", "Alarm02.wav" };
        _audioServiceMock.Setup(s => s.GetAvailableAlarmSounds())
            .Returns(expectedSounds);

        // Act
        var result = _audioServiceMock.Object.GetAvailableAlarmSounds();

        // Assert
        result.Should().BeEquivalentTo(expectedSounds);
    }

    #endregion

    #region Volume Boundary Value Tests

    [Theory]
    [InlineData(0)]    // Minimum volume (silent)
    [InlineData(50)]   // Medium volume
    [InlineData(100)]  // Maximum volume
    public async Task PlayWrapUpNotificationAsync_WithValidVolume_CompletesSuccessfully(int volume)
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(volume, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound), Times.Once);
    }

    [Theory]
    [InlineData(0)]    // Minimum volume (silent)
    [InlineData(50)]   // Medium volume
    [InlineData(100)]  // Maximum volume
    public async Task PlayAlarmAsync_WithValidVolume_CompletesSuccessfully(int volume)
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(volume, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayAlarmAsync(volume, DefaultAlarmSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(volume, DefaultAlarmSound), Times.Once);
    }

    [Theory]
    [InlineData(-1)]    // Below minimum
    [InlineData(-100)]  // Large negative
    [InlineData(101)]   // Above maximum
    [InlineData(200)]   // Large positive
    public async Task PlayWrapUpNotificationAsync_WithInvalidVolume_CallsServiceWithVolume(int volume)
    {
        // Arrange - Mock accepts any volume since implementation will clamp it
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound), Times.Once);
    }

    [Theory]
    [InlineData(-1)]    // Below minimum
    [InlineData(-100)]  // Large negative
    [InlineData(101)]   // Above maximum
    [InlineData(200)]   // Large positive
    public async Task PlayAlarmAsync_WithInvalidVolume_CallsServiceWithVolume(int volume)
    {
        // Arrange - Mock accepts any volume since implementation will clamp it
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayAlarmAsync(volume, DefaultAlarmSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(volume, DefaultAlarmSound), Times.Once);
    }

    #endregion

    #region Sound File Tests

    [Theory]
    [InlineData("Windows Notify.wav")]
    [InlineData("chimes.wav")]
    [InlineData("notify.wav")]
    public async Task PlayWrapUpNotificationAsync_WithDifferentSounds_CallsServiceWithCorrectSound(string soundFile)
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), soundFile))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, soundFile);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(50, soundFile), Times.Once);
    }

    [Theory]
    [InlineData("Alarm01.wav")]
    [InlineData("Alarm02.wav")]
    [InlineData("Ring01.wav")]
    public async Task PlayAlarmAsync_WithDifferentSounds_CallsServiceWithCorrectSound(string soundFile)
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), soundFile))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayAlarmAsync(75, soundFile);

        // Assert
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(75, soundFile), Times.Once);
    }

    #endregion

    #region Async Behavior Tests

    [Fact]
    public async Task PlayWrapUpNotificationAsync_IsAsync_CompletesAsynchronously()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(tcs.Task.ContinueWith(_ => { }));

        // Act
        var task = _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound);
        task.IsCompleted.Should().BeFalse("Task should not complete immediately");

        tcs.SetResult(true);
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task PlayAlarmAsync_IsAsync_CompletesAsynchronously()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(tcs.Task.ContinueWith(_ => { }));

        // Act
        var task = _audioServiceMock.Object.PlayAlarmAsync(75, DefaultAlarmSound);
        task.IsCompleted.Should().BeFalse("Task should not complete immediately");

        tcs.SetResult(true);
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_IsAsync_CompletesAsynchronously()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _audioServiceMock.Setup(s => s.StopAsync())
            .Returns(tcs.Task.ContinueWith(_ => { }));

        // Act
        var task = _audioServiceMock.Object.StopAsync();
        task.IsCompleted.Should().BeFalse("Task should not complete immediately");

        tcs.SetResult(true);
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region Sequential Call Tests

    [Fact]
    public async Task AudioService_CanPlayMultipleSounds_Sequentially()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound);
        await _audioServiceMock.Object.PlayAlarmAsync(75, DefaultAlarmSound);
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(60, "chimes.wav");

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(2));
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AudioService_CanStopAfterPlaying_InSequence()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _audioServiceMock.Setup(s => s.StopAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound);
        await _audioServiceMock.Object.StopAsync();

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound), Times.Once);
        _audioServiceMock.Verify(s => s.StopAsync(), Times.Once);
    }

    #endregion

    #region Volume Value Capture Tests

    [Fact]
    public async Task PlayWrapUpNotificationAsync_PassesExactVolume_ToImplementation()
    {
        // Arrange
        var capturedVolume = 0;
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((v, s) => capturedVolume = v)
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(42, DefaultWrapUpSound);

        // Assert
        capturedVolume.Should().Be(42);
    }

    [Fact]
    public async Task PlayAlarmAsync_PassesExactVolume_ToImplementation()
    {
        // Arrange
        var capturedVolume = 0;
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((v, s) => capturedVolume = v)
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayAlarmAsync(88, DefaultAlarmSound);

        // Assert
        capturedVolume.Should().Be(88);
    }

    [Fact]
    public async Task PlayWrapUpNotificationAsync_PassesExactSoundFile_ToImplementation()
    {
        // Arrange
        var capturedSound = "";
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((v, s) => capturedSound = s)
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, "chimes.wav");

        // Assert
        capturedSound.Should().Be("chimes.wav");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task PlayWrapUpNotificationAsync_WhenThrows_PropagatesException()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Playback error"));

        // Act & Assert
        await FluentActions.Invoking(() => _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Playback error");
    }

    [Fact]
    public async Task PlayAlarmAsync_WhenThrows_PropagatesException()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Playback error"));

        // Act & Assert
        await FluentActions.Invoking(() => _audioServiceMock.Object.PlayAlarmAsync(75, DefaultAlarmSound))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Playback error");
    }

    [Fact]
    public async Task StopAsync_WhenThrows_PropagatesException()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.StopAsync())
            .ThrowsAsync(new InvalidOperationException("Stop error"));

        // Act & Assert
        await FluentActions.Invoking(() => _audioServiceMock.Object.StopAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Stop error");
    }

    #endregion

    #region Mock Verification Tests

    [Fact]
    public async Task AudioService_VerifiesNoCalls_WhenNotUsed()
    {
        // Arrange - No setup needed

        // Act - Do nothing

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _audioServiceMock.Verify(s => s.StopAsync(), Times.Never);
    }

    [Fact]
    public async Task AudioService_VerifiesExactCallCount_ForMultipleCalls()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(30, DefaultWrapUpSound);
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(40, DefaultWrapUpSound);
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(30, DefaultWrapUpSound), Times.Once);
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(40, DefaultWrapUpSound), Times.Once);
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(50, DefaultWrapUpSound), Times.Once);
    }

    #endregion

    #region ViewModel Integration Pattern Tests

    [Fact]
    public async Task AudioService_SupportsViewModelPattern_WithConditionalPlayback()
    {
        // Arrange - Simulating ViewModel deciding whether to play sound based on settings
        var playSoundSetting = true;
        var volume = 60;

        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        if (playSoundSetting)
        {
            await _audioServiceMock.Object.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound);
        }

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(volume, DefaultWrapUpSound), Times.Once);
    }

    [Fact]
    public async Task AudioService_SupportsViewModelPattern_WithVolumeSetting()
    {
        // Arrange - Simulating ViewModel using volume from settings
        var wrapUpVolumeSetting = 45;
        var alarmVolumeSetting = 85;

        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act - Simulate work period ending flow
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(wrapUpVolumeSetting, DefaultWrapUpSound);
        // ... some delay ...
        await _audioServiceMock.Object.PlayAlarmAsync(alarmVolumeSetting, DefaultAlarmSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(45, DefaultWrapUpSound), Times.Once);
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(85, DefaultAlarmSound), Times.Once);
    }

    [Fact]
    public async Task AudioService_SupportsViewModelPattern_WithStopOnPause()
    {
        // Arrange - Simulating ViewModel stopping audio when timer is paused
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _audioServiceMock.Setup(s => s.StopAsync())
            .Returns(Task.CompletedTask);

        // Act - Simulate alarm playing, then user pauses timer
        await _audioServiceMock.Object.PlayAlarmAsync(70, DefaultAlarmSound);
        // User pauses timer...
        await _audioServiceMock.Object.StopAsync();

        // Assert
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(70, DefaultAlarmSound), Times.Once);
        _audioServiceMock.Verify(s => s.StopAsync(), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AudioService_HandlesZeroVolume_AsValidValue()
    {
        // Arrange - 0 volume is valid (silent)
        _audioServiceMock.Setup(s => s.PlayWrapUpNotificationAsync(0, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayWrapUpNotificationAsync(0, DefaultWrapUpSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayWrapUpNotificationAsync(0, DefaultWrapUpSound), Times.Once);
    }

    [Fact]
    public async Task AudioService_HandlesMaxVolume_AsValidValue()
    {
        // Arrange - 100 volume is valid (maximum)
        _audioServiceMock.Setup(s => s.PlayAlarmAsync(100, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _audioServiceMock.Object.PlayAlarmAsync(100, DefaultAlarmSound);

        // Assert
        _audioServiceMock.Verify(s => s.PlayAlarmAsync(100, DefaultAlarmSound), Times.Once);
    }

    [Fact]
    public async Task StopAsync_CanBeCalledMultipleTimes_Safely()
    {
        // Arrange
        _audioServiceMock.Setup(s => s.StopAsync())
            .Returns(Task.CompletedTask);

        // Act - Call stop multiple times (idempotent operation)
        await _audioServiceMock.Object.StopAsync();
        await _audioServiceMock.Object.StopAsync();
        await _audioServiceMock.Object.StopAsync();

        // Assert
        _audioServiceMock.Verify(s => s.StopAsync(), Times.Exactly(3));
    }

    #endregion
}
