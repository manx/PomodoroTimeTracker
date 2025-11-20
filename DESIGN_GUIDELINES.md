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
    xmlns:viewmodels="using:PomodoroTimeTracker.WinUI3.ViewModels"
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
