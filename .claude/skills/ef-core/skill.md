---
name: ef-core
description: Entity Framework Core patterns for the Pomodoro Time Tracker. Activates for migrations, database operations, entity configuration, or repository work.
allowed-tools:
  - Read
  - Glob
  - Grep
  - Bash
---

# EF Core Skill

**Activates when:** Migration, database, entity, repository, or EF Core mentioned.

## Shared EF Core Guidelines

@~/.claude/prompts/dotnet/ef-core/migration-commands.md
@~/.claude/prompts/dotnet/ef-core/entity-configuration.md
@~/.claude/prompts/dotnet/ef-core/configuration-pattern.md

---

## Project Database

### Location
```
%LocalAppData%\PomodoroTimeTracker\pomodoro.db
```

### Migration Commands

```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.WinUI3

# Apply
dotnet ef database update \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.WinUI3

# Remove (if not applied)
dotnet ef migrations remove \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.WinUI3
```

### Current Schema

**SessionTypes** (Lookup - seeded)
- Work, ShortBreak, LongBreak, Regular, StopWatch, Manual

**TimeEntries** (Unified)
- SessionTypeId FK → SessionTypes
- ProjectId FK (nullable) → Projects
- Description, StartTime, EndTime, DurationMinutes
- IsCompleted (nullable), IsBillable, Notes

**Projects**
- ClientId FK (nullable) → Clients
- Unique Name per Client

**Clients**
- Unique Name

**PomodoroSettings** (Singleton, Id=1)

### Repository Pattern

```csharp
public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<TimeEntry?> GetActiveEntryByTypeAsync(int sessionTypeId);
}
```

### Cascade Delete Rules
- Client deleted → Projects.ClientId = NULL
- Project deleted → TimeEntries.ProjectId = NULL
- SessionType → Restrict (never delete)
