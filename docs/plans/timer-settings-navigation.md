# Plan: Timer Settings Navigation

## Goal
From each timer's start page, allow users to navigate directly to that timer's settings tab, then return to the timer page.

## Current State
- **Settings Page**: TabView with tabs (General, Pomodoro Timer, Regular Timer*, Stopwatch*)
- **Timer Pages**: PomodoroPage, RegularTimerPage, StopWatchPage (all have setup screens)
- **Navigation**: Frame-based with GoBack() support
- *Regular Timer and Stopwatch settings tabs are placeholders ("Coming soon...")

## Design

### UI Change
Add a Settings button/hyperlink on each timer's setup screen:
- Location: Near the top of the setup form (visible only in setup state)
- Style: Subtle hyperlink or icon button (not primary action)
- Text: "Settings" or gear icon with tooltip

### Navigation Flow
```
Timer Page (Setup) → [Click Settings] → Settings Page (correct tab selected)
                                              ↓
                                        [User configures]
                                              ↓
                                        [Save/Cancel]
                                              ↓
Timer Page (Setup) ← [GoBack()] ←────────────┘
```

### Implementation

#### 1. INavigationService Changes
Add method to navigate to Settings with tab selection:
```csharp
void NavigateToSettings(int tabIndex);
// Or use an enum:
void NavigateToSettings(SettingsTab tab);
```

#### 2. NavigationService Implementation
- Navigate to SettingsPage with parameter (tab index)
- SettingsPage.OnNavigatedTo reads parameter and sets SelectedTabIndex

#### 3. SettingsViewModel Changes
- Accept navigation parameter for initial tab selection
- Property: `int SelectedTabIndex { get; set; }`

#### 4. Timer ViewModels
Add command for settings navigation:
```csharp
// PomodoroViewModel
[RelayCommand]
private void OpenSettings() => _navigationService.NavigateToSettings(SettingsTab.Pomodoro);

// RegularTimerViewModel
[RelayCommand]
private void OpenSettings() => _navigationService.NavigateToSettings(SettingsTab.RegularTimer);

// StopWatchViewModel
[RelayCommand]
private void OpenSettings() => _navigationService.NavigateToSettings(SettingsTab.Stopwatch);
```

#### 5. Timer XAML Pages
Add settings button in setup area (only visible during setup):
```xml
<HyperlinkButton Content="Settings"
                 Command="{x:Bind ViewModel.OpenSettingsCommand}"
                 Visibility="{x:Bind ViewModel.IsInSetupState, Mode=OneWay}"/>
```

#### 6. SettingsPage Back Navigation
- Save button: Save settings, then GoBack()
- Cancel button: GoBack() without saving
- Already supported pattern in detail pages

## Files to Modify

| File | Change |
|------|--------|
| `INavigationService.cs` | Add `NavigateToSettings(int tabIndex)` method |
| `NavigationService.cs` | Implement tab-aware settings navigation |
| `SettingsViewModel.cs` | Accept tab parameter, expose SelectedTabIndex |
| `SettingsPage.xaml.cs` | Read navigation parameter |
| `PomodoroViewModel.cs` | Add OpenSettingsCommand |
| `RegularTimerViewModel.cs` | Add OpenSettingsCommand |
| `StopWatchViewModel.cs` | Add OpenSettingsCommand |
| `PomodoroPage.xaml` | Add Settings button in setup area |
| `RegularTimerPage.xaml` | Add Settings button in setup area |
| `StopWatchPage.xaml` | Add Settings button in setup area |

## Tab Index Mapping
```csharp
public enum SettingsTab
{
    General = 0,
    Pomodoro = 1,
    RegularTimer = 2,
    Stopwatch = 3
}
```

## Design Decisions

- **Placement**: Top-right corner of the setup area
- **Style**: Gear icon only (SymbolIcon with tooltip "Settings")
- **Visibility**: Only during setup state (when timer is not running)
