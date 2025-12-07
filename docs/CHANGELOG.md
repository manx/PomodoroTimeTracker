# Changelog - Pomodoro Time Tracker

## 2025-12-06

### Timer Settings Navigation
- Added gear icon button to each timer's setup screen (Pomodoro, Regular Timer, Stopwatch)
- Clicking gear navigates to Settings with corresponding tab selected
- Browser back button returns to timer page
- 6 new unit tests for OpenSettingsCommand

### Windows Toast Notifications
- Added toast notifications for timer completions (PR #31)

## 2025-12-05

### Settings Window Rebuild
- Rebuilt settings as tabbed interface with TabView
- Added General Settings tab with:
  - Language override (English US/UK or system default)
  - Date format override (MM/dd/yyyy, dd/MM/yyyy, yyyy-MM-dd or system default)
  - Week start day (Sunday, Monday, Saturday)
  - Week year standard (ISO 8601, US Standard)
- Extracted Pomodoro Timer settings to separate tab
- Added placeholder tabs for Regular Timer and Stop Watch
- Implemented tab memory (remembers last opened tab)
- Created new AppSettings entity and repository (singleton pattern)
- Added week calculation methods supporting both ISO 8601 and US standards
- Updated ReportViewModel to use configurable week settings
- 82 new tests (repository, service, ViewModel, week calculation)
- PR #29

### Zero-Interaction Workflow
- Enhanced `/implement-feature` command for autonomous execution
- Added `--plan <name>` flag to load existing plans from `docs/plans/`
- Auto-creates branch, commits, and PR on completion
- PR #27, PR #28

## 2025-12-04

### Report View Implementation
- Added Report view with combined statistics
- Time period options: Daily, Weekly, Monthly, Custom date range
- Client and Project filter dropdowns (cascading)
- Summary cards: Total Time, Pomodoro Sessions, Time Entries
- Project breakdown with progress bars
- 36 new ReportViewModel tests
- PR #20

### ViewModels Extraction for Testability
- Extracted all ViewModels to `PomodoroTimeTracker.ViewModels` WinUI Class Library
- Created `IDispatcherTimer` interface for timer testability
- Added `IActiveTimerService` for single-timer enforcement
- Added `IPomodoroStateService` for pomodoro cycle tracking
- 158 new ViewModel tests
- PR #16

### INavigationService Cleanup
- Removed `NavigationFrame` property from interface
- Frame management now internal to concrete implementation
- PR #17

## 2025-12-01

### Sound Selection Feature
- Added dropdown menus for selecting WrapUp and Alarm sounds
- Test button next to each dropdown to preview sounds
- Sound plays automatically when volume slider is released
- Database migration: `AddSoundSelectionSettings`

### Slash Commands for Agent Orchestration
- Created `/implement-feature` command
- Created `/fix-tests` command
- Created `/code-review` command

## 2025-01-24

### TimerWindow Redesign
- Redesigned from circular (200x200) to compact horizontal (150x50) layout
- Changed progress from circular arc to rectangular bar
- Moved objective to tooltip (hover only)
- Implemented truly borderless window with Win32 interop

### UI Improvements
- Doubled button spacing in PomodoroPage
- Added alternative button styles (warm gray, red accent)

## Earlier Development

### Core Features
- Pomodoro timer with 4-cycle breaks
- Wrap up period implementation
- Timer window with context menu
- Settings with auto-calculate
- Client & Project CRUD pages
- Time Entry management

### Infrastructure
- Clean Architecture implementation
- EF Core with SQLite
- Repository and Unit of Work patterns
- MVVM with CommunityToolkit.Mvvm
