# Design Guidelines

This document captures design decisions and preferences for the PomodoroTimeTracker project.

## XAML Data Binding

### Page Setup Pattern with ViewModel Property

**✅ ALWAYS:** Follow this pattern when creating Pages with ViewModels

**Code-Behind (.xaml.cs):**
```csharp
public sealed partial class ClientDetailPage : Page
{
    public ClientDetailViewModel ViewModel { get; }
    private readonly ILogger<ClientDetailPage> _logger;
    private readonly IDialogService _dialogService;

    public ClientDetailPage()
    {
        // IMPORTANT: Resolve dependencies BEFORE InitializeComponent()
        // 1. Resolve simple dependencies first (Logger, DialogService)
        _logger = App.GetService<ILogger<ClientDetailPage>>();
        _dialogService = App.GetService<IDialogService>();

        // 2. Resolve ViewModel (which may have complex dependencies)
        ViewModel = App.Services.GetService(typeof(ClientDetailViewModel)) as ClientDetailViewModel
                    ?? throw new InvalidOperationException("ClientDetailViewModel not registered");

        // 3. Initialize XAML after ViewModel is ready (x:Bind needs ViewModel)
        this.InitializeComponent();
    }
}
```

**XAML:**
```xml
<Page
    x:Class="PomodoroTimeTracker.WinUI3.Views.ClientDetailPage"
    xmlns:viewmodels="using:PomodoroTimeTracker.ViewModels"
    d:DataContext="{d:DesignInstance Type=viewmodels:ClientDetailViewModel}"
    mc:Ignorable="d">

    <TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay}" />
</Page>
```

**Benefits:**
- **IntelliSense:** Get property suggestions while binding in XAML
- **Compile-time validation:** `x:Bind` catches typos at build time, not runtime
- **Design-time preview:** Visual Studio designer shows sample data
- **Better debugging:** Clear property access path
- **Proper DI:** ViewModel gets dependencies from service container

**Key Elements:**
1. Public `ViewModel` property in code-behind
2. Inject `ILogger<T>` and `IDialogService` for error handling
3. Resolve ViewModel from DI in constructor with error handling
4. **CRITICAL:** Call `InitializeComponent()` AFTER all dependencies are resolved
5. Add `xmlns:viewmodels` namespace import in XAML
6. Set `d:DataContext` for design-time support (no runtime impact)
7. Use `{x:Bind ViewModel.PropertyName}` throughout XAML

### ALWAYS Use Correct Constructor Initialization Order

**Core Principle: Resolve all dependencies BEFORE calling InitializeComponent()**

WinUI 3's `x:Bind` compiles bindings that reference your ViewModel. If `InitializeComponent()` is called before the ViewModel is set, bindings will fail.

**✅ CORRECT ORDER:**
```csharp
public ClientDetailPage()
{
    // 1. Resolve simple dependencies first
    _logger = App.GetService<ILogger<ClientDetailPage>>();
    _dialogService = App.GetService<IDialogService>();

    // 2. Resolve ViewModel (may have complex dependencies)
    ViewModel = App.Services.GetService(typeof(ClientDetailViewModel)) as ClientDetailViewModel
                ?? throw new InvalidOperationException("ClientDetailViewModel not registered");

    // 3. Initialize XAML (x:Bind needs ViewModel to be ready)
    this.InitializeComponent();

    // 4. Set up any callbacks or additional configuration
    // (after XAML is initialized if needed)
}
```

**❌ WRONG ORDER:**
```csharp
public ClientDetailPage()
{
    this.InitializeComponent(); // ❌ BAD - ViewModel not set yet!
    ViewModel = App.GetService<ClientDetailViewModel>(); // x:Bind already failed
    _logger = App.GetService<ILogger<ClientDetailPage>>();
}
```

