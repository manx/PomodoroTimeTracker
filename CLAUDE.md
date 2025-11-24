# Development Notes - Pomodoro Time Tracker

This document contains technical notes, implementation details, and development history for the Pomodoro Time Tracker application.

## Development Guidelines

### Git Operations
**IMPORTANT:** Always use git MCP tools instead of bash commands for git operations:
- Use `mcp__git__status`, `mcp__git__add`, `mcp__git__commit` instead of `git status`, `git add`, `git commit`
- Use `mcp__git__bulk_action` for multiple sequential operations (stage + commit + push)
- The MCP tools provide better integration and error handling

## Recent Development Session (2025-01-20)

### Wrap Up Period Implementation

Reworked the soft stop feature and renamed to "Wrap Up Period" for better clarity:

**Previous Behavior (Confusing):**
- "Soft stop alarm" triggered X minutes BEFORE the work period ended
- Terminology was unclear and not intuitive

**Current Behavior:**
- Work period ends at intended duration (e.g., 25 min)
- **Wrap up notification** plays when work period ends
- **Wrap up period** begins, allowing user to finish current thought
- Timer counts down the wrap up period (e.g., 3:00 → 2:59 → ... → 0:00)
- Main alarm plays when wrap up period expires
- Total time = Work Duration + Wrap Up Period

**Terminology:**
- **Wrap Up Period** - Extra time after work ends to finish up
- **Wrap Up Notification** - Gentle sound when work period completes
- **Main Alarm** - Louder alarm when wrap up period expires (break must start)

**Implementation Details:**

**Timer States:**
- Added `WrapUp` state to `PomodoroState` enum
- State flow: Setup → Running → WrapUp → Break → (repeat)
- During WrapUp, pause/resume/stop buttons remain active

**Timer Logic:**
```csharp
// When work period ends (remainingSeconds == 0 in Running state)
- Trigger wrap up notification
- Transition to WrapUp state
- Reset countdown to wrap up period duration
- Display "Wrap Up Period" label
- Show informational InfoBar

// When wrap up period ends (remainingSeconds <= 0 in WrapUp state)
- Trigger main alarm
- Mark session as completed
- Start appropriate break
```

**Session Tracking:**
- Sessions created at start record intended work duration (e.g., 25 min)
- Actual work time tracked via StartTime → EndTime
- If stopped during wrap up period: marked as completed with note
- If stopped during work: marked as partial with note

**UI Updates:**
- Added InfoBar during wrap up period: "Work time complete! You can continue working during this wrap up period to finish your current thought."
- Session label shows "Wrap Up Period" during wrap up
- Progress ring resets and counts down wrap up period
- Settings page: "Wrap Up Period Duration" and "Wrap Up Notification Volume"

**Database Schema:**
- Renamed `SoftStopDurationMinutes` → `WrapUpPeriodMinutes`
- Renamed `SoftStopAlarmVolume` → `WrapUpNotificationVolume`
- Migration: `20251120133403_RenameToWrapUpTerminology`

**Settings Configuration:**
- "Wrap Up Period Duration (minutes)" - default 3 min
- "Wrap Up Notification Volume" - plays when work ends
- Description: "Extra time after work period ends to finish your current thought. Wrap up notification plays when work ends, main alarm plays when wrap up period expires."

## Previous Development Session (2025-01-19)

### Pomodoro Timer Implementation

Implemented complete Pomodoro timer functionality with the following workflow:

#### Workflow Design

**Break Cycle Pattern:**
```
Pomodoro #1 � Short Break � Pomodoro #2 � Short Break �
Pomodoro #3 � Short Break � Pomodoro #4 � Long Break � (repeat)
```

**Session Start Flow:**
1. User selects Client (optional, remembers last)
2. User selects Project from that client (optional, remembers last)
3. User enters Objective (required, 60-120 characters)
4. User can adjust Duration (default from settings)
5. Start button enabled only when Objective is filled

