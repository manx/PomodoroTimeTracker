# Pomodoro Time Tracker

A modern desktop Pomodoro timer and time tracking application built with **WinUI 3** and **Clean Architecture** principles, optimized for **Windows 11 ARM64**.

## 🎯 Overview

This application combines the Pomodoro Technique with comprehensive time tracking, helping you manage productivity while tracking time against clients and projects.

## ✨ Features

### Pomodoro Timer ✅ IMPLEMENTED
- **Complete Pomodoro Workflow**:
  - Work sessions with customizable duration (default: 25 minutes)
  - Automatic break cycle: Work → Short Break → Work → Short Break → Work → Short Break → Work → Long Break
  - Short breaks (default: 5 minutes)
  - Long breaks (default: 15 minutes, every 4th pomodoro)

- **Session Management**:
  - Select client and project for each session
  - Set session objective (120 character limit)
  - Remembers last used client/project
  - Pause/Resume functionality
  - Stop with save, discard, or resume options

- **Timer Features**:
  - Countdown timer with progress visualization
  - Soft stop alarm (triggers 3 minutes before end by default)
  - Completion alarm
  - Configurable alarm volumes
  - Session tracking and history

### Pomodoro Settings ✅ IMPLEMENTED
- Customizable work duration
- Customizable short/long break durations
- Auto-calculate break times from work duration (work/5 for short, work/5*3 for long)
- Long break interval configuration
- Soft stop alarm duration and volume
- Main alarm volume
- Notification preferences (Windows notifications, sound, window flash)

### Client & Project Management ✅ IMPLEMENTED
- Create and manage clients
- Create projects linked to clients
- Track time and sessions per project
- View project details and history

### Time Tracking
- Pomodoro sessions automatically tracked
- Manual time entry (planned)
- Session notes and objectives
- Automatic duration calculation

### Statistics & Reporting (Planned)
- Daily statistics
- Weekly summaries
- Per-project reports
- Session history

## 🏗️ Architecture

This project follows **Clean Architecture** principles with **MVVM** pattern in the presentation layer:

```
PomodoroTimeTracker/
├── PomodoroTimeTracker.Domain/          # Core business entities
│   ├── Entities/                        # Domain models
│   │   ├── Client.cs                    # Client entity
│   │   ├── Project.cs                   # Project entity (linked to Client)
│   │   ├── PomodoroSession.cs           # Pomodoro session with objective
│   │   ├── PomodoroSettings.cs          # User preferences & timer config
│   │   ├── TimeEntry.cs                 # Manual time tracking
│   │   └── SessionType.cs               # Enum: Work, ShortBreak, LongBreak
│   └── Interfaces/                      # Repository interfaces
│
├── PomodoroTimeTracker.Application/     # Business logic & use cases
│   ├── DTOs/                            # Data Transfer Objects
│   ├── Interfaces/                      # Service interfaces
│   └── Services/                        # Application services
│       ├── ClientService.cs
│       ├── ProjectService.cs
│       ├── PomodoroSessionService.cs
│       ├── PomodoroSettingsService.cs
│       ├── TimeEntryService.cs
│       └── StatisticsService.cs
│
├── PomodoroTimeTracker.Infrastructure/  # Data access & external concerns
│   ├── Data/                            # EF Core DbContext
│   ├── Configurations/                  # Entity configurations
│   ├── Repositories/                    # Repository implementations
│   └── Migrations/                      # EF Core migrations
│
└── PomodoroTimeTracker.WinUI3/         # Presentation layer (WinUI 3)
    ├── Views/                           # XAML pages
    │   ├── PomodoroPage.xaml            # Main Pomodoro timer interface
    │   ├── PomodoroSettingsPage.xaml   # Settings configuration
    │   ├── ClientListPage.xaml
    │   ├── ClientDetailPage.xaml
    │   ├── ProjectListPage.xaml
    │   └── ProjectDetailPage.xaml
    ├── ViewModels/                      # ViewModels (MVVM)
    │   ├── PomodoroViewModel.cs         # Pomodoro timer logic
    │   ├── PomodoroSettingsViewModel.cs
    │   ├── ClientListViewModel.cs
    │   └── ...
    ├── Services/                        # UI services
    │   ├── NavigationService.cs
    │   └── DialogService.cs
    ├── MainWindow.xaml                  # Main window with NavigationView
    └── App.xaml.cs                      # Application startup & DI
```

## 🛠️ Technologies

- **.NET 9.0** with C# 13
- **WinUI 3** (Windows App SDK 1.8)
- **MVVM** pattern with CommunityToolkit.Mvvm
- **Entity Framework Core 9.0**
- **SQLite** database
- **Dependency Injection** with Microsoft.Extensions.Hosting
- **Native ARM64 support**

## 🚀 Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **Windows 11** (ARM64 or x64)
- **Windows App SDK** (installed automatically via NuGet)

### Running the Application

1. **Clone or navigate to the project directory:**
   ```bash
   cd PomodoroTimeTracker
   ```

2. **Restore dependencies and build:**
   ```bash
   dotnet build
   ```

