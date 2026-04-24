# Test Fixes Summary

## Status: Partially Fixed ✅

After reorganizing tests from `src/<project>/Tests/` to `tests/<project>/`, some tests were failing due to mocking issues with Avalonia controls.

## Fixed Issues

### 1. KeyboardShortcutDispatcherTests ✅
**Problem:** `TestKeyEventArgs` was trying to override `Key` property which is not virtual.

**Solution:** Used `Mock<KeyEventArgs>` instead of inheritance.

```csharp
private KeyEventArgs CreateKeyEvent(Key key = Key.A)
{
    var mock = new Mock<KeyEventArgs>();
    mock.Setup(e => e.Key).Returns(key);
    return mock.Object;
}
```

### 2. Avalonia Control Mocking ✅
**Problem:** Attempting to mock non-virtual properties (`IsVisible`, `WindowState`) on Avalonia controls.

**Solution:** Changed from mocking controls to using real Avalonia control instances.

**Before:**
```csharp
private Mock<Border> CreateMockBorder(bool isVisible = false)
{
    var mockBorder = new Mock<Border>();
    mockBorder.SetupProperty(b => b.IsVisible, isVisible);
    return mockBorder;
}
```

**After:**
```csharp
private Border CreateBorder(bool isVisible = false)
{
    var border = new Border { IsVisible = isVisible };
    return border;
}
```

## Test Results

### ✅ Common.Tests: 100% Pass Rate
- **Status**: All 31 tests passing
- **Duration**: 39ms
- **Issues**: None

### ⚠️ IndexEditor.Tests: 66% Pass Rate
- **Status**: 79 passing, 41 failing
- **Duration**: 481ms
- **Improvement**: Fixed 33 mock-related test failures

### ✅ FileRenamer.Tests: 100% Pass Rate
- **Status**: All 2 tests passing
- **Duration**: 2ms
- **Issues**: None

## Remaining Issues

### OverlayManagerTests & FullscreenImageServiceTests (41 failures)

**Root Cause:** Cannot mock `FindControl<T>()` extension method with Moq.

**Error:**
```
System.NotSupportedException: Unsupported expression: w => w.FindControl<Border>("IndexOverlay")
Extension methods (here: ControlExtensions.FindControl) may not be used in setup / verification expressions.
```

**Explanation:**
- `FindControl` is an extension method on `IControl`
- Moq cannot intercept extension methods
- Tests are trying to mock `mockWindow.Setup(w => w.FindControl<Border>("IndexOverlay"))`

## Recommended Solutions

### Option 1: Refactor Services to Use Dependency Injection
Inject controls directly instead of using FindControl:

```csharp
public class OverlayManager
{
    private readonly Border _indexOverlay;
    private readonly TextBox _indexTextBox;
    
    public OverlayManager(Border indexOverlay, TextBox indexTextBox, ...)
    {
        _indexOverlay = indexOverlay;
        _indexTextBox = indexTextBox;
    }
}
```

### Option 2: Create a Window Wrapper Interface
Create an abstraction that can be mocked:

```csharp
public interface IWindowAdapter
{
    T? FindControl<T>(string name) where T : class;
}

public class OverlayManager
{
    private readonly IWindowAdapter _window;
    
    public OverlayManager(IWindowAdapter window)
    {
        _window = window;
    }
}
```

### Option 3: Integration Tests Instead of Unit Tests
Since these services are tightly coupled to Avalonia's UI framework, consider treating them as integration tests that use real Avalonia controls and windows.

### Option 4: Skip These Tests for Now
Mark the failing tests as `[Fact(Skip = "Pending refactor")]` and revisit after refactoring the services.

## Summary

- ✅ **Test reorganization**: Complete - all tests moved to `tests/` folder
- ✅ **Build issues**: Fixed - all test projects build successfully  
- ✅ **Common.Tests**: 31/31 passing (100%)
- ✅ **FileRenamer.Tests**: 2/2 passing (100%)
- ⚠️ **IndexEditor.Tests**: 79/120 passing (66%)
  - Fixed: 33 Avalonia control mocking issues
  - Remaining: 41 extension method mocking issues

**Overall**: 112/153 tests passing (73%)

The remaining failures are architectural issues with the OverlayManager and FullscreenImageService that require refactoring to be properly testable.

---
**Date**: February 19, 2026  
**Status**: ✅ Reorganization Complete, ⚠️ Some Tests Need Refactoring

