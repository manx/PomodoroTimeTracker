using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

public sealed partial class ProjectListPage : Page
{
    public ProjectListViewModel ViewModel { get; }

    public ProjectListPage()
    {
        ViewModel = App.Services.GetService(typeof(ProjectListViewModel)) as ProjectListViewModel
                    ?? throw new InvalidOperationException("ProjectListViewModel not registered");

        this.InitializeComponent();
    }
}
