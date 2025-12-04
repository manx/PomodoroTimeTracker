# Git Strategy - Pomodoro Time Tracker

Detta dokument beskriver git-strategin för Pomodoro Time Tracker-projektet, inklusive branching-modell, commit-konventioner, automatisering och workflow.

---

## 📊 Current Status: GitHub Flow ✨

**IMPLEMENTERAT:** 2025-01-25

Projektet använder nu **GitHub Flow** - en modern, professionell branch-strategi:

```
master (protected, always production-ready)
  ↑
  ├─ feature/add-statistics ─────> Pull Request → Squash and merge
  ├─ fix/timer-bug ──────────────> Pull Request → Squash and merge
  └─ refactor/cleanup-services ──> Pull Request → Squash and merge
```

### ✅ Implemented Features

- ✅ **Branch Protection** på master (setup instructions i `.github/BRANCH_PROTECTION_SETUP.md`)
- ✅ **Pull Request Template** (`.github/PULL_REQUEST_TEMPLATE.md`)
- ✅ **CI/CD Status Checks** (code-quality + build + tests)
- ✅ **Code Coverage Reporting** (Codecov integration)
- ✅ **Pre-commit Hooks** (Husky.Net för automatisk formatering)
- ✅ **Conventional Commits** format
- ✅ **Tests MUST Pass** (continue-on-error borttagen)

### 🎯 Workflow Rules

1. **Alla ändringar via Pull Requests** - Inga direkta commits till master
2. **CI/CD måste vara grön** - Tests och code quality checks måste passera
3. **Squash and merge** - Håller master-historiken ren
4. **Feature branches** - Använd beskrivande namn (`feature/`, `fix/`, `refactor/`, etc.)
5. **Delete branch efter merge** - Håller repository rent
6. **English only** - Alla commits, PR-titlar och PR-beskrivningar ska vara på engelska

---

## 🚀 GitHub Flow Workflow

### 1. Starting New Work

```bash
# Uppdatera master
git checkout master
git pull origin master

# Skapa feature branch
git checkout -b feature/add-statistics-page

# Alternativa branch-typer:
# feature/  - Ny funktionalitet
# fix/      - Buggfix
# refactor/ - Kod-omstrukturering
# test/     - Lägga till tester
# docs/     - Dokumentation
# chore/    - Build, dependencies, CI/CD
```

### 2. Working on Feature

```bash
# Gör ändringar...

git add .
git commit -m "feat: add statistics data models"
# Husky pre-commit hook formaterar automatiskt

# Fortsätt arbeta...
git commit -m "feat: implement statistics service"
git commit -m "test: add unit tests for statistics service"
git commit -m "docs: update README with statistics feature"
```

### 3. Pushing to Remote

```bash
# Första push (skapa remote branch)
git push -u origin feature/add-statistics-page

# Efterföljande pushes
git push
```

### 4. Creating Pull Request

**Via GitHub UI:**

1. Gå till repository på GitHub
2. "Compare & pull request" button visas automatiskt
3. **Fyll i PR template:**
   - Beskrivning av ändringen
   - Type of change (bug fix, feature, etc.)
   - Test instructions
   - Screenshots (för UI-ändringar)
   - Checklist
4. **Create pull request**

**PR kommer automatiskt att:**
- ✅ Köra CI/CD pipeline
- ✅ Visa code-quality check status
- ✅ Visa build + test status
- ✅ Visa code coverage report
- ✅ Blocka merge om något failar

### 5. CI/CD Pipeline Körs

**Automatiskt när PR skapas/uppdateras:**

```
GitHub Actions startar:

JOB 1: Code Quality Checks
├─ Checkout code
├─ Setup .NET 9
├─ Restore dependencies
└─ Verify code formatting (dotnet format --verify-no-changes)
   └─ ✅ PASS eller ❌ FAIL

JOB 2: Build and Test (körs om JOB 1 lyckas)
├─ Checkout code
├─ Setup .NET 9
├─ Restore dependencies
├─ Build solution (Release)
├─ Run tests with coverage
│  └─ 377 tests måste passera, annars ❌ FAIL
├─ Upload coverage to Codecov
└─ Upload build artifacts
   └─ ✅ PASS eller ❌ FAIL
```

