# Implement Feature Command

Implement a feature using specialized agents. This command orchestrates backend-agent, ui-agent, and test-agent to implement features in a structured way.

## Usage
```
/implement-feature <feature description>
/implement-feature --plan <plan-name>
/implement-feature --fast <feature description>
```

**Flags:**
- `--plan <name>` - Load existing plan from `docs/plans/<name>.md`
- `--fast` - Maximum parallelism (all agents at once)

## Zero-Interaction Workflow

You are the **orchestrator**. This workflow runs autonomously with no user approval needed.

### Default Workflow
```
[Analyze] → [Backend + UI] parallel → Migration → [Tests] parallel → Validate → PR
```

- **backend-agent:** All backend layers (Domain + Application + Infrastructure)
- **ui-agent:** ViewModels + Views together
- **test-agents:** Parallel agents for Service, ViewModel, Repository tests

### --fast Workflow (Maximum Parallelism)
```
[Analyze + Full Spec] → [Backend + UI + Tests] ALL parallel → Migration → Validate → PR
```

When `--fast` flag is used:
1. **Generate detailed spec** with exact interface signatures, method names, properties
2. **Spawn ALL THREE agents in single message:**
   - backend-agent: Complete backend (Domain + App + Infra)
   - ui-agent: ViewModels + Views
   - test-agent: All unit tests
3. **Each agent gets full spec** including what others are building
4. **Migration + Validate** after all complete

**Trade-off:** Faster, but more token waste if spec is wrong.

**Output:** PR URL (user reviews via GitHub)

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

### Step 1: Analyze

**If `--plan <name>` is provided:**
- Read plan from `docs/plans/<name>.md`
- Display: `## Implementing from plan: <name>`

**If `--fast` is provided:**
- Generate detailed spec with interface signatures (see --fast section below)

**Otherwise:**
Display brief plan and proceed:
```
## Implementing: <feature>
- Backend: <changes>
- UI: <changes>
- Tests: <what will be tested>
```

### Step 2: Implementation (Default)

Spawn TWO agents IN PARALLEL:

```
# Agent 1: Backend (all layers)
Use Task tool with subagent_type="backend-agent"
Prompt: Implement complete backend for [feature]:
- Domain: entities in Domain\Entities\, interfaces in Domain\Interfaces\
- Application: DTOs, service interface, service implementation
- Infrastructure: EF config, repository, DbContext, UnitOfWork
- Leave changes unstaged

# Agent 2: UI (ViewModels + Views)
Use Task tool with subagent_type="ui-agent"
Prompt: Implement UI for [feature]:
- ViewModels in ViewModels\ folder
- XAML pages in WinUI3\Views\
- Register in NavigationService and App.xaml.cs
- Use expected service interface names: I[Feature]Service
- Leave changes unstaged
```

### Step 2: Implementation (--fast)

Spawn ALL THREE agents IN PARALLEL with full spec:

```
# Agent 1: Backend
Use Task tool with subagent_type="backend-agent"
Prompt: [Full spec with interface signatures]
Implement complete backend. Other agents building: [list ViewModels, Views, tests]

# Agent 2: UI
Use Task tool with subagent_type="ui-agent"
Prompt: [Full spec with ViewModel properties]
Implement ViewModels + Views. Backend building: [list interfaces]

# Agent 3: Tests
Use Task tool with subagent_type="test-agent"
Prompt: [Full spec with class/method names]
Create all tests. Implementation building: [list classes to test]
```

### Step 3: Migration

If entity changes were made:

```bash
dotnet ef migrations add <MigrationName> \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.Infrastructure

dotnet ef database update \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.Infrastructure
```

### Step 4: Tests (Default workflow only)

After implementation, spawn test-agents IN PARALLEL for each layer:

```
# Agent 1: Service Tests
Use Task tool with subagent_type="test-agent"
Prompt: Create unit tests for [Feature]Service:
- Test in Tests\Application\ folder
- Mock repository and other dependencies
- Follow existing service test patterns

# Agent 2: ViewModel Tests
Use Task tool with subagent_type="test-agent"
Prompt: Create unit tests for [Feature]ViewModel:
- Test in Tests\ViewModels\ folder
- Mock IDispatcherTimer for timer tests
- Mock services and navigation
- Follow existing ViewModel test patterns

# Agent 3: Repository Tests (if new repository created)
Use Task tool with subagent_type="test-agent"
Prompt: Create unit tests for [Feature]Repository:
- Test in Tests\Infrastructure\ folder
- Use in-memory database
- Follow existing repository test patterns
```

**Note:** Only spawn repository test-agent if a new repository was created.

### Step 5: Build & Validate

```bash
dotnet build PomodoroTimeTracker.sln
dotnet test PomodoroTimeTracker.Tests
```

**If failures:** Spawn appropriate agent with error context, retry up to 3x.

### Step 6: Auto-Create PR
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

### Example 1: Default workflow
User: `/implement-feature Add notification service`

```
1. Analyze: "Implementing notification service"
2. Parallel (2 agents):
   - backend-agent → INotificationService, NotificationService, all layers
   - ui-agent → ViewModels + Views integration
3. Migration (if needed)
4. Tests parallel (2-3 agents):
   - test-agent → NotificationServiceTests
   - test-agent → ViewModelNotificationTests
5. Validate: build + test
6. PR: https://github.com/manx/PomodoroTimeTracker/pull/XX
```

### Example 2: --fast workflow
User: `/implement-feature --fast Add export feature`

```
1. Analyze + Generate full spec:
   - IExportService: ExportToCsvAsync(), ExportToJsonAsync()
   - ExportViewModel: properties, commands
   - Test classes: ExportServiceTests, ExportViewModelTests

2. Parallel (ALL 3 agents at once):
   - backend-agent → complete backend with spec
   - ui-agent → ViewModels + Views with spec
   - test-agent → all tests with spec

3. Migration + Validate
4. PR: https://github.com/manx/PomodoroTimeTracker/pull/XX
```

### Example 3: From plan
User: `/implement-feature --plan settings-rebuild`

```
1. Load plan from docs/plans/settings-rebuild.md
2. Follow plan phases with parallel agents where viable
3. Migration + Validate
4. PR
```
