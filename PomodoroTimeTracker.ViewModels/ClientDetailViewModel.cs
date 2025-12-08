using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.ViewModels;

/// <summary>
/// ViewModel for adding or editing a client.
/// </summary>
public sealed partial class ClientDetailViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IPublicHolidayService _publicHolidayService;

    private int? _clientId;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isSaving;

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

    public ClientDetailViewModel(
        IClientService clientService,
        IDialogService dialogService,
        INavigationService navigationService,
        IWorkScheduleService workScheduleService,
        IPublicHolidayService publicHolidayService)
    {
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

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsEditMode => _clientId.HasValue;

    public string PageTitle => IsEditMode ? "Edit Client" : "Add New Client";

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

    /// <summary>
    /// Initialize the ViewModel for adding a new client
    /// </summary>
    public void InitializeForAdd()
    {
        _clientId = null;
        Name = string.Empty;
        Description = string.Empty;
        ResetWorkSchedule();
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    /// <summary>
    /// Initialize the ViewModel for editing an existing client
    /// </summary>
    public async Task InitializeForEditAsync(int clientId)
    {
        _clientId = clientId;
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        try
        {
            var client = await _clientService.GetClientByIdAsync(clientId);
            if (client != null)
            {
                Name = client.Name;
                Description = client.Description ?? string.Empty;
            }

            // Load work schedule
            await LoadWorkScheduleAsync(clientId);

            // Load countries for dropdown (in background)
            _ = LoadCountriesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to load client: {ex.Message}", "Error");
            Cancel();
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            int clientId;
            if (IsEditMode)
            {
                // Update existing client
                var updateDto = new UpdateClientDto
                {
                    Id = _clientId!.Value,
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description
                };

                await _clientService.UpdateClientAsync(updateDto);
                clientId = _clientId.Value;
            }
            else
            {
                // Create new client
                var createDto = new CreateClientDto
                {
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description
                };

                var createdClient = await _clientService.CreateClientAsync(createDto);
                clientId = createdClient.Id;
            }

            // Save work schedule if enabled (for edit mode or after creating)
            if (IsWorkScheduleEnabled)
            {
                await SaveWorkScheduleInternalAsync(clientId);
            }
            else if (HasWorkSchedule)
            {
                // Work schedule was disabled - remove it
                await RemoveWorkScheduleInternalAsync();
            }

            // Store the ID so the list can select it
            _navigationService.ClientIdToSelect = clientId;

            // Navigate back to list
            _navigationService.GoBack();
        }
        catch (OperationCanceledException)
        {
            // User cancelled - don't navigate, stay on page
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save client: {ex.Message}", "Error");
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

    private async Task LoadWorkScheduleAsync(int clientId)
    {
        var schedule = await _workScheduleService.GetByClientIdAsync(clientId);
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

    private async Task SaveWorkScheduleInternalAsync(int clientId)
    {
        // Check for conflicts when creating a new schedule
        if (!_workScheduleId.HasValue)
        {
            var conflictResult = await _workScheduleService.CheckClientScheduleConflictsAsync(clientId);
            if (conflictResult.HasConflicts)
            {
                var projectNames = string.Join(", ", conflictResult.ConflictingProjectNames);
                var message = conflictResult.ConflictCount == 1
                    ? $"The project \"{projectNames}\" already has its own work schedule. " +
                      "Enabling a client-level schedule will remove the project's schedule.\n\n" +
                      "Do you want to continue?"
                    : $"The following projects have their own work schedules: {projectNames}. " +
                      "Enabling a client-level schedule will remove these project schedules.\n\n" +
                      "Do you want to continue?";

                var confirmed = await _dialogService.ShowConfirmationAsync(message, "Schedule Conflict");
                if (!confirmed)
                {
                    throw new OperationCanceledException("User cancelled due to schedule conflict.");
                }

                // Delete conflicting project schedules
                await _workScheduleService.DeleteProjectSchedulesForClientAsync(clientId);
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
                ClientId = clientId,
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
