# Development Notes - Pomodoro Time Tracker

This document contains technical notes, implementation details, and development history for the Pomodoro Time Tracker application.

## Project Overview

A WinUI 3 desktop application implementing the Pomodoro Technique with comprehensive time tracking capabilities. Built using Clean Architecture and MVVM patterns.

### Technology Stack
- **.NET 9.0** with C# 13
- **WinUI 3** (Windows App SDK 1.8)
- **Entity Framework Core 9.0** with SQLite
- **CommunityToolkit.Mvvm** for MVVM helpers
- **Microsoft.Extensions.Hosting** for dependency injection
- **WinUIEx** library for borderless window support
- **Native ARM64 support** for Windows 11

## Development Guidelines

### Language Policy
**CRITICAL:** All git-related content MUST be in English:
- ✅ Commit messages (subject, body, footer)
- ✅ PR titles and descriptions
- ✅ Branch names
- ✅ Code comments
- ℹ️ Internal documentation (CLAUDE.md, GIT_STRATEGY.md) can be in Swedish
- ℹ️ Conversations and discussions can be in Swedish

### Git Operations
**IMPORTANT:** Bash commands are preferred over MCP tools for git operations (more token-efficient):
- Use bash: `git status`, `git add`, `git commit`, `git push`
- Chain commands: `git checkout master && git pull origin master`
- MCP tools have more overhead and use more tokens

### Git Commit Standards

**Commit Message Format:**
```
<type>(<scope>): <brief description>

<detailed explanation>

<footer>
```

**Types:** `feat`, `fix`, `refactor`, `test`, `docs`, `style`, `chore`, `perf`

**Brief Description Rules:**
- Imperative mood: "add", not "added" or "adds"
- Lowercase start, no period at end
- Maximum 50 characters

**Project Scopes:**
- `domain` - Domain entities, enums
- `app` - Application layer (services, DTOs)
- `infra` - Infrastructure (repositories, EF)
- `ui` - WinUI3 (ViewModels, Views)
- `test` - Test project
- `config` - Configuration files
- `ci` - CI/CD workflows

**Single vs Multiple Commits:**
- **ONE commit:** Tightly coupled changes (service + ViewModel + tests for same feature)
- **MULTIPLE commits:** Logically separate changes (feature + unrelated docs)

**Footer:**
- Always include: `🤖 Generated with [Claude Code](https://claude.com/claude-code)`
- Always include: `Co-Authored-By: Claude <noreply@anthropic.com>`
- Issue links when applicable: `Fixes #123`

### .NET Operations
**IMPORTANT:** Always use dotnet MCP tools instead of bash commands:
- Use `mcp__dotnet__dotnet_build` instead of `dotnet build`
- Use `mcp__dotnet__dotnet_test` instead of `dotnet test`
- Use `mcp__dotnet__dotnet_run` instead of `dotnet run`
- MCP tools provide better integration and error handling

### Agent Orchestration Workflow

**Available Agents** (in `.claude/agents/`):

| Agent | Purpose | Model |
|-------|---------|-------|
| backend-agent | Application/Infrastructure layer | sonnet |
| ui-agent | WinUI 3 presentation layer | sonnet |
| test-agent | Unit tests + failure analysis | sonnet |
| git-agent | Commits and PRs | haiku |

**Implementation Workflow:**
1. Spawn implementation agent(s) for feature/fix
2. Run `dotnet build` to catch compilation errors
3. Run `dotnet test` after successful build
4. If tests fail → spawn test-agent for analysis
5. test-agent produces structured failure report with layer analysis
6. Spawn appropriate agent (backend/ui) with error context
7. Repeat steps 2-6 until build and tests pass
8. Spawn git-agent to commit

**Parallel Execution:**
- backend-agent and ui-agent can run in parallel for cross-layer features
- test-agent runs after implementation is complete
- git-agent runs last to coordinate commits

**Parallel Agent Boundaries:**
| Agent | Owns | Avoid |
|-------|------|-------|
| backend-agent | `Domain/`, `Application/`, `Infrastructure/` | `WinUI3/` |
| ui-agent | `WinUI3/Views/`, `WinUI3/ViewModels/`, `WinUI3/Services/` | Other layers |
| test-agent | `Tests/` | Production code |

**Conflict Prevention:**
- Never have two agents edit the same file simultaneously
- If cross-layer coordination needed, run agents sequentially
- Shared files (e.g., `App.xaml.cs` for DI) → ui-agent owns, backend-agent requests changes

## Code Quality Standards

