# Implement Feature Command

Implement a feature using specialized agents. This command orchestrates backend-agent, ui-agent, and test-agent to implement features in a structured way.

## Usage
```
/implement-feature <feature description>
/implement-feature --plan <plan-name>
```

## Zero-Interaction Workflow

You are the **orchestrator**. This workflow runs autonomously with no user approval needed:

```
[Analyze] → Domain → [App + Infra + ViewModels] (parallel) → Migration → [Views + Tests] (parallel) → Validate → PR
```

**Output:** PR URL (user reviews via GitHub)

**Parallelization Strategy:**
- **Wave 1:** After Domain, spawn Application, Infrastructure, and ViewModels agents in parallel
- **Wave 2:** After ViewModels complete, spawn Views and Tests agents in parallel
- **Multiple backend-agents:** Split backend work by layer (Domain, Application, Infrastructure) when viable

## Agents vs Skills

**Prefer agents** for implementation work - they run autonomously and handle complex tasks.

**Use skills as fallback** when:
- The orchestrator needs to perform work directly (not delegating to agent)
- Quick fixes or small changes that don't warrant spawning an agent
- Agent isn't available for the specific task

| Skill | When to Load (if not using agent) |
|-------|-----------------------------------|
| `ef-core` | Migrations, entity configuration, repository work |
| `architect` | Design decisions, refactoring, architectural questions |
| `winui-patterns` | ViewModels, XAML pages, UI bindings |
| `unit-test-specialist` | Writing tests, improving coverage |
| `agent-workflow` | Complex multi-agent coordination issues |

### Step 1: Load Plan or Analyze

**If `--plan <name>` is provided:**
1. Read the plan file from `docs/plans/<name>.md`
2. Display: `## Implementing from plan: <name>`
3. Use the plan's phases/tasks as the implementation guide
4. **Skip analysis** - proceed directly to Step 2

**Otherwise (no --plan flag):**
Analyze the feature request and display a brief plan, then **proceed immediately**:

1. **Identify affected layers:**
   - Domain (entities, enums, interfaces)
   - Application (DTOs, services, IDispatcherTimer if timers needed)
   - Infrastructure (repositories, migrations)
   - UI (ViewModels, XAML pages)
   - Tests

2. **Display concise plan** (no approval needed):
```
## Implementing: <feature>
- Backend: <changes>
- UI: <changes>
- Tests: <what will be tested>
```

3. **Proceed immediately** - do not wait for user confirmation

### Step 2: Domain Layer (Foundation)

Domain must complete first - it defines entities and interfaces that other layers depend on:

```
Use Task tool with subagent_type="backend-agent"

Prompt: Implement Domain layer for [feature]:
- Create entities in Domain\Entities\
- Create repository interfaces in Domain\Interfaces\
- Update IUnitOfWork if needed
- Leave changes unstaged
```

**Wait for Domain completion before Step 3.**

### Step 3: Parallel Wave 1 - Application + Infrastructure + ViewModels

Spawn THREE agents IN PARALLEL using a single message with multiple Task tool calls:

```
# Agent 1: Application Layer
Use Task tool with subagent_type="backend-agent"
Prompt: Implement Application layer for [feature]:
- Create DTOs in Application\DTOs\
- Create service interface in Application\Interfaces\
- Create service implementation in Application\Services\
- Domain entities are already created
- Leave changes unstaged

# Agent 2: Infrastructure Layer
Use Task tool with subagent_type="backend-agent"
Prompt: Implement Infrastructure layer for [feature]:
- Create EF configuration in Infrastructure\Configurations\
- Create repository in Infrastructure\Repositories\
- Add DbSet to ApplicationDbContext
- Update UnitOfWork
- Domain entities/interfaces are already created
- Leave changes unstaged

# Agent 3: ViewModels
Use Task tool with subagent_type="ui-agent"
Prompt: Create ViewModels for [feature]:
- Create ViewModels in ViewModels\ folder
- Define all properties, commands, public interface
- Use IDispatcherTimer for any timers
- Service interfaces are being created in parallel - use expected interface names
- Leave changes unstaged
```

All three agents run simultaneously.

### Step 4: Database Migration

After Infrastructure agent completes, create migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.Infrastructure

dotnet ef database update \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.Infrastructure
```

**Note:** Use Infrastructure as startup project (has IDesignTimeDbContextFactory).
**If doing manually:** Load `ef-core` skill for best practices.

### Step 5: Parallel Wave 2 - Views + Tests

After ViewModels complete, spawn TWO agents IN PARALLEL:

```
# Agent 1: Views
Use Task tool with subagent_type="ui-agent"
Prompt: Create XAML pages for [feature]:
- Create pages in WinUI3\Views\
- ViewModels are already created - bind to existing properties
- Register in NavigationService and App.xaml.cs
- Leave changes unstaged

