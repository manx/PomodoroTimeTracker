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
/// Represents a selectable week option for the report.
/// </summary>
public sealed class WeekOption
{
    public int Year { get; }
    public int WeekNumber { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    // TODO: Handle week start day based on user locale/settings (Sunday vs Monday)
    // TODO: Handle week year at end of December (week 1 may belong to next year, week 52/53 may belong to previous year)
    public WeekOption(DateTime dateInWeek)
    {
        // Get week start (Sunday)
        StartDate = dateInWeek.AddDays(-(int)dateInWeek.DayOfWeek);
        EndDate = StartDate.AddDays(6);
        Year = StartDate.Year;
        WeekNumber = GetIso8601WeekNumber(StartDate);
    }

    public string Display => $"Week {WeekNumber} ({StartDate:MMM d})";

    // TODO: Handle different week number systems (ISO 8601 vs US) based on user locale/settings
    private static int GetIso8601WeekNumber(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    public override bool Equals(object? obj)
    {
        return obj is WeekOption other && Year == other.Year && WeekNumber == other.WeekNumber;
    }

    public override int GetHashCode() => HashCode.Combine(Year, WeekNumber);
}

/// <summary>
/// Represents a selectable month option for the report.
/// </summary>
public sealed class MonthOption
{
    public int Year { get; }
    public int Month { get; }

    public MonthOption(int year, int month)
    {
        Year = year;
        Month = month;
    }

    public MonthOption(DateTime date) : this(date.Year, date.Month) { }

    public string Display => $"{new DateTime(Year, Month, 1):MMMM}";

    public DateTime StartDate => new(Year, Month, 1);
    public DateTime EndDate => StartDate.AddMonths(1).AddDays(-1);

    public override bool Equals(object? obj)
    {
        return obj is MonthOption other && Year == other.Year && Month == other.Month;
    }

    public override int GetHashCode() => HashCode.Combine(Year, Month);
}

/// <summary>
/// Base class for report breakdown items.
/// </summary>
public abstract class ReportItemBase
{
    public int PomodoroCount { get; set; }
    public int PomodoroMinutes { get; set; }
    public int TimeEntryCount { get; set; }
    public int TimeEntryMinutes { get; set; }
    public int TotalMinutes => PomodoroMinutes + TimeEntryMinutes;
    public double PercentageOfTotal { get; set; }

    public string TotalTimeDisplay => FormatDuration(TotalMinutes);
    public string PomodoroDisplay => $"{PomodoroCount} sessions ({FormatDuration(PomodoroMinutes)})";
    public string TimeEntryDisplay => $"{TimeEntryCount} entries ({FormatDuration(TimeEntryMinutes)})";

    protected static string FormatDuration(int minutes)
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
/// Display item for a project in the report breakdown.
/// </summary>
public sealed class ProjectReportItem : ReportItemBase
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
}

/// <summary>
/// Filter option for client/project dropdowns.
/// </summary>
public sealed class FilterOption
{
    public int? Id { get; }
    public string Name { get; }

    public FilterOption(int? id, string name)
    {
        Id = id;
        Name = name;
    }

    public static FilterOption All { get; } = new(null, "All");
}

/// <summary>
/// ViewModel for the report view showing combined statistics.
/// </summary>
public sealed partial class ReportViewModel : ViewModelBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IProjectService _projectService;
    private readonly IDialogService _dialogService;

    private ReportTimePeriod _selectedTimePeriod = ReportTimePeriod.Daily;
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
    private DateTimeOffset _customStartDate = DateTimeOffset.Now.AddDays(-7);
    private DateTimeOffset _customEndDate = DateTimeOffset.Now;
    private bool _isLoading;

    // Week/Month selection
    private WeekOption? _selectedWeek;
    private MonthOption? _selectedMonth;
    private ObservableCollection<WeekOption> _weekOptions = new();
    private ObservableCollection<MonthOption> _monthOptions = new();

    // Year selection for Weekly/Monthly
    private ObservableCollection<int> _weekYearOptions = new();
    private ObservableCollection<int> _monthYearOptions = new();
    private int _selectedWeekYear;
    private int _selectedMonthYear;

    // Client/Project filters
    private ObservableCollection<FilterOption> _clientFilterOptions = new();
    private ObservableCollection<FilterOption> _projectFilterOptions = new();
    private FilterOption _selectedClientFilter = FilterOption.All;
    private FilterOption _selectedProjectFilter = FilterOption.All;
    private List<ProjectReportItem> _allProjectItems = new();

    // Statistics data
    private int _totalPomodoros;
    private int _completedPomodoros;
    private int _totalPomodoroMinutes;
    private int _totalTimeEntries;
    private int _totalTimeEntryMinutes;
    private ObservableCollection<ProjectReportItem> _projectBreakdown = new();

    public ReportViewModel(
        IStatisticsService statisticsService,
        IProjectService projectService,
        IDialogService dialogService)
    {
        _statisticsService = statisticsService;
        _projectService = projectService;
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

        // Initialize week/month options
        InitializeWeekOptions();
        InitializeMonthOptions();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadFilterOptionsAsync();
        await LoadReportAsync();
    }

    private async Task LoadFilterOptionsAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();

        // Build client filter options from distinct clients
        var clients = projects
            .Where(p => p.ClientId.HasValue)
            .Select(p => new { p.ClientId, p.ClientName })
            .Distinct()
            .OrderBy(c => c.ClientName)
            .ToList();

        var clientOptions = new List<FilterOption> { FilterOption.All };
        clientOptions.AddRange(clients.Select(c => new FilterOption(c.ClientId, c.ClientName ?? "Unknown")));
        ClientFilterOptions = new ObservableCollection<FilterOption>(clientOptions);
        SelectedClientFilter = FilterOption.All;

        // Build project filter options
        var projectOptions = new List<FilterOption> { FilterOption.All };
        projectOptions.AddRange(projects
            .OrderBy(p => p.Name)
            .Select(p => new FilterOption(p.Id, p.Name)));
        ProjectFilterOptions = new ObservableCollection<FilterOption>(projectOptions);
        SelectedProjectFilter = FilterOption.All;
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

    #region Client/Project Filters

    public ObservableCollection<FilterOption> ClientFilterOptions
    {
        get => _clientFilterOptions;
        set => SetProperty(ref _clientFilterOptions, value);
    }

    public FilterOption SelectedClientFilter
    {
        get => _selectedClientFilter;
        set
        {
            if (SetProperty(ref _selectedClientFilter, value))
            {
                OnClientFilterChanged();
            }
        }
    }

    public ObservableCollection<FilterOption> ProjectFilterOptions
    {
        get => _projectFilterOptions;
        set => SetProperty(ref _projectFilterOptions, value);
    }

    public FilterOption SelectedProjectFilter
    {
        get => _selectedProjectFilter;
        set
        {
            if (SetProperty(ref _selectedProjectFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private void OnClientFilterChanged()
    {
        // When client changes, update project filter options to only show that client's projects
        if (_selectedClientFilter.Id == null)
        {
            // "All" selected - show all projects
            _ = LoadFilterOptionsAsync();
        }
        else
        {
            // Filter projects by selected client
            _ = UpdateProjectFilterForClientAsync(_selectedClientFilter.Id.Value);
        }
        ApplyFilters();
    }

    private async Task UpdateProjectFilterForClientAsync(int clientId)
    {
        var projects = await _projectService.GetProjectsByClientIdAsync(clientId);
        var projectOptions = new List<FilterOption> { FilterOption.All };
        projectOptions.AddRange(projects
            .OrderBy(p => p.Name)
            .Select(p => new FilterOption(p.Id, p.Name)));
        ProjectFilterOptions = new ObservableCollection<FilterOption>(projectOptions);
        SelectedProjectFilter = FilterOption.All;
    }

    private void ApplyFilters()
    {
        if (_allProjectItems.Count == 0)
        {
            ProjectBreakdown = new ObservableCollection<ProjectReportItem>();
            return;
        }

        var filtered = _allProjectItems.AsEnumerable();

        // Apply client filter
        if (_selectedClientFilter.Id != null)
        {
            var clientName = _selectedClientFilter.Name;
            filtered = filtered.Where(p => p.ClientName == clientName);
        }

        // Apply project filter
        if (_selectedProjectFilter.Id != null)
        {
            filtered = filtered.Where(p => p.ProjectId == _selectedProjectFilter.Id);
        }

        // Recalculate percentages based on filtered total
        var filteredList = filtered.ToList();
        var filteredTotal = filteredList.Sum(p => p.TotalMinutes);

        foreach (var item in filteredList)
        {
            item.PercentageOfTotal = filteredTotal > 0
                ? (double)item.TotalMinutes / filteredTotal * 100
                : 0;
        }

        ProjectBreakdown = new ObservableCollection<ProjectReportItem>(filteredList);
    }

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

    #region Week/Month Selection

    // Weekly year selection
    public ObservableCollection<int> WeekYearOptions
    {
        get => _weekYearOptions;
        set => SetProperty(ref _weekYearOptions, value);
    }

    public int SelectedWeekYear
    {
        get => _selectedWeekYear;
        set
        {
            if (SetProperty(ref _selectedWeekYear, value))
            {
                UpdateWeekOptionsForYear(value);
            }
        }
    }

    public ObservableCollection<WeekOption> WeekOptions
    {
        get => _weekOptions;
        set => SetProperty(ref _weekOptions, value);
    }

    public WeekOption? SelectedWeek
    {
        get => _selectedWeek;
        set
        {
            if (SetProperty(ref _selectedWeek, value) && value != null)
            {
                // Update SelectedDate to match the week without triggering reload
                _selectedDate = new DateTimeOffset(value.StartDate);
                OnPropertyChanged(nameof(SelectedDate));
                OnPropertyChanged(nameof(DateRangeDisplay));
                _ = LoadReportAsync();
            }
        }
    }

    // Monthly year selection
    public ObservableCollection<int> MonthYearOptions
    {
        get => _monthYearOptions;
        set => SetProperty(ref _monthYearOptions, value);
    }

    public int SelectedMonthYear
    {
        get => _selectedMonthYear;
        set
        {
            if (SetProperty(ref _selectedMonthYear, value))
            {
                UpdateMonthOptionsForYear(value);
            }
        }
    }

    public ObservableCollection<MonthOption> MonthOptions
    {
        get => _monthOptions;
        set => SetProperty(ref _monthOptions, value);
    }

    public MonthOption? SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value) && value != null)
            {
                // Update SelectedDate to match the month without triggering reload
                _selectedDate = new DateTimeOffset(value.StartDate);
                OnPropertyChanged(nameof(SelectedDate));
                OnPropertyChanged(nameof(DateRangeDisplay));
                _ = LoadReportAsync();
            }
        }
    }

    private void InitializeWeekOptions()
    {
        var today = DateTime.Today;

        // Generate year options: 2 years back + current year
        var years = Enumerable.Range(today.Year - 2, 3).Reverse().ToList();
        WeekYearOptions = new ObservableCollection<int>(years);

        // Select current year (this will trigger UpdateWeekOptionsForYear)
        _selectedWeekYear = today.Year;
        OnPropertyChanged(nameof(SelectedWeekYear));
        UpdateWeekOptionsForYear(today.Year, selectCurrentWeek: true);
    }

    private void UpdateWeekOptionsForYear(int year, bool selectCurrentWeek = false)
    {
        var options = new List<WeekOption>();
        var today = DateTime.Today;

        // Generate all weeks for the selected year
        var startOfYear = new DateTime(year, 1, 1);
        var endOfYear = new DateTime(year, 12, 31);

        // Start from first week containing days in this year
        var firstWeek = new WeekOption(startOfYear);
        var currentDate = firstWeek.StartDate;

        while (currentDate <= endOfYear)
        {
            var week = new WeekOption(currentDate);
            // Only add weeks that haven't ended yet if current year, or all weeks for past years
            if (year < today.Year || week.EndDate <= today)
            {
                options.Add(week);
            }
            currentDate = currentDate.AddDays(7);
        }

        WeekOptions = new ObservableCollection<WeekOption>(options);

        // Select appropriate week
        if (selectCurrentWeek && year == today.Year)
        {
            var currentWeek = new WeekOption(today);
            SelectedWeek = WeekOptions.FirstOrDefault(w => w.Equals(currentWeek)) ?? WeekOptions.LastOrDefault();
        }
        else
        {
            // Select last week of the year
            _selectedWeek = WeekOptions.LastOrDefault();
            OnPropertyChanged(nameof(SelectedWeek));
            if (_selectedWeek != null)
            {
                _selectedDate = new DateTimeOffset(_selectedWeek.StartDate);
                OnPropertyChanged(nameof(SelectedDate));
                OnPropertyChanged(nameof(DateRangeDisplay));
                _ = LoadReportAsync();
            }
        }
    }

    private void InitializeMonthOptions()
    {
        var today = DateTime.Today;

        // Generate year options: 2 years back + current year
        var years = Enumerable.Range(today.Year - 2, 3).Reverse().ToList();
        MonthYearOptions = new ObservableCollection<int>(years);

        // Select current year (this will trigger UpdateMonthOptionsForYear)
        _selectedMonthYear = today.Year;
        OnPropertyChanged(nameof(SelectedMonthYear));
        UpdateMonthOptionsForYear(today.Year, selectCurrentMonth: true);
    }

    private void UpdateMonthOptionsForYear(int year, bool selectCurrentMonth = false)
    {
        var options = new List<MonthOption>();
        var today = DateTime.Today;

        // Generate months for the selected year
        var maxMonth = year == today.Year ? today.Month : 12;
        for (int month = 1; month <= maxMonth; month++)
        {
            options.Add(new MonthOption(year, month));
        }

        MonthOptions = new ObservableCollection<MonthOption>(options);

        // Select appropriate month
        if (selectCurrentMonth && year == today.Year)
        {
            var currentMonth = new MonthOption(today);
            SelectedMonth = MonthOptions.FirstOrDefault(m => m.Equals(currentMonth)) ?? MonthOptions.LastOrDefault();
        }
        else
        {
            // Select last month of the year
            _selectedMonth = MonthOptions.LastOrDefault();
            OnPropertyChanged(nameof(SelectedMonth));
            if (_selectedMonth != null)
            {
                _selectedDate = new DateTimeOffset(_selectedMonth.StartDate);
                OnPropertyChanged(nameof(SelectedDate));
                OnPropertyChanged(nameof(DateRangeDisplay));
                _ = LoadReportAsync();
            }
        }
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

    #region Breakdown

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

        // Build breakdowns
        BuildBreakdowns(pomodoroStats.ByProject, timeEntryStats.ByProject);
    }

    private void BuildBreakdowns(
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

        // Sort by total time
        _allProjectItems = projectDict.Values
            .OrderByDescending(p => p.TotalMinutes)
            .ToList();

        // Apply current filters
        ApplyFilters();
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
