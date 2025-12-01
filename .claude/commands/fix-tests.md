# Fix Tests Command

Analyze and fix failing tests using specialized agents.

## Usage
```
/fix-tests [optional: specific test filter]
```

## Workflow

### Step 1: Run Tests & Capture Failures
```bash
dotnet test PomodoroTimeTracker.Tests --verbosity normal
```

If all tests pass, report success and exit.

### Step 2: Analyze Failures
Spawn **test-agent** for analysis:

```
Use Task tool with subagent_type="test-agent"

Prompt:
Analyze these test failures and produce a structured report:
[paste test output]

Report format:
- Failed test count
- Suggested agent (backend-agent or ui-agent)
- Failure details with probable cause
- Suggested fixes
```

### Step 3: Fix Issues
Based on test-agent's analysis, spawn the appropriate agent:

**For Application/Infrastructure issues → backend-agent:**
```
Use Task tool with subagent_type="backend-agent"

Include:
- Test failure details
- Probable cause from analysis
- Files likely needing changes
```

**For UI/ViewModel issues → ui-agent:**
```
Use Task tool with subagent_type="ui-agent"

Include:
- Test failure details
- Probable cause from analysis
- Files likely needing changes
```

### Step 4: Validate Fix
Re-run tests:
```bash
dotnet test PomodoroTimeTracker.Tests --verbosity minimal
```

If still failing, loop back to Step 2 with new failure info.
Maximum 3 iterations before escalating to user.

### Step 5: Report
```markdown
## Tests Fixed

**Initially failing:** X tests
**Now passing:** All Y tests

**Changes made:**
- [file]: [what was fixed]

**Root cause:** [explanation]
```
