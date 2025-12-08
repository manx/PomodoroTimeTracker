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
    private int _workPercentage = 100;
    private double _baseHoursPerDay = 8.0;
    private bool _workOnMonday = true;
    private bool _workOnTuesday = true;
    private bool _workOnWednesday = true;
    private bool _workOnThursday = true;
    private bool _workOnFriday = true;
    private bool _workOnSaturday;
    private bool _workOnSunday;
    private bool _includePublicHolidays;
    private CountryDto? _selectedCountry;
    private ObservableCollection<CountryDto> _countries = [];
    private bool _isLoadingCountries;

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
        SaveWorkScheduleCommand = new AsyncRelayCommand(SaveWorkScheduleAsync);
        RemoveWorkScheduleCommand = new AsyncRelayCommand(RemoveWorkScheduleAsync);
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
    public ICommand SaveWorkScheduleCommand { get; }
    public ICommand RemoveWorkScheduleCommand { get; }

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

    public int WorkPercentage
    {
        get => _workPercentage;
        set => SetProperty(ref _workPercentage, value);
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

    public bool IncludePublicHolidays
    {
        get => _includePublicHolidays;
        set => SetProperty(ref _includePublicHolidays, value);
    }

    public CountryDto? SelectedCountry
    {
        get => _selectedCountry;
        set => SetProperty(ref _selectedCountry, value);
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
                // Store the ID so the list can select it
                _navigationService.ClientIdToSelect = _clientId.Value;
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
                // Store the ID so the list can select it
                _navigationService.ClientIdToSelect = createdClient.Id;
            }

            // Navigate back to list
            _navigationService.GoBack();
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
        WorkPercentage = 100;
        BaseHoursPerDay = 8.0;
        WorkOnMonday = true;
        WorkOnTuesday = true;
        WorkOnWednesday = true;
        WorkOnThursday = true;
        WorkOnFriday = true;
        WorkOnSaturday = false;
        WorkOnSunday = false;
        IncludePublicHolidays = false;
        SelectedCountry = null;
    }

    private async Task HandleWorkScheduleDisabledAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Disabling will remove the existing work schedule. Continue?",
            "Remove Work Schedule");

        if (confirmed)
        {
            await RemoveWorkScheduleInternalAsync();
        }
        else
        {
            // User cancelled - restore toggle state
            _isWorkScheduleEnabled = true;
            OnPropertyChanged(nameof(IsWorkScheduleEnabled));
        }
    }

    private async Task RemoveWorkScheduleInternalAsync()
    {
        if (!_workScheduleId.HasValue)
            return;

        try
        {
            IsSaving = true;
            await _workScheduleService.DeleteAsync(_workScheduleId.Value);
            ResetWorkSchedule();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to remove work schedule: {ex.Message}", "Error");
            // Restore toggle state on error
            _isWorkScheduleEnabled = true;
            OnPropertyChanged(nameof(IsWorkScheduleEnabled));
        }
        finally
        {
            IsSaving = false;
        }
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
            WorkPercentage = schedule.WorkPercentage;
            BaseHoursPerDay = (double)schedule.BaseHoursPerDay;
            SetWorkDaysFromFlags(schedule.WorkDays);
            IncludePublicHolidays = schedule.IncludePublicHolidays;

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

    private async Task SaveWorkScheduleAsync()
    {
        if (!_clientId.HasValue)
            return;

        try
        {
            IsSaving = true;

            // Check for conflicts when creating a new schedule
            if (!_workScheduleId.HasValue)
            {
                var conflictResult = await _workScheduleService.CheckClientScheduleConflictsAsync(_clientId.Value);
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
                        return;
                    }

                    // Delete conflicting project schedules
                    await _workScheduleService.DeleteProjectSchedulesForClientAsync(_clientId.Value);
                }
            }

            var workDays = GetWorkDaysFlags();

            if (_workScheduleId.HasValue)
            {
                var updateDto = new UpdateWorkScheduleDto
                {
                    Id = _workScheduleId.Value,
                    WorkPercentage = WorkPercentage,
                    BaseHoursPerDay = (decimal)BaseHoursPerDay,
                    WorkDays = workDays,
                    IncludePublicHolidays = IncludePublicHolidays,
                    CountryCode = SelectedCountry?.CountryCode
                };
                await _workScheduleService.UpdateAsync(updateDto);
            }
            else
            {
                var createDto = new CreateWorkScheduleDto
                {
                    ClientId = _clientId.Value,
                    WorkPercentage = WorkPercentage,
                    BaseHoursPerDay = (decimal)BaseHoursPerDay,
                    WorkDays = workDays,
                    IncludePublicHolidays = IncludePublicHolidays,
                    CountryCode = SelectedCountry?.CountryCode
                };
                var created = await _workScheduleService.CreateAsync(createDto);
                _workScheduleId = created.Id;
            }

            HasWorkSchedule = true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to save work schedule: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task RemoveWorkScheduleAsync()
    {
        if (!_workScheduleId.HasValue)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Are you sure you want to remove the work schedule?",
            "Remove Work Schedule");

        if (!confirmed)
            return;

        try
        {
            IsSaving = true;
            await _workScheduleService.DeleteAsync(_workScheduleId.Value);
            ResetWorkSchedule();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync($"Failed to remove work schedule: {ex.Message}", "Error");
        }
        finally
        {
            IsSaving = false;
        }
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