**Why This Matters:**
1. **x:Bind Compilation:** `x:Bind ViewModel.PropertyName` is compiled and evaluated during `InitializeComponent()`
2. **Logger Availability:** If ViewModel resolution fails, logger should be available to log the error
3. **Predictable Order:** Simple → Complex → XAML → Callbacks
4. **Debugging:** Clear initialization flow makes debugging easier

**Special Case - PomodoroPage:**
```csharp
public PomodoroPage()
{
    // 1. Resolve dependencies first
    _logger = App.GetService<ILogger<PomodoroPage>>();
    _dialogService = App.GetService<IDialogService>();
    ViewModel = App.GetService<PomodoroViewModel>();

    // 2. Initialize XAML
    this.InitializeComponent();

    // 3. Set up callbacks AFTER XAML initialization
    ViewModel.ShowStopDialog = ShowStopConfirmationDialogAsync;
}
```

### Prefer Explicit ViewModel Properties Over Converters

**✅ DO:** Create explicit boolean properties in ViewModels for UI state
```csharp
// ViewModel
public bool IsClientSelected => SelectedClient != null;
public bool IsNotBreakState => !IsBreakState;
public string PauseResumeText => IsPausedState ? "Resume" : "Pause";
```

```xml
<!-- XAML -->
<ComboBox IsEnabled="{x:Bind ViewModel.IsClientSelected, Mode=OneWay}" />
<Button Visibility="{x:Bind ViewModel.IsNotBreakState, Mode=OneWay}">
    <TextBlock Text="{x:Bind ViewModel.PauseResumeText, Mode=OneWay}" />
</Button>
```

**❌ DON'T:** Use value converters for simple UI state transformations
```csharp
// Avoid creating converters like:
public class NullToBoolConverter : IValueConverter { ... }
public class InverseBoolToVisibilityConverter : IValueConverter { ... }
```

**Rationale:**
- More straightforward and readable
- Easier to debug - can inspect property values directly
- No extra converter classes to maintain
- Clear intent - property names describe exactly what they represent
- WinUI 3's `x:Bind` handles bool→Visibility conversion automatically
- Better IDE support (IntelliSense, refactoring, find usages)

**Note:** Value converters may still be appropriate for:
- Complex formatting (e.g., date/time formatting with culture)
- Mathematical transformations
- Truly reusable conversions across multiple unrelated ViewModels

## Code Philosophy

### Prefer PRY (Please Repeat Yourself) Over DRY for Clarity

**Core Principle: Minimize Cognitive Load**

When there's a trade-off between reducing repetition (DRY) and improving clarity/debuggability, **choose clarity**.

Code that can be understood at a glance is better than code that requires jumping to other files to understand what's happening. The goal is to reduce the mental effort needed to read and understand the code.

**Example 1: Explicit DI Resolution with Custom Error Messages**

**✅ DO:** Repeat the DI resolution pattern with specific error messages
```csharp
// In each Page constructor
ViewModel = App.Services.GetService(typeof(ClientDetailViewModel)) as ClientDetailViewModel
            ?? throw new InvalidOperationException("ClientDetailViewModel not registered");
```

**❌ DON'T:** Use a generic helper that hides the error context
```csharp
// Avoid:
ViewModel = App.GetService<ClientDetailViewModel>(); // Generic framework error message
```

**Rationale:**
- **Low cognitive load:** Everything you need to understand is right there - no need to jump to `App.GetService<T>()` implementation
- **Better debugging:** Custom error messages clearly identify which ViewModel failed to resolve
- **Self-contained:** The intent and error handling are visible at the call site

**Example 2: Explicit ViewModel Properties Over Shared Logic**

**✅ DO:** Create specific properties in each ViewModel
```csharp
// In PomodoroSettingsViewModel
public bool IsNotSaving => !IsSaving;

// In ClientDetailViewModel
public bool IsNotSaving => !IsSaving;
```

