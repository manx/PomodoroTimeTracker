using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PomodoroTimeTracker.WinUI3.Views;

/// <summary>
/// A card control that displays a goal with progress, hours, and difference.
/// </summary>
public sealed partial class GoalCard : UserControl
{
    public GoalCard()
    {
        this.InitializeComponent();
    }

    #region Dependency Properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(GoalCard),
            new PropertyMetadata("Goal", OnTitleChanged));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(GoalCard),
            new PropertyMetadata(0.0, OnProgressChanged));

    public static readonly DependencyProperty ActualHoursProperty =
        DependencyProperty.Register(nameof(ActualHours), typeof(decimal), typeof(GoalCard),
            new PropertyMetadata(0m, OnHoursChanged));

    public static readonly DependencyProperty ExpectedHoursProperty =
        DependencyProperty.Register(nameof(ExpectedHours), typeof(decimal), typeof(GoalCard),
            new PropertyMetadata(0m, OnHoursChanged));

    public static readonly DependencyProperty DifferenceHoursProperty =
        DependencyProperty.Register(nameof(DifferenceHours), typeof(decimal), typeof(GoalCard),
            new PropertyMetadata(0m, OnDifferenceChanged));

    public static readonly DependencyProperty IsAheadProperty =
        DependencyProperty.Register(nameof(IsAhead), typeof(bool), typeof(GoalCard),
            new PropertyMetadata(true, OnDifferenceChanged));

    #endregion

    #region Properties

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public decimal ActualHours
    {
        get => (decimal)GetValue(ActualHoursProperty);
        set => SetValue(ActualHoursProperty, value);
    }

    public decimal ExpectedHours
    {
        get => (decimal)GetValue(ExpectedHoursProperty);
        set => SetValue(ExpectedHoursProperty, value);
    }

    public decimal DifferenceHours
    {
        get => (decimal)GetValue(DifferenceHoursProperty);
        set => SetValue(DifferenceHoursProperty, value);
    }

    public bool IsAhead
    {
        get => (bool)GetValue(IsAheadProperty);
        set => SetValue(IsAheadProperty, value);
    }

    #endregion

    #region Property Changed Handlers

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoalCard card)
        {
            card.TitleText.Text = e.NewValue?.ToString() ?? "Goal";
        }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoalCard card)
        {
            var progress = (double)e.NewValue;
            // Clamp to 0-100 range for display, but allow >100 for percentage text
            var displayProgress = Math.Min(Math.Max(progress, 0), 100);
            card.ProgressRing.Value = displayProgress;
            card.PercentageText.Text = $"{progress:F0}%";
        }
    }

    private static void OnHoursChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoalCard card)
        {
            card.ActualHoursText.Text = $"{card.ActualHours:F1}";
            card.ExpectedHoursText.Text = $"{card.ExpectedHours:F1}";
        }
    }

    private static void OnDifferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GoalCard card)
        {
            var diff = card.DifferenceHours;
            var isAhead = card.IsAhead;

            // Format the difference text
            var sign = diff >= 0 ? "+" : "";
            card.DifferenceText.Text = $"{sign}{diff:F1}h";

            // Set colors based on ahead/behind status
            if (isAhead)
            {
                card.DifferenceBorder.Background = new SolidColorBrush(Colors.DarkGreen);
                card.DifferenceText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                card.DifferenceBorder.Background = new SolidColorBrush(Colors.DarkRed);
                card.DifferenceText.Foreground = new SolidColorBrush(Colors.LightCoral);
            }
        }
    }

    #endregion
}
