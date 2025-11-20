using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.WinUI3.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

public sealed partial class ClientListPage : Page
{
    public ClientListViewModel ViewModel { get; }

    public ClientListPage()
    {
        // Get ViewModel from Dependency Injection
        ViewModel = App.Services.GetService(typeof(ClientListViewModel)) as ClientListViewModel
                    ?? throw new InvalidOperationException("ClientListViewModel not registered");

        this.InitializeComponent();
    }
}
