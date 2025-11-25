# Unit Test Specialist - Pomodoro Time Tracker

**Activates when:** User asks for tests, mentions testing, unit tests, test coverage, or when creating/modifying testable code.

**Purpose:** Provide expert guidance on unit testing for this WinUI 3 application using Clean Architecture and MVVM patterns.

**Important:** When tests are added/modified, update **TEST_SUMMARY.md** to reflect current test coverage.

---

## Testing Framework Stack

### Core Frameworks
- **xUnit** - Primary test framework (.NET 9)
- **Moq** - Mocking framework for interfaces
- **FluentAssertions** - Readable assertions
- **Microsoft.EntityFrameworkCore.InMemory** - EF Core testing

### Project Structure (Current)
```
PomodoroTimeTracker.Tests/
├── Application/
│   ├── Services/
│   │   ├── PomodoroSessionServiceTests.cs    (20 tests) ✅
│   │   ├── PomodoroSettingsServiceTests.cs   (22 tests) ✅
│   │   ├── ClientServiceTests.cs             (18 tests) ✅
│   │   └── ProjectServiceTests.cs            (22 tests) ✅
│   └── TestUtilities/
│       └── (reserved for shared helpers)
└── Infrastructure/
    └── Repositories/
        ├── PomodoroSessionRepositoryTests.cs  (22 tests) ✅
        ├── PomodoroSettingsRepositoryTests.cs (11 tests) ✅
        ├── ClientRepositoryTests.cs           (15 tests) ✅
        └── ProjectRepositoryTests.cs          (14 tests) ✅

Total: 144 tests, 100% pass rate
```

**Coverage Status:**
- ✅ Application Services - Complete (82 tests)
- ✅ Infrastructure Repositories - Complete (62 tests)
- ⚠️ ViewModels - Not implemented (UI thread complexity)
- ⚠️ Domain Entities - Not needed (POCOs only)

---

## Testing Patterns for This Project

### 1. ViewModel Testing Pattern

**Key Points:**
- ViewModels use `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand)
- Mock all service dependencies (IPomodoroSessionService, etc.)
- Test property change notifications
- Test command CanExecute logic
- Test state transitions (PomodoroState enum)
- Use DispatcherQueue mocking for timer tests

**Example Structure:**
```csharp
public class PomodoroViewModelTests
{
    private readonly Mock<IPomodoroSessionService> _sessionService;
    private readonly Mock<IPomodoroSettingsService> _settingsService;
    private readonly Mock<IClientService> _clientService;
    private readonly Mock<IProjectService> _projectService;
    private readonly PomodoroViewModel _viewModel;

    public PomodoroViewModelTests()
    {
        _sessionService = new Mock<IPomodoroSessionService>();
        _settingsService = new Mock<IPomodoroSettingsService>();
        _clientService = new Mock<IClientService>();
        _projectService = new Mock<IProjectService>();

        // Setup default returns
        _settingsService.Setup(s => s.GetSettingsAsync())
            .ReturnsAsync(new PomodoroSettingsDto { WorkDurationMinutes = 25 });

        _viewModel = new PomodoroViewModel(
            _sessionService.Object,
            _settingsService.Object,
            _clientService.Object,
            _projectService.Object
        );
    }

