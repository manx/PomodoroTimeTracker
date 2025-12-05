---
name: architect
description: Clean Architecture design guidance for the Pomodoro Time Tracker. Activates for design decisions, refactoring, schema changes, or architectural questions.
allowed-tools:
  - Read
  - Glob
  - Grep
---

# Architecture Skill

**Activates when:** Design, architecture, refactor, schema, or layer separation mentioned.

## Shared Architecture Guidelines

@~/.claude/prompts/dotnet/clean-architecture/layer-separation.md
@~/.claude/prompts/dotnet/clean-architecture/design-decisions.md
@~/.claude/prompts/dotnet/clean-architecture/refactoring-patterns.md

---

## Project Architecture

### Layer Structure

```
Domain        → Entities, Enums, Repository Interfaces (NO dependencies)
Application   → DTOs, Services, IDispatcherTimer (depends on Domain)
Infrastructure→ EF Core, Repositories, Migrations (depends on Domain+App)
ViewModels    → MVVM ViewModels (depends on Application) - WinUI Class Library
WinUI3        → XAML, UI Services (depends on ViewModels)
```

### Key Entities

| Entity | Purpose |
|--------|---------|
| `TimeEntry` | Unified time tracking (all session types) |
| `SessionType` | Lookup: Work, ShortBreak, LongBreak, Regular, StopWatch, Manual |
| `Client` | Client management |
| `Project` | Project with optional client FK |
| `PomodoroSettings` | Singleton settings |

### Service Responsibilities

| Service | Responsibility |
|---------|----------------|
| `ITimeEntryService` | Timer + manual entry CRUD |
| `IStatisticsService` | Report aggregation |
| `IClientService` | Client CRUD |
| `IProjectService` | Project CRUD with client filtering |
| `IPomodoroSettingsService` | Settings management |

### ViewModel Patterns

```csharp
// Timer ViewModels - Singleton (maintain state)
services.AddSingleton<PomodoroViewModel>();

// CRUD ViewModels - Transient (fresh per navigation)
services.AddTransient<ClientDetailViewModel>();
```

### Decision Records

1. **No value converters** - Use explicit bool properties
2. **IDispatcherTimer** - Abstraction for timer testability
3. **Unified TimeEntry** - Merged PomodoroSession into TimeEntry
4. **SessionType lookup** - Entity instead of enum for extensibility
