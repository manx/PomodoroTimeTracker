namespace PomodoroTimeTracker.Application.DTOs;

public class GoalComparisonDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal DifferenceHours => ActualHours - ExpectedHours;
    public decimal ProgressPercentage => ExpectedHours > 0
        ? Math.Round(ActualHours / ExpectedHours * 100, 1)
        : ActualHours > 0 ? 100 : 0;
    public bool IsAhead => DifferenceHours >= 0;
}
