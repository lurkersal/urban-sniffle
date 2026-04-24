# JSON Serialization Fix - TypeInfoResolver Issue

## Date: March 1, 2026

## Problem
When running `magazine-parser --no-insert ./` with a JSON index file, received error:
```
ERROR parsing JSON file: Reflection-based serialization has been disabled for this application. 
Either use the source generator APIs or explicitly configure the 'JsonSerializerOptions.TypeInfoResolver' property.
```

## Root Cause
.NET 8's `System.Text.Json` requires explicit configuration of `TypeInfoResolver` for reflection-based serialization when AOT compilation or trimming is enabled (or in certain runtime configurations).

The `JsonSerializerOptions` in `IndexJsonSerializer` was missing the `TypeInfoResolver` configuration.

## Solution

### 1. Added TypeInfoResolver to JsonOptions
**File:** `src/common/Shared/IndexJsonSerializer.cs`

**Changes:**
- Added `using System.Text.Json.Serialization.Metadata;`
- Added `TypeInfoResolver = new DefaultJsonTypeInfoResolver()` to JsonOptions
- Made `JsonOptions` public so other classes can use the same configuration
- Added `PropertyNameCaseInsensitive = true` for more flexible parsing

```csharp
public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};
```

### 2. Updated OverlayManager to Use Shared JsonOptions
**File:** `src/index-editor/Services/OverlayManager.cs`

**Changes:**
- Removed local `JsonSerializerOptions` creation in `ValidateJsonContent()`
- Now uses `IndexJsonSerializer.JsonOptions` to ensure consistent configuration

### 3. Fixed Type Inference Issue
**File:** `src/index-editor/MainWindow.axaml.cs`

**Changes:**
- Changed `var (isValid, errorMessage)` to `(bool isValid, string? errorMessage)` for explicit type declaration
- Fixes C# compiler type inference issue

## Benefits
- ✅ Magazine-parser can now deserialize JSON files
- ✅ Consistent JSON configuration across all projects
- ✅ Works with .NET 8 AOT/trimming requirements
- ✅ No breaking changes to existing functionality

## Testing
✅ Build successful (0 errors)
✅ Magazine-parser JSON deserialization works
✅ Index-editor validation works
✅ All projects build cleanly

## Files Modified
1. `src/common/Shared/IndexJsonSerializer.cs`
   - Added TypeInfoResolver
   - Made JsonOptions public
   - Added PropertyNameCaseInsensitive

2. `src/index-editor/Services/OverlayManager.cs`
   - Updated to use shared JsonOptions

3. `src/index-editor/MainWindow.axaml.cs`
   - Fixed type inference in tuple deconstruction

## Related Documentation
- `JSON_INDEX_PHASE2_COMPLETE.md` - Original Phase 2 implementation
- `JSON_INDEX_IMPLEMENTATION_SUMMARY.md` - Complete overview

---

**Status:** ✅ Fixed and verified
**Build Status:** Clean (0 errors, 63 warnings - all expected)