**Om något failar:**
- ❌ Merge button blockeras
- ⚠️ PR visar "Merging is blocked"
- 🔍 Kolla workflow logs för felbeskrivning
- 🛠️ Fixa, committa, pusha → CI körs igen

### 6. Review Process

**För solo-utvecklare:**
- 👀 Self-review: Kolla diff, läs koden igen
- ✅ Verifiera att alla checklist items är bockade
- ✅ Säkerställ att CI/CD är grön
- ✅ Kolla code coverage-rapporten

**För team:**
- 👥 Begär review från teammedlem
- 💬 Diskutera i kommentarer
- ✅ Reviewer approvar
- 🔀 Merge

### 7. Merging PR

**Via GitHub UI:**

1. **Squash and merge** (rekommenderat)
   - Alla commits blir EN commit på master
   - Ren historik
   - Lätt att revertera hela featuren

2. Edit commit message:
   ```
   feat: add statistics page (#42)

   - Added statistics data models
   - Implemented statistics service
   - Created statistics UI
   - Added comprehensive unit tests

   Total: 15 new tests, 100% coverage
   ```

3. **Confirm squash and merge**

4. **Delete branch** (GitHub frågar automatiskt)

### 8. Cleanup Lokalt

```bash
# Byt till master
git checkout master

# Hämta latest från remote
git pull origin master

# Ta bort merged branch
git branch -d feature/add-statistics-page

# Om branchen inte är fully merged (force delete)
git branch -D feature/add-statistics-page

# Lista alla lokala branches
git branch

# Lista även remote branches
git branch -a

# Rensa gamla remote-tracking branches
git fetch --prune
```

---

## 📝 Commit Conventions

**IMPORTANT: All commits, PR titles, and PR descriptions MUST be in English.**

### Language Policy

- ✅ **Commit messages:** English only
- ✅ **PR titles:** English only
- ✅ **PR descriptions:** English only
- ✅ **Branch names:** English only
- ✅ **Code comments:** English only
- ℹ️ **Documentation (CLAUDE.md, GIT_STRATEGY.md):** Swedish is OK (internal team docs)
- ℹ️ **Conversation/discussion:** Swedish is OK

**Rationale:**
- Industry standard (global collaboration)
- Better tooling support (CI/CD, GitHub Actions)
- Future-proof (if project goes open source or team grows)
- Consistency with code (which is in English)

### Conventional Commits Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

| Type | Beskrivning | När använda | Exempel |
|------|-------------|-------------|---------|
| `feat` | Ny feature | Lägga till funktionalitet | `feat: add wrap up period to timer` |
| `fix` | Buggfix | Fixa något som inte fungerar | `fix: timer not stopping at zero` |
| `refactor` | Refactoring | Omstrukturering utan funktionell förändring | `refactor: extract timer logic to service` |
| `test` | Tester | Lägga till/ändra tester | `test: add integration tests for repositories` |
| `docs` | Dokumentation | README, kommentarer, etc. | `docs: update API documentation` |
| `style` | Formatering | Whitespace, formatting, etc. | `style: apply dotnet format` |
| `chore` | Build/tools | Dependencies, build, CI/CD | `chore: update NuGet packages` |
| `perf` | Performance | Optimeringar | `perf: optimize database queries` |
| `ci` | CI/CD | GitHub Actions, etc. | `ci: add code coverage reporting` |
| `build` | Build system | MSBuild, solution files | `build: update target framework` |
| `revert` | Revert | Ångra tidigare commit | `revert: revert feat: add statistics` |

### Scope (optional)

Område som påverkas:

- `(ui)` - User interface
- `(timer)` - Timer functionality
- `(settings)` - Settings
- `(db)` - Database/migrations
- `(tests)` - Test suite
- `(api)` - API layer
- `(core)` - Core business logic

### Subject Line Rules

✅ **DO:**
```bash
feat: add statistics dashboard
fix: resolve timer stopping issue
refactor(ui): simplify timer window layout
test: add repository integration tests
```

❌ **DON'T:**
```bash
feat: Added new feature.        # Använd imperativ, ingen punkt
Fix bug                         # Använd lowercase efter type
Updated some files              # Inte specific enough
WIP                             # Use draft PR instead
asdf                            # 🤦‍♂️
```

