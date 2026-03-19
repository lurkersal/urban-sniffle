# Command Line Argument Parsing Fix - March 19, 2026

## Issue
When trying to set the log level using `--log-level debug`, the application attempted to open a folder named `--log-level` instead of setting the debug level.

## Root Cause
The application processes command line arguments in two places:

1. **Program.cs** (line 17-35): Parses `--log-level`, `-l`, and `--demo` flags
2. **App.axaml.cs** (line 96-110): Takes the first remaining argument as a folder path to open

The problem was that `App.axaml.cs` only filtered out the `--no-images` flag before treating the first remaining argument as a folder path. It didn't know about the other flags handled in `Program.cs`, so it treated `--log-level` as a folder path.

### Example of the Bug
```bash
# User runs:
dotnet run -- --log-level debug

# Program.cs correctly parses and sets log level to Debug
# But then App.axaml.cs sees args = ["--log-level", "debug"]
# After filtering (nothing to filter), it takes args[0] = "--log-level"
# And tries to open a folder named "--log-level"
```

## Solution
Modified `App.axaml.cs` to filter out all flags that are handled in `Program.cs` before extracting the folder path:

1. Added `using System.Collections.Generic;` directive
2. Implemented proper argument filtering that skips:
   - `--log-level` and `-l` (and their values)
   - `--demo`
   - `--no-images` (already handled)
3. Only the first non-flag argument is treated as a folder path

### Code Changes
```csharp
// Filter out arguments that are handled in Program.cs (not folder paths)
var filteredArgs = new List<string>();
for (int i = 0; i < args.Count; i++)
{
    var arg = args[i];
    
    // Skip --log-level/-l and its value
    if (arg == "--log-level" || arg == "-l")
    {
        i++; // Skip next argument (the log level value)
        continue;
    }
    
    // Skip --demo flag
    if (arg == "--demo")
    {
        continue;
    }
    
    filteredArgs.Add(arg);
}

// Take the first remaining argument as the folder path
if (filteredArgs.Count > 0)
{
    folderToOpen = filteredArgs[0];
}
```

## Files Modified
- `/home/justin/repos/urban-sniffle/src/index-editor/App.axaml.cs`
  - Added `using System.Collections.Generic;`
  - Replaced simple `args[0]` extraction with proper argument filtering

## Build Status
✅ **Build successful**
- 0 Errors
- 22 Warnings (pre-existing, none related to this change)

## Testing Instructions

### Test 1: Log Level Only
```bash
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug
```
**Expected**: App starts with debug logging enabled, no folder opened (last recent folder may open)

### Test 2: Log Level + Folder
```bash
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug /path/to/magazine
```
**Expected**: App starts with debug logging and opens `/path/to/magazine`

### Test 3: Folder Only
```bash
dotnet run --project src/index-editor/IndexEditor.csproj -- /path/to/magazine
```
**Expected**: App starts with default (Info) logging and opens `/path/to/magazine`

### Test 4: Multiple Flags
```bash
dotnet run --project src/index-editor/IndexEditor.csproj -- --log-level debug --no-images /path/to/magazine
```
**Expected**: Debug logging, images disabled, folder opened

### Test 5: Short Form
```bash
dotnet run --project src/index-editor/IndexEditor.csproj -- -l debug /path/to/magazine
```
**Expected**: Same as Test 2 (short form of --log-level works)

## Backward Compatibility
✅ All existing command line argument usage continues to work:
- Passing a folder path as the first argument still works
- `--no-images` flag still works
- `--demo` flag still works
- No breaking changes

## Related
- See `DEBUG_LEVEL_GUIDE.md` for information on using the `--log-level` flag
- The fix ensures that all examples in the guide now work correctly

