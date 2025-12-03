using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for ClientListViewModel.
/// Tests loading, navigation, and delete operations.
/// </summary>
public class ClientListViewModelTests
{
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<ILogger<ClientListViewModel>> _loggerMock;

    public ClientListViewModelTests()
    {
        _clientServiceMock = new Mock<IClientService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _loggerMock = new Mock<ILogger<ClientListViewModel>>();

        // Default setup for empty client list
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
    }

    private ClientListViewModel CreateViewModel()
    {
        return new ClientListViewModel(
            _clientServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object,
            _loggerMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesEmptyClientsCollection()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert - must wait a bit for async loading
        viewModel.Clients.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LoadClientsCommand.Should().NotBeNull();
        viewModel.AddClientCommand.Should().NotBeNull();
        viewModel.EditClientCommand.Should().NotBeNull();
        viewModel.DeleteClientCommand.Should().NotBeNull();
        viewModel.RefreshCommand.Should().NotBeNull();
    }

    #endregion

    #region Load Clients Tests

    [Fact]
    public async Task LoadClientsCommand_WithClients_PopulatesCollection()
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

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        viewModel.Clients.Should().HaveCount(2);
        viewModel.Clients.Should().Contain(c => c.Name == "Client A");
        viewModel.Clients.Should().Contain(c => c.Name == "Client B");
    }

    [Fact]
    public async Task LoadClientsCommand_WhenClientIdToSelectSet_SelectsClient()
    {
        // Arrange
        var clients = new List<ClientDto>
        {
            new() { Id = 1, Name = "Client A" },
            new() { Id = 2, Name = "Client B" }
        };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);
        _navigationServiceMock.SetupProperty(n => n.ClientIdToSelect, 2);

        var viewModel = CreateViewModel();

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        viewModel.SelectedClient.Should().NotBeNull();
        viewModel.SelectedClient!.Id.Should().Be(2);
        _navigationServiceMock.Object.ClientIdToSelect.Should().BeNull();
    }

    [Fact]
    public async Task LoadClientsCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Unable to load clients")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void EditClientCommand_WhenNoSelection_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = null;

        // Act
        var canExecute = viewModel.EditClientCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void EditClientCommand_WhenClientSelected_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = new ClientDto { Id = 1, Name = "Test Client" };

        // Act
        var canExecute = viewModel.EditClientCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void DeleteClientCommand_WhenNoSelection_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = null;

        // Act
        var canExecute = viewModel.DeleteClientCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void DeleteClientCommand_WhenClientSelected_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = new ClientDto { Id = 1, Name = "Test Client" };

        // Act
        var canExecute = viewModel.DeleteClientCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void AddClientCommand_NavigatesToClientDetailWithNullId()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddClientCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo(PageNames.ClientDetail, null), Times.Once);
    }

    [Fact]
    public void EditClientCommand_NavigatesToClientDetailWithId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedClient = new ClientDto { Id = 42, Name = "Test Client" };

        // Act
        viewModel.EditClientCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo(PageNames.ClientDetail, 42), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteClientCommand_WhenConfirmed_DeletesClient()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client to Delete" } };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedClient = clients[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteClientCommand).ExecuteAsync(null);

        // Assert
        _clientServiceMock.Verify(s => s.DeleteClientAsync(1), Times.Once);
        _dialogServiceMock.Verify(d => d.ShowInformationAsync(
            It.Is<string>(s => s.Contains("deleted successfully")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteClientCommand_WhenCancelled_DoesNotDelete()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client" } };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedClient = clients[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteClientCommand).ExecuteAsync(null);

        // Assert
        _clientServiceMock.Verify(s => s.DeleteClientAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteClientCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client" } };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _clientServiceMock.Setup(s => s.DeleteClientAsync(1))
            .ThrowsAsync(new Exception("Delete failed"));

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedClient = clients[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteClientCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Unable to delete")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void SelectedClient_WhenChanged_NotifiesCommands()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var editCanExecuteChanged = false;
        var deleteCanExecuteChanged = false;

        viewModel.EditClientCommand.CanExecuteChanged += (s, e) => editCanExecuteChanged = true;
        viewModel.DeleteClientCommand.CanExecuteChanged += (s, e) => deleteCanExecuteChanged = true;

        // Act
        viewModel.SelectedClient = new ClientDto { Id = 1, Name = "Test" };

        // Assert
        editCanExecuteChanged.Should().BeTrue();
        deleteCanExecuteChanged.Should().BeTrue();
    }

    #endregion
}
