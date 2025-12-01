# Code Review Command

Review staged or unstaged changes using specialized agents.

## Usage
```
/code-review [staged|unstaged|all]
```

Default: `all` (both staged and unstaged)

## Workflow

### Step 1: Get Changes
```bash
# For staged changes
git diff --cached --name-only

# For unstaged changes
git diff --name-only

# Get full diff
git diff [--cached]
```

### Step 2: Categorize Files
Group changed files by layer:
- **Backend:** Domain/, Application/, Infrastructure/
- **UI:** WinUI3/ViewModels/, WinUI3/Views/
- **Tests:** Tests/

### Step 3: Parallel Review
Spawn agents in parallel based on changed files:

**If backend files changed → backend-agent:**
```
Review these backend changes for:
- Clean Architecture compliance
- Proper async/await usage
- CancellationToken propagation
- Logging best practices
- Security issues
```

**If UI files changed → ui-agent:**
```
Review these UI changes for:
- MVVM compliance (no UI in ViewModel)
- x:Bind usage (not Binding)
- No value converters (use explicit properties)
- Proper disposal of timers/subscriptions
```

**If test files changed → test-agent:**
```
Review these test changes for:
- AAA pattern
- Meaningful test names
- Edge case coverage
- No logic in tests
```

### Step 4: Compile Report
```markdown
## Code Review Report

### Summary
- Files reviewed: X
- Issues found: Y (Z critical)

### Backend Review
[Agent findings]

### UI Review
[Agent findings]

### Test Review
[Agent findings]

### Recommendations
1. [Priority fixes]
2. [Nice to have]
```

### Step 5: Optionally Fix Issues
Ask user:
> Found X issues. Would you like me to fix them?

If yes, spawn appropriate agents to fix issues.
