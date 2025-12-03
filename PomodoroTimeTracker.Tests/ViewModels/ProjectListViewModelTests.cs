using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using PomodoroTimeTracker.Application.DTOs;
using PomodoroTimeTracker.Application.Interfaces;
using PomodoroTimeTracker.ViewModels;
using PomodoroTimeTracker.ViewModels.Services;

namespace PomodoroTimeTracker.Tests.ViewModels;

/// <summary>
/// Unit tests for ProjectListViewModel.
/// Tests loading, navigation, and delete operations.
/// </summary>
public class ProjectListViewModelTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;

    public ProjectListViewModelTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _navigationServiceMock = new Mock<INavigationService>();

        // Default setup for empty project list
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(new List<ProjectDto>());
    }

    private ProjectListViewModel CreateViewModel()
    {
        return new ProjectListViewModel(
            _projectServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesEmptyProjectsCollection()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Projects.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LoadProjectsCommand.Should().NotBeNull();
        viewModel.AddProjectCommand.Should().NotBeNull();
        viewModel.EditProjectCommand.Should().NotBeNull();
        viewModel.DeleteProjectCommand.Should().NotBeNull();
        viewModel.RefreshCommand.Should().NotBeNull();
    }

    #endregion

    #region Load Projects Tests

    [Fact]
    public async Task LoadProjectsCommand_WithProjects_PopulatesCollection()
    {
        // Arrange
        var projects = new List<ProjectDto>
        {
            new() { Id = 1, Name = "Project A" },
            new() { Id = 2, Name = "Project B" }
        };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);

        var viewModel = CreateViewModel();

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        viewModel.Projects.Should().HaveCount(2);
        viewModel.Projects.Should().Contain(p => p.Name == "Project A");
        viewModel.Projects.Should().Contain(p => p.Name == "Project B");
    }

    [Fact]
    public async Task LoadProjectsCommand_WhenProjectIdToSelectSet_SelectsProject()
    {
        // Arrange
        var projects = new List<ProjectDto>
        {
            new() { Id = 1, Name = "Project A" },
            new() { Id = 2, Name = "Project B" }
        };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        _navigationServiceMock.SetupProperty(n => n.ProjectIdToSelect, 2);

        var viewModel = CreateViewModel();

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        viewModel.SelectedProject.Should().NotBeNull();
        viewModel.SelectedProject!.Id.Should().Be(2);
        _navigationServiceMock.Object.ProjectIdToSelect.Should().BeNull();
    }

    [Fact]
    public async Task LoadProjectsCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ThrowsAsync(new Exception("Database error"));

        var viewModel = CreateViewModel();

        // Act - wait for initial load
        await Task.Delay(50);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to load projects")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void EditProjectCommand_WhenNoSelection_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedProject = null;

        // Act
        var canExecute = viewModel.EditProjectCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void EditProjectCommand_WhenProjectSelected_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedProject = new ProjectDto { Id = 1, Name = "Test Project" };

        // Act
        var canExecute = viewModel.EditProjectCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void DeleteProjectCommand_WhenNoSelection_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedProject = null;

        // Act
        var canExecute = viewModel.DeleteProjectCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void DeleteProjectCommand_WhenProjectSelected_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedProject = new ProjectDto { Id = 1, Name = "Test Project" };

        // Act
        var canExecute = viewModel.DeleteProjectCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void AddProjectCommand_NavigatesToProjectDetailWithNullId()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddProjectCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo(PageNames.ProjectDetail, null), Times.Once);
    }

    [Fact]
    public void EditProjectCommand_NavigatesToProjectDetailWithId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedProject = new ProjectDto { Id = 42, Name = "Test Project" };

        // Act
        viewModel.EditProjectCommand.Execute(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateTo(PageNames.ProjectDetail, 42), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteProjectCommand_WhenConfirmed_DeletesProject()
    {
        // Arrange
        var projects = new List<ProjectDto> { new() { Id = 1, Name = "Project to Delete" } };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedProject = projects[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteProjectCommand).ExecuteAsync(null);

        // Assert
        _projectServiceMock.Verify(s => s.DeleteProjectAsync(1), Times.Once);
        _dialogServiceMock.Verify(d => d.ShowInformationAsync(
            It.Is<string>(s => s.Contains("deleted successfully")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectCommand_WhenCancelled_DoesNotDelete()
    {
        // Arrange
        var projects = new List<ProjectDto> { new() { Id = 1, Name = "Project" } };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedProject = projects[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteProjectCommand).ExecuteAsync(null);

        // Assert
        _projectServiceMock.Verify(s => s.DeleteProjectAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProjectCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        var projects = new List<ProjectDto> { new() { Id = 1, Name = "Project" } };
        _projectServiceMock.Setup(s => s.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        _dialogServiceMock.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _projectServiceMock.Setup(s => s.DeleteProjectAsync(1))
            .ThrowsAsync(new Exception("Delete failed"));

        var viewModel = CreateViewModel();
        await Task.Delay(50);
        viewModel.SelectedProject = projects[0];

        // Act
        await ((AsyncRelayCommand)viewModel.DeleteProjectCommand).ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(d => d.ShowErrorAsync(
            It.Is<string>(s => s.Contains("Failed to delete project")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void SelectedProject_WhenChanged_NotifiesCommands()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var editCanExecuteChanged = false;
        var deleteCanExecuteChanged = false;

        viewModel.EditProjectCommand.CanExecuteChanged += (s, e) => editCanExecuteChanged = true;
        viewModel.DeleteProjectCommand.CanExecuteChanged += (s, e) => deleteCanExecuteChanged = true;

        // Act
        viewModel.SelectedProject = new ProjectDto { Id = 1, Name = "Test" };

        // Assert
        editCanExecuteChanged.Should().BeTrue();
        deleteCanExecuteChanged.Should().BeTrue();
    }

    #endregion
}
