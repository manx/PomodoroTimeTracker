---
name: winui-patterns
description: WinUI 3 and XAML patterns for the Pomodoro Time Tracker. Activates when working with ViewModels, XAML pages, data binding, or UI components.
allowed-tools:
  - Read
  - Glob
  - Grep
---

# WinUI 3 Patterns Skill

Expert knowledge for WinUI 3 development in the Pomodoro Time Tracker project.

## Project-Specific Conventions

### NO Value Converters

This project does NOT use XAML value converters. Use explicit ViewModel properties instead.

```csharp
// ❌ NEVER DO THIS
Visibility="{x:Bind Converter={StaticResource BoolToVisibility}}"

// ✅ ALWAYS DO THIS
public bool IsVisible => SomeCondition;
Visibility="{x:Bind ViewModel.IsVisible, Mode=OneWay}"
```

**Why:** WinUI 3's x:Bind automatically converts `bool` to `Visibility`.

---

## Data Binding

### Always Use x:Bind

```xml
<!-- ✅ CORRECT - Compile-time checked -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- ❌ WRONG - Runtime binding, slower, no compile-time check -->
<TextBox Text="{Binding Name, Mode=TwoWay}" />
```

### Binding Modes

| Mode | Use Case |
|------|----------|
| `OneTime` | Static data that never changes |
| `OneWay` | Display-only, updates from ViewModel |
| `TwoWay` | User input (TextBox, CheckBox, etc.) |

### Common Bindings

```xml
<!-- Text input -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- Command -->
<Button Command="{x:Bind ViewModel.SaveCommand}" Content="Save" />

<!-- Visibility (bool auto-converts) -->
<ProgressRing Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />

<!-- IsEnabled -->
<Button IsEnabled="{x:Bind ViewModel.CanSave, Mode=OneWay}" />

<!-- ItemsSource -->
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}" />

<!-- SelectedItem -->
<ComboBox SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}" />
```

---

## Page Setup Pattern

### Constructor Order (CRITICAL)

```csharp
public sealed partial class ExamplePage : Page
{
    public ExampleViewModel ViewModel { get; }
    private readonly ILogger<ExamplePage> _logger;
    private readonly IDialogService _dialogService;

    public ExamplePage()
    {
        // 1. Resolve simple dependencies FIRST
        _logger = App.GetService<ILogger<ExamplePage>>();
        _dialogService = App.GetService<IDialogService>();

        // 2. Resolve ViewModel
        ViewModel = App.Services.GetService(typeof(ExampleViewModel)) as ExampleViewModel
            ?? throw new InvalidOperationException("ExampleViewModel not registered");

        // 3. Initialize XAML AFTER ViewModel is ready
        this.InitializeComponent();

        // 4. Set up callbacks if needed
        ViewModel.ShowDialog = ShowDialogAsync;
    }
}
```

**Why this order:**
- x:Bind compiles bindings during `InitializeComponent()`
- ViewModel must exist before x:Bind evaluates
- Logger must be ready to catch resolution errors

### XAML Setup

```xml
<Page
    x:Class="PomodoroTimeTracker.WinUI3.Views.ExamplePage"
    xmlns:viewmodels="using:PomodoroTimeTracker.ViewModels"
    d:DataContext="{d:DesignInstance Type=viewmodels:ExampleViewModel}">
```

---

## ViewModel Pattern

### Using CommunityToolkit.Mvvm

```csharp
internal partial class ExampleViewModel : ViewModelBase
{
    private readonly IExampleService _service;
    private readonly ILogger<ExampleViewModel> _logger;

    // Observable properties (generates PropertyChanged)
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // Computed properties (NO converters!)
    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && !IsLoading;
    public bool IsNotLoading => !IsLoading;

    // Property change handlers
    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotLoading));
        OnPropertyChanged(nameof(CanSave));
    }

    // Commands
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            await _service.SaveAsync(Name);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### State Properties Pattern

For every boolean that controls UI visibility/enabled state, create explicit properties:

```csharp
// Primary state
[ObservableProperty]
private bool _isLoading;

[ObservableProperty]
private PomodoroState _state;

// Derived states (no converters needed!)
public bool IsNotLoading => !IsLoading;
public bool IsSetupState => State == PomodoroState.Setup;
public bool IsRunningState => State == PomodoroState.Running;
public bool IsNotBreakState => State != PomodoroState.Break;
```

---

## Dialog Callback Pattern

Keep ViewModel testable by using callbacks instead of direct UI access:

```csharp
// In ViewModel
public Func<Task<bool>>? ShowConfirmDialog { get; set; }
public Func<string, Task>? ShowErrorDialog { get; set; }

