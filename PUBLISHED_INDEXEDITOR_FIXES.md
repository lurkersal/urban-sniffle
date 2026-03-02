# Published IndexEditor Fixes

**Date**: March 2, 2026  
**Issue**: IndexEditor published with `publish-tools.sh` was failing to open folder dialog and had JSON serialization errors

## Problems Identified

### 1. WindowStateStore JSON Serialization Errors
**Error Message**:
```
fail: IndexEditor[0] WindowStateStore.GetWindowState System.InvalidOperationException: 
Reflection-based serialization has been disabled for this application. 
Either use the source generator APIs or explicitly configure the 
'JsonSerializerOptions.TypeInfoResolver' property.
```

**Cause**: When publishing with `-p:PublishTrimmed=true`, the .NET trimmer removes reflection-based JSON serialization support to reduce binary size. The `WindowStateStore` was using `JsonSerializer.Deserialize<T>()` without a source-generated context.

### 2. FolderPicker Not Working
**Error Message** (from console log):
```
13:38:51 info: IndexEditor[0] FolderPicker: Starting with absolute path: /home/justin/...
13:38:51 info: IndexEditor[0] TopBar.OpenClick: FolderPicker returned: (null)
13:38:51 info: IndexEditor[0] TopBar.OpenClick: Path is null or empty, returning
```

**Cause**: The `FolderPicker` was using reflection extensively to dynamically invoke Avalonia's StorageProvider APIs. With IL trimming enabled, reflection-based code is unreliable and can be trimmed away.

## Solutions Implemented

### 1. WindowStateStore - Source-Generated JSON Context

**File**: `/src/index-editor/Shared/WindowStateStore.cs`

**Changes**:
- Added a source-generated JSON serialization context `WindowStateJsonContext`
- Used `[JsonSerializable(typeof(WindowState))]` attribute
- Changed `JsonSerializer.Deserialize<WindowState>(txt, opts)` to use the context: `JsonSerializer.Deserialize(txt, WindowStateJsonContext.Default.WindowState)`
- Same for serialization

**Code**:
```csharp
[JsonSerializable(typeof(WindowState))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal partial class WindowStateJsonContext : JsonSerializerContext
{
}
```

**Benefits**:
- Compatible with native AOT and IL trimming
- Better performance (no reflection at runtime)
- Smaller binary size

### 2. FolderPicker - Direct StorageProvider API Usage

**File**: `/src/index-editor/Shared/FolderPicker.cs`

**Changes**:
- Removed all reflection-based code
- Used Avalonia's `StorageProvider` API directly
- Used proper `IStorageFolder` and `FolderPickerOpenOptions` types
- Added `using Avalonia.Platform.Storage;` for storage types

**Before** (reflection-based):
```csharp
var storageProp = topType.GetProperty("StorageProvider", BindingFlags...);
var storage = storageProp.GetValue(top);
var method = storageType.GetMethod("OpenFolderPickerAsync");
invokeResult = method.Invoke(storage, args);
```

**After** (direct API):
```csharp
var storageProvider = topLevel.StorageProvider;
var options = new FolderPickerOpenOptions
{
    Title = "Select Folder",
    AllowMultiple = false,
    SuggestedStartLocation = suggestedStartLocation
};
var result = await storageProvider.OpenFolderPickerAsync(options);
```

**Benefits**:
- Works with IL trimming and native AOT
- More maintainable and readable code
- Type-safe (compile-time checking)
- Better IDE support and intellisense

## Testing Results

### Before Fix
```bash
$ IndexEditor ~/Magazines/...
13:38:51 info: IndexEditor[0] FolderPicker: Starting with absolute path: ...
13:38:51 info: IndexEditor[0] TopBar.OpenClick: FolderPicker returned: (null)
```
❌ Open button does nothing
❌ WindowStateStore errors in log

### After Fix
```bash
$ IndexEditor ~/Magazines/...
13:48:16 info: IndexEditor[0] TopBar.OpenClick: FolderPicker returned: /home/justin/Magazines/...
13:48:16 info: IndexEditor[0] TopBar.OpenClick: Loading folder: /home/justin/Magazines/...
```
✅ Open button works correctly
✅ No WindowStateStore errors
✅ Application window state persists between sessions

## Build Configuration

The publish script uses these settings which require the fixes:
```bash
dotnet publish \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true
```

**Key Setting**: `-p:PublishTrimmed=true`
- Enables IL trimming to reduce binary size
- Removes unused code via static analysis
- Breaks reflection-based patterns
- Requires source-generated JSON and direct API calls

## Related Files Modified

1. `/src/index-editor/Shared/WindowStateStore.cs`
   - Added source-generated JSON context
   - Fixed nullable warnings

2. `/src/index-editor/Shared/FolderPicker.cs`
   - Complete rewrite to use direct Avalonia APIs
   - Removed 200+ lines of reflection code
   - Reduced to ~160 lines of clean, direct API calls

## Impact

### Published Binary
- ✅ Works correctly with trimming enabled
- ✅ Folder picker dialog opens and works
- ✅ Window state saves and restores
- ✅ No reflection errors
- ✅ Smaller binary size (trimming effective)

### Development (Debug)
- ✅ No impact on debug builds
- ✅ Still works in IDE (Rider)
- ✅ All existing functionality preserved

## Best Practices Applied

1. **Use source-generated JSON serialization** for all types when trimming is enabled
2. **Avoid reflection** in trimmed/AOT scenarios
3. **Use direct API calls** instead of dynamic invocation
4. **Test published builds**, not just debug builds
5. **Monitor trim warnings** and address them proactively

## Future Considerations

The build currently shows trim warnings about XAML reflection bindings:
```
Trim analysis warning IL2026: ... ReflectionBindingExtension ... 
Consider using CompiledBindings instead.
```

These warnings don't break functionality but could be addressed by:
- Enabling compiled bindings in XAML: `<Binding Path="..." CompileBindings="True"/>`
- Or using `x:CompileBindings="True"` at the root level
- This would improve performance and eliminate trim warnings

## Conclusion

✅ **FIXED**: IndexEditor now works correctly when published with IL trimming enabled  
✅ **VERIFIED**: Open button opens folder selection dialog  
✅ **VERIFIED**: Window state persists correctly  
✅ **VERIFIED**: No runtime errors in published binary  

The fixes ensure that the IndexEditor can be published as a trimmed, self-contained binary while maintaining all functionality.

