# Implement Feature Command

Implement a feature using specialized agents. This command orchestrates backend-agent, ui-agent, and test-agent to implement features in a structured way.

## Usage
```
/implement-feature <feature description>
```

## Workflow

You are the **orchestrator**. Follow these steps:

### Step 1: Analyze & Plan
First, analyze the feature request and create a plan:

1. **Identify affected layers:**
   - Domain (entities, enums, interfaces)
   - Application (DTOs, services)
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

### Step 4: UI Implementation
If the feature requires UI changes, spawn **ui-agent**:

```
Use Task tool with subagent_type="ui-agent"

Prompt should include:
- ViewModel properties/commands to add
- XAML changes needed
- Any bindings or event handlers
- Reminder: Leave changes unstaged
```

**Wait for completion before proceeding.**

### Step 5: Test Updates
Spawn **test-agent** to update/create tests:

```
Use Task tool with subagent_type="test-agent"

Prompt should include:
- What was implemented
- Which services/methods need tests
- Expected test coverage
- Reminder: Leave changes unstaged
```

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

## Error Handling

- If an agent reports an error, analyze and retry with more context
- If build fails, identify which layer caused it and spawn appropriate agent
- If tests fail, spawn test-agent first for analysis, then the appropriate fixing agent
- Maximum 2 retries per agent before escalating to user

## Example

User: `/implement-feature Add dark mode toggle to settings`

Orchestrator:
1. Plans: Need settings property, DTO update, ViewModel property, XAML toggle
2. Spawns backend-agent: Add IsDarkMode to PomodoroSettings, update DTOs
3. Creates migration
4. Spawns ui-agent: Add toggle to SettingsPage, bind to ViewModel
5. Spawns test-agent: Add tests for dark mode setting
6. Validates build and tests
7. Presents summary and offers to commit
