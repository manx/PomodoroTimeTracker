# Changelog - Pomodoro Time Tracker

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
