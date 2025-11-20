# API Documentation - Application Services

This document describes the public interfaces and services available in the Application layer.

## Table of Contents
- [Pomodoro Services](#pomodoro-services)
- [Client & Project Services](#client--project-services)
- [Time Entry Services](#time-entry-services)
- [Statistics Services](#statistics-services)

---

## Pomodoro Services

### IPomodoroSessionService

Manages Pomodoro work sessions and break periods.

#### Methods

**GetAllSessionsAsync()**
```csharp
Task<IEnumerable<PomodoroSessionDto>> GetAllSessionsAsync()
```
Returns all Pomodoro sessions with project and client information.

**GetSessionByIdAsync(int id)**
```csharp
Task<PomodoroSessionDto?> GetSessionByIdAsync(int id)
```
Returns a specific session by ID, or null if not found.

**GetSessionsByProjectIdAsync(int projectId)**
```csharp
Task<IEnumerable<PomodoroSessionDto>> GetSessionsByProjectIdAsync(int projectId)
```
Returns all sessions for a specific project.

**GetActiveSessionAsync()**
```csharp
Task<PomodoroSessionDto?> GetActiveSessionAsync()
```
Returns the currently active (incomplete) session, or null if none.

**CreateSessionAsync(CreatePomodoroSessionDto dto)**
```csharp
Task<PomodoroSessionDto> CreateSessionAsync(CreatePomodoroSessionDto dto)
```
Creates a new Pomodoro session.

**Parameters:**
- `ProjectId` (int?, optional) - Associated project
- `DurationMinutes` (int, required) - Session length
- `SessionType` (SessionType, required) - Work, ShortBreak, or LongBreak
- `Objective` (string?, optional) - Session goal/objective
- `Notes` (string?, optional) - Additional notes

**UpdateSessionAsync(UpdatePomodoroSessionDto dto)**
```csharp
Task UpdateSessionAsync(UpdatePomodoroSessionDto dto)
```
Updates an existing session.

**CompleteSessionAsync(int id)**
```csharp
Task CompleteSessionAsync(int id)
```
Marks a session as completed with current timestamp.

**DeleteSessionAsync(int id)**
```csharp
Task DeleteSessionAsync(int id)
```
Deletes a session permanently.

---

### IPomodoroSettingsService

Manages user preferences and timer configuration.

#### Methods

**GetSettingsAsync()**
```csharp
Task<PomodoroSettingsDto> GetSettingsAsync()
```
Returns current settings (creates default if none exist).

**UpdateSettingsAsync(UpdatePomodoroSettingsDto dto)**
```csharp
Task<PomodoroSettingsDto> UpdateSettingsAsync(UpdatePomodoroSettingsDto dto)
```
Updates settings and returns the updated values.

**Settings Properties:**
- `WorkDurationMinutes` (int) - Default: 25
- `ShortBreakDurationMinutes` (int) - Default: 5
- `LongBreakDurationMinutes` (int) - Default: 15
- `LongBreakInterval` (int) - Default: 4 (every 4 pomodoros)
- `ShowNotification` (bool) - Default: true
- `PlaySound` (bool) - Default: true
- `FlashWindow` (bool) - Default: false
- `SoftStopDurationMinutes` (int) - Default: 3 (minutes before end)
- `SoftStopAlarmVolume` (int) - Default: 50 (0-100)
- `UseAlarm` (bool) - Default: true
- `AlarmVolume` (int) - Default: 50 (0-100)

**CalculateDefaultShortBreak(int workDurationMinutes)**
```csharp
Task<int> CalculateDefaultShortBreak(int workDurationMinutes)
```
Calculates recommended short break: `workDuration / 5`

**CalculateDefaultLongBreak(int workDurationMinutes)**
```csharp
Task<int> CalculateDefaultLongBreak(int workDurationMinutes)
```
Calculates recommended long break: `(workDuration / 5) * 3`

---

## Client & Project Services

### IClientService

Manages client information.

#### Methods

**GetAllClientsAsync()**
```csharp
Task<IEnumerable<ClientDto>> GetAllClientsAsync()
```
Returns all clients.

**GetClientByIdAsync(int id)**
```csharp
Task<ClientDto?> GetClientByIdAsync(int id)
```
Returns specific client by ID.

**CreateClientAsync(CreateClientDto dto)**
```csharp
Task<ClientDto> CreateClientAsync(CreateClientDto dto)
```
Creates a new client.

**Parameters:**
- `Name` (string, required)
- `Email` (string?, optional)
- `Phone` (string?, optional)
- `Description` (string?, optional)

**UpdateClientAsync(UpdateClientDto dto)**
```csharp
Task UpdateClientAsync(UpdateClientDto dto)
```
Updates existing client.

**DeleteClientAsync(int id)**
```csharp
Task DeleteClientAsync(int id)
```
Deletes client (if no associated projects).

**ActivateClientAsync(int id) / DeactivateClientAsync(int id)**
```csharp
Task ActivateClientAsync(int id)
Task DeactivateClientAsync(int id)
```
Toggles client active status.

---

### IProjectService

Manages projects linked to clients.

#### Methods

**GetAllProjectsAsync()**
```csharp
Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
```
Returns all projects with client information.

**GetProjectByIdAsync(int id)**
```csharp
Task<ProjectDto?> GetProjectByIdAsync(int id)
```
Returns specific project by ID.

**GetProjectsByClientIdAsync(int clientId)**
```csharp
Task<IEnumerable<ProjectDto>> GetProjectsByClientIdAsync(int clientId)
```
Returns all projects for a specific client.

**CreateProjectAsync(CreateProjectDto dto)**
```csharp
Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
```
Creates a new project.

**Parameters:**
- `ClientId` (int, required) - Parent client
- `Name` (string, required)
- `Description` (string?, optional)
- `HourlyRate` (decimal?, optional) - Billing rate

**UpdateProjectAsync(UpdateProjectDto dto)**
```csharp
Task UpdateProjectAsync(UpdateProjectDto dto)
```
Updates existing project.

**DeleteProjectAsync(int id)**
```csharp
Task DeleteProjectAsync(int id)
```
Deletes project (if no associated time entries or sessions).

**ActivateProjectAsync(int id) / DeactivateProjectAsync(int id)**
```csharp
Task ActivateProjectAsync(int id)
Task DeactivateProjectAsync(int id)
```
Toggles project active status.

---

## Time Entry Services

### ITimeEntryService

Manages manual time tracking entries.

#### Methods

**GetAllTimeEntriesAsync()**
```csharp
Task<IEnumerable<TimeEntryDto>> GetAllTimeEntriesAsync()
```
Returns all time entries with project information.

**GetTimeEntryByIdAsync(int id)**
```csharp
Task<TimeEntryDto?> GetTimeEntryByIdAsync(int id)
```
Returns specific entry by ID.

**GetTimeEntriesByProjectIdAsync(int projectId)**
```csharp
Task<IEnumerable<TimeEntryDto>> GetTimeEntriesByProjectIdAsync(int projectId)
```
Returns all entries for a specific project.

**GetTimeEntriesByDateRangeAsync(DateTime start, DateTime end)**
```csharp
Task<IEnumerable<TimeEntryDto>> GetTimeEntriesByDateRangeAsync(DateTime start, DateTime end)
```
Returns entries within date range.

**CreateTimeEntryAsync(CreateTimeEntryDto dto)**
```csharp
Task<TimeEntryDto> CreateTimeEntryAsync(CreateTimeEntryDto dto)
```
Creates a new time entry.

**Parameters:**
- `ProjectId` (int, required)
- `StartTime` (DateTime, required)
- `EndTime` (DateTime?, optional)
- `DurationMinutes` (int?, optional) - Calculated if null
- `Description` (string?, optional)
- `IsBillable` (bool) - Default: false

**UpdateTimeEntryAsync(UpdateTimeEntryDto dto)**
```csharp
Task UpdateTimeEntryAsync(UpdateTimeEntryDto dto)
```
Updates existing entry.

**DeleteTimeEntryAsync(int id)**
```csharp
Task DeleteTimeEntryAsync(int id)
```
Deletes time entry.

---

## Statistics Services

### IStatisticsService

Provides analytics and reporting data.

#### Methods

**GetDailyStatsAsync(DateTime date)**
```csharp
Task<DailyStatsDto> GetDailyStatsAsync(DateTime date)
```
Returns statistics for a specific day.

**Returns:**
- Total work time
- Number of pomodoros completed
- Number of breaks taken
- Time per project

**GetWeeklyStatsAsync(DateTime weekStart)**
```csharp
Task<WeeklyStatsDto> GetWeeklyStatsAsync(DateTime weekStart)
```
Returns statistics for a week.

**GetProjectStatsAsync(int projectId, DateTime? start, DateTime? end)**
```csharp
Task<ProjectStatsDto> GetProjectStatsAsync(int projectId, DateTime? start, DateTime? end)
```
Returns statistics for a specific project within optional date range.

---

## DTOs Overview

### Session DTOs

**PomodoroSessionDto**
```csharp
public class PomodoroSessionDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ClientName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public SessionType SessionType { get; set; }
    public string? Objective { get; set; }
    public string? Notes { get; set; }
}
```

### Settings DTOs

**PomodoroSettingsDto**
```csharp
public class PomodoroSettingsDto
{
    public int Id { get; set; }
    public int WorkDurationMinutes { get; set; }
    public int ShortBreakDurationMinutes { get; set; }
    public int LongBreakDurationMinutes { get; set; }
    public int LongBreakInterval { get; set; }
    public bool ShowNotification { get; set; }
    public bool PlaySound { get; set; }
    public bool FlashWindow { get; set; }
    public int SoftStopDurationMinutes { get; set; }
    public int SoftStopAlarmVolume { get; set; }
    public bool UseAlarm { get; set; }
    public int AlarmVolume { get; set; }
    public DateTime LastModified { get; set; }
}
```

### Client & Project DTOs

**ClientDto**
```csharp
public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

**ProjectDto**
```csharp
public class ProjectDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

---

## Enums

### SessionType
```csharp
public enum SessionType
{
    Work = 0,
    ShortBreak = 1,
    LongBreak = 2
}
```

---

## Error Handling

All services throw exceptions on errors:
- `KeyNotFoundException` - Entity not found
- `InvalidOperationException` - Business rule violation
- `ArgumentException` - Invalid parameters

Recommended to wrap service calls in try-catch blocks in ViewModels.

---

## Usage Examples

### Starting a Pomodoro Session

```csharp
var createDto = new CreatePomodoroSessionDto
{
    ProjectId = selectedProject?.Id,
    DurationMinutes = 25,
    SessionType = SessionType.Work,
    Objective = "Complete login feature"
};

var session = await _pomodoroSessionService.CreateSessionAsync(createDto);
```

### Updating Settings

```csharp
var updateDto = new UpdatePomodoroSettingsDto
{
    WorkDurationMinutes = 45,
    ShortBreakDurationMinutes = 9,
    LongBreakDurationMinutes = 27,
    // ... other properties
};

var settings = await _pomodoroSettingsService.UpdateSettingsAsync(updateDto);
```

### Creating a Client and Project

```csharp
// Create client
var client = await _clientService.CreateClientAsync(new CreateClientDto
{
    Name = "Acme Corp",
    Email = "contact@acme.com"
});

// Create project for that client
var project = await _projectService.CreateProjectAsync(new CreateProjectDto
{
    ClientId = client.Id,
    Name = "Website Redesign",
    HourlyRate = 150.00m
});
```

---

**For implementation details, see the source code in `PomodoroTimeTracker.Application/Services/`**