**CRITICAL:** Before delivering ANY code, Claude MUST ensure it meets these quality standards:

### 1. Security Requirements

- ✅ **No SQL Injection vulnerabilities** - Always use parameterized queries (EF Core handles this)
- ✅ **Input validation** - Validate all user inputs at service layer
- ✅ **Proper error handling** - Never expose stack traces or sensitive info to UI
- ✅ **Secure defaults** - No hardcoded credentials, use user secrets for sensitive data

### 2. Architecture & Design

- ✅ **Clean Architecture layers** - Strictly maintain Domain → Application → Infrastructure → UI separation
- ✅ **Dependency inversion** - Depend on abstractions (interfaces), not concrete implementations
- ✅ **Single Responsibility** - Each class/method has ONE clear purpose
- ✅ **DRY principle (with pragmatism)** - Extract common logic when it genuinely reduces complexity. Don't create helper functions for simple operations where inlined code is clearer. Cognitive load matters: jumping to another file to understand a simple operation is worse than repeating a few lines
- ✅ **MVVM pattern** - ViewModels never reference UI controls, Views never contain business logic
- ✅ **Keep it simple** - Don't add features, error handling, fallbacks, or abstractions beyond what's needed for the current task

### 3. .NET & C# Best Practices

- ✅ **Async/await properly** - All I/O operations async, propagate CancellationToken where applicable
- ✅ **IDisposable pattern** - Implement correctly for resources (DbContext, etc.)
- ✅ **Null handling** - Use nullable reference types, check nulls appropriately
- ✅ **LINQ usage** - Prefer LINQ over manual loops where readable
- ✅ **Exception handling** - Catch specific exceptions, don't swallow exceptions silently

### 4. WinUI 3 / XAML Specific

- ✅ **No value converters** - Use explicit ViewModel boolean properties instead (project convention)
- ✅ **Dispatcher thread** - All UI updates on correct thread (DispatcherQueue for timers)
- ✅ **ViewModel lifecycle** - Properly dispose subscriptions, timers, event handlers
- ✅ **Data binding** - Prefer x:Bind over Binding for performance and compile-time checking
- ✅ **Resource management** - Properly handle XAML resources, avoid memory leaks

### 5. Testing Requirements

- ✅ **Unit tests for all business logic** - Service layer must have comprehensive tests
- ✅ **AAA pattern** - All tests follow Arrange-Act-Assert structure
- ✅ **Edge cases covered** - Test null, empty, boundary values, error conditions
- ✅ **Meaningful test names** - Test names describe what is being tested and expected outcome
- ✅ **No logic in tests** - Tests should be simple and obvious
- ✅ **Mock external dependencies** - Use Moq for interfaces, InMemory for repositories

### 6. Documentation

- ✅ **XML documentation** - All public APIs have XML doc comments
- ✅ **Complex logic comments** - Explain WHY, not WHAT (code shows what)
- ✅ **Update CLAUDE.md** - Document significant architectural decisions
- ✅ **README updates** - Keep user-facing documentation current

### 7. Self-Review Checklist

Before presenting ANY code, Claude must verify:

- [ ] **Can this throw unhandled exceptions?** - All exception paths considered
- [ ] **Are there race conditions?** - Async code properly coordinated
- [ ] **Is null handling correct?** - All nullable paths handled
- [ ] **Does this follow project patterns?** - Consistent with existing codebase
- [ ] **Are tests comprehensive?** - All paths and edge cases tested
- [ ] **Is documentation updated?** - CLAUDE.md, README, XML docs current
- [ ] **Performance acceptable?** - No obvious performance issues (N+1 queries, etc.)
- [ ] **Memory leaks prevented?** - Disposable resources properly managed

## Implementation Workflow

### For Simple Features (< 200 lines, straightforward logic)

**Direct Implementation:**
1. Write implementation following all quality standards
2. Run self-review checklist
3. Write comprehensive unit tests
4. Present with: "✅ Implemented, self-reviewed, and tested"

### For Complex Features (> 200 lines OR complex UI OR architectural decisions)

**Plan Mode First:**
1. **Enter Plan Mode** - Use EnterPlanMode tool to design structure
2. **Explore codebase** - Understand existing patterns and architecture
3. **Design approach** - Create detailed implementation plan
4. **Present plan** - Show user the design BEFORE coding
5. **Get approval** - Wait for user feedback and approval
6. **Implement** - Follow approved plan with quality standards
7. **Test thoroughly** - Comprehensive tests for complex logic
8. **Present** - "✅ Implemented according to approved plan"

