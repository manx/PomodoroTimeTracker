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

---

## Project Structure

```
PomodoroTimeTracker.Domain        → Entities, repository interfaces
PomodoroTimeTracker.Application   → DTOs, services, IDispatcherTimer
PomodoroTimeTracker.Infrastructure→ EF Core, repositories, migrations
PomodoroTimeTracker.ViewModels    → All ViewModels (testable, no UI deps)
PomodoroTimeTracker.WinUI3        → XAML views, UI services
PomodoroTimeTracker.Tests         → Unit tests
```

---

## Code Quality

**Project Rules:**
- No value converters - use explicit ViewModel boolean properties
- x:Bind over Binding
- All tests must pass before merging
- Keep it simple - minimum complexity for current task

---

## Current Status

**Implemented:**
- Pomodoro timer with 4-cycle breaks, wrap-up period, billable breaks
- Regular timer and stopwatch
- Timer window (compact, always-on-top, borderless)
- Client/Project/TimeEntry management
- Report view with daily/weekly/monthly/custom filters
- Unified TimeEntry + SessionTypes schema
- Settings with TabView (General, Pomodoro Timer, + placeholders)
- Configurable week settings (start day, ISO 8601 vs US standard)

**TODO:**
- Windows toast notifications
- Dashboard view
- Export data (CSV, JSON)

---

## Skills (loaded on-demand)

| Skill | Triggers |
|-------|----------|
| `architect` | Design, refactor, schema, architecture |
| `ef-core` | Migration, database, entity, repository |
| `agent-workflow` | Multi-agent, parallel, test failures |
| `winui-patterns` | ViewModel, XAML, binding, UI |
| `unit-test-specialist` | Tests, coverage, testable code |

---

## Documentation

- [Features](docs/FEATURES.md) - Detailed feature documentation
- [Testing](docs/TESTING.md) - Manual testing checklist
- [Changelog](docs/CHANGELOG.md) - Development history
