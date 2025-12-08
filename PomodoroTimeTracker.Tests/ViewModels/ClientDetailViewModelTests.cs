using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for ClientDetailViewModel.
/// Tests initialization, validation, and save/cancel operations.
/// </summary>
public class ClientDetailViewModelTests
{
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IWorkScheduleService> _workScheduleServiceMock;
    private readonly Mock<IPublicHolidayService> _publicHolidayServiceMock;

    public ClientDetailViewModelTests()
    {
        _clientServiceMock = new Mock<IClientService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _workScheduleServiceMock = new Mock<IWorkScheduleService>();
        _publicHolidayServiceMock = new Mock<IPublicHolidayService>();
    }

    private ClientDetailViewModel CreateViewModel()
    {
        return new ClientDetailViewModel(
            _clientServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object,
            _workScheduleServiceMock.Object,
            _publicHolidayServiceMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public void InitializeForAdd_SetsAddMode()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.InitializeForAdd();

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
        viewModel.PageTitle.Should().Be("Add New Client");
        viewModel.Name.Should().BeEmpty();
        viewModel.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeForEditAsync_LoadsClientData()
    {
        // Arrange
        var client = new ClientDto
        {
            Id = 1,
            Name = "Test Client",
            Description = "Test Description"
        };
        _clientServiceMock.Setup(s => s.GetClientByIdAsync(1))
            .ReturnsAsync(client);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForEditAsync(1);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
        viewModel.PageTitle.Should().Be("Edit Client");
        viewModel.Name.Should().Be("Test Client");
        viewModel.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task InitializeForEditAsync_WhenClientNotFound_HandlesGracefully()
    {
        // Arrange
        _clientServiceMock.Setup(s => s.GetClientByIdAsync(999))
            .ReturnsAsync((ClientDto?)null);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForEditAsync(999);

        // Assert
        viewModel.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeForEditAsync_WhenServiceThrows_ShowsErrorAndNavigatesBack()
    {
        // Arrange
        _clientServiceMock.Setup(s => s.GetClientByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForEditAsync(1);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to load client")),
            It.IsAny<string>()), Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    #endregion

    #region Save Command CanExecute Tests

    [Fact]
    public void SaveCommand_WhenNameEmpty_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "";

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_WhenNameWhitespace_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "   ";

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void SaveCommand_WhenNameProvided_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "Valid Name";

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void Name_WhenChanged_NotifiesSaveCommand()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        var canExecuteChanged = false;
        viewModel.SaveCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

        // Act
        viewModel.Name = "New Name";

        // Assert
        canExecuteChanged.Should().BeTrue();
    }

    #endregion

    #region Save Operation Tests

    [Fact]
    public async Task SaveCommand_InAddMode_CreatesNewClient()
    {
        // Arrange
        var createdClient = new ClientDto { Id = 1, Name = "New Client" };
        _clientServiceMock.Setup(s => s.CreateClientAsync(It.IsAny<CreateClientDto>()))
            .ReturnsAsync(createdClient);

        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "New Client";
        viewModel.Description = "Description";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _clientServiceMock.Verify(s => s.CreateClientAsync(
            It.Is<CreateClientDto>(dto =>
                dto.Name == "New Client" &&
                dto.Description == "Description")), Times.Once);
        _navigationServiceMock.VerifySet(n => n.ClientIdToSelect = 1, Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_InEditMode_UpdatesExistingClient()
    {
        // Arrange
        var existingClient = new ClientDto { Id = 5, Name = "Old Name", Description = "Old Desc" };
        _clientServiceMock.Setup(s => s.GetClientByIdAsync(5))
            .ReturnsAsync(existingClient);

        var viewModel = CreateViewModel();
        await viewModel.InitializeForEditAsync(5);
        viewModel.Name = "Updated Name";
        viewModel.Description = "Updated Desc";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _clientServiceMock.Verify(s => s.UpdateClientAsync(
            It.Is<UpdateClientDto>(dto =>
                dto.Id == 5 &&
                dto.Name == "Updated Name" &&
                dto.Description == "Updated Desc")), Times.Once);
        _navigationServiceMock.VerifySet(n => n.ClientIdToSelect = 5, Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_WithEmptyDescription_SavesNullDescription()
    {
        // Arrange
        var createdClient = new ClientDto { Id = 1, Name = "Client" };
        _clientServiceMock.Setup(s => s.CreateClientAsync(It.IsAny<CreateClientDto>()))
            .ReturnsAsync(createdClient);

        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "Client";
        viewModel.Description = "   "; // Whitespace only

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _clientServiceMock.Verify(s => s.CreateClientAsync(
            It.Is<CreateClientDto>(dto => dto.Description == null)), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _clientServiceMock.Setup(s => s.CreateClientAsync(It.IsAny<CreateClientDto>()))
            .ThrowsAsync(new Exception("Save failed"));

        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "Test";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to save client")),
            It.IsAny<string>()), Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_SetsIsSavingDuringOperation()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ClientDto>();
        _clientServiceMock.Setup(s => s.CreateClientAsync(It.IsAny<CreateClientDto>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();
        viewModel.InitializeForAdd();
        viewModel.Name = "Test";

        // Act
        var saveTask = ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert - during save
        viewModel.IsSaving.Should().BeTrue();

        // Complete the save
        tcs.SetResult(new ClientDto { Id = 1, Name = "Test" });
        await saveTask;

        // Assert - after save
        viewModel.IsSaving.Should().BeFalse();
    }

    #endregion

    #region Cancel Operation Tests

    [Fact]
    public void CancelCommand_NavigatesBack()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Description_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Description))
                propertyChanged = true;
        };

        // Act
        viewModel.Description = "New Description";

        // Assert
        propertyChanged.Should().BeTrue();
        viewModel.Description.Should().Be("New Description");
    }

    #endregion
}
