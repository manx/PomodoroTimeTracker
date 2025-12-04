using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for ReportViewModel.
/// Tests period selection, date navigation, and statistics loading.
/// </summary>
public class ReportViewModelTests
{
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public ReportViewModelTests()
    {
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _dialogServiceMock = new Mock<IDialogService>();

        // Default setup for empty statistics
        SetupEmptyStatistics();
    }

    private void SetupEmptyStatistics()
    {
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto(),
                TimeEntries = new TimeEntryStatsDto()
            });

        _statisticsServiceMock.Setup(s => s.GetWeeklyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new WeeklyStatisticsDto
            {
                WeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek).ToString("yyyy-MM-dd"),
                WeekEnd = DateTime.Today.AddDays(6 - (int)DateTime.Today.DayOfWeek).ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto(),
                TimeEntries = new TimeEntryStatsDto()
            });

        _statisticsServiceMock.Setup(s => s.GetDateRangeStatisticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new DateRangeStatisticsDto
            {
                StartDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"),
                EndDate = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto(),
                TimeEntries = new TimeEntryStatsDto()
            });
    }

    private ReportViewModel CreateViewModel()
    {
        return new ReportViewModel(
            _statisticsServiceMock.Object,
            _dialogServiceMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesWithDailyPeriod()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SelectedTimePeriod.Should().Be(ReportTimePeriod.Daily);
        viewModel.IsDailySelected.Should().BeTrue();
        viewModel.IsWeeklySelected.Should().BeFalse();
        viewModel.IsCustomSelected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LoadReportCommand.Should().NotBeNull();
        viewModel.RefreshCommand.Should().NotBeNull();
        viewModel.SelectDailyCommand.Should().NotBeNull();
        viewModel.SelectWeeklyCommand.Should().NotBeNull();
        viewModel.SelectCustomCommand.Should().NotBeNull();
        viewModel.PreviousPeriodCommand.Should().NotBeNull();
        viewModel.NextPeriodCommand.Should().NotBeNull();
        viewModel.GoToTodayCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesEmptyProjectBreakdown()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ProjectBreakdown.Should().NotBeNull();
    }

    [Fact]
    public async Task Constructor_TriggersInitialLoad()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()), Times.AtLeastOnce);
    }

    #endregion

    #region Period Selection Tests

    [Fact]
    public async Task SelectDailyCommand_SetsIsDailySelectedTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly; // Start with different period

        // Act
        viewModel.SelectDailyCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        viewModel.IsDailySelected.Should().BeTrue();
        viewModel.IsWeeklySelected.Should().BeFalse();
        viewModel.IsCustomSelected.Should().BeFalse();
    }

    [Fact]
    public async Task SelectWeeklyCommand_SetsIsWeeklySelectedTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectWeeklyCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        viewModel.IsDailySelected.Should().BeFalse();
        viewModel.IsWeeklySelected.Should().BeTrue();
        viewModel.IsCustomSelected.Should().BeFalse();
    }

    [Fact]
    public async Task SelectCustomCommand_SetsIsCustomSelectedTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectCustomCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        viewModel.IsDailySelected.Should().BeFalse();
        viewModel.IsWeeklySelected.Should().BeFalse();
        viewModel.IsCustomSelected.Should().BeTrue();
    }

    [Fact]
    public async Task SelectedTimePeriod_WhenChanged_TriggersReload()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await Task.Delay(50);
        _statisticsServiceMock.Invocations.Clear();

        // Act
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly;
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetWeeklyStatisticsAsync(It.IsAny<DateTime?>()), Times.AtLeastOnce);
    }

    #endregion

    #region Date Navigation Tests

    [Fact]
    public void PreviousPeriodCommand_Daily_DecrementsByOneDay()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var originalDate = viewModel.SelectedDate;

        // Act
        viewModel.PreviousPeriodCommand.Execute(null);

        // Assert
        viewModel.SelectedDate.Date.Should().Be(originalDate.Date.AddDays(-1));
    }

    [Fact]
    public void PreviousPeriodCommand_Weekly_DecrementsBySevenDays()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly;
        var originalDate = viewModel.SelectedDate;

        // Act
        viewModel.PreviousPeriodCommand.Execute(null);

        // Assert
        viewModel.SelectedDate.Date.Should().Be(originalDate.Date.AddDays(-7));
    }

    [Fact]
    public void NextPeriodCommand_Daily_IncrementsByOneDay()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var originalDate = viewModel.SelectedDate;

        // Act
        viewModel.NextPeriodCommand.Execute(null);

        // Assert
        viewModel.SelectedDate.Date.Should().Be(originalDate.Date.AddDays(1));
    }

    [Fact]
    public void NextPeriodCommand_Weekly_IncrementsBySevenDays()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly;
        var originalDate = viewModel.SelectedDate;

        // Act
        viewModel.NextPeriodCommand.Execute(null);

        // Assert
        viewModel.SelectedDate.Date.Should().Be(originalDate.Date.AddDays(7));
    }

    [Fact]
    public void GoToTodayCommand_SetsDateToToday()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedDate = DateTimeOffset.Now.AddDays(-10);

        // Act
        viewModel.GoToTodayCommand.Execute(null);

        // Assert
        viewModel.SelectedDate.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task SelectedDate_WhenChanged_TriggersReload()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await Task.Delay(50);
        _statisticsServiceMock.Invocations.Clear();

        // Act
        viewModel.SelectedDate = DateTimeOffset.Now.AddDays(-1);
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()), Times.AtLeastOnce);
    }

    #endregion

    #region Load Report Tests

    [Fact]
    public async Task LoadReportAsync_Daily_CallsGetDailyStatisticsAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await Task.Delay(50);
        _statisticsServiceMock.Invocations.Clear();

        // Act
        viewModel.LoadReportCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task LoadReportAsync_Weekly_CallsGetWeeklyStatisticsAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly;
        await Task.Delay(50);
        _statisticsServiceMock.Invocations.Clear();

        // Act
        viewModel.LoadReportCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetWeeklyStatisticsAsync(It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task LoadReportAsync_Custom_CallsGetDateRangeStatisticsAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedTimePeriod = ReportTimePeriod.Custom;
        await Task.Delay(50);
        _statisticsServiceMock.Invocations.Clear();

        // Act
        viewModel.LoadReportCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        _statisticsServiceMock.Verify(s => s.GetDateRangeStatisticsAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task LoadReportAsync_WithData_UpdatesStatisticsProperties()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto
                {
                    Total = 5,
                    Completed = 4,
                    TotalMinutes = 125
                },
                TimeEntries = new TimeEntryStatsDto
                {
                    Total = 3,
                    TotalMinutes = 180
                }
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.TotalPomodoros.Should().Be(5);
        viewModel.CompletedPomodoros.Should().Be(4);
        viewModel.TotalPomodoroMinutes.Should().Be(125);
        viewModel.TotalTimeEntries.Should().Be(3);
        viewModel.TotalTimeEntryMinutes.Should().Be(180);
        viewModel.CombinedTotalMinutes.Should().Be(305);
    }

    [Fact]
    public async Task LoadReportAsync_OnError_ShowsErrorDialog()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to load report")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Project Breakdown Tests

    [Fact]
    public async Task LoadReportAsync_WithProjects_BuildsProjectBreakdown()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto
                {
                    Total = 5,
                    Completed = 4,
                    TotalMinutes = 125,
                    ByProject = new List<ProjectStatsDto>
                    {
                        new() { ProjectId = 1, ProjectName = "Project A", ClientName = "Client X", Count = 3, Minutes = 75 },
                        new() { ProjectId = 2, ProjectName = "Project B", ClientName = "Client Y", Count = 2, Minutes = 50 }
                    }
                },
                TimeEntries = new TimeEntryStatsDto
                {
                    Total = 2,
                    TotalMinutes = 90,
                    ByProject = new List<ProjectStatsDto>
                    {
                        new() { ProjectId = 1, ProjectName = "Project A", ClientName = "Client X", Count = 1, Minutes = 60 },
                        new() { ProjectId = 3, ProjectName = "Project C", ClientName = "Client Z", Count = 1, Minutes = 30 }
                    }
                }
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.ProjectBreakdown.Should().HaveCount(3);

        var projectA = viewModel.ProjectBreakdown.First(p => p.ProjectId == 1);
        projectA.PomodoroCount.Should().Be(3);
        projectA.PomodoroMinutes.Should().Be(75);
        projectA.TimeEntryCount.Should().Be(1);
        projectA.TimeEntryMinutes.Should().Be(60);
        projectA.TotalMinutes.Should().Be(135);
    }

    [Fact]
    public async Task LoadReportAsync_WithProjects_CalculatesPercentages()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto
                {
                    Total = 2,
                    TotalMinutes = 100,
                    ByProject = new List<ProjectStatsDto>
                    {
                        new() { ProjectId = 1, ProjectName = "Project A", Count = 2, Minutes = 100 }
                    }
                },
                TimeEntries = new TimeEntryStatsDto
                {
                    Total = 0,
                    TotalMinutes = 0,
                    ByProject = new List<ProjectStatsDto>()
                }
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.ProjectBreakdown.Should().HaveCount(1);
        viewModel.ProjectBreakdown[0].PercentageOfTotal.Should().Be(100);
    }

    [Fact]
    public async Task LoadReportAsync_SortsProjectsByTotalTime()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                PomodoroSessions = new PomodoroStatsDto
                {
                    Total = 3,
                    TotalMinutes = 90,
                    ByProject = new List<ProjectStatsDto>
                    {
                        new() { ProjectId = 1, ProjectName = "Small Project", Count = 1, Minutes = 30 },
                        new() { ProjectId = 2, ProjectName = "Large Project", Count = 2, Minutes = 60 }
                    }
                },
                TimeEntries = new TimeEntryStatsDto()
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.ProjectBreakdown[0].ProjectName.Should().Be("Large Project");
        viewModel.ProjectBreakdown[1].ProjectName.Should().Be("Small Project");
    }

    #endregion

    #region Empty State Tests

    [Fact]
    public async Task IsEmpty_WhenNoData_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.IsEmpty.Should().BeTrue();
        viewModel.HasData.Should().BeFalse();
    }

    [Fact]
    public async Task HasData_WhenPomodorosExist_ReturnsTrue()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                PomodoroSessions = new PomodoroStatsDto { Total = 1 },
                TimeEntries = new TimeEntryStatsDto()
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.HasData.Should().BeTrue();
        viewModel.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task HasData_WhenTimeEntriesExist_ReturnsTrue()
    {
        // Arrange
        _statisticsServiceMock.Setup(s => s.GetDailyStatisticsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new DailyStatisticsDto
            {
                PomodoroSessions = new PomodoroStatsDto(),
                TimeEntries = new TimeEntryStatsDto { Total = 1 }
            });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // Assert
        viewModel.HasData.Should().BeTrue();
        viewModel.IsEmpty.Should().BeFalse();
    }

    #endregion

    #region Display Format Tests

    [Fact]
    public void TotalPomodoroTimeDisplay_FormatsCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TotalPomodoroMinutes = 125; // 2h 5m

        // Assert
        viewModel.TotalPomodoroTimeDisplay.Should().Be("2h 5m");
    }

    [Fact]
    public void TotalPomodoroTimeDisplay_WhenZero_ReturnsZeroMinutes()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TotalPomodoroMinutes = 0;

        // Assert
        viewModel.TotalPomodoroTimeDisplay.Should().Be("0m");
    }

    [Fact]
    public void CompletionPercentage_CalculatesCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TotalPomodoros = 10;
        viewModel.CompletedPomodoros = 8;

        // Assert
        viewModel.CompletionPercentage.Should().Be(80);
    }

    [Fact]
    public void CompletionPercentage_WhenNoPomodoros_ReturnsZero()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TotalPomodoros = 0;
        viewModel.CompletedPomodoros = 0;

        // Assert
        viewModel.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public void DateRangeDisplay_Daily_ShowsFormattedDate()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act - Set to today
        viewModel.SelectedDate = DateTimeOffset.Now;

        // Assert
        viewModel.DateRangeDisplay.Should().Contain("Today");
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void SelectedTimePeriod_WhenChanged_NotifiesDependentProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.SelectedTimePeriod = ReportTimePeriod.Weekly;

        // Assert
        changedProperties.Should().Contain("IsDailySelected");
        changedProperties.Should().Contain("IsWeeklySelected");
        changedProperties.Should().Contain("IsCustomSelected");
        changedProperties.Should().Contain("IsNotCustomSelected");
    }

    [Fact]
    public void TotalPomodoroMinutes_WhenChanged_NotifiesCombinedTotal()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.TotalPomodoroMinutes = 100;

        // Assert
        changedProperties.Should().Contain("CombinedTotalMinutes");
        changedProperties.Should().Contain("CombinedTotalTimeDisplay");
    }

    #endregion
}