**Triggers for Plan Mode:**
- Complex UI (multiple views, custom controls, complex layouts)
- New architectural patterns (first of its kind in project)
- Significant refactoring (touching 5+ files)
- Performance-critical features
- Database schema changes
- Integration with external services

### When User Provides Insufficient Detail for Complex UI

If the user requests complex UI without specifying structure:

**Do NOT:**
- ❌ Guess at the structure
- ❌ Implement and hope it's right
- ❌ Create overly complex solution

**Instead:**
- ✅ Enter Plan Mode
- ✅ Propose 2-3 design alternatives
- ✅ Explain trade-offs
- ✅ Ask for user preference
- ✅ Implement chosen design

## Quality Assurance

### Code Delivery Format

When delivering code, always include:

```markdown
## Implementation Summary

**Feature:** [Brief description]

**Files Changed:**
- Path/To/File.cs (Added/Modified)
- Path/To/Test.cs (Added)

**Quality Checklist:**
- ✅ Self-reviewed against quality standards
- ✅ Unit tests written (X tests, 100% coverage of new code)
- ✅ Edge cases handled (null, empty, boundaries)
- ✅ No security vulnerabilities
- ✅ Follows Clean Architecture
- ✅ Documentation updated

**Testing:**
All X tests passing locally.

**Notes:**
[Any important considerations, decisions, or follow-ups]
```

### If Code Doesn't Meet Standards

If user identifies quality issues:

1. **Acknowledge** - "You're right, this doesn't meet the standards"
2. **Identify gap** - Which standard was violated
3. **Fix immediately** - Don't argue, improve the code
4. **Learn** - Update approach to prevent similar issues

## Continuous Improvement

This section should be updated when:
- New patterns emerge in the codebase
- Quality issues are discovered and fixed
- New best practices are adopted
- User provides feedback on code quality

## Architecture

### Project Structure

**PomodoroTimeTracker.Domain** (Core Layer)
- Business entities: `Client`, `Project`, `PomodoroSession`, `PomodoroSettings`, `TimeEntry`
- Enums: `SessionType` (Work, ShortBreak, LongBreak)
- Repository interfaces
- No external dependencies

**PomodoroTimeTracker.Application** (Business Logic Layer)
- DTOs for data transfer
- Service interfaces and implementations:
  - `IPomodoroSessionService` - Session CRUD and tracking
  - `IPomodoroSettingsService` - Settings management
  - `IClientService` - Client management
  - `IProjectService` - Project management with client filtering
  - `ITimeEntryService` - Manual time entry (structure in place)
  - `IStatisticsService` - Reporting (structure in place)
- References: Domain only

**PomodoroTimeTracker.Infrastructure** (Data Layer)
- Entity Framework Core configurations
- Repository implementations
- Unit of Work pattern
- SQLite database provider
- Automatic migrations on startup
- References: Domain, Application

**PomodoroTimeTracker.WinUI3** (Presentation Layer)
- MVVM pattern with ViewModels
- XAML views and pages
- UI services: NavigationService, DialogService
- Timer window with advanced features
- References: Application (not Infrastructure directly)

### Architectural Patterns

**Clean Architecture**
- Domain-driven design
- Dependency inversion (inner layers independent of outer)
- Separation of concerns
- Repository pattern for data access
- Unit of Work for transaction management

**MVVM (Model-View-ViewModel)**
- ViewModelBase using `CommunityToolkit.Mvvm`
- `RelayCommand` and `AsyncRelayCommand` for actions
- Explicit state properties (no value converters)
- Property change notifications cascade to dependent properties
- Dialog callback pattern for UI separation

**Design Decision: No Value Converters**
- Use explicit ViewModel boolean properties instead of XAML converters
- More straightforward and readable
- Easier to debug
- Better IDE support
- WinUI 3's x:Bind handles bool→Visibility automatically
- Documented in: DESIGN_GUIDELINES.md

### Data Flow
```
User Action → ViewModel Command → Application Service →
Repository → Database → back through layers → UI Update
```

### Dependency Injection
```csharp
// Scoped: DbContext, Repositories, Services
services.AddScoped<IPomodoroSessionService, PomodoroSessionService>();
services.AddScoped<IPomodoroSettingsService, PomodoroSettingsService>();
services.AddScoped<IClientService, ClientService>();
services.AddScoped<IProjectService, ProjectService>();

// Transient: ViewModels
services.AddTransient<PomodoroViewModel>();
services.AddTransient<PomodoroSettingsViewModel>();
services.AddTransient<ClientListViewModel>();
services.AddTransient<ClientDetailViewModel>();
services.AddTransient<ProjectListViewModel>();
services.AddTransient<ProjectDetailViewModel>();

// Singleton: UI Services
services.AddSingleton<INavigationService, NavigationService>();
services.AddSingleton<IDialogService, DialogService>();

// Helper Method
public static T GetService<T>() where T : class
{
    return Services.GetRequiredService<T>();
}
```

