using Microsoft.UI.Xaml.Controls;
using PomodoroTimeTracker.ViewModels;

namespace PomodoroTimeTracker.WinUI3.Views;

internal sealed partial class ReportPage : Page
{
    public ReportViewModel ViewModel { get; }

    public ReportPage()
    {
        ViewModel = App.Services.GetService(typeof(ReportViewModel)) as ReportViewModel
                    ?? throw new InvalidOperationException("ReportViewModel not registered");

        this.InitializeComponent();
    }
}
