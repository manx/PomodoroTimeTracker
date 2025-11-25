# CI/CD Guide - GitHub Actions för Pomodoro Time Tracker

## Innehållsförteckning
1. [Grundläggande Koncept](#grundläggande-koncept)
2. [Filstruktur](#filstruktur)
3. [Workflow Anatomi](#workflow-anatomi)
4. [Triggers - När körs workflow](#triggers---när-körs-workflow)
5. [Jobs och Steps](#jobs-och-steps)
6. [Actions - Återanvändbara komponenter](#actions---återanvändbara-komponenter)
7. [Miljövariabler och Secrets](#miljövariabler-och-secrets)
8. [Praktisk Implementation](#praktisk-implementation)

---

## Grundläggande Koncept

### Vad är CI/CD?

**CI (Continuous Integration)**
- Automatisk build och test vid varje kodändring
- Upptäcker problem tidigt
- Håller main-branchen stabil

**CD (Continuous Deployment)**
- Automatisk leverans av färdiga builds
- Skapar releases automatiskt
- Distribuerar till användare

### GitHub Actions - Tre Nyckelkoncept

```
┌─────────────────────────────────────────┐
│ WORKFLOW                                │
│ En komplett automatiseringsprocess      │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ JOB 1: Build                      │ │
│  │                                   │ │
│  │  • Step 1: Checkout code          │ │
│  │  • Step 2: Setup .NET             │ │
│  │  • Step 3: Build project          │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ JOB 2: Test                       │ │
│  │                                   │ │
│  │  • Step 1: Run unit tests         │ │
│  │  • Step 2: Generate coverage      │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

**Workflow**: Hela automatiseringsprocessen (t.ex. "CI Build")
**Job**: En grupp av relaterade steg (t.ex. "Build", "Test")
**Step**: En specifik åtgärd (t.ex. "dotnet build")

### Hur fungerar det?

```
1. Du pushar kod till GitHub
   ↓
2. GitHub upptäcker .yml-fil i .github/workflows/
   ↓
3. GitHub startar en virtuell maskin (Windows/Linux/macOS)
   ↓
4. Kör alla jobs och steps i ordning
   ↓
5. Visar resultat (✅ Success eller ❌ Failed)
```

---

## Filstruktur

GitHub Actions kräver denna **exakta mappstruktur**:

```
PomodoroTimeTracker/
├── .github/                    ← Måste heta .github
│   └── workflows/              ← Måste heta workflows
│       ├── ci.yml              ← Dina workflow-filer
│       ├── release.yml         ← (valfritt)
│       └── code-quality.yml    ← (valfritt)
├── src/
├── tests/
└── README.md
```

**Viktigt:**
- Mappen måste heta `.github` (med punkt!)
- Undermappen måste heta `workflows`
- Filer måste ha `.yml` eller `.yaml` extension

---

## Workflow Anatomi

### Minimal workflow

```yaml
name: CI Build                      # Namnet som visas i GitHub UI

on: [push, pull_request]           # När ska workflow köras?

jobs:                              # Vad ska göras?
  build:                           # Job-namn (valfritt)
    runs-on: windows-latest        # Vilken OS?

    steps:                         # Lista av steg
      - name: Checkout code        # Steg 1
        uses: actions/checkout@v4

      - name: Build                # Steg 2
        run: dotnet build
```

### Fullständig workflow med kommentarer

```yaml
# ============================================
# SEKTION 1: Metadata
# ============================================
name: CI Build Pipeline              # Visas i GitHub Actions tab
run-name: Building ${{ github.ref }} # Dynamiskt namn per körning

# ============================================
# SEKTION 2: Triggers (När körs detta?)
# ============================================
on:
  push:
    branches: [ main, master, develop ]  # Endast dessa branches
    paths-ignore:                        # Skippa om bara dessa ändrats
      - '**.md'
      - 'docs/**'

  pull_request:
    branches: [ main, master ]

  workflow_dispatch:  # Möjliggör manuell körning från GitHub UI

# ============================================
# SEKTION 3: Miljövariabler (Globala)
# ============================================
env:
  DOTNET_VERSION: '9.0.x'
  BUILD_CONFIGURATION: 'Release'

# ============================================
# SEKTION 4: Jobs (Arbetsuppgifter)
# ============================================
jobs:
  build:
    name: Build and Test             # Visningsnamn
    runs-on: windows-latest          # OS: windows/ubuntu/macos

    # Miljövariabler för denna job
    env:
      PROJECT_PATH: './PomodoroTimeTracker.WinUI3/PomodoroTimeTracker.WinUI3.csproj'

    steps:
      # ------------------------------------------
      # Steg 1: Hämta kod från repo
      # ------------------------------------------
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Hämta all git-historik (för versioning)

      # ------------------------------------------
      # Steg 2: Installera .NET
      # ------------------------------------------
      - name: Setup .NET ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      # ------------------------------------------
      # Steg 3: Restore NuGet packages
      # ------------------------------------------
      - name: Restore dependencies
        run: dotnet restore

      # ------------------------------------------
      # Steg 4: Build projektet
      # ------------------------------------------
      - name: Build solution
        run: dotnet build --configuration ${{ env.BUILD_CONFIGURATION }} --no-restore

      # ------------------------------------------
      # Steg 5: Kör tester (när de finns)
      # ------------------------------------------
      - name: Run tests
        run: dotnet test --no-build --verbosity normal
        continue-on-error: true  # Fortsätt även om tester failar

      # ------------------------------------------
      # Steg 6: Ladda upp build-artefakter
      # ------------------------------------------
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: '**/bin/${{ env.BUILD_CONFIGURATION }}/**'
```

---

## Triggers - När körs workflow?

### Push trigger

```yaml
on:
  push:
    branches:
      - main           # Endast main-branch
      - 'feature/**'   # Alla branches som börjar med feature/

    paths:             # Kör endast om dessa filer ändras
      - 'src/**'
      - '**.csproj'

    paths-ignore:      # Skippa om bara dessa ändras
      - '**.md'
      - 'docs/**'
```

### Pull Request trigger

```yaml
on:
  pull_request:
    types: [opened, synchronize, reopened]  # När PR öppnas/uppdateras
    branches: [main]
```

### Schedule trigger (Cron)

```yaml
on:
  schedule:
    - cron: '0 2 * * *'  # Kör varje natt kl 02:00 UTC
```

### Manual trigger

```yaml
on:
  workflow_dispatch:     # Lägg till "Run workflow" knapp i GitHub UI
    inputs:
      build-config:
        description: 'Build configuration'
        required: true
        default: 'Release'
        type: choice
        options:
          - Debug
          - Release
```

### Kombinera flera triggers

```yaml
on:
  push:
    branches: [main]
  pull_request:
  workflow_dispatch:
```

---

## Jobs och Steps

### Enkel Job

```yaml
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet build
```

### Flera Jobs (parallella)

```yaml
jobs:
  build:
    runs-on: windows-latest
    steps:
      - run: dotnet build

  test:
    runs-on: windows-latest
    steps:
      - run: dotnet test

  lint:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet format --verify-no-changes
```

**OBS:** Dessa körs parallellt! Om `test` behöver `build`, använd `needs`:

### Jobs med beroenden (sekventiella)

```yaml
jobs:
  build:
    runs-on: windows-latest
    steps:
      - run: dotnet build

  test:
    needs: build              # Vänta på build först
    runs-on: windows-latest
    steps:
      - run: dotnet test

  deploy:
    needs: [build, test]      # Vänta på både build OCH test
    runs-on: windows-latest
    steps:
      - run: echo "Deploying..."
```

### Matrix Strategy (testa flera versioner)

```yaml
jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest]
        dotnet: ['8.0.x', '9.0.x']

    steps:
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet }}
      - run: dotnet build
```

Detta skapar **4 jobs**: Windows .NET 8, Windows .NET 9, Ubuntu .NET 8, Ubuntu .NET 9

---

## Actions - Återanvändbara komponenter

### Vad är en Action?

En förkonfigurerad komponent som utför en specifik uppgift.

**Två typer:**
1. **Officiella GitHub Actions** (verified, trusted)
2. **Community Actions** (tredjepartsbyggda)

### Vanliga Actions för .NET

#### 1. Checkout kod

```yaml
- name: Checkout repository
  uses: actions/checkout@v4
  with:
    fetch-depth: 0        # 0 = all historik, 1 = bara senaste commit
    submodules: true      # Inkludera git submodules
    lfs: true             # Hämta Git LFS filer
```

#### 2. Setup .NET

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'      # Specifik version
    # eller flera versioner:
    dotnet-version: |
      8.0.x
      9.0.x
```

#### 3. Cache NuGet packages

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

#### 4. Upload artefakter

```yaml
- name: Upload build output
  uses: actions/upload-artifact@v4
  with:
    name: my-app-build
    path: |
      **/bin/Release/**
      !**/*.pdb
    retention-days: 30
```

#### 5. Download artefakter

```yaml
- name: Download build
  uses: actions/download-artifact@v4
  with:
    name: my-app-build
    path: ./downloads
```

#### 6. Create Release

```yaml
- name: Create GitHub Release
  uses: softprops/action-gh-release@v1
  with:
    files: |
      ./output/*.msix
      ./output/*.zip
    tag_name: v${{ github.run_number }}
    draft: false
    prerelease: false
```

---

## Miljövariabler och Secrets

### Miljövariabler

#### Global nivå

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  BUILD_CONFIG: 'Release'

jobs:
  build:
    steps:
      - run: echo ${{ env.DOTNET_VERSION }}
```

#### Job-nivå

```yaml
jobs:
  build:
    env:
      PROJECT_PATH: './src/App.csproj'
    steps:
      - run: dotnet build ${{ env.PROJECT_PATH }}
```

#### Step-nivå

```yaml
steps:
  - name: Build
    env:
      CUSTOM_VAR: 'value'
    run: echo $CUSTOM_VAR
```

### GitHub Context Variabler

Inbyggda variabler som GitHub tillhandahåller:

```yaml
${{ github.repository }}      # "owner/repo-name"
${{ github.ref }}             # "refs/heads/main"
${{ github.sha }}             # Commit SHA
${{ github.actor }}           # Användare som triggade workflow
${{ github.event_name }}      # "push", "pull_request", etc.
${{ github.run_number }}      # Körningsnummer (auto-incrementing)
${{ runner.os }}              # "Windows", "Linux", "macOS"
```

### Secrets (känslig information)

**Lägg till secrets i GitHub:**
1. Gå till repo → Settings → Secrets and variables → Actions
2. Klicka "New repository secret"
3. Namn: `MY_SECRET`, Värde: `hemlig-data`

**Använd i workflow:**

```yaml
steps:
  - name: Deploy
    env:
      API_KEY: ${{ secrets.MY_SECRET }}
    run: deploy.exe --key $API_KEY
```

**Viktigt:**
- Secrets loggas ALDRIG i output
- Visas som `***` i logs
- Kan inte läsas av forks (säkerhet)

---

## Praktisk Implementation

### För detta projekt (Pomodoro Time Tracker)

#### Minimal CI Workflow

```yaml
name: CI Build

on:
  push:
    branches: [ main, master ]
  pull_request:
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal
        continue-on-error: true
```

#### Fullständig CI/CD Workflow

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, master ]
    tags: [ 'v*' ]
  pull_request:
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'
  BUILD_CONFIGURATION: Release
  SOLUTION_PATH: ./PomodoroTimeTracker.sln

jobs:
  build:
    name: Build and Test
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

      - name: Restore
        run: dotnet restore ${{ env.SOLUTION_PATH }}

      - name: Build
        run: dotnet build ${{ env.SOLUTION_PATH }} --configuration ${{ env.BUILD_CONFIGURATION }} --no-restore

      - name: Test
        run: dotnet test ${{ env.SOLUTION_PATH }} --no-build --verbosity normal
        continue-on-error: true

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build-output
          path: '**/bin/${{ env.BUILD_CONFIGURATION }}/**'

  create-release:
    name: Create Release
    needs: build
    runs-on: windows-latest
    if: startsWith(github.ref, 'refs/tags/v')

    steps:
      - name: Download artifacts
        uses: actions/download-artifact@v4
        with:
          name: build-output

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            **/*.exe
            **/*.msix
          draft: false
```

---

## Felsökning

### Vanliga problem

**Problem: "No workflow runs found"**
- Kontrollera att filen ligger i `.github/workflows/`
- Kontrollera YAML-syntax (använd YAML validator)
- Kontrollera att triggern matchar (t.ex. push till rätt branch)

**Problem: "Build failed - dotnet not found"**
- Lägg till `actions/setup-dotnet@v4` step

**Problem: "Permission denied"**
- Gå till Settings → Actions → General
- Sätt "Workflow permissions" till "Read and write permissions"

**Problem: "Checkout failed"**
- Kontrollera att repo är publikt, eller att secrets är konfigurerade för privata repos

### Debugging tips

```yaml
- name: Debug - Show environment
  run: |
    echo "OS: ${{ runner.os }}"
    echo "Repo: ${{ github.repository }}"
    echo "Branch: ${{ github.ref }}"
    dotnet --version
    dotnet --list-sdks
```

### Testa lokalt med act

```bash
# Installera act (GitHub Actions local runner)
choco install act-cli

# Kör workflow lokalt
act push
```

---

## Kod-kvalitetskontroller

### Vad är Code Quality Checks?

Automatiska kontroller som säkerställer:
- 📏 **Konsekvent formatering** - Samma kodstil överallt
- ⚠️ **Inga varningar** - Clean build utan compiler warnings
- 🔍 **Kod-analys** - Upptäcker potentiella buggar
- 📐 **.NET best practices** - Följer Microsofts riktlinjer

### Verktyg

#### 1. EditorConfig (.editorconfig)

Definierar formaterings-regler som fungerar i alla IDE:n.

**Exempel `.editorconfig`:**
```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
dotnet_sort_system_directives_first = true

# Naming conventions
dotnet_naming_rule.private_fields_should_be_underscore_camel_case.severity = suggestion
dotnet_naming_rule.private_fields_should_be_underscore_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_underscore_camel_case.style = underscore_camel_case_style
```

**Placering:** Root-katalogen i projektet

#### 2. dotnet format

Kommando som verifierar/fixar kodformatering baserat på `.editorconfig`.

**Kontrollera formatering:**
```bash
dotnet format --verify-no-changes --verbosity diagnostic
```

**Fixa formatering automatiskt:**
```bash
dotnet format
```

**I CI/CD:**
```yaml
- name: Verify code formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic
```

#### 3. Code Analysis (Roslyn Analyzers)

Inbyggda kod-analysatorer i .NET SDK som upptäcker:
- Null reference risks
- Unused variables
- Security vulnerabilities
- Performance issues
- Best practice violations

**Aktivera i .csproj:**
```xml
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest-all</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

**Exempel på varningar:**
```
CA1062: Validate arguments of public methods
CA1031: Do not catch general exception types
CA1822: Mark members as static
IDE0055: Fix formatting
```

**Konfigurera severity i .editorconfig:**
```ini
# CA1062: Too strict for app code, disable it
dotnet_diagnostic.CA1062.severity = none

# CA1031: Catching general exceptions is a warning
dotnet_diagnostic.CA1031.severity = warning

# IDE0055: Formatting issues are warnings
dotnet_diagnostic.IDE0055.severity = warning
```

### Implementera Code Quality i CI

**Steg 1: Lägg till .editorconfig**
```bash
# Skapa fil i projektets root
# Se fullständigt exempel ovan
```

**Steg 2: Aktivera analyzers i alla .csproj**
```xml
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest-all</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

**Steg 3: Lägg till code-quality job i workflow**
```yaml
jobs:
  code-quality:
    name: Code Quality Checks
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Verify code formatting
        run: dotnet format --verify-no-changes --verbosity diagnostic

  build:
    needs: code-quality  # Kör endast om quality checks passerar
    runs-on: windows-latest
    # ... build steps
```

### Fördelar med Code Quality Checks

**Fail Fast**
```
Push → Code Quality Fails → Ingen build körs → Snabb feedback
```

**Konsistent kodstil**
- Alla utvecklare följer samma regler
- Inga merge conflicts pga formatering
- Lättare code reviews

**Upptäck buggar tidigt**
- Null reference warnings
- Unused code
- Potentiella säkerhetsproblem

**Lokal utveckling**
```bash
# Före commit
dotnet format           # Fixa formatering
dotnet build           # Se analyzers warnings lokalt
```

### Vanliga Analyzer Rules

| Rule | Beskrivning | Severity |
|------|-------------|----------|
| **CA1062** | Validate public method arguments | suggestion/none |
| **CA1303** | Do not pass literals as localized parameters | none (för icke-lokaliserade appar) |
| **CA1031** | Do not catch general exception types | warning |
| **CA1822** | Mark members as static | suggestion |
| **CA2007** | ConfigureAwait on awaited task | none (för UI appar) |
| **IDE0055** | Fix formatting | warning |
| **IDE0058** | Expression value never used | none |

### Felsökning

**Problem: dotnet format failar på CI men inte lokalt**

**Lösning:**
```bash
# Kör exakt samma kommando som CI
dotnet format --verify-no-changes --verbosity diagnostic

# Se detaljerad output
dotnet format --verbosity diagnostic

# Fixa automatiskt
dotnet format
```

**Problem: För många analyzer warnings**

**Lösning:** Konfigurera severity i `.editorconfig`
```ini
# Disable specifika regler
dotnet_diagnostic.CA1062.severity = none

# Eller ändra till suggestion istället för warning
dotnet_diagnostic.CA1822.severity = suggestion
```

**Problem: Build tar längre tid med analyzers**

**Lösning:** Inkrementell build cachar resultat
```bash
# Första bygget: långsamt
dotnet build

# Senare builds: snabbt (cachar analyzer-resultat)
dotnet build
```

---

## Git Hooks med Husky.Net

### Vad är Git Hooks?

Git hooks är scripts som körs automatiskt vid Git-händelser:
- **pre-commit**: Före varje commit
- **pre-push**: Före push till remote
- **commit-msg**: Validera commit-meddelanden

### Varför Husky.Net?

**Problem utan hooks:**
```
Dev skriver kod → Glömmer formatera → Pushar → CI failar → Fix → Push igen
```

**Med hooks:**
```
Dev skriver kod → Commit → Hook formaterar automatiskt → Push → CI lyckas ✅
```

**Fördelar:**
- ✅ Automatisk kod-formatering vid commit
- ✅ Förhindra CI-failures lokalt
- ✅ Konsekvent kodstil i teamet
- ✅ Hooks committade i repo (alla får samma)

### Installation

**Steg 1: Installera Husky.Net som dotnet tool**

```bash
# Skapa tool manifest (om det inte finns)
dotnet new tool-manifest

# Installera Husky
dotnet tool install Husky
```

**Steg 2: Initialisera Husky i projektet**

```bash
dotnet husky install
```

Detta skapar `.husky/` mapp med:
- `_/husky.sh` - Hook runner script
- `task-runner.json` - Task configuration

**Steg 3: Lägg till pre-commit hook**

```bash
dotnet husky add pre-commit
```

**Steg 4: Konfigurera tasks i `.husky/task-runner.json`**

```json
{
   "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
   "tasks": [
      {
         "name": "format-code",
         "command": "dotnet",
         "args": [ "format", "--no-restore" ],
         "output": "always",
         "group": "pre-commit"
      },
      {
         "name": "stage-all-changes",
         "command": "git",
         "args": [ "add", "-u" ],
         "group": "pre-commit"
      }
   ]
}
```

**Steg 5: Uppdatera `.husky/pre-commit` för att köra gruppen**

```bash
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

dotnet husky run --group pre-commit
```

### Hur det fungerar

**När du kör `git commit`:**

1. Git detecterar `.git/hooks/pre-commit`
2. Husky läser `.husky/task-runner.json`
3. Kör alla tasks i "pre-commit" gruppen:
   - `dotnet format` - Formaterar koden
   - `git add -u` - Staged formaterade ändringar
4. Commit fortsätter med formaterad kod

**Output:**
```
[Husky] 🚀 Loading tasks ...
[Husky] ⚡ Preparing task 'format-code'
[Husky] ⌛ Executing task 'format-code' ...
[Husky] ✔ Successfully executed in 8 310ms
[Husky] ⚡ Preparing task 'stage-all-changes'
[Husky] ✔ Successfully executed in 41ms
[master abc1234] Your commit message
```

### Andra Användbara Hooks

#### Pre-push Hook (kör tester före push)

```bash
dotnet husky add pre-push
```

**.husky/task-runner.json:**
```json
{
   "name": "run-tests",
   "command": "dotnet",
   "args": [ "test", "--no-build" ],
   "group": "pre-push"
}
```

#### Commit-msg Hook (validera commit-meddelanden)

```bash
dotnet husky add commit-msg
```

**.husky/task-runner.json:**
```json
{
   "name": "validate-commit-msg",
   "command": "bash",
   "args": [ "-c", "grep -E '^(feat|fix|docs|style|refactor|test|chore):' $1" ],
   "group": "commit-msg"
}
```

### Felsökning

**Problem: Hook körs inte**

**Lösning:**
```bash
# Verifiera att hooks är installerade
ls .git/hooks/

# Återinstallera hooks
dotnet husky install
```

**Problem: "dotnet: command not found" i hook**

**Lösning:** Lägg till dotnet i PATH, eller använd full path:
```json
{
   "command": "C:\\Program Files\\dotnet\\dotnet.exe",
   "args": [ "format" ]
}
```

**Problem: Hook tar för lång tid**

**Lösning:** Använd `--no-restore` och cache:
```json
{
   "command": "dotnet",
   "args": [ "format", "--no-restore", "--verbosity", "quiet" ]
}
```

### Skip Hooks (vid behov)

**Skippa hooks för en enda commit:**
```bash
git commit --no-verify -m "Emergency fix"
```

**OBS:** Använd bara vid nödsituationer!

### Kända Begränsningar

**`dotnet format` på Windows:**
- Kan ha problem med line endings (CRLF vs LF)
- Ibland rapporterar det formaterar men filen ändras inte
- CI-verifikation är fortfarande viktig backup

**Workaround:**
Hooks kör `dotnet format` som "best effort" - CI är den slutgiltiga kontrollen.

---

## Nästa Steg

1. ✅ Skapa `.github/workflows/ci.yml`
2. ✅ Commit och push till GitHub
3. ✅ Gå till repo → Actions tab
4. ✅ Se workflow köra
5. ✅ Lägg till code quality checks
6. ✅ Konfigurera Git hooks med Husky.Net
7. ⏳ Lägg till tester
8. ⏳ Lägg till release automation

---

## Resurser

- [GitHub Actions Dokumentation](https://docs.github.com/en/actions)
- [Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
- [Actions Marketplace](https://github.com/marketplace?type=actions)
- [.NET CI/CD Guide](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)

---

**Skapad:** 2025-01-25
**Projekt:** Pomodoro Time Tracker
**Författare:** CI/CD Learning Session