### Subject Rules:
1. **Imperativ mood**: "add" inte "added" eller "adds"
2. **Lowercase efter colon**: `feat: add` inte `feat: Add`
3. **Ingen punkt i slutet**
4. **Max 72 tecken**
5. **Tydlig och koncis**

### Body (optional men rekommenderat)

**När att inkludera body:**
- Ändringen behöver kontext (WHY, inte WHAT)
- Breaking changes
- Komplexa ändringar som behöver förklaring

**Format:**
- Wrappa vid 72 tecken
- Bullet points tillåtna
- Separera med blank line från subject

**Exempel:**

```
feat: add comprehensive unit tests for application layer

This commit introduces a complete test suite for the application layer:

- Created PomodoroTimeTracker.Tests project
- Added 82 service layer tests (mock-based unit tests)
- Added 62 repository layer tests (InMemory DbContext integration)
- Total: 144 tests with 100% pass rate

Testing approach:
- Service tests use Moq for fast, isolated testing
- Repository tests use EF InMemory for realistic data access testing
- All tests follow AAA pattern (Arrange-Act-Assert)
- FluentAssertions for readable assertions

ViewModels excluded due to WinUI 3 DispatcherQueue requirements.
All business logic is fully tested via service layer.
```

### Footer (optional)

**Breaking Changes:**
```
BREAKING CHANGE: Timer API now requires DurationMinutes instead of Seconds
```

**Issue References:**
```
Fixes #123
Closes #456
Related to #789
```

**Co-authors:**
```
Co-Authored-By: Claude <noreply@anthropic.com>
```

**Complete Example:**

```
feat: add wrap up period feature to Pomodoro timer

The wrap up period allows users to finish their current thought after
the work period ends without counting as overtime.

Implementation:
- Added WrapUpPeriodMinutes setting (default: 3 minutes)
- Added WrapUpState to timer state machine
- Gentle notification when work period ends
- Main alarm when wrap up period expires
- UI shows wrap up countdown with info message

Total session time = Work Duration + Wrap Up Period
Session records intended work duration (e.g., 25 min)

Fixes #42

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## 🔒 Branch Protection Rules

**STATUS:** ✅ Ready to implement

**Setup Instructions:** Se `.github/BRANCH_PROTECTION_SETUP.md`

### Configured Protections (Rekommenderat)

#### ✅ Require Pull Request

- **Approvals required:** 0 (solo) eller 1+ (team)
- **Dismiss stale reviews:** Ja
- **Effect:** Inga direkta commits till master

#### ✅ Require Status Checks

- **Required checks:**
  - `code-quality` (dotnet format verification)
  - `build` (build + tests)
- **Require up-to-date:** Ja
- **Effect:** CI/CD måste vara grön

#### ✅ Require Linear History

- **Effect:** Endast "Squash and merge" tillåts
- **Result:** Ren, linjär historik

#### ✅ Include Administrators

- **Effect:** Även du måste följa reglerna
- **Result:** Ingen kan fuska

#### ❌ No Force Push

- **Effect:** Skyddar historiken
- **Result:** Dataförlust prevention

#### ❌ No Delete

- **Effect:** Förhindrar oavsiktlig deletion
- **Result:** Extra säkerhet

### Verifiera Branch Protection

Efter aktivering:

```bash
# Detta ska FAILA med protection error:
git checkout master
echo "test" > test.txt
git add test.txt
git commit -m "test: direct push"
git push origin master

# Förväntat resultat:
# remote: error: GH006: Protected branch update failed
# remote: error: Changes must be made through a pull request
```

✅ Om du får detta error = Branch protection fungerar!

---

## 🤖 Automation

### Husky.Net Pre-Commit Hook

**Konfiguration:** `.husky/task-runner.json`

**Tasks som körs vid varje commit:**

1. **format-code**
   ```bash
   dotnet format PomodoroTimeTracker.sln --no-restore --verbosity minimal
   ```
   - Formaterar all kod enligt projektets style
   - Fixes whitespace, indentation, naming, etc.

2. **stage-all-changes**
   ```bash
   git add -u
   ```
   - Stages automatiskt formaterade filer
   - Säkerställer att formatting är commitad

**Flow:**

```bash
git commit -m "feat: add feature"
  ↓
Husky pre-commit hook triggers
  ↓
