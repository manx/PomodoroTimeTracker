using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// ViewModel for adding or editing a project.
/// </summary>
public sealed partial class ProjectDetailViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IClientService _clientService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IPublicHolidayService _publicHolidayService;

    private int? _projectId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private ClientDto? _selectedClient;
    private ObservableCollection<ClientDto> _clients = new();
    private bool _isSaving;
    private bool _isLoading;

    // Work Schedule fields
    private bool _hasWorkSchedule;
    private bool _isWorkScheduleEnabled;
    private int? _workScheduleId;
    private double _baseHoursPerDay = 8.0;
    private bool _workOnMonday = true;
    private bool _workOnTuesday = true;
    private bool _workOnWednesday = true;
    private bool _workOnThursday = true;
    private bool _workOnFriday = true;
    private bool _workOnSaturday;
    private bool _workOnSunday;
    private bool _excludePublicHolidays;
    private CountryDto? _selectedCountry;
    private ObservableCollection<CountryDto> _countries = [];
    private bool _isLoadingCountries;
    private bool _isFetchingHolidays;

    public ProjectDetailViewModel(
        IProjectService projectService,
        IClientService clientService,
        IDialogService dialogService,
        INavigationService navigationService,
        IWorkScheduleService workScheduleService,
        IPublicHolidayService publicHolidayService)
    {
        _projectService = projectService;
        _clientService = clientService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _workScheduleService = workScheduleService;
        _publicHolidayService = publicHolidayService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        FetchHolidaysCommand = new AsyncRelayCommand(FetchHolidaysAsync, CanFetchHolidays);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ((AsyncRelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public ClientDto? SelectedClient
    {
        get => _selectedClient;
        set => SetProperty(ref _selectedClient, value);
    }

    public ObservableCollection<ClientDto> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditMode => _projectId.HasValue;

    public string PageTitle => IsEditMode ? "Edit Project" : "Add New Project";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand FetchHolidaysCommand { get; }

    // Work Schedule Properties
    public bool HasWorkSchedule
    {
        get => _hasWorkSchedule;
        set => SetProperty(ref _hasWorkSchedule, value);
    }

    public bool IsWorkScheduleEnabled
    {
        get => _isWorkScheduleEnabled;
        set
        {
            if (SetProperty(ref _isWorkScheduleEnabled, value))
            {
                if (!value && HasWorkSchedule)
                {
                    // User disabled the toggle - prompt to remove schedule
                    _ = HandleWorkScheduleDisabledAsync();
                }
            }
        }
    }

    public double BaseHoursPerDay
    {
        get => _baseHoursPerDay;
        set => SetProperty(ref _baseHoursPerDay, value);
    }

    public bool WorkOnMonday
    {
        get => _workOnMonday;
        set => SetProperty(ref _workOnMonday, value);
    }

    public bool WorkOnTuesday
    {
        get => _workOnTuesday;
        set => SetProperty(ref _workOnTuesday, value);
    }

    public bool WorkOnWednesday
    {
        get => _workOnWednesday;
        set => SetProperty(ref _workOnWednesday, value);
    }

    public bool WorkOnThursday
    {
        get => _workOnThursday;
        set => SetProperty(ref _workOnThursday, value);
    }

    public bool WorkOnFriday
    {
        get => _workOnFriday;
        set => SetProperty(ref _workOnFriday, value);
    }

    public bool WorkOnSaturday
    {
        get => _workOnSaturday;
        set => SetProperty(ref _workOnSaturday, value);
    }

    public bool WorkOnSunday
    {
        get => _workOnSunday;
        set => SetProperty(ref _workOnSunday, value);
    }

    public bool ExcludePublicHolidays
    {
        get => _excludePublicHolidays;
        set => SetProperty(ref _excludePublicHolidays, value);
    }

    public CountryDto? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            if (SetProperty(ref _selectedCountry, value))
            {
                ((AsyncRelayCommand)FetchHolidaysCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<CountryDto> Countries
    {
        get => _countries;
        set => SetProperty(ref _countries, value);
    }

    public bool IsLoadingCountries
    {
        get => _isLoadingCountries;
        set => SetProperty(ref _isLoadingCountries, value);
    }

    public bool IsFetchingHolidays
    {
        get => _isFetchingHolidays;
        set => SetProperty(ref _isFetchingHolidays, value);
    }

    public async Task InitializeForAddAsync()
    {
        _projectId = null;
        Name = string.Empty;
        Description = string.Empty;
        SelectedClient = null;
        ResetWorkSchedule();

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        await LoadClientsAsync();
    }

    public async Task InitializeForEditAsync(int projectId)
    {
        _projectId = projectId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        await LoadClientsAsync();

        try
        {
            IsLoading = true;
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project != null)
            {
                Name = project.Name;
                Description = project.Description ?? string.Empty;

                if (project.ClientId.HasValue)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == project.ClientId.Value);
                }
            }

            // Load work schedule
            await LoadWorkScheduleAsync(projectId);

            // Load countries for dropdown (in background)
            _ = LoadCountriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load project: {ex.Message}", "Error");
            Cancel();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load clients: {ex.Message}", "Error");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            int projectId;
            if (IsEditMode)
            {
                var updateDto = new UpdateProjectDto
                {
                    Id = _projectId!.Value,
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                    ClientId = SelectedClient?.Id
                };

                await _projectService.UpdateProjectAsync(updateDto);
                projectId = _projectId.Value;
            }
            else
            {
                var createDto = new CreateProjectDto
                {
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                    ClientId = SelectedClient?.Id
                };

                var createdProject = await _projectService.CreateProjectAsync(createDto);
                projectId = createdProject.Id;
            }

            // Save work schedule if enabled (for edit mode or after creating)
            if (IsWorkScheduleEnabled)
            {
                await SaveWorkScheduleInternalAsync(projectId);
            }
            else if (HasWorkSchedule)
            {
                // Work schedule was disabled - remove it
                await RemoveWorkScheduleInternalAsync();
            }

            // Store the ID so the list can select it
            _navigationService.ProjectIdToSelect = projectId;

            _navigationService.GoBack();
        }
        catch (OperationCanceledException)
        {
            // User cancelled - don't navigate, stay on page
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save project: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) && !IsSaving;
    }

    private void Cancel()
    {
        _navigationService.GoBack();
    }

    private void ResetWorkSchedule()
    {
        _workScheduleId = null;
        HasWorkSchedule = false;
        _isWorkScheduleEnabled = false;
        OnPropertyChanged(nameof(IsWorkScheduleEnabled));
        BaseHoursPerDay = 8.0;
        WorkOnMonday = true;
        WorkOnTuesday = true;
        WorkOnWednesday = true;
        WorkOnThursday = true;
        WorkOnFriday = true;
        WorkOnSaturday = false;
        WorkOnSunday = false;
        ExcludePublicHolidays = false;
        SelectedCountry = null;
    }

    private async Task HandleWorkScheduleDisabledAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Disabling will remove the existing work schedule when you save. Continue?",
            "Remove Work Schedule");

        if (!confirmed)
        {
            // User cancelled - restore toggle state
            _isWorkScheduleEnabled = true;
            OnPropertyChanged(nameof(IsWorkScheduleEnabled));
        }
        // If confirmed, the schedule will be deleted when Save is clicked
    }

    private async Task RemoveWorkScheduleInternalAsync()
    {
        if (!_workScheduleId.HasValue)
            return;

        await _workScheduleService.DeleteAsync(_workScheduleId.Value);
        _workScheduleId = null;
        HasWorkSchedule = false;
    }

    private async Task LoadWorkScheduleAsync(int projectId)
    {
        var schedule = await _workScheduleService.GetByProjectIdAsync(projectId);
        if (schedule is not null)
        {
            _workScheduleId = schedule.Id;
            HasWorkSchedule = true;
            _isWorkScheduleEnabled = true;
            OnPropertyChanged(nameof(IsWorkScheduleEnabled));
            BaseHoursPerDay = (double)schedule.BaseHoursPerDay;
            SetWorkDaysFromFlags(schedule.WorkDays);
            // Invert: IncludePublicHolidays in DTO means count them as work days
            // ExcludePublicHolidays in UI means DON'T count them
            ExcludePublicHolidays = !schedule.IncludePublicHolidays;

            // Country will be selected after countries are loaded
            if (!string.IsNullOrEmpty(schedule.CountryCode))
            {
                // Load countries first if needed, then select
                await LoadCountriesAsync();
                SelectedCountry = Countries.FirstOrDefault(c => c.CountryCode == schedule.CountryCode);
            }
        }
        else
        {
            ResetWorkSchedule();
        }
    }

    private async Task LoadCountriesAsync()
    {
        if (Countries.Count > 0 || IsLoadingCountries)
            return;

        try
        {
            IsLoadingCountries = true;
            var countries = await _publicHolidayService.GetAvailableCountriesAsync();
            Countries = new ObservableCollection<CountryDto>(countries.OrderBy(c => c.Name));
        }
        catch
        {
            // Silently fail - countries dropdown will be empty
        }
        finally
        {
            IsLoadingCountries = false;
        }
    }

    private bool CanFetchHolidays()
    {
        return SelectedCountry != null && !IsFetchingHolidays;
    }

    private async Task FetchHolidaysAsync()
    {
        if (SelectedCountry == null)
            return;

        try
        {
            IsFetchingHolidays = true;
            ((AsyncRelayCommand)FetchHolidaysCommand).NotifyCanExecuteChanged();

            var currentYear = DateTime.Now.Year;
            await _publicHolidayService.RefreshHolidaysAsync(SelectedCountry.CountryCode, currentYear);

            _dialogService.ShowToast(
                $"Downloaded public holidays for {currentYear} ({SelectedCountry.Name})",
                "Public Holidays");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to fetch holidays: {ex.Message}", "Error");
        }
        finally
        {
            IsFetchingHolidays = false;
            ((AsyncRelayCommand)FetchHolidaysCommand).NotifyCanExecuteChanged();
        }
    }

    private async Task SaveWorkScheduleInternalAsync(int projectId)
    {
        // Check for conflicts when creating a new schedule
        if (!_workScheduleId.HasValue)
        {
            var conflictResult = await _workScheduleService.CheckProjectScheduleConflictsAsync(projectId);
            if (conflictResult.HasConflicts)
            {
                var message = $"The client \"{conflictResult.ConflictingClientName}\" already has a work schedule. " +
                              "Enabling a project-level schedule will remove the client's schedule.\n\n" +
                              "Do you want to continue?";

                var confirmed = await _dialogService.ShowConfirmationAsync(message, "Schedule Conflict");
                if (!confirmed)
                {
                    throw new OperationCanceledException("User cancelled due to schedule conflict.");
                }

                // Delete the client's schedule
                if (SelectedClient is not null)
                {
                    var clientSchedule = await _workScheduleService.GetByClientIdAsync(SelectedClient.Id);
                    if (clientSchedule is not null)
                    {
                        await _workScheduleService.DeleteAsync(clientSchedule.Id);
                    }
                }
            }
        }

        var workDays = GetWorkDaysFlags();

        if (_workScheduleId.HasValue)
        {
            var updateDto = new UpdateWorkScheduleDto
            {
                Id = _workScheduleId.Value,
                BaseHoursPerDay = (decimal)BaseHoursPerDay,
                WorkDays = workDays,
                // Invert: ExcludePublicHolidays in UI -> IncludePublicHolidays in DTO
                IncludePublicHolidays = !ExcludePublicHolidays,
                CountryCode = SelectedCountry?.CountryCode
            };
            await _workScheduleService.UpdateAsync(updateDto);
        }
        else
        {
            var createDto = new CreateWorkScheduleDto
            {
                ProjectId = projectId,
                BaseHoursPerDay = (decimal)BaseHoursPerDay,
                WorkDays = workDays,
                // Invert: ExcludePublicHolidays in UI -> IncludePublicHolidays in DTO
                IncludePublicHolidays = !ExcludePublicHolidays,
                CountryCode = SelectedCountry?.CountryCode
            };
            var created = await _workScheduleService.CreateAsync(createDto);
            _workScheduleId = created.Id;
        }

        HasWorkSchedule = true;

        // Fetch holidays in background if excluding public holidays and country is set
        if (!ExcludePublicHolidays || SelectedCountry == null)
            return;

        // Fire and forget - don't block saving
        _ = Task.Run(async () =>
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                await _publicHolidayService.RefreshHolidaysAsync(SelectedCountry.CountryCode, currentYear);
                _dialogService.ShowToast(
                    $"Downloaded public holidays for {currentYear} ({SelectedCountry.Name})",
                    "Public Holidays");
            }
            catch
            {
                // Silently fail - holidays will be fetched on demand anyway
            }
        });
    }

    private WorkDaysFlags GetWorkDaysFlags()
    {
        var flags = WorkDaysFlags.None;
        if (WorkOnSunday)
            flags |= WorkDaysFlags.Sunday;
        if (WorkOnMonday)
            flags |= WorkDaysFlags.Monday;
        if (WorkOnTuesday)
            flags |= WorkDaysFlags.Tuesday;
        if (WorkOnWednesday)
            flags |= WorkDaysFlags.Wednesday;
        if (WorkOnThursday)
            flags |= WorkDaysFlags.Thursday;
        if (WorkOnFriday)
            flags |= WorkDaysFlags.Friday;
        if (WorkOnSaturday)
            flags |= WorkDaysFlags.Saturday;
        return flags;
    }

    private void SetWorkDaysFromFlags(WorkDaysFlags flags)
    {
        WorkOnSunday = (flags & WorkDaysFlags.Sunday) != 0;
        WorkOnMonday = (flags & WorkDaysFlags.Monday) != 0;
        WorkOnTuesday = (flags & WorkDaysFlags.Tuesday) != 0;
        WorkOnWednesday = (flags & WorkDaysFlags.Wednesday) != 0;
        WorkOnThursday = (flags & WorkDaysFlags.Thursday) != 0;
        WorkOnFriday = (flags & WorkDaysFlags.Friday) != 0;
        WorkOnSaturday = (flags & WorkDaysFlags.Saturday) != 0;
    }
}