**❌ DON'T:** Create complex base class logic or converters to avoid repetition
```csharp
// Avoid:
public class ViewModelBase
{
    private readonly Dictionary<string, Func<bool>> _inverters = new();
    public bool GetInverted(string propertyName) { ... } // Too clever
}
```

**Rationale:**
- **Glanceable:** You see `IsNotSaving => !IsSaving` right in the ViewModel - understand it immediately
- **No context switching:** Don't need to navigate to base classes or converter files
- **Low cognitive load:** The logic is trivial and visible at a glance
- **IDE-friendly:** Find all usages, rename refactoring work perfectly

**When to Use DRY:**
- Business logic that encapsulates domain rules
- Complex algorithms that would be error-prone if duplicated
- Data access patterns (Repository pattern)
- Infrastructure concerns (logging, error handling frameworks)

**When to Use PRY:**
- Simple property wrappers and transformations
- UI-specific state properties
- Error messages and user-facing text
- Configuration and setup code that benefits from being explicit

## Error Handling and Logging

### ALWAYS Use Comprehensive Logging

**Core Principle: Every operation should be logged**

Logging is not optional - it's a critical part of production-ready applications. All applications must have comprehensive logging throughout all layers.

**✅ ALWAYS:**
1. Inject `ILogger<T>` into every service and ViewModel
2. Log operation start, success, and failure
3. Use structured logging with parameters
4. Log appropriate levels: Information, Warning, Error, Critical

**Service Layer Example:**
```csharp
public class ClientService(IUnitOfWork unitOfWork, ILogger<ClientService> logger) : IClientService
{
    public async Task<ClientDto> CreateClientAsync(CreateClientDto dto)
    {
        try
        {
            logger.LogInformation("Creating new client: {ClientName}", dto.Name);

            // Business rule validation
            if (await unitOfWork.Clients.ExistsWithNameAsync(dto.Name))
            {
                logger.LogWarning("Attempt to create client with duplicate name: {ClientName}", dto.Name);
                throw new InvalidOperationException($"A client with the name '{dto.Name}' already exists");
            }

            // ... create logic ...

            logger.LogInformation("Successfully created client {ClientId}: {ClientName}", client.Id, client.Name);
            return MapToDto(client);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Error creating client: {ClientName}", dto.Name);
            throw;
        }
    }
}
```

**ViewModel Layer Example:**
```csharp
public partial class ClientListViewModel : ViewModelBase
{
    private readonly ILogger<ClientListViewModel> _logger;

    private async Task LoadClientsAsync()
    {
        try
        {
            _logger.LogInformation("Loading clients in UI");
            IsLoading = true;
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<ClientDto>(clients);
            _logger.LogInformation("Successfully loaded {Count} clients in UI", Clients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients in UI");
            await _dialogService.ShowErrorAsync(
                "Unable to load clients. Please try again or contact support if the problem persists.",
                "Error Loading Clients");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

**Logging Levels:**
- **LogInformation**: Normal operations (start, success, counts)
- **LogWarning**: Business rule violations, expected failures
- **LogError**: Unexpected exceptions, operation failures
- **LogCritical**: Application startup failures, fatal errors

### ALWAYS Show User-Friendly Error Messages

**Core Principle: Never show technical details to users**

Users should see helpful, actionable messages - not exception details or stack traces.

**❌ DON'T:**
```csharp
catch (Exception ex)
{
    await _dialogService.ShowErrorAsync($"Failed to load clients: {ex.Message}", "Error");
}
```

**✅ DO:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error loading clients in UI");
    await _dialogService.ShowErrorAsync(
        "Unable to load clients. Please try again or contact support if the problem persists.",
        "Error Loading Clients");
}
```

**Benefits:**
- Technical details are logged for developers
- Users see friendly, actionable messages
- Reduces user anxiety and confusion
- Maintains professional appearance

### ALWAYS Wrap Try-Catch Around Async Operations

Every async operation should have proper error handling. Never let exceptions escape unhandled.