## Database Schema

### Entities and Relationships

**Client**
- `Id` (PK)
- `Name` (200 chars, unique, indexed)
- `Description` (1000 chars, nullable)
- `CreatedAt` (DateTime)
- **Navigation**: `Projects` (1-to-many)

**Project**
- `Id` (PK)
- `Name` (200 chars, unique per client)
- `Description` (1000 chars, nullable)
- `ClientId` (FK, nullable)
- `CreatedAt` (DateTime)
- **Navigation**: `Client`, `PomodoroSessions`, `TimeEntries`
- **Index**: Composite unique (Name, ClientId)

**PomodoroSession**
- `Id` (PK)
- `ProjectId` (FK, nullable)
- `StartTime` (DateTime, indexed)
- `EndTime` (DateTime, nullable for active sessions)
- `DurationMinutes` (int, planned duration)
- `IsCompleted` (bool)
- `SessionType` (enum: Work/ShortBreak/LongBreak)
- `Objective` (string, 90 chars max, session goal)
- `Notes` (string, 500 chars, nullable)
- **Navigation**: `Project`

**PomodoroSettings** (Singleton)
- `Id` (PK)
- `WorkDurationMinutes` (default: 25)
- `ShortBreakDurationMinutes` (default: 5)
- `LongBreakDurationMinutes` (default: 15)
- `LongBreakInterval` (default: 4)
- `ShowNotification` (default: true)
- `PlaySound` (default: true)
- `FlashWindow` (default: false)
- `WrapUpPeriodMinutes` (default: 3)
- `WrapUpNotificationVolume` (0-100, default: 50)
- `UseAlarm` (default: true)
- `AlarmVolume` (0-100, default: 50)
- `LastModified` (DateTime)

**TimeEntry**
- `Id` (PK)
- `ProjectId` (FK, nullable)
- `Description` (500 chars)
- `StartTime` (DateTime, indexed)
- `EndTime` (DateTime, nullable for running entries)
- `DurationMinutes` (int, nullable)
- `CreatedAt` (DateTime)
- **Navigation**: `Project`

**Cascade Delete Rules**
- Client deletion → Projects set ClientId to NULL
- Project deletion → Sessions/TimeEntries set ProjectId to NULL

### Migration History

**20251119194358_InitialCreate**
- Created all 5 tables
- Established relationships and indexes
- Initial schema with Objective and WrapUpPeriod fields

**20251120133403_RenameToWrapUpTerminology**
- Renamed `SoftStopDurationMinutes` → `WrapUpPeriodMinutes`
- Renamed `SoftStopAlarmVolume` → `WrapUpNotificationVolume`
- Improved terminology clarity

**Database Location**
```
%LocalAppData%\PomodoroTimeTracker\pomodoro.db
```

## Core Features

### Pomodoro Timer (Fully Implemented)

**Workflow Pattern**
```
Work #1 → Short Break → Work #2 → Short Break →
Work #3 → Short Break → Work #4 → Long Break → (cycle repeats)
```

**Session Start Flow**
1. Select Client (optional, remembers last selection)
2. Select Project from that client (optional, remembers last selection)
3. Enter Objective (required, 90 characters max)
4. Adjust Duration if needed (defaults from settings)
5. Click Start (enabled only when Objective is filled)

**Timer States**
- **Setup**: Configuring new session
- **Running**: Timer actively counting down work period
- **Paused**: Timer stopped, can be resumed
- **WrapUp**: Wrap up period after work completes (allows finishing current thought)
- **Break**: Automatic break period (no pause/stop controls available)

**State Flow**: Setup → Running → WrapUp → Break → Setup (repeat)

**Stop Button Behavior**
When Stop is pressed during a running session:
1. Timer automatically pauses
2. Dialog appears with 3 options:
   - **Resume**: Continue as if only paused
   - **Save**: Save partial session with "Stopped early at XX:XX" note
   - **Discard**: Delete session entirely from database

**Break Cycle Management**
- Tracks position in 4-pomodoro cycle using `_pomodoroCount` (0-3)
- Short break after pomodoros 1-3
- Long break after pomodoro 4
- Counter resets to 0 after long break

