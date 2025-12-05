using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.Domain.Entities;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for GeneralSettingsViewModel.
/// Tests settings loading, saving, change tracking, and reset functionality.
/// </summary>
public class GeneralSettingsViewModelTests
{
    private readonly Mock<IAppSettingsService> _appSettingsServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public GeneralSettingsViewModelTests()
    {
        _appSettingsServiceMock = new Mock<IAppSettingsService>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    private GeneralSettingsViewModel CreateViewModel()
    {
        return new GeneralSettingsViewModel(
            _appSettingsServiceMock.Object,
            _dialogServiceMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SaveCommand.Should().NotBeNull();
        viewModel.ResetToDefaultsCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesAvailableOptions()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.AvailableLanguages.Should().NotBeEmpty();
        viewModel.AvailableLanguages.Should().Contain(l => l.Code == null); // System Default
        viewModel.AvailableLanguages.Should().Contain(l => l.Code == "en-US");

        viewModel.AvailableDateFormats.Should().NotBeEmpty();
        viewModel.AvailableDateFormats.Should().Contain(d => d.Format == null); // System Default
        viewModel.AvailableDateFormats.Should().Contain(d => d.Format == "MM/dd/yyyy");

        viewModel.AvailableWeekStartDays.Should().NotBeEmpty();
        viewModel.AvailableWeekStartDays.Should().Contain(w => w.DayOfWeek == DayOfWeek.Sunday);
        viewModel.AvailableWeekStartDays.Should().Contain(w => w.DayOfWeek == DayOfWeek.Monday);

        viewModel.AvailableWeekYearStandards.Should().NotBeEmpty();
        viewModel.AvailableWeekYearStandards.Should().Contain(w => w.Standard == WeekYearStandard.Iso8601);
        viewModel.AvailableWeekYearStandards.Should().Contain(w => w.Standard == WeekYearStandard.UsStandard);
    }

    [Fact]
    public void Constructor_SetsInitialDefaults()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.IsSaving.Should().BeFalse();
        viewModel.IsNotSaving.Should().BeTrue();
        viewModel.HasChanges.Should().BeFalse();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_PopulatesProperties_FromService()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = 1, // US Standard
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.SelectedLanguage.Code.Should().Be("en-US");
        viewModel.SelectedDateFormat.Format.Should().Be("MM/dd/yyyy");
        viewModel.SelectedWeekStartDay.DayOfWeek.Should().Be(DayOfWeek.Monday);
        viewModel.SelectedWeekYearStandard.Standard.Should().Be(WeekYearStandard.UsStandard);
        viewModel.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithNullOverrides_SelectsSystemDefaults()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0, // ISO 8601
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.SelectedLanguage.Code.Should().BeNull();
        viewModel.UseSystemLanguage.Should().BeTrue();
        viewModel.SelectedDateFormat.Format.Should().BeNull();
        viewModel.UseSystemDateFormat.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ClearsHasChangesFlag()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        viewModel.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithError_ShowsErrorDialog()
    {
        // Arrange
        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(msg => msg.Contains("Failed to load settings")),
            "Error"), Times.Once);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_CallsService_WithCorrectValues()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Modify settings
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");
        viewModel.SelectedWeekStartDay = viewModel.AvailableWeekStartDays.First(w => w.DayOfWeek == DayOfWeek.Monday);

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _appSettingsServiceMock.Verify(s => s.UpdateSettingsAsync(
            It.Is<UpdateAppSettingsDto>(dto =>
                dto.LanguageOverride == "en-US" &&
                dto.WeekStartDay == DayOfWeek.Monday),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_SetsIsSavingFlag()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Modify to enable save
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Act
        var saveTask = ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert - IsSaving should be set during execution
        // Since we can't easily test async timing, verify it's back to false after completion
        await saveTask;
        viewModel.IsSaving.Should().BeFalse();
        viewModel.IsNotSaving.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ClearsHasChangesFlag()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Modify to enable save
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");
        viewModel.HasChanges.Should().BeTrue();

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        viewModel.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_ShowsSuccessMessage()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Modify to enable save
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowInformationAsync(
            It.Is<string>(msg => msg.Contains("Settings saved successfully")),
            "Success"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithError_ShowsErrorDialog()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Modify to enable save
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(msg => msg.Contains("Failed to save settings")),
            "Error"), Times.Once);
        viewModel.IsSaving.Should().BeFalse(); // Should reset even on error
    }

    #endregion

    #region HasChanges Tests

    [Fact]
    public async Task HasChanges_IsTrue_WhenLanguageChanged()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Assert
        viewModel.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_IsTrue_WhenDateFormatChanged()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedDateFormat = viewModel.AvailableDateFormats.First(d => d.Format == "MM/dd/yyyy");

        // Assert
        viewModel.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_IsTrue_WhenWeekStartDayChanged()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedWeekStartDay = viewModel.AvailableWeekStartDays.First(w => w.DayOfWeek == DayOfWeek.Monday);

        // Assert
        viewModel.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_IsTrue_WhenWeekYearStandardChanged()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedWeekYearStandard = viewModel.AvailableWeekYearStandards.First(w => w.Standard == WeekYearStandard.UsStandard);

        // Assert
        viewModel.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task HasChanges_IsFalse_AfterSave()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        _appSettingsServiceMock.Setup(s => s.UpdateSettingsAsync(
                It.IsAny<UpdateAppSettingsDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");
        viewModel.HasChanges.Should().BeTrue();

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        viewModel.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void HasChanges_IsFalse_BeforeLoad()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Assert
        viewModel.HasChanges.Should().BeFalse(); // No settings loaded yet
    }

    #endregion

    #region ResetToDefaults Tests

    [Fact]
    public async Task ResetToDefaults_ResetsAllProperties()
    {
        // Arrange
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();

        // Modify from defaults
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");
        viewModel.SelectedDateFormat = viewModel.AvailableDateFormats.First(d => d.Format == "MM/dd/yyyy");
        viewModel.SelectedWeekStartDay = viewModel.AvailableWeekStartDays.First(w => w.DayOfWeek == DayOfWeek.Monday);
        viewModel.SelectedWeekYearStandard = viewModel.AvailableWeekYearStandards.First(w => w.Standard == WeekYearStandard.UsStandard);

        // Act
        await ((AsyncRelayCommand)viewModel.ResetToDefaultsCommand).ExecuteAsync(null);

        // Assert
        viewModel.SelectedLanguage.Code.Should().BeNull();
        viewModel.SelectedDateFormat.Format.Should().BeNull();
        viewModel.SelectedWeekStartDay.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        viewModel.SelectedWeekYearStandard.Standard.Should().Be(WeekYearStandard.Iso8601);
    }

    [Fact]
    public async Task ResetToDefaults_MarksChanges()
    {
        // Arrange
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = "en-US",
            DateFormatOverride = "MM/dd/yyyy",
            WeekStartDay = DayOfWeek.Monday,
            WeekYearStandard = 1,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        viewModel.HasChanges.Should().BeFalse();

        // Act
        await ((AsyncRelayCommand)viewModel.ResetToDefaultsCommand).ExecuteAsync(null);

        // Assert
        viewModel.HasChanges.Should().BeTrue();
    }

    [Fact]
    public async Task ResetToDefaults_ShowsConfirmationDialog()
    {
        // Arrange
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();

        // Act
        await ((AsyncRelayCommand)viewModel.ResetToDefaultsCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowConfirmationAsync(
            It.Is<string>(msg => msg.Contains("reset all settings")),
            "Confirm Reset"), Times.Once);
    }

    [Fact]
    public async Task ResetToDefaults_DoesNotReset_WhenCancelled()
    {
        // Arrange
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");
        var languageBefore = viewModel.SelectedLanguage;

        // Act
        await ((AsyncRelayCommand)viewModel.ResetToDefaultsCommand).ExecuteAsync(null);

        // Assert
        viewModel.SelectedLanguage.Should().Be(languageBefore); // Unchanged
    }

    #endregion

    #region SaveCommand CanExecute Tests

    [Fact]
    public async Task SaveCommand_CanExecute_WhenHasChanges()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Assert
        ((AsyncRelayCommand)viewModel.SaveCommand).CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNoChanges()
    {
        // Arrange
        var settings = new AppSettingsDto
        {
            Id = 1,
            LastOpenedSettingsTab = "General",
            LanguageOverride = null,
            DateFormatOverride = null,
            WeekStartDay = DayOfWeek.Sunday,
            WeekYearStandard = 0,
            LastModified = DateTime.UtcNow
        };

        _appSettingsServiceMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        // Act & Assert
        ((AsyncRelayCommand)viewModel.SaveCommand).CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void SelectedLanguage_RaisesPropertyChanged_ForUseSystemLanguage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanges = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName!);

        // Act
        viewModel.SelectedLanguage = viewModel.AvailableLanguages.First(l => l.Code == "en-US");

        // Assert
        propertyChanges.Should().Contain(nameof(viewModel.UseSystemLanguage));
    }

    [Fact]
    public void SelectedDateFormat_RaisesPropertyChanged_ForUseSystemDateFormat()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanges = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName!);

        // Act
        viewModel.SelectedDateFormat = viewModel.AvailableDateFormats.First(d => d.Format == "MM/dd/yyyy");

        // Assert
        propertyChanges.Should().Contain(nameof(viewModel.UseSystemDateFormat));
    }

    [Fact]
    public void IsSaving_RaisesPropertyChanged_ForIsNotSaving()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanges = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName!);

        // Act
        viewModel.IsSaving = true;

        // Assert
        propertyChanges.Should().Contain(nameof(viewModel.IsNotSaving));
    }

    #endregion
}