**✅ DO:** Wrap all async operations in try-catch
```csharp
private async Task LoadDataAsync()
{
    try
    {
        _logger.LogInformation("Starting data load");
        await _service.GetDataAsync();
        _logger.LogInformation("Data loaded successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load data");
        await ShowUserFriendlyError();
    }
}
```

### ALWAYS Use Structured Logging in Pages

**Core Principle: Use ILogger instead of Debug.WriteLine for capturing errors in telemetry**

Pages should use proper structured logging with `ILogger<T>` instead of `Debug.WriteLine` so errors are captured in log sinks and telemetry systems.

**❌ DON'T:** Use Debug.WriteLine
```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Error initializing page: {ex}");
}
```

**✅ DO:** Use structured logging with parameters and show user-friendly errors
```csharp
public sealed partial class ProjectDetailPage : Page
{
    public ProjectDetailViewModel ViewModel { get; }
    private readonly ILogger<ProjectDetailPage> _logger;
    private readonly IDialogService _dialogService;

    public ProjectDetailPage()
    {
        // Resolve dependencies first (BEFORE InitializeComponent)
        _logger = App.GetService<ILogger<ProjectDetailPage>>();
        _dialogService = App.GetService<IDialogService>();
        ViewModel = App.Services.GetService(typeof(ProjectDetailViewModel)) as ProjectDetailViewModel
                    ?? throw new InvalidOperationException("ProjectDetailViewModel not registered");

        // Initialize XAML after ViewModel is ready
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = InitializeAsync(e.Parameter);
    }

    private async Task InitializeAsync(object? parameter)
    {
        try
        {
            _logger.LogInformation("Initializing ProjectDetailPage with parameter {Parameter}", parameter);

            if (parameter is int projectId)
            {
                await ViewModel.InitializeForEditAsync(projectId);
                _logger.LogInformation("ProjectDetailPage initialized for editing project {ProjectId}", projectId);
            }
            else
            {
                await ViewModel.InitializeForAddAsync();
                _logger.LogInformation("ProjectDetailPage initialized for adding new project");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ProjectDetailPage with parameter {Parameter}", parameter);
            await _dialogService.ShowErrorAsync("Unable to load project. Please try again.");
        }
    }
}
```

**Key Points:**
1. **Inject ILogger<TPage> and IDialogService** in constructor using `App.GetService<T>()`
2. **Resolve dependencies BEFORE InitializeComponent()** (see Constructor Initialization Order section)
3. **Use structured logging** with parameters: `_logger.LogError(ex, "Message {Param}", value)`
4. **Never use string interpolation** in log messages - always use parameters
5. **Log initialization start** with relevant parameters
6. **Log success states** for important operations
7. **Log errors** with full exception and context parameters
8. **Show user-friendly error dialogs** using DialogService (never show technical details to users)

**Benefits:**
- Errors captured in Application Insights, Serilog sinks, etc.
- Structured parameters enable querying and filtering
- Exception details preserved for debugging
- Telemetry tracks page navigation and initialization success/failure
- Production debugging capabilities

**Structured Logging Format:**
```csharp
// Use parameters, not string interpolation
_logger.LogError(ex, "Error initializing {PageName} with parameter {Parameter}", "ProjectDetailPage", parameter);

// NOT this:
_logger.LogError(ex, $"Error initializing ProjectDetailPage with parameter {parameter}");
```

## Dependency Injection and Disposal

### NEVER Dispose Injected Dependencies

**Core Principle: Only dispose what you create**

When a dependency is injected via constructor (from the DI container), the container owns its lifetime. Do NOT dispose it manually.

**❌ NEVER DO:**
```csharp
public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;

    public void Dispose()
    {
        _context.Dispose(); // ❌ WRONG - DI container owns this!
    }
}
```

**✅ ALWAYS DO:**
```csharp
public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ApplicationDbContext _context = context;
    private IDbContextTransaction? _transaction;

    public void Dispose()
    {
        // Only dispose resources WE created
        _transaction?.Dispose();

        // DO NOT dispose _context - it's owned by the DI container
    }
}
```

