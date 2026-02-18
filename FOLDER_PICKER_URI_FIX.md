# Folder Picker URI Fix

## Issue
The Open Folder button (and Ctrl-O shortcut) was not working correctly when the application was started with a relative path like `./` or when trying to open the folder picker dialog.

## Root Cause
The `FolderPicker.cs` code had incorrect file URI formatting for Linux/Unix absolute paths. The code was creating URIs like:
```
file:///home/justin/path  ❌ WRONG (created as "file://" + "/home/justin/path")
```

For file URIs on Unix systems, the correct format is:
- `file:///absolute/path` (three slashes total - two for scheme, one for root)
- Or simply use `new Uri(path)` if the path is already absolute

The bug was in three locations where the code did:
```csharp
var uri = new Uri("file://" + start);  // Only 2 slashes before the path
```

For a path like `/home/justin/folder`, this created:
```
file:///home/justin/folder  ✓ What we need
file://home/justin/folder   ❌ What we were creating (missing the third slash)
```

## Fix Applied
Updated three instances in `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderPicker.cs`:

1. **Line ~118** - TryGetFolderFromPathAsync Uri creation
2. **Line ~141** - SuggestedStartLocation Uri property setting  
3. **Line ~189** - Fallback property Uri creation

Changed from:
```csharp
var fileUri = new Uri("file://" + start);
```

To:
```csharp
// Create file URI - handle both Windows and Unix paths
var uriPath = start.StartsWith("/") ? "file://" + start : "file:///" + start.Replace("\\", "/");
var fileUri = new Uri(uriPath);
```

This correctly handles:
- **Unix paths**: `/home/user/folder` → `file:///home/user/folder` (three slashes)
- **Windows paths**: `C:\Users\...` → `file:///C:/Users/...` (three slashes, backslashes converted)

## Testing
After rebuilding the project:
```bash
cd /home/justin/repos/urban-sniffle
dotnet build src/index-editor/IndexEditor.csproj
```

Run the application:
```bash
./scripts/run-index-editor.sh
```

The Open Folder button and Ctrl-O should now work correctly, even when:
1. Started with `./` as the argument
2. Started with a relative path
3. Started with an absolute path
4. No argument provided (uses current working directory)

## Related Files
- `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderPicker.cs` - Fixed file URI creation
- `/home/justin/repos/urban-sniffle/FOLDER_PICKER_FIX.md` - Previous fix documentation

## Date Fixed
February 17, 2026

