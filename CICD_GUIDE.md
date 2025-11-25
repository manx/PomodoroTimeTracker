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

## Nästa Steg

1. ✅ Skapa `.github/workflows/ci.yml`
2. ✅ Commit och push till GitHub
3. ✅ Gå till repo → Actions tab
4. ✅ Se workflow köra
5. ⏳ Lägg till tester
6. ⏳ Lägg till release automation

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