**Why This Matters:**
- **Ownership Rule**: If you didn't create it (via `new`), don't dispose it
- **DI Container Manages Lifetime**: Registered services are disposed automatically at end of scope
- **Prevents Double Disposal**: Manual + automatic disposal causes errors
- **Shared Instances**: Multiple services might share the same instance in a scope

**When to Dispose:**
- ✅ Objects you create with `new` (like `IDbContextTransaction`)
- ✅ Unmanaged resources you allocate
- ❌ Dependencies injected via constructor
- ❌ Services resolved from DI container in constructor

**Example - DbContext Registration:**
```csharp
// In App.xaml.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
services.AddScoped<IUnitOfWork, UnitOfWork>();

// The DI container will:
// 1. Create ApplicationDbContext per scope
// 2. Inject it into UnitOfWork
// 3. Dispose it automatically at end of scope
```

## Async/Await Patterns

### ALWAYS Use CancellationToken for Async Methods

**Core Principle: All async I/O operations should support cancellation**

CancellationToken enables responsive UIs, timeout support, and proper resource cleanup.

**✅ ALWAYS ADD:**
Add `CancellationToken cancellationToken = default` as the last parameter to all async methods that perform I/O.

**Repository Layer Example:**
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
}

public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : class
{
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }
}
```

**Service Layer Example:**
```csharp
public interface IClientService
{
    Task<IEnumerable<ClientDto>> GetAllClientsAsync(CancellationToken cancellationToken = default);
    Task<ClientDto> CreateClientAsync(CreateClientDto dto, CancellationToken cancellationToken = default);
}

public class ClientService(IUnitOfWork unitOfWork, ILogger<ClientService> logger) : IClientService
{
    public async Task<IEnumerable<ClientDto>> GetAllClientsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrieving all clients");
            var clients = await unitOfWork.Clients.GetAllWithProjectsAsync(cancellationToken);
            return clients.Select(MapToDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all clients");
            throw;
        }
    }
}
```

**ViewModel Layer Example:**
```csharp
public partial class ClientListViewModel : ViewModelBase
{
    private CancellationTokenSource? _loadCancellation;

