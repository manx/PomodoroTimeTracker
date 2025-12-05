# Plan: Rebuild Settings Window

## Summary
Rebuild settings window with TabView containing 4 sections: General, Pomodoro Timer, Regular Timer, Stop Watch. Create new `AppSettings` entity for general preferences. General tab shown by default, otherwise remembers last opened tab.

## Requirements
- **General Settings**: Language, Date Format, Week Start, Week Year Standard (all with "use system" option)
- **Pomodoro Timer**: Existing settings (move to tab)
- **Regular Timer**: Placeholder (future)
- **Stop Watch**: Placeholder (future)
- **Tab Memory**: Store last opened tab, show General on first launch

---

## Implementation Phases

### Phase 1: Domain Layer
| File | Action |
|------|--------|
| `Domain\Entities\WeekYearStandard.cs` | Create enum (Iso8601, UsStandard) |
| `Domain\Entities\AppSettings.cs` | Create entity with all properties |
| `Domain\Interfaces\IAppSettingsRepository.cs` | Create interface |
| `Domain\Interfaces\IUnitOfWork.cs` | Add AppSettings property |

**AppSettings properties:**
- `Id`, `LastModified`
- `LastOpenedSettingsTab` (string, default "General")
- `LanguageOverride` (string?, null = system)
- `DateFormatOverride` (string?, null = system)
- `WeekStartDay` (DayOfWeek, default Sunday)
- `WeekYearStandard` (enum, default Iso8601)

### Phase 2: Infrastructure Layer
| File | Action |
|------|--------|
| `Infrastructure\Configurations\AppSettingsConfiguration.cs` | Create EF config |
| `Infrastructure\Repositories\AppSettingsRepository.cs` | Create (singleton pattern) |
| `Infrastructure\Data\ApplicationDbContext.cs` | Add DbSet<AppSettings> |
| `Infrastructure\Repositories\UnitOfWork.cs` | Add AppSettings property |
| Migration | `dotnet ef migrations add AddAppSettings` |

### Phase 3: Application Layer
| File | Action |
|------|--------|
| `Application\DTOs\AppSettingsDto.cs` | Create DTO + UpdateDto |
| `Application\Interfaces\IAppSettingsService.cs` | Create interface |
| `Application\Services\AppSettingsService.cs` | Create with week calc helpers |
| `Application\Services\StatisticsService.cs` | Inject IAppSettingsService, use for week calc |

**IAppSettingsService methods:**
- `GetSettingsAsync()`, `UpdateSettingsAsync()`, `UpdateLastOpenedTabAsync()`
- `GetWeekStartDay()`, `GetWeekNumber(date)`, `GetWeekStart(date)`, `GetWeekEnd(date)`

### Phase 4: ViewModels
| File | Action |
|------|--------|
| `ViewModels\GeneralSettingsViewModel.cs` | Create new |
| `ViewModels\SettingsViewModel.cs` | Create container (manages TabView index) |
| `ViewModels\ReportViewModel.cs` | Inject IAppSettingsService, replace hardcoded week calc |
| `ViewModels\Services\INavigationService.cs` | Update PageNames.Settings |

### Phase 5: Views
| File | Action |
|------|--------|
| `WinUI3\Views\GeneralSettingsTab.xaml` | Create UserControl |
| `WinUI3\Views\PomodoroSettingsTab.xaml` | Extract from existing page |
| `WinUI3\Views\SettingsPage.xaml` | Create with TabView |
| `WinUI3\Views\PomodoroSettingsPage.xaml` | Delete |
| `WinUI3\Services\NavigationService.cs` | Update PageMap |
| `WinUI3\App.xaml.cs` | Register new services/ViewModels |

### Phase 6: Tests
| File | Action |
|------|--------|
| `Tests\Infrastructure\AppSettingsRepositoryTests.cs` | Create |
| `Tests\Application\AppSettingsServiceTests.cs` | Create |
| `Tests\ViewModels\GeneralSettingsViewModelTests.cs` | Create |
| `Tests\Application\WeekCalculationTests.cs` | Create (edge cases) |

---

## UI Structure

```
SettingsPage
└── TabView (SelectedIndex bound to SettingsViewModel.SelectedTabIndex)
    ├── [0] General (GeneralSettingsTab UserControl)
    │   ├── Language ComboBox + "Use system" toggle
    │   ├── Date Format ComboBox + "Use system" toggle
    │   ├── Week Start Day ComboBox
    │   ├── Week Year Standard RadioButtons
    │   └── Save / Reset buttons
    ├── [1] Pomodoro Timer (PomodoroSettingsTab UserControl)
    │   └── [existing settings content]
    ├── [2] Regular Timer (placeholder, disabled)
    └── [3] Stop Watch (placeholder, disabled)
```

---

## Critical Files
- `Domain\Entities\AppSettings.cs` - Core entity
- `Application\Services\AppSettingsService.cs` - Week calculation logic
- `ViewModels\SettingsViewModel.cs` - Tab state management
- `ViewModels\ReportViewModel.cs` - Refactor week calculations
- `WinUI3\Views\SettingsPage.xaml` - TabView container
