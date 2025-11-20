namespace PomodoroTimeTracker.Domain.Entities;

/// <summary>
/// Represents a Pomodoro work session or break period.
/// Tracks time spent on focused work or rest periods with project association.
/// </summary>
public class PomodoroSession
{
    /// <summary>
    /// Gets or sets the unique identifier for this session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated project ID. Null for personal/untracked sessions.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Gets or sets when the session started (UTC).
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets when the session ended (UTC). Null if session is still active.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the planned duration of the session in minutes.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether the session was completed successfully.
    /// False indicates the session was stopped early or abandoned.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the type of session (Work, ShortBreak, LongBreak).
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Work;

    /// <summary>
    /// Gets or sets the objective or goal for this session.
    /// Describes what the user plans to accomplish during the work period.
    /// </summary>
    public string? Objective { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the session.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated project.
    /// </summary>
    public Project? Project { get; set; }
}