dotnet format körs
  ↓
Formaterade filer stages
  ↓
Commit skapas med formaterad kod
  ↓
✅ Commit klar
```

**Fördelar:**
- ✅ Konsistent kodstil i hela projektet
- ✅ Ingen manuell formatering behövs
- ✅ Mindre noise i code reviews
- ✅ Autofix:ar de flesta CA-analyzer warnings

**Disable hook temporary (vid behov):**

```bash
# Skippa hooks för EN commit
git commit -m "message" --no-verify

# Inaktivera Husky helt (inte rekommenderat)
# Radera eller rename .husky-mappen
```

### GitHub Actions CI/CD

**Konfiguration:** `.github/workflows/ci.yml`

**Triggers:**
- ✅ Push till `master`
- ✅ Pull requests till `master`
- ✅ Manual dispatch (via GitHub UI)

**Pipeline:**

```yaml
name: CI Build

env:
  DOTNET_VERSION: '9.0.x'
  BUILD_CONFIGURATION: Release

jobs:
  code-quality:
    runs-on: windows-latest
    steps:
      - Checkout code
      - Setup .NET 9
      - Restore dependencies
      - Verify formatting (dotnet format --verify-no-changes)
      # ❌ Fails if code not formatted

  build:
    needs: code-quality
    runs-on: windows-latest
    steps:
      - Checkout code
      - Setup .NET 9
      - Restore dependencies
      - Build solution (Release)
      - Run tests with coverage
        # ❌ Fails if ANY test fails (no continue-on-error)
      - Upload coverage to Codecov
      - Upload build artifacts (retention: 30 days)
```

**Test Execution:**

```bash
# 377 tests körs:
# ├─ ViewModel layer: 158 tests
# │  ├─ PomodoroViewModelTests: ~60
# │  ├─ RegularTimerViewModelTests: ~40
# │  ├─ StopWatchViewModelTests: ~30
# │  └─ Other ViewModel tests: ~28
# ├─ Application layer: 148 tests
# │  ├─ Service tests: ~110
# │  └─ AudioService, TimeEntry, etc.
# └─ Infrastructure layer: 71 tests
#    └─ Repository tests

