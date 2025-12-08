using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for DashboardViewModel.
/// Tests initialization, filtering, and goal display.
/// </summary>
public class DashboardViewModelTests
{
    private readonly Mock<IGoalService> _goalServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public DashboardViewModelTests()
    {
        _goalServiceMock = new Mock<IGoalService>();
        _clientServiceMock = new Mock<IClientService>();
        _projectServiceMock = new Mock<IProjectService>();
        _dialogServiceMock = new Mock<IDialogService>();

        // Default setups
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(new List<ProjectDto>());

        SetupNullGoalResponses(); // No schedule by default (returns null)
    }

    private void SetupNullGoalResponses()
    {
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoalComparisonDto?)null);

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoalComparisonDto?)null);

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoalComparisonDto?)null);
    }

    private void SetupGoalResponses(decimal expected, decimal actual)
    {
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = expected, ActualHours = actual });

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = expected, ActualHours = actual });

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = expected, ActualHours = actual });
    }

    private DashboardViewModel CreateViewModel()
    {
        return new DashboardViewModel(
            _goalServiceMock.Object,
            _clientServiceMock.Object,
            _projectServiceMock.Object,
            _dialogServiceMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public async Task InitializeAsync_LoadsClients()
    {
        // Arrange
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Client A" },
            new() { Id = 2, Name = "Client B" }
        };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.Clients.Should().HaveCount(2);
    }

    [Fact]
    public async Task InitializeAsync_LoadsGoalComparisons()
    {
        // Arrange
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 8, ActualHours = 4 });

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 40, ActualHours = 30 });

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 160, ActualHours = 80 });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.DailyActualHours.Should().Be(4);
        viewModel.DailyExpectedHours.Should().Be(8);
        viewModel.DailyDifferenceHours.Should().Be(-4); // 4 - 8 = -4
        viewModel.DailyProgress.Should().Be(50); // 4/8 * 100 = 50
        viewModel.DailyIsAhead.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_SetsHasScheduleTrue_WhenExpectedHoursExist()
    {
        // Arrange
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 8, ActualHours = 0 });

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 0, ActualHours = 0 });

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 0, ActualHours = 0 });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.HasSchedule.Should().BeTrue();
        viewModel.ShowNoScheduleMessage.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_SetsHasScheduleFalse_WhenNoExpectedHours()
    {
        // Arrange - all comparisons return 0 expected hours (default setup)
        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.HasSchedule.Should().BeFalse();
        viewModel.ShowNoScheduleMessage.Should().BeTrue();
    }

    #endregion

    #region Client Selection Tests

    [Fact]
    public async Task SelectedClient_WhenChanged_RefreshesGoals()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client A" } };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        // Reset mock to verify new call
        _goalServiceMock.Invocations.Clear();

        // Act
        viewModel.SelectedClient = clients[0];

        // Give async operations time to complete
        await Task.Delay(100);

        // Assert
        _goalServiceMock.Verify(s => s.GetDailyComparisonAsync(
            1, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SelectedClient_WhenChanged_LoadsProjectsForClient()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client A" } };
        var projects = new List<ProjectDto>
        {
            new() { Id = 10, Name = "Project 1", ClientId = 1 }
        };

        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);
        _projectServiceMock.Setup(s => s.GetProjectsByClientIdAsync(1))
            .ReturnsAsync(projects);

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        // Act
        viewModel.SelectedClient = clients[0];

        // Give async operations time to complete
        await Task.Delay(100);

        // Assert
        viewModel.Projects.Should().HaveCount(1);
        viewModel.Projects[0].Name.Should().Be("Project 1");
    }

    [Fact]
    public async Task SelectedClient_WhenCleared_LoadsAllProjects()
    {
        // Arrange
        var allProjects = new List<ProjectDto>
        {
            new() { Id = 10, Name = "Project 1" },
            new() { Id = 20, Name = "Project 2" }
        };

        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(allProjects);

        var viewModel = CreateViewModel();
        viewModel.SelectedClient = new ClientDto { Id = 1 };
        await viewModel.InitializeAsync();

        // Act
        viewModel.SelectedClient = null;

        // Give async operations time to complete
        await Task.Delay(100);

        // Assert
        _projectServiceMock.Verify(s => s.GetAllProjectsAsync(), Times.AtLeastOnce);
    }

    #endregion

    #region Project Selection Tests

    [Fact]
    public async Task SelectedProject_WhenChanged_RefreshesGoals()
    {
        // Arrange
        var projects = new List<ProjectDto> { new() { Id = 10, Name = "Project A" } };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();
        viewModel.Projects = new System.Collections.ObjectModel.ObservableCollection<ProjectDto>(projects);

        // Reset mock
        _goalServiceMock.Invocations.Clear();

        // Act
        viewModel.SelectedProject = projects[0];

        // Give async operations time to complete
        await Task.Delay(100);

        // Assert
        _goalServiceMock.Verify(s => s.GetDailyComparisonAsync(
            null, 10, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Refresh Command Tests

    [Fact]
    public async Task RefreshCommand_RefreshesAllGoals()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();
        _goalServiceMock.Invocations.Clear();

        // Act
        await viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert
        _goalServiceMock.Verify(s => s.GetDailyComparisonAsync(
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        _goalServiceMock.Verify(s => s.GetWeeklyComparisonAsync(
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        _goalServiceMock.Verify(s => s.GetMonthlyComparisonAsync(
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshCommand_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<GoalComparisonDto?>();
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();

        // Act
        var refreshTask = viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert - during refresh
        viewModel.IsLoading.Should().BeTrue();

        // Complete the task
        tcs.SetResult(new GoalComparisonDto());
        await refreshTask;

        // Assert - after refresh
        viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InitializeAsync_OnError_ShowsErrorDialog()
    {
        // Arrange
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to load dashboard")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Goal Display Tests

    [Fact]
    public async Task WeeklyGoal_DisplaysCorrectValues()
    {
        // Arrange
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 8, ActualHours = 4 });

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 40, ActualHours = 30 });

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 160, ActualHours = 80 });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.WeeklyActualHours.Should().Be(30);
        viewModel.WeeklyExpectedHours.Should().Be(40);
        viewModel.WeeklyDifferenceHours.Should().Be(-10); // 30 - 40 = -10
        viewModel.WeeklyProgress.Should().Be(75); // 30/40 * 100 = 75
        viewModel.WeeklyIsAhead.Should().BeFalse();
    }

    [Fact]
    public async Task MonthlyGoal_DisplaysCorrectValues_WhenAhead()
    {
        // Arrange
        _goalServiceMock.Setup(s => s.GetDailyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 8, ActualHours = 10 });

        _goalServiceMock.Setup(s => s.GetWeeklyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 40, ActualHours = 50 });

        _goalServiceMock.Setup(s => s.GetMonthlyComparisonAsync(
                null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalComparisonDto { ExpectedHours = 160, ActualHours = 180 });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.MonthlyActualHours.Should().Be(180);
        viewModel.MonthlyExpectedHours.Should().Be(160);
        viewModel.MonthlyDifferenceHours.Should().Be(20); // 180 - 160 = 20
        viewModel.MonthlyProgress.Should().BeApproximately(112.5, 0.1); // 180/160 * 100 = 112.5
        viewModel.MonthlyIsAhead.Should().BeTrue();
    }

    #endregion
}