3. **Run the WinUI 3 application:**
   ```bash
   dotnet run --project PomodoroTimeTracker.WinUI3
   ```

   Or from the WinUI3 project directory:
   ```bash
   cd PomodoroTimeTracker.WinUI3
   dotnet run
   ```

## 💾 Database

The application uses **SQLite** for data persistence. The database file is created automatically at:
```
%LocalAppData%\PomodoroTimeTracker\pomodoro.db
```

### Database Schema

**Clients**
- Id, Name, Email, Phone, Description, IsActive, CreatedDate

**Projects**
- Id, ClientId, Name, Description, HourlyRate, IsActive, CreatedDate

**PomodoroSessions**
- Id, ProjectId, StartTime, EndTime, DurationMinutes, IsCompleted, SessionType, Objective, Notes

**PomodoroSettings** (Singleton)
- Id, WorkDurationMinutes, ShortBreakDurationMinutes, LongBreakDurationMinutes, LongBreakInterval
- ShowNotification, PlaySound, FlashWindow
- SoftStopDurationMinutes, SoftStopAlarmVolume, UseAlarm, AlarmVolume

**TimeEntries**
- Id, ProjectId, StartTime, EndTime, DurationMinutes, Description, IsBillable

### Database Migrations

The database schema is managed through **Entity Framework Core migrations**.

**To create a new migration after modifying entities:**

```bash
dotnet ef migrations add YourMigrationName --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3
```

**To apply migrations manually:**

```bash
dotnet ef database update --project PomodoroTimeTracker.Infrastructure --startup-project PomodoroTimeTracker.WinUI3
```

Migrations are applied automatically on app startup.

## 📱 User Interface

The app features a **modern Windows 11 UI** with:
- **NavigationView** sidebar for easy navigation
- **Fluent Design System** styling
- **Responsive layout**
- **Native Windows 11 controls**

### Navigation Structure
- 🏠 Dashboard (Coming Soon)
- ⏰ **Pomodoro Timer** ✅ Fully implemented
- ⚙️ **Pomodoro Settings** ✅ Fully implemented
- 📅 Time Entry (Coming Soon)
- 👥 **Clients** ✅ Basic CRUD
- 📁 **Projects** ✅ Basic CRUD
- 📊 Statistics (Coming Soon)

## 🎮 Using the Pomodoro Timer

### Starting a Session

1. Navigate to the Pomodoro page
2. Select a **Client** (optional, remembers last selection)
3. Select a **Project** under that client (optional)
4. Enter your **Objective** for this pomodoro (required, 120 char max)
5. Adjust **Duration** if needed (default from settings)
6. Click **Start Pomodoro**

### During a Session

- **Timer Display**: Shows countdown and progress ring
- **Pause/Resume**: Pause the timer and resume when ready
- **Stop**: Opens dialog with three options:
  - **Resume**: Continue the session
  - **Save**: Save partial session with timestamp
  - **Discard**: Delete the session entirely

### Break Cycle

After completing pomodoros, breaks start automatically:
- After Pomodoro 1, 2, 3: **Short Break**
- After Pomodoro 4: **Long Break** → cycle resets

During breaks, timer runs automatically (no pause/stop buttons).

### Alarms

- **Soft Stop Alarm**: Plays at configurable time before end (default: 3 min)
- **Completion Alarm**: Plays when timer reaches zero

## 📋 Development Status

### ✅ Completed
- Clean Architecture foundation (Domain, Application, Infrastructure, Presentation)
- WinUI 3 project setup with ARM64 support
- Dependency Injection configured
- Database integration (SQLite + EF Core)
- Navigation system with NavigationView
- Dialog service
- **Pomodoro Timer** with full workflow
- **Pomodoro Settings** page
- Client & Project management (basic CRUD)
- Session tracking and history

### 🚧 In Progress
- Dashboard view
- Manual time entry
- Statistics and reporting
- Data export features

### 📝 Planned
- Export reports (CSV/PDF)
- Charts and visualizations
- Weekly/monthly goal setting
- Task/TODO integration
- Notification improvements
- Keyboard shortcuts

## 🎨 Why WinUI 3?

This project uses **WinUI 3** (Microsoft's recommended framework for Windows desktop apps) because:

✅ **Native ARM64 optimization** - Perfect for Surface Pro X and ARM-based Windows 11 devices
✅ **Modern Fluent Design** - Native Windows 11 look and feel
✅ **Better performance** - Improved rendering and battery life
✅ **Future-proof** - Microsoft's strategic direction for Windows UI
✅ **Active development** - Regular updates and new features

## 🔧 Design Guidelines

See [DESIGN_GUIDELINES.md](DESIGN_GUIDELINES.md) for project design decisions and coding patterns.

**Key principle:** Prefer explicit ViewModel properties over XAML value converters for better readability and maintainability.

## 📚 Additional Documentation

- **[DESIGN_GUIDELINES.md](DESIGN_GUIDELINES.md)** - Design patterns and preferences
- **[CLAUDE.md](CLAUDE.md)** - Development notes and technical details

## 📄 License

This project is for **personal/educational use**.

---

**Built with ❤️ using WinUI 3 and Clean Architecture**