    [Fact]
    public void State_WhenChanged_NotifiesDependentProperties()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName);

        // Act
        _viewModel.State = PomodoroState.Running;

        // Assert
        propertyChangedEvents.Should().Contain(nameof(_viewModel.IsRunningState));
        propertyChangedEvents.Should().Contain(nameof(_viewModel.IsSetupState));
        propertyChangedEvents.Should().Contain(nameof(_viewModel.IsPausedState));
    }

    [Fact]
    public async Task StartPomodoroCommand_WithValidObjective_CreatesSession()
    {
        // Arrange
        _viewModel.Objective = "Test objective";
        _viewModel.DurationMinutes = 25;

        _sessionService.Setup(s => s.CreateSessionAsync(It.IsAny<CreatePomodoroSessionDto>()))
            .ReturnsAsync(new PomodoroSessionDto { Id = Guid.NewGuid() });

        // Act
        await _viewModel.StartPomodoroCommand.ExecuteAsync(null);

        // Assert
        _sessionService.Verify(s => s.CreateSessionAsync(
            It.Is<CreatePomodoroSessionDto>(dto =>
                dto.Objective == "Test objective" &&
                dto.DurationMinutes == 25
            )), Times.Once);
        _viewModel.State.Should().Be(PomodoroState.Running);
    }
}
```

### 2. Service Testing Pattern

**Key Points:**
- Services depend on repositories via interfaces
- Use InMemory DbContext for integration-style tests
- Test business logic, not EF Core itself
- Verify correct repository method calls
- Test error handling and validation

**Example Structure:**
```csharp
public class PomodoroSessionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly PomodoroSessionService _service;

    public PomodoroSessionServiceTests()
    {
        // Setup InMemory DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.PomodoroSessions).Returns(new PomodoroSessionRepository(_context));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PomodoroSessionService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidData_ReturnsSessionDto()
    {
        // Arrange
        var createDto = new CreatePomodoroSessionDto
        {
            Objective = "Test task",
            DurationMinutes = 25,
            SessionType = SessionType.Work
        };

        // Act
        var result = await _service.CreateSessionAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Objective.Should().Be("Test task");
        result.DurationMinutes.Should().Be(25);
        _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WhenNoActiveSession_ReturnsNull()
    {
        // Arrange - no active sessions in database

        // Act
        var result = await _service.GetActiveSessionAsync();

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 3. Repository Testing Pattern

**Key Points:**
- Test against InMemory DbContext
- Verify LINQ queries work correctly
- Test filtering, sorting, pagination
- Verify cascade delete behavior
- Test unique constraints

**Example Structure:**
```csharp
public class PomodoroSessionRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PomodoroSessionRepository _repository;

    public PomodoroSessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new PomodoroSessionRepository(_context);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithRunningSession_ReturnsSession()
    {
        // Arrange
        var activeSession = new PomodoroSession
        {
            Id = Guid.NewGuid(),
            Objective = "Active task",
            StartTime = DateTime.UtcNow,
            EndTime = null,
            IsCompleted = false,
            SessionType = SessionType.Work
        };

        await _context.PomodoroSessions.AddAsync(activeSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSessionAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(activeSession.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

---

## Testing Timer Logic

### Challenge: DispatcherQueueTimer
The timer uses `DispatcherQueueTimer` which runs on UI thread. For testing:

**Option 1: Extract Timer Interface**
```csharp
public interface ITimerService
{
    void Start();
    void Stop();
    event EventHandler Tick;
}

// Mock in tests
var mockTimer = new Mock<ITimerService>();
mockTimer.Raise(t => t.Tick += null, EventArgs.Empty);
```

**Option 2: Test State Changes Without Timer**
```csharp
[Fact]
public void Timer_Tick_DecrementsRemainingSeconds()
{
    // Arrange
    _viewModel.State = PomodoroState.Running;
    var initialSeconds = _viewModel.RemainingSeconds;

    // Act - Call the tick handler directly (make it internal for testing)
    _viewModel.OnTimerTick(); // Internal method for testing

    // Assert
    _viewModel.RemainingSeconds.Should().Be(initialSeconds - 1);
}
```

---

## Test Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedResult`

**Examples:**
```csharp
StartPomodoroCommand_WithEmptyObjective_CannotExecute()
StartPomodoroCommand_WithValidObjective_CreatesSession()
State_WhenChangedToRunning_NotifiesDependentProperties()
GetActiveSessionAsync_WhenMultipleSessions_ReturnsOnlyActive()
CreateSessionAsync_WithInvalidData_ThrowsValidationException()
```

---

## AAA Pattern (Arrange-Act-Assert)

Always structure tests in three clear sections:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange - Setup test data and mocks
    var input = new SomeDto { /* ... */ };
    _mockService.Setup(s => s.MethodAsync()).ReturnsAsync(result);

    // Act - Execute the method under test
    var result = await _sut.MethodAsync(input);

    // Assert - Verify expectations
    result.Should().NotBeNull();
    result.Property.Should().Be(expectedValue);
    _mockService.Verify(s => s.MethodAsync(), Times.Once);
}
```

---

## FluentAssertions Examples

Use FluentAssertions for readable assertions:

```csharp
// Collections
result.Should().HaveCount(5);
result.Should().Contain(x => x.Id == expectedId);
result.Should().BeInAscendingOrder(x => x.StartTime);

// Objects
result.Should().NotBeNull();
result.Should().BeOfType<PomodoroSessionDto>();
result.Should().BeEquivalentTo(expected, options => options.Excluding(x => x.Id));

// Strings
result.Objective.Should().NotBeNullOrWhiteSpace();
result.Objective.Should().HaveLength(90);

// Booleans
_viewModel.IsSetupState.Should().BeTrue();
_viewModel.CanStart.Should().BeFalse();

// Exceptions
await FluentActions.Invoking(() => _service.MethodAsync(null))
    .Should().ThrowAsync<ArgumentNullException>();
```

---

## Mock Setup Patterns

### Async Methods
```csharp
_mockService.Setup(s => s.GetAsync(It.IsAny<Guid>()))
    .ReturnsAsync(new PomodoroSessionDto { /* ... */ });
```

### Void Methods
```csharp
_mockService.Setup(s => s.Delete(It.IsAny<Guid>()));
```

### Throwing Exceptions
```csharp
_mockService.Setup(s => s.MethodAsync())
    .ThrowsAsync(new InvalidOperationException("Test error"));
```

### Conditional Returns
```csharp
_mockService.Setup(s => s.GetAsync(It.Is<Guid>(id => id == specificId)))
    .ReturnsAsync(specificResult);
```

### Verify Calls
```csharp
_mockService.Verify(s => s.MethodAsync(), Times.Once);
_mockService.Verify(s => s.MethodAsync(), Times.Never);
_mockService.Verify(s => s.MethodAsync(It.Is<int>(x => x > 0)), Times.Exactly(2));
```

---

## Test Generation Strategy

When creating tests for any function/method, use this systematic approach:

### 1. Core Functionality Tests
- **Test the main purpose** - What is the method supposed to do?
- **Verify return values** with typical inputs
- **Test realistic scenarios** - Common use cases
- **Happy path first** - Ensure basic functionality works

**Example:**
```csharp
[Fact]
public async Task CreateSessionAsync_WithValidData_ReturnsSessionDto()
{
    // Arrange
    var createDto = new CreatePomodoroSessionDto
    {
        Objective = "Complete feature",
        DurationMinutes = 25
    };

    // Act
    var result = await _service.CreateSessionAsync(createDto);

    // Assert
    result.Should().NotBeNull();
    result.Objective.Should().Be("Complete feature");
    result.DurationMinutes.Should().Be(25);
}
```

### 2. Input Validation Tests
- **Invalid input types** - Wrong data types if applicable
- **Null/empty values** - Test null strings, empty collections, etc.
- **Boundary values** - Min/max, zero, negative numbers
- **Invalid combinations** - Conflicting parameters

**Example:**
```csharp
[Theory]
[InlineData(null)]         // Null objective
[InlineData("")]           // Empty string
[InlineData("   ")]        // Whitespace only
public async Task CreateSessionAsync_WithInvalidObjective_ThrowsArgumentException(string objective)
{
    // Arrange
    var createDto = new CreatePomodoroSessionDto { Objective = objective };

    // Act & Assert
    await FluentActions.Invoking(() => _service.CreateSessionAsync(createDto))
        .Should().ThrowAsync<ArgumentException>()
        .WithMessage("*objective*");
}

[Theory]
[InlineData(0)]            // Zero
[InlineData(-1)]           // Negative
[InlineData(121)]          // Above max (assuming 120 is max)
public async Task CreateSessionAsync_WithInvalidDuration_ThrowsArgumentException(int duration)
{
    // Arrange
    var createDto = new CreatePomodoroSessionDto
    {
        Objective = "Valid",
        DurationMinutes = duration
    };

    // Act & Assert
    await FluentActions.Invoking(() => _service.CreateSessionAsync(createDto))
        .Should().ThrowAsync<ArgumentException>()
        .WithMessage("*duration*");
}
```

### 3. Error Handling Tests
- **Expected exceptions** are thrown
- **Error messages** are meaningful and specific
- **Graceful degradation** - System doesn't crash
- **Resource cleanup** - Disposables are disposed

**Example:**
```csharp
[Fact]
public async Task CompleteSessionAsync_WithNonExistentId_ThrowsNotFoundException()
{
    // Arrange
    var nonExistentId = 999;

    // Act & Assert
    await FluentActions.Invoking(() => _service.CompleteSessionAsync(nonExistentId))
        .Should().ThrowAsync<NotFoundException>()
        .WithMessage($"Session with ID {nonExistentId} not found");
}

[Fact]
public async Task CreateSessionAsync_WhenDatabaseFailsAsync_ThrowsAndDoesNotPartiallyCommit()
{
    // Arrange
    _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new DbUpdateException("Database error"));

    // Act & Assert
    await FluentActions.Invoking(() => _service.CreateSessionAsync(validDto))
        .Should().ThrowAsync<DbUpdateException>();

    // Verify no partial state
    var sessions = await _repository.GetAllAsync();
    sessions.Should().BeEmpty();
}
```

### 4. Side Effects Tests
- **External calls** are made correctly
- **State changes** occur as expected
- **Dependency interactions** are verified
- **Event raising** if applicable

**Example:**
```csharp
[Fact]
public async Task CompleteSessionAsync_UpdatesSessionAndSavesChanges()
{
    // Arrange
    var session = await CreateTestSession();

    // Act
    await _service.CompleteSessionAsync(session.Id);

    // Assert - Verify state change
    var updated = await _service.GetByIdAsync(session.Id);
    updated.IsCompleted.Should().BeTrue();
    updated.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

    // Assert - Verify save was called
    _unitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
}
```

### 5. Boundary and Edge Cases
- **Empty collections** - How does method handle empty lists?
- **Single item** vs **multiple items**
- **Maximum limits** - What happens at capacity?
- **Concurrent access** - Thread safety if relevant

**Example:**
```csharp
[Fact]
public async Task GetSessionsByDateRange_WithNoSessions_ReturnsEmptyList()
{
    // Act
    var result = await _service.GetSessionsByDateRangeAsync(DateTime.UtcNow, DateTime.UtcNow);

    // Assert
    result.Should().BeEmpty();
}

[Theory]
[InlineData(1)]     // Single session
[InlineData(10)]    // Multiple sessions
[InlineData(100)]   // Many sessions
public async Task GetAllAsync_ReturnsAllSessions(int sessionCount)
{
    // Arrange
    await CreateTestSessions(sessionCount);

    // Act
    var result = await _service.GetAllAsync();

    // Assert
    result.Should().HaveCount(sessionCount);
}
```

---

## Test Organization

### Using Regions for Clarity

Group related tests using `#region`:

```csharp
public class PomodoroSessionServiceTests
{
    #region Constructor and Setup

    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly PomodoroSessionService _service;

    // Constructor...

    #endregion

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidData_ReturnsSessionDto() { }

    [Fact]
    public async Task CreateSessionAsync_WithNullObjective_ThrowsArgumentException() { }

    #endregion

    #region GetActiveSessionAsync Tests

    [Fact]
    public async Task GetActiveSessionAsync_WhenNoActiveSession_ReturnsNull() { }

    [Fact]
    public async Task GetActiveSessionAsync_WithActiveSession_ReturnsSession() { }

    #endregion

    #region CompleteSessionAsync Tests

    // ...

    #endregion
}
```

### Alternative: Nested Classes

For complex classes, use nested test classes:

```csharp
public class PomodoroSessionServiceTests
{
    public class CreateSessionAsyncTests : PomodoroSessionServiceTests
    {
        [Fact]
        public async Task WithValidData_ReturnsSessionDto() { }

        [Fact]
        public async Task WithNullObjective_ThrowsException() { }
    }

    public class CompleteSessionAsyncTests : PomodoroSessionServiceTests
    {
        [Fact]
        public async Task WithValidId_CompletesSession() { }

        [Fact]
        public async Task WithInvalidId_ThrowsNotFoundException() { }
    }
}
```

---

## Testing Checklist

When adding tests for new code:

### ViewModels
- [ ] **Core Functionality**
  - [ ] Property change notifications
  - [ ] Command CanExecute logic
  - [ ] Command execution behavior
  - [ ] State transitions
- [ ] **Input Validation**
  - [ ] Empty/null string properties
  - [ ] Numeric boundaries (duration, counts)
  - [ ] Invalid state transitions
- [ ] **Error Handling**
  - [ ] Service call failures
  - [ ] Validation errors
  - [ ] Graceful degradation
- [ ] **Side Effects**
  - [ ] Service method calls
  - [ ] Collection updates (ObservableCollection)
  - [ ] Event raising

### Services
- [ ] **Core Functionality**
  - [ ] CRUD operations (Create, Read, Update, Delete)
  - [ ] Business logic execution
  - [ ] DTO mapping
- [ ] **Input Validation**
  - [ ] Null/empty parameters
  - [ ] Invalid IDs (negative, zero, non-existent)
  - [ ] Boundary values (min/max durations, etc.)
  - [ ] Invalid data combinations
- [ ] **Error Handling**
  - [ ] Repository exceptions
  - [ ] Validation failures
  - [ ] Database errors
  - [ ] Meaningful error messages
- [ ] **Side Effects**
  - [ ] Repository method calls (Verify)
  - [ ] Unit of work SaveChanges calls
  - [ ] Transaction handling

### Repositories
- [ ] **Core Functionality**
  - [ ] Query correctness (LINQ)
  - [ ] Filtering logic
  - [ ] Sorting logic
  - [ ] Include (eager loading) behavior
- [ ] **Input Validation**
  - [ ] Null parameters
  - [ ] Invalid IDs
  - [ ] Empty filter criteria
- [ ] **Edge Cases**
  - [ ] Empty result sets
  - [ ] Single vs multiple results
  - [ ] Large datasets
- [ ] **Data Integrity**
  - [ ] Unique constraint violations
  - [ ] Cascade delete behavior
  - [ ] Foreign key constraints

---

## Test Data Builders

For complex entities, use builder pattern:

```csharp
public class PomodoroSessionBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _objective = "Default objective";
    private DateTime _startTime = DateTime.UtcNow;
    private DateTime? _endTime = null;
    private int _durationMinutes = 25;
    private bool _isCompleted = false;
    private SessionType _sessionType = SessionType.Work;

    public PomodoroSessionBuilder WithObjective(string objective)
    {
        _objective = objective;
        return this;
    }

    public PomodoroSessionBuilder AsCompleted()
    {
        _isCompleted = true;
        _endTime = _startTime.AddMinutes(_durationMinutes);
        return this;
    }

    public PomodoroSession Build()
    {
        return new PomodoroSession
        {
            Id = _id,
            Objective = _objective,
            StartTime = _startTime,
            EndTime = _endTime,
            DurationMinutes = _durationMinutes,
            IsCompleted = _isCompleted,
            SessionType = _sessionType
        };
    }
}

// Usage
var session = new PomodoroSessionBuilder()
    .WithObjective("Test task")
    .AsCompleted()
    .Build();
```

---

## Known Testing Gaps in Project

Based on CLAUDE.md documentation:

### HIGH PRIORITY
1. **PomodoroViewModel** - No tests exist (~700 lines of complex logic)
   - State machine transitions
   - Timer tick behavior
   - Break cycle management
   - Stop dialog logic

2. **Service Layer** - No integration tests
   - PomodoroSessionService
   - PomodoroSettingsService
   - ClientService
   - ProjectService

### MEDIUM PRIORITY
3. **Repository Layer** - Basic CRUD tests needed
4. **Domain Entities** - Validation logic tests

### LOW PRIORITY
5. **ViewModels** (other than Pomodoro) - Settings, Client, Project ViewModels

---

## NuGet Packages Required

Add to test project:

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
</ItemGroup>
```

---

## Quick Start Command

When user asks to create tests, immediately:

1. **Identify the target** (ViewModel, Service, Repository)
2. **Choose the pattern** from above
3. **Create test class** with appropriate mocks
4. **Write tests** covering:
   - Happy path
   - Edge cases
   - Error conditions
   - State changes

**Always use:**
- xUnit `[Fact]` attribute
- Moq for mocking
- FluentAssertions for assertions
- AAA pattern
- Descriptive test names

---

## Maintaining TEST_SUMMARY.md

**IMPORTANT:** When adding or modifying tests, you MUST update `TEST_SUMMARY.md` to keep it current.

### When to Update TEST_SUMMARY.md

1. **After adding new test class:**
   - Update test file count
   - Update total test count
   - Add new section describing the test class
   - Update test distribution chart

2. **After adding tests to existing class:**
   - Update test count for that class
   - Update total test count
   - Update coverage description if new scenarios added

3. **After modifying business rules:**
   - Update "Business Rules Validated" section
   - Document new edge cases if added
   - Update related test descriptions

4. **After running full test suite:**
   - Update execution time if significantly changed
   - Update pass rate if any failures
   - Add entry to "Test Statistics History" table

### Update Checklist

Before committing test changes:

- [ ] Run full test suite: `dotnet test`
- [ ] Verify 100% pass rate
- [ ] Update test count in TEST_SUMMARY.md header
- [ ] Update relevant test class section
- [ ] Update "Test Statistics" table
- [ ] Update "Last Updated" date
- [ ] Commit TEST_SUMMARY.md with test changes

### Quick Update Commands

**Get current test count:**
```bash
dotnet test --list-tests | wc -l
```

**Run tests with timing:**
```bash
dotnet test --verbosity normal
```

**Filter by namespace:**
```bash
# Application layer
dotnet test --filter "FullyQualifiedName~Application.Services"

# Infrastructure layer
dotnet test --filter "FullyQualifiedName~Infrastructure.Repositories"
```

### TEST_SUMMARY.md Structure

Keep these sections synchronized:

1. **Test Statistics** - Overall numbers
2. **Application Layer Tests** - Service test details
3. **Infrastructure Layer Tests** - Repository test details
4. **Test Statistics History** - Historical tracking

**Example Update:**
```markdown
## Test Statistics

**Total Tests:** 150  <-- Update this
**Pass Rate:** 100%
**Execution Time:** ~500ms  <-- Update if changed

### Test Distribution

Application Layer Tests (85)  <-- Update counts
├── NewServiceTests (3)  <-- Add if new
...
```

---

**Last Updated:** 2025-11-25
**Current Test Count:** 144 tests (82 service, 62 repository)
**Maintained By:** Development Team