private async Task DeleteAsync()
{
    if (ShowConfirmDialog != null)
    {
        var confirmed = await ShowConfirmDialog();
        if (!confirmed) return;
    }

    try
    {
        await _service.DeleteAsync(_id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Delete failed");
        if (ShowErrorDialog != null)
            await ShowErrorDialog("Unable to delete. Please try again.");
    }
}

// In Page code-behind
ViewModel.ShowConfirmDialog = async () =>
{
    var dialog = new ContentDialog
    {
        Title = "Confirm Delete",
        Content = "Are you sure you want to delete this item?",
        PrimaryButtonText = "Delete",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.XamlRoot  // Required!
    };
    return await dialog.ShowAsync() == ContentDialogResult.Primary;
};

ViewModel.ShowErrorDialog = async (message) =>
{
    var dialog = new ContentDialog
    {
        Title = "Error",
        Content = message,
        CloseButtonText = "OK",
        XamlRoot = this.XamlRoot
    };
    await dialog.ShowAsync();
};
```

---

## Timer Pattern

Use `DispatcherQueueTimer` for UI timers:

```csharp
private readonly DispatcherQueueTimer _timer;
private readonly DispatcherQueue _dispatcherQueue;

public ExampleViewModel()
{
    _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    _timer = _dispatcherQueue.CreateTimer();
    _timer.Interval = TimeSpan.FromSeconds(1);
    _timer.Tick += Timer_Tick;
}

private void Timer_Tick(DispatcherQueueTimer sender, object args)
{
    // Already on UI thread - safe to update properties directly
    RemainingSeconds--;

    if (RemainingSeconds <= 0)
    {
        _timer.Stop();
        OnTimerComplete();
    }
}

public void StartTimer()
{
    _timer.Start();
}

public void StopTimer()
{
    _timer.Stop();
}
```

---

## Navigation

### Page Navigation

```csharp
// Navigate to page
var navigationService = App.GetService<INavigationService>();
navigationService.NavigateTo<ClientDetailPage>(clientId);

// In receiving page
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);

    if (e.Parameter is int clientId)
    {
        _ = ViewModel.LoadAsync(clientId);
    }
}
```

### Async Loading Pattern

```csharp
// ❌ WRONG - async void is dangerous
protected override async void OnNavigatedTo(NavigationEventArgs e)
{
    await ViewModel.LoadAsync();
}

// ✅ CORRECT - Fire-and-forget with error handling
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    _ = InitializeAsync(e.Parameter);
}

private async Task InitializeAsync(object? parameter)
{
    try
    {
        _logger.LogInformation("Initializing page");
        await ViewModel.LoadAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error initializing page");
        await _dialogService.ShowErrorAsync("Unable to load. Please try again.");
    }
}
```

---

## DI Registration

```csharp
// In App.xaml.cs ConfigureServices()

// ViewModels - Transient (new instance each navigation)
services.AddTransient<ClientListViewModel>();
services.AddTransient<ClientDetailViewModel>();
services.AddTransient<PomodoroViewModel>();

// UI Services - Singleton (shared across app)
services.AddSingleton<INavigationService, NavigationService>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<IAudioService, AudioService>();
```

---

## Borderless Window (Advanced)

For floating windows like TimerWindow:

```csharp
// Remove window chrome
var presenter = _appWindow.Presenter as OverlappedPresenter;
presenter.SetBorderAndTitleBar(false, false);
presenter.IsAlwaysOnTop = true;

// Handle WM_NCCALCSIZE to remove non-client area
if (msg == WM_NCCALCSIZE && wParam != IntPtr.Zero)
    return IntPtr.Zero;

// DWM extension for full transparency
var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1,
                            cyTopHeight = -1, cyBottomHeight = -1 };
DwmExtendFrameIntoClientArea(_hWnd, ref margins);
```

**Important:** Apply after window activation to avoid white bars.

---

## Common Mistakes

### 1. Wrong binding syntax
```xml
<!-- ❌ Missing Mode -->
<TextBox Text="{x:Bind ViewModel.Name}" />

<!-- ✅ Explicit Mode -->
<TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
```

### 2. InitializeComponent before ViewModel
```csharp
// ❌ x:Bind fails - ViewModel is null
this.InitializeComponent();
ViewModel = App.GetService<MyViewModel>();

// ✅ ViewModel ready for x:Bind
ViewModel = App.GetService<MyViewModel>();
this.InitializeComponent();
```

### 3. Using async void
```csharp
// ❌ Exceptions escape
private async void LoadData() { ... }

// ✅ Proper async Task
private async Task LoadDataAsync() { ... }
```

### 4. Missing XamlRoot on dialogs
```csharp
// ❌ Dialog won't show
var dialog = new ContentDialog { ... };
await dialog.ShowAsync();

// ✅ Required for WinUI 3
dialog.XamlRoot = this.XamlRoot;
await dialog.ShowAsync();
```