**Timer States:**
- **Setup**: Configuring new session
- **Running**: Timer actively counting down work period
- **Paused**: Timer stopped, can be resumed
- **WrapUp**: Wrap up period after work completes (allows finishing current thought)
- **Break**: Automatic break period (no pause/stop controls)

**Stop Button Behavior:**
When Stop is pressed during a running session:
- Timer automatically pauses
- Dialog shows with 3 options:
  - **Resume**: Continue as if only paused
  - **Save**: Save partial session with "Stopped early at XX:XX" note
  - **Discard**: Delete session entirely

#### Technical Implementation

**PomodoroViewModel.cs:**
- State management with explicit boolean properties (no converters)
- Timer using `DispatcherQueueTimer` (WinUI 3 recommended)
- Break cycle tracking with `_pomodoroCount` (0-3)
- Wrap up period after work completes
- Async dialog callback pattern for UI separation

**Key Properties:**
```csharp
public bool IsSetupState => State == PomodoroState.Setup;
public bool IsRunningState => State == PomodoroState.Running;
public bool IsPausedState => State == PomodoroState.Paused;
public bool IsWrapUpState => State == PomodoroState.WrapUp;
public bool IsBreakState => State == PomodoroState.Break;
public bool IsNotBreakState => !IsBreakState;
public bool IsClientSelected => SelectedClient != null;
public string PauseResumeText => IsPausedState ? "Resume" : "Pause";
```

**PomodoroPage.xaml:**
- Two main views: Setup screen and Running timer
- Visibility controlled by state properties
- Progress ring for visual timer feedback
- Objective display during work sessions
- Control buttons (Pause/Resume, Stop) hidden during breaks

**Dialog Pattern:**
```csharp
// In ViewModel
public Func<Task<StopDialogResult>>? ShowStopDialog { get; set; }

// In Page code-behind
ViewModel.ShowStopDialog = ShowStopConfirmationDialogAsync;

private async Task<StopDialogResult> ShowStopConfirmationDialogAsync()
{
    var dialog = new ContentDialog { ... };
    var result = await dialog.ShowAsync();
    return result switch { ... };
}
```

### Settings Configuration

**PomodoroSettings Entity:**
- Work duration (default: 25 min)
- Short break duration (default: 5 min)
- Long break duration (default: 15 min)
- Long break interval (default: every 4 pomodoros)
- Wrap up period duration (default: 3 min) - Time allowed to finish up after work period ends
- Wrap up notification volume (0-100) - Plays when work period ends
- Main alarm volume (0-100) - Plays when wrap up period expires
- Notification preferences (show, sound, flash)

**Auto-calculate Feature:**
- Short break = Work duration / 5
- Long break = (Work duration / 5) * 3
- Button to apply these calculations automatically

### Database Schema Updates

**Migrations Applied:**
1. `AddSoftStopDurationMinutes` - Added configurable soft stop timing
2. `RemoveUseSoftStopAlarm` - Removed enable/disable toggle
3. `AddObjectiveToPomodoroSession` - Added objective field to sessions

**PomodoroSession Entity Changes:**
```csharp
public string? Objective { get; set; }  // NEW: Session objective/goal
```

**PomodoroSettings Entity Changes:**
```csharp
public int SoftStopDurationMinutes { get; set; } = 3;  // NEW
// REMOVED: public bool UseSoftStopAlarm { get; set; }
```

### Design Decision: No Value Converters

**Decision:** Use explicit ViewModel properties instead of XAML value converters

**Rationale:**
- More straightforward and readable
- Easier to debug (can inspect values)
- No extra converter classes to maintain
- Clear intent through property names
- Better IDE support (IntelliSense, refactoring)
- WinUI 3's x:Bind handles bool�Visibility automatically

**Documented in:** DESIGN_GUIDELINES.md

### Dependency Injection Setup

