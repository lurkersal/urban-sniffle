# Logging DI Registration Fix - Complete

**Date:** March 18, 2026  
**Issue:** `Unable to resolve service for type 'Microsoft.Extensions.Logging.ILogger`1[IndexEditor.Services.IndexFileService]'`  
**Status:** ✅ **FIXED**

---

## Problem Description

When running the IndexEditor from the command line after building with `scripts/clean-rebuild-indexeditor.sh`, the application crashed with:

```
Unhandled exception. System.InvalidOperationException: 
Unable to resolve service for type 'Microsoft.Extensions.Logging.ILogger`1[IndexEditor.Services.IndexFileService]' 
while attempting to activate 'IndexEditor.Services.IndexFileService'.
```

### Root Cause

The `IndexFileService` constructor requires `ILogger<IndexFileService>`:

```csharp
public class IndexFileService : IIndexFileService
{
    private readonly ILogger<IndexFileService> _logger;

    public IndexFileService(ILogger<IndexFileService> logger)
    {
        _logger = logger;
    }
    // ...
}
```

However, the DI container in `App.axaml.cs` was not configured with logging services. The logging was being set up separately using a `LoggerFactory`, but this factory was not registered in the DI container.

---

## Solution

### Before: Separate Logging Setup (❌ Broken)

```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Configure logging OUTSIDE of DI
    var factory = LoggerFactory.Create(builder =>
    {
        builder.AddSimpleConsole(...);
        builder.SetMinimumLevel(LogLevel.Debug);
    });
    DebugLogger.Initialize(factory);
    
    // Setup DI
    var services = new ServiceCollection();
    
    // Register services (but NOT logging)
    services.AddSingleton<IIndexFileService, IndexFileService>(); // ❌ Fails - needs ILogger
    
    var serviceProvider = services.BuildServiceProvider();
    // ...
}
```

**Problem:** Services registered in DI can't access `ILogger<T>` because logging wasn't registered.

### After: Integrated Logging in DI (✅ Fixed)

```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Setup DI FIRST
    var services = new ServiceCollection();
    
    // Register logging services IN DI (FIRST)
    services.AddLogging(builder =>
    {
        builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
        builder.SetMinimumLevel(LogLevel.Debug);
    });
    
    // Register other services (can now use ILogger<T>)
    services.AddSingleton<IEditorState, EditorStateService>();
    services.AddSingleton<IIndexFileService, IndexFileService>(); // ✅ Works - ILogger available
    // ... other services
    
    // Build provider
    var serviceProvider = services.BuildServiceProvider();
    
    // Initialize DebugLogger with DI-configured logging
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    DebugLogger.Initialize(loggerFactory);
    DebugLogger.Log("Logging initialized");
    // ...
}
```

**Benefits:**
- ✅ All services can inject `ILogger<T>`
- ✅ Centralized logging configuration
- ✅ Consistent with DI best practices
- ✅ DebugLogger uses same logger factory as DI services

---

## Changes Made

### File Modified: App.axaml.cs

#### Change 1: Register Logging Services in DI

**Line ~20-40** (Before):
```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Configure logging for application
    try
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        DebugLogger.Initialize(factory);
        DebugLogger.Log("Logging initialized");
    }
    catch (Exception ex)
    {
        // Error handling...
    }

    // Setup DI
    var services = new ServiceCollection();
```

**Line ~20-35** (After):
```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Setup DI (must happen before logging initialization so we can register logging)
    var services = new ServiceCollection();
    
    // Register logging services FIRST (required by other services)
    services.AddLogging(builder =>
    {
        builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
        builder.SetMinimumLevel(LogLevel.Debug);
    });
```

#### Change 2: Initialize DebugLogger After ServiceProvider Build

**Line ~65-70** (Before):
```csharp
    // Build provider
    var serviceProvider = services.BuildServiceProvider();

    // Set static providers for backwards-compatible static API
    ToastService.Provider = serviceProvider.GetRequiredService<IToastService>();
```

**Line ~65-78** (After):
```csharp
    // Build provider
    var serviceProvider = services.BuildServiceProvider();
    
    // Initialize DebugLogger with the DI-configured logging factory
    try
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        DebugLogger.Initialize(loggerFactory);
        DebugLogger.Log("Logging initialized");
    }
    catch (Exception ex)
    {
        // Fallback logging if DebugLogger initialization fails
        Console.WriteLine($"Failed to initialize DebugLogger: {ex.Message}");
    }

    // Set static providers for backwards-compatible static API
    ToastService.Provider = serviceProvider.GetRequiredService<IToastService>();