    private async Task LoadClientsAsync()
    {
        try
        {
            // Cancel previous operation if still running
            _loadCancellation?.Cancel();
            _loadCancellation = new CancellationTokenSource();

            _logger.LogInformation("Loading clients in UI");
            IsLoading = true;

            var clients = await _clientService.GetAllClientsAsync(_loadCancellation.Token);
            Clients = new ObservableCollection<ClientDto>(clients);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client loading was cancelled");
            // Don't show error to user - cancellation is expected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading clients in UI");
            await _dialogService.ShowErrorAsync(
                "Unable to load clients. Please try again.",
                "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Clean up on dispose
    public void Dispose()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
```

**Benefits:**
- **Responsive UI**: Cancel operations when user navigates away
- **Timeout Support**: Implement operation timeouts
- **Resource Cleanup**: Database connections released promptly
- **Cooperative Cancellation**: Proper async cancellation pattern

**Implementation Rules:**
1. **Last Parameter**: Always make it the last parameter with default value
2. **Pass Through**: Always pass the token to underlying async methods
3. **EF Core**: Use `ToListAsync(cancellationToken)`, `FirstOrDefaultAsync(..., cancellationToken)`, etc.
4. **Handle OperationCanceledException**: Catch and handle cancellation gracefully
5. **Don't Show Errors**: Cancellation is normal, don't show error dialogs to users

**What Methods Need CancellationToken:**
- ✅ Database queries (EF Core methods)
- ✅ File I/O operations
- ✅ Network calls
- ✅ Any Task-returning method that does I/O
- ❌ Synchronous methods (Update, Delete without async)
- ❌ Pure computation methods

### NEVER Use Async Void (Except Event Handlers)

**Core Principle: Async void is dangerous and should be avoided**

`async void` methods cannot be awaited, exceptions escape, and they cause unpredictable behavior.

**❌ NEVER DO:**
```csharp
// BAD - Exceptions escape, cannot be awaited
protected override async void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    await ViewModel.LoadAsync();
}
```

**✅ ALWAYS DO:**
```csharp
// GOOD - Synchronous override calls async Task helper
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    _ = InitializeAsync();
}

private async Task InitializeAsync()
{
    try
    {
        await ViewModel.LoadAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading page");
        // Show user-friendly message
    }
}
```

**Pattern for Lifecycle Method Overrides:**

1. Override as **synchronous** (remove `async void`)
2. Call fire-and-forget with `_ = MethodAsync();`
3. Create private `async Task` helper method
4. Include try-catch with logging and user-friendly errors

**Why This Matters:**
- Exceptions are caught and logged properly
- Users see friendly error messages instead of crashes
- Follows async best practices
- Debuggable and maintainable

**Only Exception:**
- Framework event handlers that require `async void` (like `App.OnLaunched` in WinUI3)
- These must use try-catch internally for safety

**Example Pattern for All Pages:**
```csharp
public sealed partial class MyPage : Page
{
    public MyViewModel ViewModel { get; }
    private readonly ILogger<MyPage> _logger;

    public MyPage()
    {
        ViewModel = App.Services.GetService(typeof(MyViewModel)) as MyViewModel
                    ?? throw new InvalidOperationException("MyViewModel not registered");
        _logger = App.GetService<ILogger<MyPage>>();
        this.InitializeComponent();
    }

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
            _logger.LogInformation("Page initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing page");
            await _dialogService.ShowErrorAsync("Unable to load page. Please try again.");
        }
    }
}
```

**Future Improvement - CancellationToken Support:**

While the current implementation doesn't include CancellationToken, consider adding it once ViewModels support cancellation:

```csharp
private async Task InitializeAsync(object? parameter, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Initializing page with parameter {Parameter}", parameter);
        await ViewModel.LoadAsync(cancellationToken);
        _logger.LogInformation("Page initialized successfully");
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Page initialization was cancelled");
        // Don't show error - cancellation is expected
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error initializing page");
        await _dialogService.ShowErrorAsync("Unable to load page. Please try again.");
    }
}
```

This would enable:
- Cancellation when navigating away before load completes
- Timeout support for slow operations
- Better resource management

## WinUI 3 Specific Patterns and Solutions

This section documents WinUI 3-specific technical challenges and their solutions.

### Borderless Windows with Resize Functionality

**Challenge:** Creating a completely borderless window (no title bar, no visible borders) while maintaining resize functionality.

**Key Problem:** WinUI 3's `AppWindow.Presenter.SetBorderAndTitleBar(false, false)` doesn't fully remove borders, and `WM_NCCALCSIZE` can break resize if implemented incorrectly.

**✅ Complete Solution:**

```csharp
public sealed partial class TimerWindow : Window
{
    // Win32 API imports
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;

    private IntPtr _hWnd;
    private IntPtr _oldWndProc;
    private WndProcDelegate? _wndProcDelegate;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public TimerWindow()
    {
        this.InitializeComponent();
        InitializeWindow();

        // Trigger border removal after window is activated
        this.Activated += TimerWindow_Activated;
    }

    private void TimerWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // Only do this once on first activation
        this.Activated -= TimerWindow_Activated;