### Wrap Up Period Feature

**Purpose**: Extra time after work period ends to finish your current thought without counting as overtime.

**Behavior**
- Work period ends at intended duration (e.g., 25 minutes)
- **Wrap up notification** plays (gentle sound, low volume)
- **Wrap up period** begins (default: 3 minutes)
- Timer counts down: 3:00 → 2:59 → ... → 0:00
- **Main alarm** plays when wrap up period expires
- Break must start after wrap up period expires
- Total time = Work Duration + Wrap Up Period

**UI During Wrap Up**
- InfoBar message: "Work time complete! You can continue working during this wrap up period to finish your current thought."
- Session label shows "Wrap Up Period"
- Progress ring resets and counts down wrap up period
- Pause/Resume/Stop buttons remain active

**Session Tracking**
- Sessions record intended work duration (e.g., 25 min)
- Actual work time tracked via StartTime → EndTime
- If stopped during wrap up: marked as completed with note
- If stopped during work: marked as partial with note

**Settings**
- Wrap Up Period Duration (minutes) - default 3 min
- Wrap Up Notification Volume (0-100) - default 50

### Timer Window (Advanced Features)

**Design** (Updated 2025-01-24)
- Ultra-compact always-on-top window (150x50 pixels)
- Truly borderless design using `DwmExtendFrameIntoClientArea` and `WM_NCCALCSIZE`
- Horizontal layout: narrow vertically, wide horizontally
- Minimal margins for maximum compactness
- Draggable via entire window surface
- Resizable from all edges and corners

