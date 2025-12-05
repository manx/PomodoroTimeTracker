# Pomodoro Time Tracker

WinUI 3 desktop application implementing the Pomodoro Technique with time tracking.

## Shared Guidelines

@~/.claude/prompts/general/behavior/critical-thinking.md
@~/.claude/prompts/general/behavior/communication-style.md
@~/.claude/prompts/general/code-quality/self-review-checklist.md
@~/.claude/prompts/general/code-quality/security-fundamentals.md

---

## Technology Stack

- **.NET 9.0** with C# 13
- **WinUI 3** (Windows App SDK 1.8)
- **Entity Framework Core 9.0** with SQLite
- **CommunityToolkit.Mvvm** for MVVM
- **WinUIEx** for borderless window support

---

## Development Guidelines

### Language Policy
All git content (commits, PRs, branches, code comments) MUST be in English.

### Git Operations
Bash commands preferred over MCP tools for git (more token-efficient).

See @~/.claude/prompts/general/git/commit-conventions.md for commit format.

**Project Scopes:** `domain`, `app`, `infra`, `ui`, `test`, `config`, `ci`

### Agent Workflow

| Agent | Purpose |
|-------|---------|
| backend-agent | Application/Infrastructure layer |
| ui-agent | WinUI 3 presentation layer |
| test-agent | Unit tests + failure analysis |
| git-agent | Commits and PRs |

**Flow:** Implementation agents → `dotnet test` → test-agent if failures → git-agent

---

## Code Quality

See shared prompts:
- @~/.claude/prompts/dotnet/clean-architecture/layer-separation.md
- @~/.claude/prompts/winui/mvvm/no-value-converters.md

**Project Rules:**
- No value converters - use explicit ViewModel boolean properties
- x:Bind over Binding
- 377+ tests must pass
- Keep it simple

---

## Architecture

### Project Structure

```
PomodoroTimeTracker.Domain        → Entities, repository interfaces
PomodoroTimeTracker.Application   → DTOs, services, IDispatcherTimer
PomodoroTimeTracker.Infrastructure→ EF Core, repositories, migrations
PomodoroTimeTracker.ViewModels    → All ViewModels (WinUI Class Library)
PomodoroTimeTracker.WinUI3        → XAML views, UI services
PomodoroTimeTracker.Tests         → Unit tests (377+)
```

### Key Services

| Service | Purpose |
|---------|---------|
| `IPomodoroSessionService` | Pomodoro session CRUD |
| `ITimeEntryService` | Manual time entry |
| `IStatisticsService` | Report statistics |
| `IClientService` | Client CRUD |
| `IProjectService` | Project CRUD |
| `IPomodoroSettingsService` | Settings management |

### DI Registration Pattern

```csharp
// Scoped: DbContext, Repositories, Services
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IPomodoroSessionService, PomodoroSessionService>();
services.AddScoped<ITimeEntryService, TimeEntryService>();

// Singleton: Timer ViewModels (maintain state)
services.AddSingleton<PomodoroViewModel>();
services.AddSingleton<RegularTimerViewModel>();
services.AddSingleton<StopWatchViewModel>();

// Transient: CRUD ViewModels, IDispatcherTimer
services.AddTransient<ClientListViewModel>();
services.AddTransient<IDispatcherTimer, DispatcherTimerWrapper>();
```

---

## Database Schema

### PomodoroSessions
| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| ProjectId | int? FK | → Projects |
| StartTime | DateTime | Indexed |
| EndTime | DateTime? | |
| DurationMinutes | int | Planned duration |
| IsCompleted | bool | |
| SessionType | enum | Work/ShortBreak/LongBreak |
| Objective | string(90) | |
| Notes | string(500)? | |

### TimeEntries
| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| ProjectId | int? FK | → Projects |
| Description | string(500) | |
| StartTime | DateTime | Indexed |
| EndTime | DateTime? | |
| DurationMinutes | int? | |
| CreatedAt | DateTime | |

### Other Entities
- **Client**: Id, Name (unique), Description, CreatedAt
- **Project**: Id, Name, ClientId (FK), Description, CreatedAt
- **PomodoroSettings**: Singleton with timer durations and audio settings

**Database Location:** `%LocalAppData%\PomodoroTimeTracker\pomodoro.db`

---

## Current Status

**Implemented:**
- Pomodoro timer with 4-cycle breaks and wrap-up period
- Regular timer and stopwatch
- Timer window (compact, always-on-top)
- Client/Project management
- Time Entry management
- Report view with filters
- 377 unit tests passing

**TODO:**
- Merge PomodoroSession into TimeEntry (simplify schema)
- Windows toast notifications
- Dashboard view
- Export data (CSV, JSON)

---

## EF Migrations

```bash
# Create migration
dotnet ef migrations add Name --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3

# Apply migrations
dotnet ef database update --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3
```

---

## Documentation

- [Features](docs/FEATURES.md) - Detailed feature documentation
- [Testing](docs/TESTING.md) - Manual testing checklist
- [Changelog](docs/CHANGELOG.md) - Development history

---

## References

- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [WinUIEx Library](https://github.com/dotMorten/WinUIEx)

---

**Last Updated:** 2025-12-05