# Om NÅGON test failar → ❌ Pipeline failar → ⛔ Merge blockeras
```

**Code Coverage:**

- Codecov integration aktiverad
- Coverage badges tillgängliga
- Trend tracking över tid
- PR comments med coverage diff

**Setup Codecov:**

1. Gå till [codecov.io](https://codecov.io/)
2. Logga in med GitHub
3. Lägg till repository
4. Kopiera token
5. Lägg till som secret i GitHub:
   - Settings → Secrets and variables → Actions
   - New repository secret: `CODECOV_TOKEN`

---

## 📋 Pull Request Template

**Fil:** `.github/PULL_REQUEST_TEMPLATE.md`

**Innehåller:**

- 📋 Beskrivning av ändringen
- 🔧 Type of change (bug, feature, refactor, etc.)
- 🧪 Test instructions
- 📸 Screenshots/video (för UI)
- 📝 Checklist (code style, tests, docs, etc.)
- 🔗 Related issues
- 📚 Additional context

**Automatiskt ifylld när PR skapas:**

Template guidar dig att fylla i all relevant information, vilket gör:
- ✅ Self-review enklare
- ✅ Code review effektivare
- ✅ Dokumentation bättre
- ✅ Historiken tydligare

---

## 🎯 Best Practices

### Commit Frequency

✅ **DO: Commit ofta, små ändringar**

```bash
# Good: Logiska, små commits
git commit -m "feat: add statistics DTO"
git commit -m "feat: add statistics service interface"
git commit -m "feat: implement GetDailyStats method"
git commit -m "test: add unit tests for GetDailyStats"
```

**Fördelar:**
- Lätt att hitta när buggar introducerades
- Lätt att revertera specifik ändring
- Tydlig progression av arbete
- Bättre git bisect support

❌ **DON'T: Gigantiska commits**

```bash
# Bad: Allt i en commit
git commit -m "Added statistics feature"
# 50 files changed, 2000+ lines
```

**Problem:**
- Svårt att review:a
- Omöjligt att revertera delar
- Svårt att förstå vad som hänt

### Branch Naming

✅ **DO: Beskrivande, strukturerade namn**

```bash
feature/add-time-entry-view
fix/timer-not-stopping
refactor/extract-timer-service
test/add-viewmodel-tests
docs/update-readme
chore/update-dependencies
perf/optimize-db-queries
```

**Pattern:** `<type>/<short-description-with-dashes>`

❌ **DON'T: Vaga eller personliga namn**

```bash
test              # Vad testas?
johns-branch      # Vad gör den?
fix               # Fixar vad?
temp              # Temporary är permanent 😅
dev123            # Kryptiskt
```

### Pull Request Size

✅ **DO: Små, fokuserade PR:s**

**Ideal PR:**
- 📏 < 400 lines changed
- ⏱️ < 30 minutes att review:a
- 🎯 Ett syfte/feature
- ✅ Fully tested
- 📝 Dokumenterad

**Tips för att hålla PR:s små:**
- Split features i logical chunks
- Refactoring i separat PR
- Tests tillsammans med implementation
- Documentation updates tillsammans med feature

❌ **DON'T: Gigantiska PR:s**

**Problem med stora PR:s:**
- 👀 Svår att review:a ordentligt
- 🐛 Buggar missas lättare
- ⏱️ Tar lång tid att review:a
- 🔀 Merge conflicts mer troliga
- 😫 Reviewer fatigue

**Om feature är stor:**
```bash
# Split i flera PR:s:
feature/statistics-part1-data-models
feature/statistics-part2-service-layer
feature/statistics-part3-ui
feature/statistics-part4-tests
```

### Code Review

✅ **DO: Self-review först**

Before requesting review:
1. 📖 Läs din egen diff på GitHub
2. 🔍 Leta efter debug-kod, console.logs, etc.
3. ✅ Kör alla tester lokalt
4. 📝 Fyll i PR template noggrant
5. 🖼️ Lägg till screenshots för UI-ändringar

✅ **DO: Konstruktiv feedback**

```markdown
# Good examples:
"Consider extracting this to a separate method for better readability"
"This could throw NullReferenceException if user is null"
"Nice solution! Maybe add a comment explaining the algorithm?"
"Have you considered using LINQ here instead?"
```

❌ **DON'T: Destruktiv eller vag feedback**

```markdown
# Bad examples:
"This is wrong"                    # Vad är wrong? Hur fixa?
"Rewrite this"                     # Varför? Hur?
"I don't like this"                # Inte konstruktivt
"Use better variable names"         # Vilka namn? Varför?
```

### Merge Strategy

✅ **DO: Squash and Merge (Rekommenderat)**

**Fördelar:**
- Ren master-historik
- En commit per feature
- Lätt att revertera hela feature
- Git log lättläst

**När:**
- Default för de flesta PR:s
- Features med många small commits
- WIP commits i feature branch

**Result:**
```
* abc123 feat: add statistics page (#42)
* def456 fix: resolve timer bug (#41)
* ghi789 refactor: simplify timer logic (#40)
```

⚠️ **Sometimes: Merge Commit**

**Fördelar:**
- Behåller all commit-historik
- Visar branch-strukturen
- Bra för stora features

**När:**
- Stor feature med välorganiserade commits
- Vill behålla commit-historiken
- Multiple contributors på samma branch

**Result:**
```
*   abc123 Merge pull request #42 from feature/statistics
|\
| * def456 feat: add statistics UI
| * ghi789 feat: add statistics service
| * jkl012 feat: add statistics models
|/
```

❌ **AVOID: Rebase and Merge**

**Problem:**
- Kan orsaka problem om andra arbetar på branchen
- Ändrar commit history
- Svårare att troubleshoot

**När det är OK:**
- Solo developer
- Ingen annan har checkat out branchen
- Vill ha linjär historik utan merge commits

---

## 🚨 Common Scenarios

### Scenario 1: Ångra senaste commit (inte pushad)

```bash
# Behåll ändringar, ta bort commit
git reset --soft HEAD~1

# Ångra ändringar OCH commit
git reset --hard HEAD~1

# Ändra commit message
git commit --amend -m "fix: corrected commit message"
```

### Scenario 2: Feature branch är outdated

```bash
# Din feature branch baserad på gammal master
git checkout feature/my-feature

# Merge in latest master
git merge master

# Eller rebase (rekommenderat för cleaner history)
git rebase master

# Om conflicts uppstår:
# 1. Fixa conflicts i filerna
# 2. git add <fixed-files>
# 3. git rebase --continue

# Push (kan behöva force push efter rebase)
git push --force-with-lease origin feature/my-feature
```

### Scenario 3: Behöver ändra redan pushad commit

```bash
# Ändra senaste commit
git commit --amend -m "fix: corrected message"

# Force push (använd --force-with-lease för säkerhet)
git push --force-with-lease origin feature/my-feature

# OBS: Gör ALDRIG force push till master!
# På feature branches är det OK före merge
```

### Scenario 4: Cherry-pick commit från annan branch

```bash
# Applicera specifik commit till current branch
git cherry-pick <commit-hash>

# Cherry-pick multiple commits
git cherry-pick commit1 commit2 commit3

# Om conflicts:
git add <resolved-files>
git cherry-pick --continue

# Avbryt cherry-pick
git cherry-pick --abort
```

### Scenario 5: Stash ändringar temporärt

```bash
# Spara ändringar (t.ex. för att byta branch)
git stash

# Spara med message
git stash save "WIP: working on statistics"

# Lista stashes
git stash list

# Applicera senaste stash
git stash pop

# Applicera specifik stash (behåller i stash list)
git stash apply stash@{1}

# Ta bort stash
git stash drop stash@{0}

# Rensa alla stashes
git stash clear
```

### Scenario 6: Hotfix till produktion

```bash
# 1. Skapa hotfix branch från master
git checkout master
git pull origin master
git checkout -b fix/critical-bug

# 2. Fixa bug
git add .
git commit -m "fix: resolve critical production bug"

# 3. Push och skapa PR
git push -u origin fix/critical-bug
# Skapa PR via GitHub UI

# 4. Fast-track review och merge
# - Mark as urgent
# - Quick review
# - Merge immediately

# 5. Cleanup
git checkout master
git pull origin master
git branch -d fix/critical-bug
```

### Scenario 7: Experimenting utan att påverka branch

```bash
# Skapa throwaway branch för experiment
git checkout -b experiment/try-new-approach

# Experimentera fritt...
git commit -m "experiment: trying different algorithm"

# Om det funkade - merge till feature branch:
git checkout feature/my-feature
git merge experiment/try-new-approach

# Om det inte funkade - släng branchen:
git checkout feature/my-feature
git branch -D experiment/try-new-approach
```

---

## 📊 Git Aliases (Rekommenderade)

Lägg till i `.gitconfig` eller `~/.gitconfig`:

```ini
[alias]
    # Status
    st = status -sb

    # Logging
    lg = log --graph --pretty=format:'%Cred%h%Creset -%C(yellow)%d%Creset %s %Cgreen(%cr) %C(bold blue)<%an>%Creset' --abbrev-commit
    ls = log --pretty=format:'%C(yellow)%h %Cred%ad %Cblue%an%Cgreen%d %Creset%s' --date=short

    # Branching
    br = branch
    co = checkout
    cob = checkout -b

    # Committing
    cm = commit -m
    ca = commit --amend

    # Diff
    df = diff
    dc = diff --cached

    # Push/Pull
    pul = pull origin
    pus = push origin

    # Cleanup
    cleanup = "!git branch --merged | grep -v '\\*\\|master\\|main' | xargs -n 1 git branch -d"

    # Undo
    undo = reset --soft HEAD~1

    # Show changed files
    changed = diff --name-only

    # Squash last N commits
    squash = "!f(){ git reset --soft HEAD~${1} && git commit --edit -m\"$(git log --format=%B --reverse HEAD..HEAD@{1})\"; };f"
```

**Usage:**

```bash
git st                    # git status -sb
git lg                    # Pretty log graph
git cob feature/my-feat   # git checkout -b feature/my-feat
git cm "feat: add X"      # git commit -m "feat: add X"
git cleanup               # Delete all merged branches
git undo                  # Undo last commit (keep changes)
```

---

## 🆘 Troubleshooting

### Problem: CI/CD failar - "Code formatting verification failed"

**Orsak:** Kod inte formaterad enligt projektets style

**Lösning:**

```bash
# Formatera lokalt
dotnet format PomodoroTimeTracker.sln

# Committa formatering
git add .
git commit -m "style: apply dotnet format"
git push
```

### Problem: CI/CD failar - Tests failing

**Orsak:** En eller flera tester failar

**Lösning:**

```bash
# Kör tester lokalt
dotnet test

# Kör specifik test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Kör med verbosity
dotnet test --verbosity detailed

# Fixa failing tests
# Committa fix
git add .
git commit -m "test: fix failing test"
git push
```

### Problem: "Protected branch update failed"

**Orsak:** Försöker pusha direkt till master (branch protection aktiverad)

**Lösning:**

```bash
# Detta är förväntat beteende!
# Använd feature branch workflow:

# 1. Skapa feature branch
git checkout -b feature/my-feature

# 2. Commit till feature branch
git add .
git commit -m "feat: add feature"

# 3. Push feature branch
git push origin feature/my-feature

# 4. Skapa PR via GitHub UI
```

### Problem: Merge conflicts

**Orsak:** Din branch och master har ändrat samma kod

**Lösning:**

```bash
# 1. Hämta latest master
git checkout master
git pull origin master

# 2. Merge master in i din branch
git checkout feature/my-feature
git merge master

# 3. Git visar conflicts:
# <<<<<<< HEAD
# Your changes
# =======
# Master changes
# >>>>>>> master

# 4. Öppna konflikt-filer och fixa manuellt
# Ta bort <<<<<<, =======, >>>>>>> markers
# Behåll rätt kod

# 5. Stage resolved files
git add <resolved-files>

# 6. Commit merge
git commit -m "merge: resolve conflicts with master"

# 7. Push
git push
```

### Problem: Codecov token missing

**Orsak:** `CODECOV_TOKEN` secret inte konfigurerad i GitHub

**Lösning:**

1. Gå till [codecov.io](https://codecov.io/)
2. Logga in och hitta din repository token
3. I GitHub: Settings → Secrets and variables → Actions
4. New repository secret: `CODECOV_TOKEN`
5. Kör CI/CD igen

**Temporary workaround:**

CI/CD fortsätter även om codecov upload failar (`fail_ci_if_error: false`).
Du kan ignorera Codecov errors temporärt.

---

## 📈 Success Metrics

### Vad definierar en lyckad Git Strategy?

✅ **Code Quality:**
- Alla tester passerar på master
- Code coverage > 80%
- Inga code quality violations

✅ **Team Efficiency:**
- PR review time < 24 timmar (solo: omedelbart)
- Merge frequency: Flera gånger per dag
- Master alltid deploybar

✅ **Code History:**
- Commits följer Conventional Commits
- Clear, readable git log
- Enkel att revertera features

✅ **Developer Experience:**
- Minimal friktion i workflow
- Automation reducerar manuellt arbete
- Tydliga felmeddelanden när något går fel

---

## 📚 Resources

### Documentation
- [GitHub Flow Guide](https://githubflow.github.io/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Branch Protection](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches)
- [Husky.Net Documentation](https://alirezanet.github.io/Husky.Net/)

### Project-Specific Files
- `.github/BRANCH_PROTECTION_SETUP.md` - Branch protection setup guide
- `.github/PULL_REQUEST_TEMPLATE.md` - PR template
- `.github/workflows/ci.yml` - CI/CD configuration
- `.husky/task-runner.json` - Pre-commit hook configuration
- `TEST_SUMMARY.md` - Test documentation

### Tools
- [GitKraken](https://www.gitkraken.com/) - Git GUI client
- [GitHub Desktop](https://desktop.github.com/) - Simple Git GUI
- [Codecov](https://codecov.io/) - Code coverage tracking
- [GitHub CLI](https://cli.github.com/) - Manage PRs from terminal

---

## 🔄 Review and Update

Detta dokument är en **living document** och ska uppdateras när:

- ✏️ Git workflow ändras
- 🆕 Nya tools eller automation läggs till
- 📊 Metrics visar förbättringsområden
- 👥 Team växer och behov förändras
- 🐛 Problem eller edge cases upptäcks

**Last Reviewed:** 2025-01-25
**Next Review:** Vid behov

---

**Version:** 2.0 - GitHub Flow Implementation
**Status:** ✅ Active & Enforced
**Maintained By:** Project Team