**Layout**
- Timer text centered (Consolas, 24pt)
- Objective shown only on hover via tooltip
- Rectangular progress bar (red #E74C3C, 30% opacity)
- Progress fills left-to-right as time counts down

**Features**
- **Rectangular Progress Bar**: Fills from left to right, full window height
- **Tooltip Objective**: Hover over timer to see session objective
- **Right-Click Context Menu**:
  - Pause/Resume timer
  - Stop with submenu (Save/Discard/Resume)
  - Add Time (+1, +2, +5 minutes)
- **Custom Win32 Integration**: Borderless window implementation

**Technical Implementation**
- `DwmExtendFrameIntoClientArea` with margins set to -1 for full frame extension
- `WM_NCCALCSIZE` handler returns 0 to remove non-client area
- `SetWindowPos` with `SWP_FRAMECHANGED` to fix initial display issues
- No white bars or borders visible
- Position: Top-right corner (TODO: Move to top-left, close to edges)

### Pomodoro Settings

**Timer Durations**
- Work duration: 1-120 minutes (default: 25)
- Short break: manual or auto-calculated (default: 5)
- Long break: manual or auto-calculated (default: 15)
- Long break interval: pomodoros before long break (default: 4)
- Wrap up period: extra time after work ends (default: 3)

**Audio Settings**
- Wrap up notification volume: 0-100% (default: 50)
- Main alarm volume: 0-100% (default: 50)
- Use alarm: enable/disable main alarm (default: true)
- Play sound: enable/disable all sounds (default: true)

**Notification Settings**
- Show notification: Windows toast notifications (default: true)
- Flash window: flash on completion (default: false)

**Auto-Calculate Feature**
- Button to auto-calculate break durations from work duration
- Short break = work duration ÷ 5
- Long break = (work duration ÷ 5) × 3
- Example: 25 min work → 5 min short, 15 min long

### Client & Project Management

**Features**
- Full CRUD operations for Clients and Projects
- Client list with search/filter
- Project list filtered by client
- Detail pages for editing
- Navigation between related entities
- One-to-many relationship (Client → Projects)
- Optional client association for projects
- Database relationships maintained through EF Core

**Navigation Structure**
- ClientListPage → ClientDetailPage (create/edit)
- ProjectListPage → ProjectDetailPage (create/edit)
- Cascade behavior: deleting client sets project's ClientId to NULL

## Implementation Details

### PomodoroViewModel.cs

**Responsibilities**
- Complete timer logic (~700+ lines)
- State management with explicit boolean properties
- Timer using `DispatcherQueueTimer` (WinUI 3 recommended)
- Break cycle tracking
- Session CRUD via service layer
- Async dialog callback pattern

**Key Properties**
```csharp
public PomodoroState State { get; set; }  // Triggers dependent property notifications
public bool IsSetupState => State == PomodoroState.Setup;
public bool IsRunningState => State == PomodoroState.Running;
public bool IsPausedState => State == PomodoroState.Paused;
public bool IsWrapUpState => State == PomodoroState.WrapUp;
public bool IsBreakState => State == PomodoroState.Break;
public bool IsNotBreakState => !IsBreakState;
public bool IsClientSelected => SelectedClient != null;
public string PauseResumeText => IsPausedState ? "Resume" : "Pause";
```

**Timer Implementation**
```csharp
_timer = _dispatcherQueue.CreateTimer();
_timer.Interval = TimeSpan.FromSeconds(1);
_timer.Tick += Timer_Tick;

// Runs on UI thread automatically, no dispatcher invoke needed
```

**Dialog Callback Pattern**
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

### PomodoroPage.xaml

**Structure**
- Two main views: Setup screen and Running timer
- Visibility controlled by state properties (no converters)
- Progress ring for visual timer feedback
- Objective text display during work sessions
- Control buttons (Pause/Resume, Stop) hidden during breaks
- InfoBar for wrap up period message

**Data Binding**
- Uses x:Bind for performance and compile-time checking
- Binds directly to ViewModel boolean properties
- No need for value converters

### UI Pages

**Implemented**
- **PomodoroPage**: Main timer with setup form and running display
- **TimerWindow**: Compact floating timer with context menu
- **PomodoroSettingsPage**: Settings configuration UI
- **ClientListPage**: Client management list
- **ClientDetailPage**: Client create/edit form
- **ProjectListPage**: Project management list
- **ProjectDetailPage**: Project create/edit form
- **MainWindow**: Application shell with NavigationView

**Planned** (structure exists but not implemented)
- Dashboard view
- Time Entry view
- Statistics view

## Technical Challenges & Solutions

### Challenge 1: Timer Precision on UI Thread

**Problem**: Need accurate 1-second timer that updates UI without blocking

**Solution**: `DispatcherQueueTimer` from WinUI 3
- Runs on UI thread automatically
- No need for manual dispatcher invoke
- Clean start/stop API
- Acceptable for 1-second intervals

### Challenge 2: Dialog from ViewModel

**Problem**: ViewModel shouldn't directly create dialogs (violates MVVM and testability)

**Solution**: Callback pattern
- ViewModel exposes `Func<Task<TResult>>` property
- Page sets the callback in code-behind
- ViewModel calls callback when needed
- Keeps ViewModel testable and UI-agnostic

### Challenge 3: Break Cycle Management

**Problem**: Track position in 4-pomodoro cycle and determine break type

**Solution**: Simple counter variable
```csharp
private int _pomodoroCount = 0;  // Current position in cycle (0-3)

// After completing pomodoro
_pomodoroCount++;
bool isLongBreak = _pomodoroCount >= 4;
if (isLongBreak) _pomodoroCount = 0;  // Reset cycle
```

### Challenge 4: Multiple State-Dependent Properties

**Problem**: Many UI elements depend on timer state

**Solution**: Computed properties with cascading notifications
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
            OnPropertyChanged(nameof(IsWrapUpState));
            OnPropertyChanged(nameof(IsBreakState));
            OnPropertyChanged(nameof(IsNotBreakState));

            // Update command states
            StartPomodoroCommand.NotifyCanExecuteChanged();
            PauseResumeCommand.NotifyCanExecuteChanged();
            StopPomodoroCommand.NotifyCanExecuteChanged();
        }
    }
}
```

### Challenge 5: Borderless Window Transparency

**Problem**: WinUI 3 shows white bar at top of borderless windows

**Solution**: WinUIEx library workaround
- Provides `WindowEx` base class
- Better control over window styling
- TODO: Check if Microsoft has fixed this in newer versions

## Current Status

### Fully Implemented ✅

**Core Functionality**
- Complete Pomodoro workflow with 4-cycle breaks
- All 5 timer states (Setup/Running/Paused/WrapUp/Break)
- Session tracking with objectives (90 char max)
- Client/Project association with memory
- Pause/Resume functionality
- Stop with save/discard/resume options
- Wrap up period implementation
- Timer window with advanced features
- Settings configuration with auto-calculate

**Data Layer**
- Complete database schema
- EF Core configurations
- Migrations system working
- Repository pattern implemented
- Unit of Work pattern
- Automatic migration on startup

**UI Layer**
- MVVM pattern throughout
- All ViewModels implemented
- Client/Project CRUD pages
- Pomodoro timer and settings pages
- Compact timer window
- Navigation system
- Dialog service

### Incomplete / TODO ⚠️

**Sound/Alarm Implementation** (HIGH PRIORITY)
- Wrap up notification sound (TODO line 685-686 in PomodoroViewModel)
- Main alarm sound (TODO line 693-694 in PomodoroViewModel)
- Currently just Debug.WriteLine statements
- Volume settings exist but not used
- Need to implement audio playback

**Windows Notifications** (MEDIUM PRIORITY)
- Toast notification integration
- FlashWindow implementation
- Settings exist but not implemented

**Testing** (HIGH PRIORITY)
- No unit tests for PomodoroViewModel
- No integration tests for service layer
- Need comprehensive test coverage

**Error Handling** (MEDIUM PRIORITY)
- Some basic try-catch but needs user-friendly messages
- TODO comments in PomodoroSettingsViewModel
- Need consistent error handling strategy

**UI Pages Not Implemented** (LOW PRIORITY)
- Dashboard view (structure exists)
- Time Entry view (structure exists)
- Statistics view (structure exists)

**Known Issues**
- WinUI 3 borderless window white bar (framework limitation, using WinUIEx workaround)

## Recent Development History

### TimerWindow Redesign to Horizontal Layout (2025-01-24)
- Complete redesign from circular (200x200) to compact horizontal (150x50) layout
- Changed progress indicator from circular arc to rectangular bar filling left-to-right
- Moved objective from always-visible to tooltip (shows on hover only)
- Removed square aspect ratio enforcement, allowing free window resizing
- Implemented truly borderless window:
  - `DwmExtendFrameIntoClientArea` with -1 margins for full frame extension
  - `WM_NCCALCSIZE` handler to remove non-client area
  - `SetWindowPos` with `SWP_FRAMECHANGED` to fix initial white bar issue
- Reduced window dimensions for minimal desktop footprint
- Reduced margins (6px horizontal, 2px vertical) and font size (24pt)
- Commit: 3dc11b0

### UI Spacing and Alternative Button Styles (2025-01-24)
- Doubled button spacing in PomodoroPage from 16px to 32px
- Added alternative button styles to App.xaml:
  - `PomodoroButtonStyle`: Warm gray (#5A5A5A) base style
  - `PomodoroAccentButtonStyle`: Red accent (#E74C3C) matching progress bar
- Styles available for future use but currently using default blue
- Commit: 3d6972b

### UI Layout Improvements (Previous commits)
- Container-based layout for timer alignment (f9f0277)
- Removed stray characters from XAML (9d63cb4)
- Spacing adjustments between timer and buttons (ddc8ffc, 39de27e)

### WinUIEx Integration
- Added WinUIEx package for borderless window support (a0b602d)
- Improved window transparency handling

### Objective Field Refinements
- Reduced max length from 120 to 90 characters (709a7fc)
- Increased field height for better UX (8c6ed6f)
- Improved character counter display (98413b0)
- Extracted OBJECTIVE_MAX_LENGTH constant (11eb1d0)

### Timer Window Enhancements
- Added circular progress meter with Time Timer style (27c62cf)
- Simplified objective display (5da40a7)
- Show objective in timer window during work sessions (a503ce9)

### App Close Confirmation
- Added confirmation dialog when closing with active session (56aed49)
- Cleanup of unused files

### Compact Timer Window
- Implemented always-on-top timer window (d0f6541)
- Added right-click context menu functionality
- Draggable and resizable with square aspect ratio

## Testing Checklist

### Pomodoro Start
- [ ] Client dropdown populated from database
- [ ] Last selected client pre-selected
- [ ] Project dropdown filtered by selected client
- [ ] Last selected project pre-selected
- [ ] Objective field required (90 chars max)
- [ ] Start button disabled without objective
- [ ] Duration editable before start

### Pomodoro Running
- [ ] Timer counts down correctly (1 second intervals)
- [ ] Progress ring updates smoothly
- [ ] Pause button works immediately
- [ ] Resume restores timer exactly
- [ ] Stop shows confirmation dialog
- [ ] Resume from dialog continues timer
- [ ] Save from dialog creates partial session with note
- [ ] Discard from dialog deletes session from database

### Wrap Up Period
- [ ] Wrap up notification plays when work period ends
- [ ] Timer transitions to WrapUp state
- [ ] InfoBar message displays correctly
- [ ] Progress ring resets and counts down wrap up period
- [ ] Pause/Resume/Stop buttons remain active
- [ ] Main alarm plays when wrap up period expires
- [ ] Break starts automatically after wrap up

### Break Cycle
- [ ] Short break after pomodoros 1-3
- [ ] Long break after pomodoro 4
- [ ] Cycle resets to 0 after long break
- [ ] No pause/stop buttons during breaks
- [ ] Session type label shows correct break type
- [ ] Break duration matches settings

### Timer Window
- [ ] Window stays on top of other windows
- [ ] Draggable via entire surface
- [ ] Resizable from corners only
- [ ] Maintains square aspect ratio
- [ ] Progress meter decreases correctly
- [ ] Right-click menu appears
- [ ] Context menu commands work
- [ ] Add time feature works (+1, +2, +5 min)

### Settings
- [ ] All settings save to database
- [ ] Auto-calculate feature works correctly
- [ ] Defaults restore properly
- [ ] Settings persist between app sessions
- [ ] Changes immediately affect new sessions

### Client & Project Management
- [ ] CRUD operations work for clients
- [ ] CRUD operations work for projects
- [ ] Client filter works in project list
- [ ] Deleting client sets project ClientId to NULL
- [ ] Navigation between pages works
- [ ] Data persists correctly

## Development Environment

### Requirements
- Visual Studio 2022 (or VS Code with C# Dev Kit)
- .NET 9.0 SDK
- Windows App SDK 1.8
- Windows 11 (recommended, ARM64 supported)

### Useful Tools
- SQLite browser for database inspection
- WinUI 3 Gallery app for reference
- Git for version control

### MCP Commands (Preferred)

**Git Operations**
```csharp
mcp__git__status()
mcp__git__add({ files: ["path"] })
mcp__git__commit({ message: "..." })
mcp__git__push({ branch: "master" })
mcp__git__bulk_action({ actions: [...] })
```

**.NET Operations**
```csharp
mcp__dotnet__dotnet_build({ configuration: "Debug" })
mcp__dotnet__dotnet_run({ project: "PomodoroTimeTracker.WinUI3" })
mcp__dotnet__dotnet_test()
mcp__dotnet__dotnet_clean()
```

### Fallback Bash Commands

**Build & Run**
```bash
dotnet build
dotnet run --project PomodoroTimeTracker.WinUI3
```

**Entity Framework Migrations**
```bash
# Create migration
dotnet ef migrations add MigrationName --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# Apply migrations
dotnet ef database update --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# Remove last migration
dotnet ef migrations remove --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# List migrations
dotnet ef migrations list --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3
```

## Future Roadmap

### Immediate TODOs (Active)
1. **Move TimerWindow to top-left corner** - Reposition with minimal margins from screen borders
2. **Implement sound alarms** - Wrap up notification and main alarm (currently TODO in code)
3. **Implement Dashboard view** - Home page with overview/summary
4. **Implement Time Entry view** - Manual time tracking page
5. **Implement Statistics view** - Reporting and analytics page

### Short Term (Next Sprint)
1. **Add unit tests** - Start with PomodoroViewModel
2. **Improve error handling** - User-friendly messages throughout
3. **Windows toast notifications** - Complete notification system
4. **FlashWindow implementation** - Visual alert on completion

### Medium Term
1. Session history view with filtering and search
2. Export session data (CSV, JSON)
3. Keyboard shortcuts (Space = pause/resume, Esc = stop)
4. Session notes editing after completion
5. Button color scheme refinement (warm gray/red alternative styles available)

### Long Term
1. Cloud sync (OneDrive integration?)
2. Multiple timer presets
3. Task integration (Microsoft To Do?)
4. Productivity reports and insights
5. Calendar integration
6. Team/collaboration features

### Performance Optimizations (If Needed)
- Timer runs on UI thread (currently acceptable)
- Consider background service for long-running tracking
- Database queries are async (good)
- Could add caching for clients/projects lists
- Consider virtual scrolling for large lists

## References

- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MVVM Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Pomodoro Technique](https://francescocirillo.com/pages/pomodoro-technique)
- [Windows App SDK](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [WinUIEx Library](https://github.com/dotMorten/WinUIEx)

## Contributing Guidelines

### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose
- Avoid magic numbers (use constants)

### Commit Messages
- Use imperative mood ("Add feature" not "Added feature")
- Keep first line under 72 characters
- Reference issue numbers when applicable
- Group related changes in single commit

### Pull Request Process
1. Ensure all tests pass (when tests exist)
2. Update documentation if needed
3. Add entry to this file under "Recent Development History"
4. Request review from maintainer

---

**Last Updated**: 2025-01-24
**Current Version**: 1.0.0-beta
**Status**: Active Development
