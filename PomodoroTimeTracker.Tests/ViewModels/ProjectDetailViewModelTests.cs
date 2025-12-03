using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for ProjectDetailViewModel.
/// Tests initialization, validation, client selection, and save/cancel operations.
/// </summary>
public class ProjectDetailViewModelTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IClientService> _clientServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;

    public ProjectDetailViewModelTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _clientServiceMock = new Mock<IClientService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _navigationServiceMock = new Mock<INavigationService>();

        // Default setup
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(new List<ClientDto>());
    }

    private ProjectDetailViewModel CreateViewModel()
    {
        return new ProjectDetailViewModel(
            _projectServiceMock.Object,
            _clientServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);
    }

    #region Initialization Tests

    [Fact]
    public async Task InitializeForAddAsync_SetsAddMode()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForAddAsync();

        // Assert
        viewModel.IsEditMode.Should().BeFalse();
        viewModel.PageTitle.Should().Be("Add New Project");
        viewModel.Name.Should().BeEmpty();
        viewModel.Description.Should().BeEmpty();
        viewModel.SelectedClient.Should().BeNull();
    }

    [Fact]
    public async Task InitializeForAddAsync_LoadsClients()
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
        await viewModel.InitializeForAddAsync();

        // Assert
        viewModel.Clients.Should().HaveCount(2);
    }

    [Fact]
    public async Task InitializeForEditAsync_LoadsProjectData()
    {
        // Arrange
        var clients = new List<ClientDto> { new() { Id = 1, Name = "Client" } };
        _clientServiceMock.Setup(s => s.GetAllClientsAsync())
            .ReturnsAsync(clients);

        var project = new ProjectDto
        {
            Id = 1,
            Name = "Test Project",
            Description = "Test Description",
            ClientId = 1
        };
        _projectServiceMock.Setup(s => s.GetProjectByIdAsync(1))
            .ReturnsAsync(project);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForEditAsync(1);

        // Assert
        viewModel.IsEditMode.Should().BeTrue();
        viewModel.PageTitle.Should().Be("Edit Project");
        viewModel.Name.Should().Be("Test Project");
        viewModel.Description.Should().Be("Test Description");
        viewModel.SelectedClient.Should().NotBeNull();
        viewModel.SelectedClient!.Id.Should().Be(1);
    }

    [Fact]
    public async Task InitializeForEditAsync_WhenServiceThrows_ShowsErrorAndNavigatesBack()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetProjectByIdAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeForEditAsync(1);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to load project")),
            It.IsAny<string>()), Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    #endregion

    #region Save Command CanExecute Tests

    [Fact]
    public async Task SaveCommand_WhenNameEmpty_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await viewModel.InitializeForAddAsync();
        viewModel.Name = "";

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_WhenNameProvided_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        await viewModel.InitializeForAddAsync();
        viewModel.Name = "Valid Name";

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    #endregion

    #region Save Operation Tests

    [Fact]
    public async Task SaveCommand_InAddMode_CreatesNewProject()
    {
        // Arrange
        var createdProject = new ProjectDto { Id = 1, Name = "New Project" };
        _projectServiceMock.Setup(s => s.CreateProjectAsync(It.IsAny<CreateProjectDto>()))
            .ReturnsAsync(createdProject);

        var viewModel = CreateViewModel();
        await viewModel.InitializeForAddAsync();
        viewModel.Name = "New Project";
        viewModel.Description = "Description";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _projectServiceMock.Verify(s => s.CreateProjectAsync(
            It.Is<CreateProjectDto>(dto =>
                dto.Name == "New Project" &&
                dto.Description == "Description")), Times.Once);
        _navigationServiceMock.VerifySet(n => n.ProjectIdToSelect = 1, Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_InEditMode_UpdatesExistingProject()
    {
        // Arrange
        var existingProject = new ProjectDto { Id = 5, Name = "Old Name", Description = "Old Desc" };
        _projectServiceMock.Setup(s => s.GetProjectByIdAsync(5))
            .ReturnsAsync(existingProject);

        var viewModel = CreateViewModel();
        await viewModel.InitializeForEditAsync(5);
        viewModel.Name = "Updated Name";
        viewModel.Description = "Updated Desc";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _projectServiceMock.Verify(s => s.UpdateProjectAsync(
            It.Is<UpdateProjectDto>(dto =>
                dto.Id == 5 &&
                dto.Name == "Updated Name" &&
                dto.Description == "Updated Desc")), Times.Once);
        _navigationServiceMock.VerifySet(n => n.ProjectIdToSelect = 5, Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.CreateProjectAsync(It.IsAny<CreateProjectDto>()))
            .ThrowsAsync(new Exception("Save failed"));

        var viewModel = CreateViewModel();
        await viewModel.InitializeForAddAsync();
        viewModel.Name = "Test";

        // Act
        await ((AsyncRelayCommand)viewModel.SaveCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to save project")),
            It.IsAny<string>()), Times.Once);
        _navigationServiceMock.Verify(n => n.GoBack(), Times.Never);
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
}
