# Implement Feature Command

Implement a feature using specialized agents. This command orchestrates backend-agent, ui-agent, and test-agent to implement features in a structured way.

## Usage
```
/implement-feature <feature description>
```

## Optimized Workflow

You are the **orchestrator**. This workflow maximizes parallelization:

```
Backend → ViewModels → ┬─ Views (ui-agent)      ← parallel
                       └─ Tests (test-agent)    ← parallel
```

### Step 1: Analyze & Plan
First, analyze the feature request and create a plan:

1. **Identify affected layers:**
   - Domain (entities, enums, interfaces)
   - Application (DTOs, services, IDispatcherTimer if timers needed)
   - Infrastructure (repositories, migrations)
   - UI (ViewModels, XAML pages)
   - Tests

2. **Break down into tasks** for each agent
3. **Identify dependencies** between tasks (order matters!)
4. **Present the plan** to the user for approval before proceeding

### Step 2: Backend Implementation
If the feature requires backend changes, spawn **backend-agent**:

```
Use Task tool with subagent_type="backend-agent"

Prompt should include:
- Specific files to modify/create
- Properties/methods to add
- Any test failure context if retrying
- Reminder: Leave changes unstaged
```

**Wait for completion before proceeding.**

### Step 3: Database Migration
If entity changes were made, create migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.WinUI3

dotnet ef database update \
  --project PomodoroTimeTracker.Infrastructure \
  --startup-project PomodoroTimeTracker.WinUI3
```

### Step 4: ViewModels First (Critical for Parallelization)

Create ViewModels BEFORE Views to enable parallel execution:

```
Use Task tool with subagent_type="ui-agent"

Prompt should include:
- Create ONLY ViewModels (no XAML yet)
- Define all properties, commands, and public interface
- Use IDispatcherTimer from Application layer for any timers
- Implement business logic
- Reminder: Leave changes unstaged
```

**Wait for ViewModel completion before Step 5.**

### Step 5: Parallel Execution - Views + Tests

**IMPORTANT:** Run these TWO agents IN PARALLEL using a single message with multiple Task tool calls:

```
# Agent 1: Views
Use Task tool with subagent_type="ui-agent"
Prompt: Create XAML pages and code-behind for [feature].
ViewModels are already created. Bind to existing ViewModel properties.

# Agent 2: Tests
Use Task tool with subagent_type="test-agent"
Prompt: Create unit tests for [feature] services and ViewModels.
ViewModels use IDispatcherTimer which can be mocked.
```

Both agents run simultaneously, reducing total implementation time.

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

### Step 7: Summary & Commit (Optional)
Present summary to user:

```markdown
## Implementation Complete

**Feature:** [description]

**Changes by layer:**
- Domain: [files]
- Application: [files]
- Infrastructure: [files]
- UI: [files]
- Tests: [files]

**Test Results:** X tests passing

Would you like me to commit these changes?
```

If user wants to commit, spawn **git-agent**:

```
Use Task tool with subagent_type="git-agent"

Prompt: Create a commit for the implemented feature with appropriate message.
```

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
- Maximum 2 retries per agent before escalating to user

## Example

User: `/implement-feature Add time entry tracking`

Orchestrator:
1. **Plan:** Backend exists, need ViewModels, Views, Tests
2. **ViewModels first:** Spawn ui-agent for TimeEntryListViewModel, TimeEntryDetailViewModel
3. **Parallel execution:**
   - Spawn ui-agent: Create TimeEntryListPage.xaml, TimeEntryDetailPage.xaml
   - Spawn test-agent: Create TimeEntryService tests (in same message!)
4. **Validate:** Build and run tests
5. **Summary:** Present results, offer to commit