        // Force a window style refresh to apply the borderless style
        if (_appWindow != null)
        {
            var currentSize = _appWindow.Size;
            _appWindow.Resize(new SizeInt32(currentSize.Width + 1, currentSize.Height + 1));
            _appWindow.Resize(currentSize);
        }
    }

    private void InitializeWindow()
    {
        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = true;
                presenter.IsResizable = true;
            }
        }

        // Remove all window borders using Win32 styles
        RemoveWindowBorders();

        // Hook window procedure to handle borderless resize
        _wndProcDelegate = new WndProcDelegate(WndProc);
        _oldWndProc = GetWindowLongPtr(_hWnd, -4); // GWL_WNDPROC
        SetWindowLongPtr(_hWnd, -4, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }

    private void RemoveWindowBorders()
    {
        var style = GetWindowLong(_hWnd, GWL_STYLE);
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);

        // Remove caption and borders but keep thick frame for resizing
        style &= ~WS_CAPTION;
        style |= WS_THICKFRAME;

        SetWindowLong(_hWnd, GWL_STYLE, style);
        SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);

        // Extend DWM frame into entire window (removes non-client area)
        var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmExtendFrameIntoClientArea(_hWnd, ref margins);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Remove non-client area to make window fully borderless
        if (msg == WM_NCCALCSIZE && wParam != IntPtr.Zero)
        {
            return IntPtr.Zero; // Entire window is client area
        }

        // Create invisible resize border through hit testing
        if (msg == WM_NCHITTEST)
        {
            var result = CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);

            // Get mouse position
            var screenPoint = lParam.ToInt32();
            var x = (short)(screenPoint & 0xFFFF);
            var y = (short)((screenPoint >> 16) & 0xFFFF);

            GetWindowRect(hWnd, out var rect);

            const int borderWidth = 8; // Invisible hit area for resize

            var leftDist = x - rect.Left;
            var rightDist = rect.Right - x;
            var topDist = y - rect.Top;
            var bottomDist = rect.Bottom - y;

            bool isLeft = leftDist < borderWidth;
            bool isRight = rightDist < borderWidth;
            bool isTop = topDist < borderWidth;
            bool isBottom = bottomDist < borderWidth;

            // Return corner resize handles (only corners, not edges)
            if (isTop && isLeft) return new IntPtr(HTTOPLEFT);
            if (isTop && isRight) return new IntPtr(HTTOPRIGHT);
            if (isBottom && isLeft) return new IntPtr(HTBOTTOMLEFT);
            if (isBottom && isRight) return new IntPtr(HTBOTTOMRIGHT);

            // Block edge resizing
            if (isLeft || isRight || isTop || isBottom)
                return new IntPtr(1); // HTCLIENT

            return result;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }
}
```

**Key Points:**

1. **Timing Issue**: Border removal must be triggered AFTER window activation using the `Activated` event
2. **WM_NCCALCSIZE**: Return `IntPtr.Zero` to make entire window client area
3. **WM_NCHITTEST**: Create invisible 8px border for resize detection
4. **DWM Margins**: Set to -1 to extend frame into entire client area
5. **Window Styles**: Remove `WS_CAPTION` but keep `WS_THICKFRAME` for resizing
6. **Corner-Only Resize**: Manually detect corner positions in hit testing

**Common Pitfalls:**
- ❌ Removing borders before window activation - won't work
- ❌ Not preserving `WS_THICKFRAME` - loses resize capability
- ❌ Forgetting DWM extension - leaves visible borders
- ❌ Not handling WM_NCHITTEST - no resize handles

### Square Aspect Ratio Window Enforcement

**Challenge:** Force a window to maintain a square aspect ratio during resize operations.

**Key Problem:** Restricting to corner-only resize doesn't automatically maintain square shape - users can still create rectangles.

**✅ Solution:**

```csharp
// Add to WndProc in the borderless window implementation above
private const int WM_SIZING = 0x0214;

