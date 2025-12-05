# Plan: Windows Toast Notifications

## Summary
Add Windows toast notifications for timer completion events with interactive action buttons.

## Requirements
- **Trigger Events:** Timer completions only (work done, wrap-up done, break done)
- **Action Buttons:** Interactive (e.g., "Start Break", "Dismiss", "Skip Break")
- **Settings:** Toggle to enable/disable (use existing `ShowNotification` property)
- **Pattern:** Follow IAudioService pattern (interface in Application, impl in WinUI3)

---

## Implementation Phases

### Phase 1: Application Layer
| File | Action |
|------|--------|
| `Application\Interfaces\INotificationService.cs` | Create interface |

**INotificationService methods:**
- `ShowWorkCompleteAsync(string objective)` - "Work session complete!" + Start Break button
- `ShowWrapUpCompleteAsync(string objective)` - "Time to take a break!" + Start Break button
- `ShowBreakCompleteAsync(bool isLongBreak)` - "Break is over!" + Dismiss button
- `IsSupported` (bool property) - Check if toasts are available

### Phase 2: WinUI3 Implementation
| File | Action |
|------|--------|
| `WinUI3\Services\NotificationService.cs` | Create implementation |
| `WinUI3\App.xaml.cs` | Register service |

**Implementation details:**
- Use `Microsoft.Windows.AppNotifications` from Windows App SDK
- Register for activation (handle button clicks)
- App must handle `AppNotificationManager` lifecycle

### Phase 3: ViewModel Integration
| File | Action |
|------|--------|
| `ViewModels\PomodoroViewModel.cs` | Inject INotificationService, call at state transitions |
| `ViewModels\RegularTimerViewModel.cs` | Same pattern |

**Integration points in PomodoroViewModel:**
- `TriggerWrapUpNotification()` - After audio, show work complete toast
- `OnTimerComplete()` - Show wrap-up complete or break complete toast
- Check `_settings.ShowNotification` before showing

### Phase 4: Tests
| File | Action |
|------|--------|
| `Tests\ViewModels\PomodoroViewModelNotificationTests.cs` | Test notification calls |

---

## Toast Content

### Work Complete Toast
```
Title: "Work Session Complete!"
Body: "{objective}"
Buttons: [Start Break] [Dismiss]
```

### Wrap-Up Complete Toast
```
Title: "Time for a Break!"
Body: "You've completed a pomodoro. Take a {short/long} break."
Buttons: [Start Break] [Skip Break]
```

### Break Complete Toast
```
Title: "Break is Over!"
Body: "Ready to start your next work session?"
Buttons: [Dismiss]
```

---

## Critical Files
- `Application\Interfaces\INotificationService.cs` - Service contract
- `WinUI3\Services\NotificationService.cs` - Windows App SDK implementation
- `ViewModels\PomodoroViewModel.cs` - Integration (lines ~553-592)
- `WinUI3\App.xaml.cs` - Service registration + activation handling
