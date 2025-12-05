---
name: agent-workflow
description: Agent orchestration patterns for the Pomodoro Time Tracker. Activates for multi-agent tasks, test failures, or complex implementations.
allowed-tools:
  - Read
  - Glob
  - Grep
  - Task
---

# Agent Workflow Skill

**Activates when:** Multi-agent, parallel, test failure, or complex implementation mentioned.

## Shared Agent Guidelines

@~/.claude/prompts/agents/orchestration/agent-workflow.md

---

## Project Agents

| Agent | Purpose | Model |
|-------|---------|-------|
| `backend-agent` | Application/Infrastructure layer | sonnet |
| `ui-agent` | WinUI 3 ViewModels/XAML | sonnet |
| `test-agent` | Test analysis + fixes | sonnet |
| `git-agent` | Commits and PRs | haiku |

## Workflow Patterns

### Simple Feature (single layer)
```
1. Implement with appropriate agent
2. dotnet test
3. git-agent to commit
```

### Cross-Layer Feature
```
1. backend-agent + ui-agent (parallel)
2. dotnet test
3. If failures → test-agent
4. Fix with appropriate agent
5. Repeat until green
6. git-agent to commit
```

### Test Failure Loop
```
1. Run dotnet test
2. If failures:
   a. Spawn test-agent with error output
   b. test-agent analyzes and identifies layer
   c. Spawn backend-agent or ui-agent with fix instructions
   d. Run tests again
3. Repeat until all pass
```

## Agent Selection

| Task Type | Agent |
|-----------|-------|
| Service, Repository, DTO | backend-agent |
| ViewModel, XAML, UI Service | ui-agent |
| Test analysis, coverage | test-agent |
| Commit, PR, git ops | git-agent |

## Communication Rules

1. **Agents don't commit** - Leave changes unstaged
2. **Report what was done** - Clear summary of changes
3. **Include file paths** - List all modified files
4. **Test failures first** - Fix before new features