private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
{
    // ... WM_NCCALCSIZE and WM_NCHITTEST handlers ...

    // Enforce square aspect ratio during resize
    if (msg == WM_SIZING)
    {
        var rect = Marshal.PtrToStructure<RECT>(lParam);
        var size = Math.Max(rect.Width, rect.Height);
        var edge = wParam.ToInt32();

        // Adjust based on which corner is being dragged
        switch (edge)
        {
            case 1: // WMSZ_LEFT
            case 4: // WMSZ_TOPLEFT
            case 7: // WMSZ_BOTTOMLEFT
                rect.Left = rect.Right - size;
                rect.Bottom = rect.Top + size;
                break;

            case 2: // WMSZ_RIGHT
            case 5: // WMSZ_TOPRIGHT
            case 8: // WMSZ_BOTTOMRIGHT
                rect.Right = rect.Left + size;
                rect.Bottom = rect.Top + size;
                break;

            case 3: // WMSZ_TOP
            case 6: // WMSZ_BOTTOM
                rect.Right = rect.Left + size;
                rect.Bottom = rect.Top + size;
                break;
        }

        Marshal.StructureToPtr(rect, lParam, true);
        return new IntPtr(1); // TRUE - we handled it
    }

    return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
}
```

**How It Works:**

1. **WM_SIZING Message**: Intercepted during the resize operation (before applied)
2. **Calculate Square Size**: Use `Math.Max(width, height)` to get target dimension
3. **Adjust Rectangle**: Modify the proposed rectangle based on which corner is being dragged
4. **Left/Bottom Corners**: Adjust left edge and bottom edge to maintain square
5. **Right Corners**: Adjust right edge and bottom edge to maintain square
6. **Write Back**: Marshal the modified rectangle back to the lParam

**Key Points:**

- Intercept `WM_SIZING` during resize, not after (no flicker)
- Use larger dimension to ensure window doesn't shrink unexpectedly
- Adjust edges based on resize direction to feel natural
- Return `IntPtr(1)` to indicate we handled the message

**Benefits:**
- ✅ Real-time enforcement during resize
- ✅ No flickering or jumping
- ✅ Natural feel - grows in direction user drags
- ✅ Works with corner-only resize pattern

### Always-on-Top Windows

**Challenge:** Keep a window floating above all other windows.

**✅ Solution:**

```csharp
private void InitializeWindow()
{
    _hWnd = WindowNative.GetWindowHandle(this);
    var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
    _appWindow = AppWindow.GetFromWindowId(windowId);

    if (_appWindow != null)
    {
        var presenter = _appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true; // Keep window on top
        }
    }
}
```

**Key Points:**
- Use `OverlappedPresenter.IsAlwaysOnTop = true`
- Window stays on top even when unfocused
- Useful for timers, notifications, floating tools

### Combining Patterns: Borderless + Square + Always-On-Top

The complete implementation combines all three patterns:

```csharp
public sealed partial class TimerWindow : Window
{
    public TimerWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        this.Activated += TimerWindow_Activated;
    }

    private void InitializeWindow()
    {
        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            _appWindow.Resize(new SizeInt32(200, 200));

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false); // Borderless
                presenter.IsAlwaysOnTop = true;                // Float on top
                presenter.IsResizable = true;                  // Allow resize
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }

        RemoveWindowBorders(); // DWM + window styles

        // Hook WndProc for: WM_NCCALCSIZE, WM_NCHITTEST, WM_SIZING
        _wndProcDelegate = new WndProcDelegate(WndProc);
        _oldWndProc = GetWindowLongPtr(_hWnd, -4);
        SetWindowLongPtr(_hWnd, -4, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCCALCSIZE && wParam != IntPtr.Zero)
            return IntPtr.Zero; // Borderless

        if (msg == WM_NCHITTEST)
        {
            // ... corner-only resize hit testing ...
        }

        if (msg == WM_SIZING)
        {
            // ... square aspect ratio enforcement ...
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }
}
```

**Result:**
- Completely borderless window
- Always visible on top of other windows
- Resizable from corners only
- Maintains perfect square aspect ratio
- No flicker, smooth resize experience

**Use Cases:**
- Floating timer displays
- Always-visible notification panels
- Picture-in-picture style windows
- Widget-style UI elements