**Registered Services:**
```csharp
// Application Services (Scoped)
services.AddScoped<IPomodoroSessionService, PomodoroSessionService>();
services.AddScoped<IPomodoroSettingsService, PomodoroSettingsService>();
services.AddScoped<IClientService, ClientService>();
services.AddScoped<IProjectService, ProjectService>();

// ViewModels (Transient)
services.AddTransient<PomodoroViewModel>();
services.AddTransient<PomodoroSettingsViewModel>();

// Helper Method
public static T GetService<T>() where T : class
{
    return Services.GetRequiredService<T>();
}
```

## Architecture Patterns

### MVVM Implementation

**ViewModel Base:**
```csharp
public abstract class ViewModelBase : ObservableObject { }
```

Using `CommunityToolkit.Mvvm` for:
- `ObservableObject` base class
- `RelayCommand` and `AsyncRelayCommand`
- Property change notifications

**State Management:**
- State properties trigger multiple property notifications
- Command `CanExecute` updates on state changes
- UI binds to computed boolean properties

### Clean Architecture Layers

**Domain � Application � Infrastructure � Presentation**

**Key Principles:**
- Domain has no dependencies
- Application references Domain only
- Infrastructure implements interfaces from Domain
- Presentation references Application (not Infrastructure directly)
- DI container wires everything together

### Data Flow

```
User Action � ViewModel Command � Application Service �
Repository � Database � back through layers � UI Update
```

**Example: Starting Pomodoro**
```
1. User clicks Start
2. StartPomodoroCommand executes
3. Creates CreatePomodoroSessionDto
4. PomodoroSessionService.CreateSessionAsync()
5. Maps to PomodoroSession entity
6. PomodoroSessionRepository.AddAsync()
7. SaveChanges commits to database
8. Returns PomodoroSessionDto
9. ViewModel updates state
10. UI reflects new state via bindings
```

## Technical Challenges & Solutions

### Challenge 1: Timer Precision on UI Thread

**Problem:** Need accurate 1-second timer that updates UI

**Solution:** `DispatcherQueueTimer` from WinUI 3
- Runs on UI thread automatically
- No need for dispatcher invoke
- Clean start/stop API

```csharp
_timer = _dispatcherQueue.CreateTimer();
_timer.Interval = TimeSpan.FromSeconds(1);
_timer.Tick += Timer_Tick;
```

### Challenge 2: Dialog from ViewModel

**Problem:** ViewModel shouldn't directly create dialogs (violates MVVM)

**Solution:** Callback pattern
- ViewModel exposes `Func<Task<TResult>>` property
- Page sets the callback in code-behind
- ViewModel calls callback when needed
- Keeps ViewModel testable

### Challenge 3: Break Cycle Management

**Problem:** Track position in 4-pomodoro cycle

**Solution:** Simple counter (0-3)
```csharp
private int _pomodoroCount = 0;  // Current position in cycle

// After completing pomodoro
_pomodoroCount++;
bool isLongBreak = _pomodoroCount >= 4;
if (isLongBreak) _pomodoroCount = 0;  // Reset
```

### Challenge 4: Multiple State Properties

**Problem:** Many UI elements depend on state

**Solution:** Computed properties with notifications
```csharp
public PomodoroState State
{
    get => _state;
    set
    {
        if (SetProperty(ref _state, value))
        {
            // Notify all dependent properties
            OnPropertyChanged(nameof(IsSetupState));
            OnPropertyChanged(nameof(IsRunningState));
            OnPropertyChanged(nameof(IsPausedState));
            // ... etc
        }
    }
}
```

## File Structure

### New Files Created

**ViewModels:**
- `PomodoroViewModel.cs` - Complete timer logic (450+ lines)
- `PomodoroSettingsViewModel.cs` - Settings management

**Views:**
- `PomodoroPage.xaml` - Timer UI with setup and running views
- `PomodoroPage.xaml.cs` - Dialog callback implementation
- `PomodoroSettingsPage.xaml` - Settings configuration UI

