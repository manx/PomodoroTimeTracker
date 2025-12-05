# Development Notes - Pomodoro Time Tracker

This document contains technical notes, implementation details, and development history for the Pomodoro Time Tracker application.

## Shared Guidelines

@~/.claude/prompts/general/behavior/critical-thinking.md
@~/.claude/prompts/general/behavior/communication-style.md
@~/.claude/prompts/general/code-quality/self-review-checklist.md
@~/.claude/prompts/general/code-quality/security-fundamentals.md

---

## Centralized Prompt Library

This project uses a centralized prompt library located at `~/.claude/prompts/` to share reusable guidelines across multiple projects.

### Purpose
- **Single source of truth** for cross-project prompts
- **Reduces duplication** - common patterns defined once
- **Prevents contradictions** - all projects use same definitions
- **Easy maintenance** - update once, applies everywhere

### Usage Syntax
Reference prompts using the `@` syntax in CLAUDE.md or agent files:
```markdown
@~/.claude/prompts/general/git/commit-conventions.md
@~/.claude/prompts/dotnet/clean-architecture/layer-separation.md
```

### Directory Structure
```
~/.claude/prompts/
├── general/                    # Universal patterns (any language/framework)
│   ├── behavior/               # AI behavior and communication style
│   │   ├── critical-thinking.md
│   │   ├── communication-style.md
│   │   └── explain-concepts.md
│   ├── code-quality/           # Security, error handling, checklists
│   │   ├── security-fundamentals.md
│   │   ├── error-handling.md
│   │   └── self-review-checklist.md
│   └── git/                    # Commit conventions, branch naming, PRs
│       ├── commit-conventions.md
│       ├── commit-templates.md
│       ├── pr-workflow.md
│       └── safety-rules.md
│
├── dotnet/                     # .NET-specific patterns
│   ├── fundamentals/           # Async, nullable, LINQ, disposal
│   ├── clean-architecture/     # Layer separation, DI
│   ├── ef-core/                # Entity Framework patterns
│   └── testing/                # xUnit, Moq, FluentAssertions
│
├── winui/                      # WinUI 3-specific patterns
│   ├── fundamentals/           # Binding, navigation, pages
│   ├── mvvm/                   # ViewModel patterns, dialogs
│   └── advanced/               # Borderless windows, DI
│
└── agents/                     # Agent orchestration templates
    ├── orchestration/          # Shared agent workflow rules
    └── templates/              # Agent-specific templates
```

### Key Prompts Used in This Project
| Prompt | Purpose |
|--------|---------|
| `general/behavior/critical-thinking.md` | Analyze assumptions, offer alternatives |
| `general/behavior/communication-style.md` | Concise responses, clarifying questions |
| `general/code-quality/self-review-checklist.md` | Pre-delivery verification |
| `general/code-quality/security-fundamentals.md` | Input validation, no SQL injection |
| `general/git/commit-conventions.md` | Conventional commit format |
| `dotnet/clean-architecture/layer-separation.md` | Domain → App → Infra → UI |
| `winui/mvvm/no-value-converters.md` | Explicit bool properties |

### Conflict Resolution
1. **Shared prompts are canonical** - If conflict exists, update the shared prompt
2. **Project files add, don't contradict** - Projects can add context but not override
3. **Verify with `/memory`** - Use Claude Code command to check loaded prompts

### Adding New Prompts
1. Place in appropriate category folder in `~/.claude/prompts/`
2. Follow existing format and style
3. Add entry to `~/.claude/prompts/index.md`
4. Reference in project CLAUDE.md files as needed

---

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

---

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

See @~/.claude/prompts/general/git/commit-conventions.md for full format.

**Project Scopes:**
`domain`, `app`, `infra`, `ui`, `test`, `config`, `ci`

**Footer:**
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

**Test Failure Feedback Loop:**
1. Spawn implementation agent(s) for feature/fix
2. Run `dotnet test` after implementation
3. If tests fail → spawn test-agent for analysis
4. test-agent produces structured failure report with layer analysis
5. Spawn appropriate agent (backend/ui) with error context
6. Repeat until all tests pass
7. Spawn git-agent to commit

**Parallel Execution:**
- backend-agent and ui-agent can run in parallel for cross-layer features
- test-agent runs after implementation is complete
- git-agent runs last to coordinate commits

## Code Quality Standards

See shared prompts for detailed guidelines:
- @~/.claude/prompts/general/code-quality/security-fundamentals.md
- @~/.claude/prompts/general/code-quality/self-review-checklist.md
- @~/.claude/prompts/dotnet/clean-architecture/layer-separation.md
- @~/.claude/prompts/winui/mvvm/no-value-converters.md

### Project-Specific Quality Rules

- ✅ **No value converters** - Use explicit ViewModel boolean properties (project convention)
- ✅ **x:Bind over Binding** - Compile-time checking preferred
- ✅ **377+ tests required** - All tests must pass before delivery
- ✅ **Keep it simple** - Don't over-engineer; minimum complexity for current task