# Agent 2: Tests
Use Task tool with subagent_type="test-agent"
Prompt: Create unit tests for [feature]:
- Test services, repositories, and ViewModels
- ViewModels use IDispatcherTimer which can be mocked
- Follow existing test patterns in Tests\ folder
- Leave changes unstaged
```

Both agents run simultaneously.

### Step 6: Build & Validate
Run build and tests to validate:

```bash
dotnet build PomodoroTimeTracker.sln
dotnet test PomodoroTimeTracker.Tests
```

**If tests fail:**
1. Analyze the failure
2. Determine which agent should fix it (backend-agent or ui-agent)
3. Spawn that agent with the failure context
4. Repeat validation

### Step 7: Auto-Create PR
**Automatically** create branch, commit, push, and PR:

1. **Create feature branch:**
```bash
git checkout -b feat/<feature-slug>
```

2. **Stage and commit all changes:**
```bash
git add -A
git commit -m "feat(<scope>): <description>"
```

3. **Push and create PR:**
```bash
git push -u origin feat/<feature-slug>
gh pr create --title "feat(<scope>): <description>" --body "## Summary
- <bullet points of changes>

## Test plan
- [ ] <testing checklist>"
```

4. **Output PR URL** as final result

## Agent Responsibilities

| Agent | Responsibility |
|-------|---------------|
| backend-agent | Domain entities, DTOs, Services, Repositories |
| ui-agent | ViewModels, XAML pages, UI services |
| test-agent | Unit tests, integration tests |
| git-agent | Git commits, branches, PRs |

## Testability Guidelines

To ensure ViewModels are testable:

1. **Use IDispatcherTimer** from `PomodoroTimeTracker.Application.Interfaces` for timers
2. **Inject dependencies** via constructor (services, navigation, dialog, timer)
3. **No direct DispatcherQueue usage** in ViewModels

Example testable ViewModel:
```csharp
public class MyViewModel : ViewModelBase
{
    private readonly IMyService _service;
    private readonly IDispatcherTimer _timer;

    public MyViewModel(IMyService service, IDispatcherTimer timer)
    {
        _service = service;
        _timer = timer;
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTick;
    }
}
```

## Error Handling

- If an agent reports an error, analyze and retry with more context
- If build fails, identify which layer caused it and spawn appropriate agent
- If tests fail, spawn test-agent first for analysis, then the appropriate fixing agent
- Maximum 3 retries per agent before escalating to user
- Only escalate if truly blocked (e.g., ambiguous requirements, external dependencies)

## Examples

### Example 1: Direct feature request
User: `/implement-feature Add time entry tracking`

Orchestrator:
1. **Analyze:** Display "Implementing: time entry tracking" with layer breakdown
2. **Domain:** Spawn backend-agent → TimeEntry entity, ITimeEntryRepository
3. **Wave 1 (parallel):** Single message with 3 Task calls:
   - backend-agent → TimeEntryService, DTOs
   - backend-agent → TimeEntryRepository, EF config
   - ui-agent → TimeEntryListViewModel, TimeEntryDetailViewModel
4. **Migration:** Create and apply migration
5. **Wave 2 (parallel):** Single message with 2 Task calls:
   - ui-agent → TimeEntryListPage.xaml, TimeEntryDetailPage.xaml
   - test-agent → Service, repository, ViewModel tests
6. **Validate:** Build and run tests (auto-retry up to 3x if failures)
7. **Auto-PR:** Create branch, commit, push, create PR
8. **Output:** `https://github.com/manx/PomodoroTimeTracker/pull/XX`

### Example 2: From existing plan
User: `/implement-feature --plan settings-rebuild`

Orchestrator:
1. **Load plan:** Read `docs/plans/settings-rebuild.md`
2. **Domain:** Spawn backend-agent for Phase 1 (entities, interfaces)
3. **Wave 1 (parallel):** Phases 2-4 in parallel where viable:
   - backend-agent → Application layer (services, DTOs)
   - backend-agent → Infrastructure layer (repos, EF config)
   - ui-agent → ViewModels
4. **Migration:** Create and apply migration
5. **Wave 2 (parallel):** Phases 5-6 in parallel:
   - ui-agent → Views
   - test-agent → All tests
6. **Validate:** Build and run tests
7. **Auto-PR:** Create branch, commit, push, create PR
8. **Output:** `https://github.com/manx/PomodoroTimeTracker/pull/XX`
