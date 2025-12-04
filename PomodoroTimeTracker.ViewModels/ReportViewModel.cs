using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// Time period options for the report view.
/// </summary>
public enum ReportTimePeriod
{
    Daily,
    Weekly,
    Monthly,
    Custom
}

/// <summary>
/// Display item for a project in the report breakdown.
/// </summary>
public sealed class ProjectReportItem
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int PomodoroCount { get; set; }
    public int PomodoroMinutes { get; set; }
    public int TimeEntryCount { get; set; }
    public int TimeEntryMinutes { get; set; }
    public int TotalMinutes => PomodoroMinutes + TimeEntryMinutes;
    public double PercentageOfTotal { get; set; }

    public string TotalTimeDisplay => FormatDuration(TotalMinutes);
    public string PomodoroDisplay => $"{PomodoroCount} sessions ({FormatDuration(PomodoroMinutes)})";
    public string TimeEntryDisplay => $"{TimeEntryCount} entries ({FormatDuration(TimeEntryMinutes)})";

    private static string FormatDuration(int minutes)
    {
        if (minutes == 0)
            return "0m";

        var hours = minutes / 60;
        var mins = minutes % 60;

        if (hours > 0 && mins > 0)
            return $"{hours}h {mins}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{mins}m";
    }
}

/// <summary>
/// ViewModel for the report view showing combined statistics.
/// </summary>
public sealed partial class ReportViewModel : ViewModelBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IDialogService _dialogService;

    private ReportTimePeriod _selectedTimePeriod = ReportTimePeriod.Daily;
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
    private DateTimeOffset _customStartDate = DateTimeOffset.Now.AddDays(-7);
    private DateTimeOffset _customEndDate = DateTimeOffset.Now;
    private bool _isLoading;

    // Statistics data
    private int _totalPomodoros;
    private int _completedPomodoros;
    private int _totalPomodoroMinutes;
    private int _totalTimeEntries;
    private int _totalTimeEntryMinutes;
    private ObservableCollection<ProjectReportItem> _projectBreakdown = new();

    public ReportViewModel(
        IStatisticsService statisticsService,
        IDialogService dialogService)
    {
        _statisticsService = statisticsService;
        _dialogService = dialogService;

        LoadReportCommand = new AsyncRelayCommand(LoadReportAsync);
        RefreshCommand = new AsyncRelayCommand(LoadReportAsync);
        SelectDailyCommand = new RelayCommand(() => SelectedTimePeriod = ReportTimePeriod.Daily);
        SelectWeeklyCommand = new RelayCommand(() => SelectedTimePeriod = ReportTimePeriod.Weekly);
        SelectMonthlyCommand = new RelayCommand(() => SelectedTimePeriod = ReportTimePeriod.Monthly);
        SelectCustomCommand = new RelayCommand(() => SelectedTimePeriod = ReportTimePeriod.Custom);
        PreviousPeriodCommand = new RelayCommand(GoToPreviousPeriod);
        NextPeriodCommand = new RelayCommand(GoToNextPeriod);
        GoToTodayCommand = new RelayCommand(GoToToday);

        _ = LoadReportAsync();
    }

    #region Time Period Selection

    public ReportTimePeriod SelectedTimePeriod
    {
        get => _selectedTimePeriod;
        set
        {
            if (SetProperty(ref _selectedTimePeriod, value))
            {
                OnPropertyChanged(nameof(IsDailySelected));
                OnPropertyChanged(nameof(IsWeeklySelected));
                OnPropertyChanged(nameof(IsMonthlySelected));
                OnPropertyChanged(nameof(IsCustomSelected));
                OnPropertyChanged(nameof(IsNotCustomSelected));
                _ = LoadReportAsync();
            }
        }
    }

    public bool IsDailySelected => SelectedTimePeriod == ReportTimePeriod.Daily;
    public bool IsWeeklySelected => SelectedTimePeriod == ReportTimePeriod.Weekly;
    public bool IsMonthlySelected => SelectedTimePeriod == ReportTimePeriod.Monthly;
    public bool IsCustomSelected => SelectedTimePeriod == ReportTimePeriod.Custom;
    public bool IsNotCustomSelected => !IsCustomSelected;

    #endregion

    #region Date Selection

    public DateTimeOffset SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                OnPropertyChanged(nameof(DateRangeDisplay));
                if (!IsCustomSelected)
                {
                    _ = LoadReportAsync();
                }
            }
        }
    }

    public DateTimeOffset CustomStartDate
    {
        get => _customStartDate;
        set
        {
            if (SetProperty(ref _customStartDate, value))
            {
                OnPropertyChanged(nameof(DateRangeDisplay));
            }
        }
    }

    public DateTimeOffset CustomEndDate
    {
        get => _customEndDate;
        set
        {
            if (SetProperty(ref _customEndDate, value))
            {
                OnPropertyChanged(nameof(DateRangeDisplay));
            }
        }
    }

    public string DateRangeDisplay
    {
        get
        {
            var today = DateTime.Today;

            return SelectedTimePeriod switch
            {
                ReportTimePeriod.Daily when SelectedDate.Date == today =>
                    $"Today, {SelectedDate:MMM d, yyyy}",
                ReportTimePeriod.Daily when SelectedDate.Date == today.AddDays(-1) =>
                    $"Yesterday, {SelectedDate:MMM d, yyyy}",
                ReportTimePeriod.Daily =>
                    SelectedDate.ToString("ddd, MMM d, yyyy"),
                ReportTimePeriod.Weekly =>
                    $"{GetWeekStart(SelectedDate.DateTime):MMM d} - {GetWeekEnd(SelectedDate.DateTime):MMM d, yyyy}",
                ReportTimePeriod.Monthly =>
                    SelectedDate.ToString("MMMM yyyy"),
                ReportTimePeriod.Custom =>
                    $"{CustomStartDate:MMM d} - {CustomEndDate:MMM d, yyyy}",
                _ => string.Empty
            };
        }
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        return date.AddDays(-(int)date.DayOfWeek);
    }

    private static DateTime GetWeekEnd(DateTime date)
    {
        return GetWeekStart(date).AddDays(6);
    }

    private static DateTime GetMonthStart(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    private static DateTime GetMonthEnd(DateTime date)
    {
        return GetMonthStart(date).AddMonths(1).AddDays(-1);
    }

    #endregion

    #region Loading State

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool HasData => !IsLoading && (TotalPomodoros > 0 || TotalTimeEntries > 0);
    public bool IsEmpty => !IsLoading && TotalPomodoros == 0 && TotalTimeEntries == 0;

    #endregion

    #region Pomodoro Statistics

    public int TotalPomodoros
    {
        get => _totalPomodoros;
        set
        {
            if (SetProperty(ref _totalPomodoros, value))
            {
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public int CompletedPomodoros
    {
        get => _completedPomodoros;
        set
        {
            if (SetProperty(ref _completedPomodoros, value))
            {
                OnPropertyChanged(nameof(CompletionPercentage));
            }
        }
    }

    public int TotalPomodoroMinutes
    {
        get => _totalPomodoroMinutes;
        set
        {
            if (SetProperty(ref _totalPomodoroMinutes, value))
            {
                OnPropertyChanged(nameof(TotalPomodoroTimeDisplay));
                OnPropertyChanged(nameof(CombinedTotalMinutes));
                OnPropertyChanged(nameof(CombinedTotalTimeDisplay));
            }
        }
    }

    public string TotalPomodoroTimeDisplay => FormatDuration(TotalPomodoroMinutes);

    public double CompletionPercentage =>
        TotalPomodoros > 0 ? (double)CompletedPomodoros / TotalPomodoros * 100 : 0;

    #endregion

    #region Time Entry Statistics

    public int TotalTimeEntries
    {
        get => _totalTimeEntries;
        set
        {
            if (SetProperty(ref _totalTimeEntries, value))
            {
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public int TotalTimeEntryMinutes
    {
        get => _totalTimeEntryMinutes;
        set
        {
            if (SetProperty(ref _totalTimeEntryMinutes, value))
            {
                OnPropertyChanged(nameof(TotalTimeEntryTimeDisplay));
                OnPropertyChanged(nameof(CombinedTotalMinutes));
                OnPropertyChanged(nameof(CombinedTotalTimeDisplay));
            }
        }
    }

    public string TotalTimeEntryTimeDisplay => FormatDuration(TotalTimeEntryMinutes);

    #endregion

    #region Combined Statistics

    public int CombinedTotalMinutes => TotalPomodoroMinutes + TotalTimeEntryMinutes;
    public string CombinedTotalTimeDisplay => FormatDuration(CombinedTotalMinutes);

    #endregion

    #region Project Breakdown

    public ObservableCollection<ProjectReportItem> ProjectBreakdown
    {
        get => _projectBreakdown;
        set => SetProperty(ref _projectBreakdown, value);
    }

    #endregion

    #region Commands

    public ICommand LoadReportCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectDailyCommand { get; }
    public ICommand SelectWeeklyCommand { get; }
    public ICommand SelectMonthlyCommand { get; }
    public ICommand SelectCustomCommand { get; }
    public ICommand PreviousPeriodCommand { get; }
    public ICommand NextPeriodCommand { get; }
    public ICommand GoToTodayCommand { get; }

    #endregion

    #region Navigation Methods

    private void GoToPreviousPeriod()
    {
        SelectedDate = SelectedTimePeriod switch
        {
            ReportTimePeriod.Weekly => SelectedDate.AddDays(-7),
            ReportTimePeriod.Monthly => SelectedDate.AddMonths(-1),
            _ => SelectedDate.AddDays(-1)
        };
    }

    private void GoToNextPeriod()
    {
        SelectedDate = SelectedTimePeriod switch
        {
            ReportTimePeriod.Weekly => SelectedDate.AddDays(7),
            ReportTimePeriod.Monthly => SelectedDate.AddMonths(1),
            _ => SelectedDate.AddDays(1)
        };
    }

    private void GoToToday()
    {
        SelectedDate = DateTimeOffset.Now;
    }

    #endregion

    #region Data Loading

    private async Task LoadReportAsync()
    {
        try
        {
            IsLoading = true;

            PomodoroStatsDto pomodoroStats;
            TimeEntryStatsDto timeEntryStats;

            switch (SelectedTimePeriod)
            {
                case ReportTimePeriod.Daily:
                    var daily = await _statisticsService.GetDailyStatisticsAsync(SelectedDate.DateTime);
                    pomodoroStats = daily.PomodoroSessions;
                    timeEntryStats = daily.TimeEntries;
                    break;

                case ReportTimePeriod.Weekly:
                    var weekly = await _statisticsService.GetWeeklyStatisticsAsync(SelectedDate.DateTime);
                    pomodoroStats = weekly.PomodoroSessions;
                    timeEntryStats = weekly.TimeEntries;
                    break;

                case ReportTimePeriod.Monthly:
                    var monthly = await _statisticsService.GetDateRangeStatisticsAsync(
                        GetMonthStart(SelectedDate.DateTime), GetMonthEnd(SelectedDate.DateTime));
                    pomodoroStats = monthly.PomodoroSessions;
                    timeEntryStats = monthly.TimeEntries;
                    break;

                case ReportTimePeriod.Custom:
                    var custom = await _statisticsService.GetDateRangeStatisticsAsync(
                        CustomStartDate.DateTime, CustomEndDate.DateTime);
                    pomodoroStats = custom.PomodoroSessions;
                    timeEntryStats = custom.TimeEntries;
                    break;

                default:
                    return;
            }

            UpdateFromStats(pomodoroStats, timeEntryStats);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load report: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateFromStats(PomodoroStatsDto pomodoroStats, TimeEntryStatsDto timeEntryStats)
    {
        // Update Pomodoro statistics
        TotalPomodoros = pomodoroStats.Total;
        CompletedPomodoros = pomodoroStats.Completed;
        TotalPomodoroMinutes = pomodoroStats.TotalMinutes;

        // Update Time Entry statistics
        TotalTimeEntries = timeEntryStats.Total;
        TotalTimeEntryMinutes = timeEntryStats.TotalMinutes;

        // Build project breakdown
        BuildProjectBreakdown(pomodoroStats.ByProject, timeEntryStats.ByProject);
    }

    private void BuildProjectBreakdown(
        List<ProjectStatsDto> pomodoroByProject,
        List<ProjectStatsDto> timeEntryByProject)
    {
        // Merge projects from both sources
        var projectDict = new Dictionary<int, ProjectReportItem>();

        // Add pomodoro stats
        foreach (var p in pomodoroByProject)
        {
            if (!projectDict.ContainsKey(p.ProjectId))
            {
                projectDict[p.ProjectId] = new ProjectReportItem
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    ClientName = p.ClientName
                };
            }
            projectDict[p.ProjectId].PomodoroCount = p.Count;
            projectDict[p.ProjectId].PomodoroMinutes = p.Minutes;
        }

        // Add time entry stats
        foreach (var t in timeEntryByProject)
        {
            if (!projectDict.ContainsKey(t.ProjectId))
            {
                projectDict[t.ProjectId] = new ProjectReportItem
                {
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectName,
                    ClientName = t.ClientName
                };
            }
            projectDict[t.ProjectId].TimeEntryCount = t.Count;
            projectDict[t.ProjectId].TimeEntryMinutes = t.Minutes;
        }

        // Calculate percentages and sort by total time
        var totalMinutes = CombinedTotalMinutes;
        var items = projectDict.Values
            .OrderByDescending(p => p.TotalMinutes)
            .ToList();

        foreach (var item in items)
        {
            item.PercentageOfTotal = totalMinutes > 0
                ? (double)item.TotalMinutes / totalMinutes * 100
                : 0;
        }

        ProjectBreakdown = new ObservableCollection<ProjectReportItem>(items);
    }

    #endregion

    #region Helpers

    private static string FormatDuration(int minutes)
    {
        if (minutes == 0)
            return "0m";

        var hours = minutes / 60;
        var mins = minutes % 60;

        if (hours > 0 && mins > 0)
            return $"{hours}h {mins}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{mins}m";
    }

    #endregion
}