## Implementation Workflow

### Pre-Implementation Checklist
**BEFORE starting any new feature, run the pre-implementation check:**

```bash
.claude/scripts/pre-implementation-check.sh
```

This script checks for:
- Uncommitted changes
- Open PRs
- Sync status with origin/master

If issues are found, ask user how to proceed before starting new work.

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
- Enums: `SessionType` (Work, ShortBreak, LongBreak, Regular, StopWatch)
- Repository interfaces
- No external dependencies

**PomodoroTimeTracker.Application** (Business Logic Layer)
- DTOs for data transfer
- Service interfaces and implementations:
  - `IPomodoroSessionService` - Session CRUD and tracking
  - `IPomodoroSettingsService` - Settings management
  - `IClientService` - Client management
  - `IProjectService` - Project management with client filtering
  - `ITimeEntryService` - Manual time entry
  - `IStatisticsService` - Report statistics (daily/weekly/monthly/custom date ranges)
- `IDispatcherTimer` - Timer abstraction for testability
- References: Domain only

**PomodoroTimeTracker.Infrastructure** (Data Layer)
- Entity Framework Core configurations
- Repository implementations
- Unit of Work pattern
- SQLite database provider
- Automatic migrations on startup
- References: Domain, Application

**PomodoroTimeTracker.ViewModels** (ViewModel Layer - WinUI Class Library)
- All ViewModels extracted for testability (413 tests)
- Timer ViewModels: `PomodoroViewModel`, `RegularTimerViewModel`, `StopWatchViewModel`
- CRUD ViewModels: `ClientListViewModel`, `ClientDetailViewModel`, `ProjectListViewModel`, etc.
- Report ViewModel: `ReportViewModel` with period selection and filtering
- Service interfaces for UI abstraction:
  - `INavigationService` - Page navigation
  - `IDialogService` - Dialog display
  - `IActiveTimerService` - Single timer enforcement
  - `IPomodoroStateService` - Pomodoro cycle tracking
- References: Domain, Application

**PomodoroTimeTracker.WinUI3** (Presentation Layer)
- XAML views and pages
- UI service implementations: NavigationService, DialogService, AudioService
- `DispatcherTimerWrapper` - IDispatcherTimer implementation
- Timer window with Win32 interop
- References: Application, ViewModels

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
// Scoped: DbContext, Repositories
services.AddDbContext<ApplicationDbContext>(...);
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IClientRepository, ClientRepository>();
services.AddScoped<IProjectRepository, ProjectRepository>();
services.AddScoped<IPomodoroSessionRepository, PomodoroSessionRepository>();
services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();

// Scoped: Application Services
services.AddScoped<IClientService, ClientService>();
services.AddScoped<IProjectService, ProjectService>();
services.AddScoped<IPomodoroSessionService, PomodoroSessionService>();
services.AddScoped<ITimeEntryService, TimeEntryService>();
services.AddScoped<IStatisticsService, StatisticsService>();
services.AddScoped<IPomodoroSettingsService, PomodoroSettingsService>();

// Transient: CRUD ViewModels (new instance per request)
services.AddTransient<MainWindowViewModel>();
services.AddTransient<ClientListViewModel>();
services.AddTransient<ClientDetailViewModel>();
services.AddTransient<ProjectListViewModel>();
services.AddTransient<ProjectDetailViewModel>();
services.AddTransient<TimeEntryListViewModel>();
services.AddTransient<TimeEntryDetailViewModel>();
services.AddTransient<PomodoroSettingsViewModel>();

// Singleton: Timer ViewModels (maintain state across navigation)
services.AddSingleton<PomodoroViewModel>();
services.AddSingleton<RegularTimerViewModel>();
services.AddSingleton<StopWatchViewModel>();

// Singleton: UI Services
services.AddSingleton<INavigationService, NavigationService>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<IAudioService, AudioService>();
services.AddSingleton<IActiveTimerService, ActiveTimerService>();
services.AddSingleton<IPomodoroStateService, PomodoroStateService>();

// Transient: Timer abstraction for testability
services.AddTransient<IDispatcherTimer, DispatcherTimerWrapper>();

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
- **TimeEntryListPage**: Time entry management list
- **TimeEntryDetailPage**: Time entry create/edit form
- **ReportPage**: Statistics with daily/weekly/monthly/custom periods
- **MainWindow**: Application shell with NavigationView

**Planned** (structure exists but not implemented)
- Dashboard view

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
- Sound/alarm notifications with selectable sounds
- Time Entry view with manual time tracking

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
- Time Entry list and detail pages
- Compact timer window
- Navigation system
- Dialog service

**Testing**
- 377 unit tests passing (100% pass rate)
- ViewModel layer: 158 tests (PomodoroViewModel, RegularTimerViewModel, StopWatchViewModel, etc.)
- Application layer: 148 tests (services, DTOs)
- Infrastructure layer: 71 tests (repositories)
- IDispatcherTimer abstraction enables ViewModel testing without UI thread