```

---

## Verification

### Build Status
```
✅ Build: SUCCESS
   - 0 Errors
   - 21 Warnings (pre-existing)
```

### Test Status
```
✅ All Refactoring Tests: 41/41 PASSING (100%)
   - ArticleCardRendererTests: 8/8
   - ArticleDisplayCoordinatorTests: 8/8  
   - ArticleFocusManagerTests: 5/5
   - SegmentManagementServiceTests: 8/8
   - PageNavigationCoordinatorTests: 12/12
```

### Runtime Verification
```bash
$ dotnet run --project src/index-editor/IndexEditor.csproj

Output:
info: IndexEditor[0]
      IndexEditor starting
09:13:31 info: IndexEditor[0] Logging initialized
09:13:31 info: IndexEditor[0] ArticleList: attached list handlers immediately in constructor
[DEBUG] PageControllerView: parameterless constructor (XAML)
[DEBUG] PageControllerView: services injected via SetServices
09:13:32 info: IndexEditor.Services.IndexFileService[0] LoadFromFolder: Input folder: '...'
```

✅ **Application launches successfully!**  
✅ **Logging works correctly**  
✅ **IndexFileService resolves ILogger<IndexFileService> from DI**

---

## Benefits of This Fix

### 1. **Proper Dependency Injection**
All services can now inject `ILogger<T>` through the DI container:

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)  // ✅ Works now!
    {
        _logger = logger;
    }
}
```

### 2. **Centralized Logging Configuration**
Logging is configured once in the DI container and used everywhere:
- Services get loggers via DI
- DebugLogger uses the same factory
- Consistent logging format across the app

### 3. **Testability**
Services can be tested with mocked loggers:

```csharp
// Unit test
var mockLogger = new Mock<ILogger<IndexFileService>>();
var service = new IndexFileService(mockLogger.Object);
```

### 4. **Follows Microsoft Best Practices**
The Microsoft.Extensions.Logging documentation recommends registering logging in the DI container, which we now do.

---

## Impact

| Aspect | Before | After |
|--------|--------|-------|
| **DI Container** | No logging | ✅ Logging registered |
| **ILogger<T> Resolution** | ❌ Failed | ✅ Works |
| **Application Launch** | ❌ Crashed | ✅ Launches |
| **Service Registration** | Manual factory | ✅ Through DI |
| **DebugLogger** | Separate factory | ✅ Uses DI factory |

---

## Technical Details

### DI Container Logging Registration

```csharp
services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;          // One line per log
        options.TimestampFormat = "HH:mm:ss ";  // Time prefix
    });
    builder.SetMinimumLevel(LogLevel.Debug);  // Log everything
});
```

This registers:
- `ILoggerFactory` - Factory for creating loggers
- `ILogger<T>` - Generic logger for any type T
- Console provider - Outputs to console

### Service Resolution Flow

1. DI container is configured with logging
2. `IndexFileService` is registered: `services.AddSingleton<IIndexFileService, IndexFileService>()`
3. When resolved, DI sees constructor needs `ILogger<IndexFileService>`
4. DI creates logger using registered `ILoggerFactory`
5. Service instantiated with logger ✅

---

## Related Services Using ILogger

The following services can now properly inject `ILogger<T>`:

- ✅ `IndexFileService` - File loading/saving
- ✅ (Any future service can now use ILogger<T>)

---

## Summary

The logging DI registration fix resolved the critical startup crash by properly integrating Microsoft.Extensions.Logging with the dependency injection container. This ensures all services can resolve `ILogger<T>` dependencies and follows Microsoft's recommended patterns for ASP.NET Core and .NET applications.

### Key Changes
1. ✅ Moved logging configuration into DI container
2. ✅ Registered logging BEFORE other services
3. ✅ Initialize DebugLogger from DI's ILoggerFactory
4. ✅ All services can now inject ILogger<T>

### Results
- ✅ Application launches successfully
- ✅ No DI resolution errors
- ✅ Logging works throughout the app
- ✅ All tests pass
- ✅ Follows best practices

---

**Date Fixed:** March 18, 2026  
**Build Status:** ✅ SUCCESS (0 errors)  
**Test Status:** ✅ 41/41 PASSING  
**Runtime Status:** ✅ APPLICATION LAUNCHES