**Migrations:**
- `20251119092236_AddSoftStopDurationMinutes.cs`
- `20251119093443_RemoveUseSoftStopAlarm.cs`
- `20251119094143_AddObjectiveToPomodoroSession.cs`

**Documentation:**
- `DESIGN_GUIDELINES.md` - Design patterns and decisions

### Modified Files

**Domain:**
- `PomodoroSession.cs` - Added Objective field
- `PomodoroSettings.cs` - Added SoftStopDurationMinutes, removed UseSoftStopAlarm

**Application:**
- `PomodoroSessionDto.cs` - Added Objective to all DTOs
- `PomodoroSessionService.cs` - Updated mapping for Objective
- `PomodoroSettingsService.cs` - Updated for soft stop changes

**Infrastructure:**
- `PomodoroSettingsConfiguration.cs` - Updated EF configuration

**Presentation:**
- `App.xaml` - Removed converter registrations
- `App.xaml.cs` - Added PomodoroViewModel registration, GetService helper

## Testing Notes

### Manual Test Checklist

**Pomodoro Start:**
- [ ] Client dropdown populated
- [ ] Last client pre-selected
- [ ] Project dropdown filtered by client
- [ ] Last project pre-selected
- [ ] Objective field required
- [ ] Start button disabled without objective
- [ ] Duration editable

**Pomodoro Running:**
- [ ] Timer counts down correctly
- [ ] Progress ring updates
- [ ] Pause button works
- [ ] Resume restores timer
- [ ] Stop shows dialog
- [ ] Resume from dialog continues
- [ ] Save from dialog creates partial session
- [ ] Discard from dialog deletes session

**Break Cycle:**
- [ ] Short break after pomodoros 1-3
- [ ] Long break after pomodoro 4
- [ ] Cycle resets after long break
- [ ] No pause/stop during breaks
- [ ] Session type label correct

**Settings:**
- [ ] All settings save
- [ ] Auto-calculate works
- [ ] Defaults restore correctly
- [ ] Settings persist between sessions

## Future Improvements

### Planned Features
1. Sound/alarm implementation (currently just Debug.WriteLine)
2. Notification integration (Windows toast notifications)
3. Session history view
4. Statistics dashboard
5. Export session data
6. Keyboard shortcuts (Space = pause/resume, Esc = stop)

### Technical Debt
1. TODO: Implement actual alarm sounds
2. TODO: Add unit tests for PomodoroViewModel
3. TODO: Add integration tests for service layer
4. TODO: Error handling improvements (user-friendly messages)
5. TODO: Navigation to Pomodoro page (currently not wired in MainWindow)
6. TODO: Check if Microsoft has fixed WinUI 3 transparent borderless window issue (https://github.com/microsoft/microsoft-ui-xaml/issues/1247) - Currently there's a visible white bar at the top of the timer window that cannot be removed due to framework limitations. Using WinUIEx library as workaround.

### Performance Considerations
- Timer runs on UI thread (acceptable for 1-second intervals)
- Consider background service for long-running tracking
- Database queries are async (good)
- Could add caching for clients/projects lists

## Development Environment

- Visual Studio 2022 (or VS Code with C# Dev Kit)
- .NET 9.0 SDK
- Windows App SDK 1.8
- SQLite browser for database inspection

**Useful Commands:**
```bash
# Build
dotnet build

# Run
dotnet run --project PomodoroTimeTracker.WinUI3

# Create migration
dotnet ef migrations add MigrationName --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# Apply migrations
dotnet ef database update --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# Remove last migration
dotnet ef migrations remove --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3
```

**Database Location:**
```
%LocalAppData%\PomodoroTimeTracker\pomodoro.db
```

## References

- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MVVM Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Pomodoro Technique](https://francescocirillo.com/pages/pomodoro-technique)
- to memorize Always use dotnet MCP tools instead of bash