### Incomplete / TODO ⚠️

**Windows Notifications** (MEDIUM PRIORITY)
- Toast notification integration
- FlashWindow implementation
- Settings exist but not implemented

**Testing** (LOW PRIORITY - mostly complete)
- Integration tests for end-to-end scenarios
- UI automation tests for complex workflows

**Error Handling** (MEDIUM PRIORITY)
- Some basic try-catch but needs user-friendly messages
- TODO comments in PomodoroSettingsViewModel
- Need consistent error handling strategy

**UI Pages Not Implemented** (LOW PRIORITY)
- Dashboard view (structure exists)

**Known Issues**
- WinUI 3 borderless window white bar (framework limitation, using WinUIEx workaround)

## Recent Development History

### Report View Implementation (2025-12-04)
- Added Report view with combined Pomodoro and Time Entry statistics
- Time period options: Daily, Weekly, Monthly, Custom date range
- Weekly/Monthly use separate Year + Period dropdowns for easy navigation
- Client and Project filter dropdowns (cascading: client selection filters projects)
- Summary cards: Total Time, Pomodoro Sessions (with completion percentage), Time Entries
- Project breakdown list with progress bars showing percentage of total
- 36 new ReportViewModel tests (total now 413 tests)
- Files changed across all layers:
  - Application: `DateRangeStatisticsDto`, `IStatisticsService.GetDateRangeStatisticsAsync`
  - ViewModels: `ReportViewModel`, `ProjectReportItem`, `WeekOption`, `MonthOption`, `FilterOption`
  - UI: `ReportPage.xaml`, navigation updates
- PR #20: Report view feature

### ViewModels Extraction for Testability (2025-12-04)
- Extracted all ViewModels from WinUI3 project to new `PomodoroTimeTracker.ViewModels` WinUI Class Library
- Created `IDispatcherTimer` interface for timer testability without UI thread
- Added `IActiveTimerService` for single-timer enforcement across timer types
- Added `IPomodoroStateService` to replace static `PomodoroViewModel.CurrentPomodoroCount`
- 158 new ViewModel tests (total now 377 tests)
- Enabled comprehensive testing of timer logic, state transitions, and business rules
- PR #16: Major architectural improvement for testability

### INavigationService Cleanup (2025-12-04)
- Removed `NavigationFrame` property from `INavigationService` interface
- Frame management now internal to concrete `NavigationService` implementation
- Keeps interface UI-agnostic (ViewModels never needed Frame access)
- PR #17: Minor interface cleanup

### Sound Selection Feature (2025-12-01)
- Added dropdown menus for selecting WrapUp and Alarm sounds in Settings
- Test button (🔊) next to each dropdown to preview sounds
- Sound plays automatically when volume slider is released
- UI layout: Sound dropdown + test button placed above volume slider
- Database migration: `AddSoundSelectionSettings`
- Files changed across all layers:
  - Domain: `PomodoroSettings.cs` (new properties)
  - Application: DTOs, `IAudioService`, `PomodoroSettingsService`
  - Infrastructure: EF migration
  - UI: `PomodoroSettingsViewModel`, `PomodoroSettingsPage`
  - Tests: `AudioServiceTests.cs` rewritten

### Slash Commands for Agent Orchestration (2025-12-01)
- Created `/implement-feature` command for structured feature implementation
- Created `/fix-tests` command for analyzing and fixing test failures
- Created `/code-review` command for code review with specialized agents
- Commands use Task tool with appropriate subagent_type

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
1. ~~**TimerWindow position**~~ - ✅ Done (defaults to top-left, persists position between sessions)
2. ~~**Implement sound alarms**~~ - ✅ Done (Sound Selection feature, 2025-12-01)
3. **Implement Dashboard view** - Home page with overview/summary
4. ~~**Implement Time Entry view**~~ - ✅ Done (PR #11, 2025-12-01)
5. ~~**Implement Report view**~~ - ✅ Done (PR #20, 2025-12-04)

### Short Term (Next Sprint)
1. **Merge PomodoroSession into TimeEntry** - Unify all time tracking into single table
   - Add `SessionType` enum (Work, ShortBreak, LongBreak, Regular, StopWatch, Manual)
   - Add `IsBillable` column for billable time tracking
   - Add `IsCompleted` column (nullable)
   - Store breaks in database (currently only in-memory)
   - Migrate existing PomodoroSession data, then drop table
   - Simplifies reporting (no UNION of two tables)
2. **Add unit tests** - Start with PomodoroViewModel
3. **Improve error handling** - User-friendly messages throughout
4. **Windows toast notifications** - Complete notification system
5. **FlashWindow implementation** - Visual alert on completion

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

**Last Updated**: 2025-12-04
**Current Version**: 1.0.0-beta
**Status**: Active Development
