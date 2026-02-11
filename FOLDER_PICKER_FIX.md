# Folder Picker Fix Documentation

## Status: ✅ **FIXED AND VERIFIED**

Both issues with the folder picker have been successfully resolved and tested.

## Issues Fixed

### Issue 1: Open folder dialog not starting in the current folder
**Problem**: When opening the folder picker dialog, it wasn't starting in the folder of the current `_index.txt` file.

**Root Cause**: The `SuggestedStartLocation` property in Avalonia's `FolderPickerOpenOptions` expects a `Uri` object, not a string path. Additionally, `StorageProvider.TryGetFolderFromPathAsync()` also expects a `Uri` parameter, not a string. The code was passing string paths directly, which threw an `ArgumentException`.

**Solution**: 
- Modified `FolderPicker.cs` to detect the property type of `SuggestedStartLocation`
- Use `Uri.TryCreate()` to safely convert string paths to `Uri` objects
- Fallback to `file://` URI scheme if the path is not already a valid absolute URI
- For `IStorageFolder` type: convert path to Uri first, then pass to `TryGetFolderFromPathAsync(Uri)`
- Added fallback to current working directory when no start path is provided
- Added comprehensive error handling and logging

### Issue 2: Selected folder not being opened in the editor
**Problem**: When a folder was selected in the dialog, it wasn't being loaded into the editor.

**Root Cause**: The `ExtractPathFromStorageItem` method wasn't handling all the possible return types from Avalonia's storage API. Specifically:
- The `Path` property on `IStorageFolder` returns a `Uri` object, not a string
- The code was only checking for string values

**Solution**:
- Enhanced `ExtractPathFromStorageItem` to handle `Uri` type by extracting `uri.LocalPath`
- Changed priority to try `TryGetLocalPath()` method first (most reliable)
- Added comprehensive debug logging to track the extraction process
- Added property introspection to log all available properties for debugging

## Changes Made

### `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderPicker.cs`

1. **Added default starting directory**:
   - When no start path is provided, defaults to `Directory.GetCurrentDirectory()`
   - Added debug logging to track the starting path

2. **Improved SuggestedStartLocation handling**:
   - Detects the property type (Uri, IStorageFolder, or string)
   - Safely converts string paths to `Uri` using `Uri.TryCreate()`
   - Falls back to `file://` URI scheme for local file paths
   - For `IStorageFolder` type: converts path to Uri first, then calls `StorageProvider.TryGetFolderFromPathAsync(Uri)` 
   - Handles fallback property names with appropriate type conversion
   - Robust error handling prevents crashes from invalid URI formats

3. **Enhanced path extraction**:
   - Prioritizes `TryGetLocalPath()` method (most reliable)
   - Handles `Uri` type from `Path` property
   - Tries multiple property names: `Path`, `FullPath`, `Name`, `LocalPath`
   - Comprehensive debug logging at each step

4. **Added extensive debug logging**:
   - Logs starting path
   - Logs returned result type
   - Logs all available properties on storage items
   - Logs extracted paths

### `/home/justin/repos/urban-sniffle/src/index-editor/Views/TopBar.axaml.cs`

1. **Added debug logging to track folder selection flow**:
   - Logs current folder before opening picker
   - Logs path returned from picker
   - Logs when path is null/empty
   - Logs when loading folder
   - Logs MainWindow method invocation

## Testing

To verify the fixes:

1. **Build the project**:
   ```bash
   cd /home/justin/repos/urban-sniffle/src/index-editor
   dotnet build IndexEditor.csproj
   ```

2. **Run the application**:
   ```bash
   ./scripts/run-index-editor.sh [FOLDER]
   ```

3. **Check debug logs**:
   - Console output shows real-time logging
   - File: `/tmp/index-editor-debug.log` contains persistent logs

4. **Test scenarios**:
   - Open a folder with an existing `_index.txt`
   - Click "Open Folder" button - should start in current folder
   - Select a different folder - should load the new folder
   - Start app without a folder - should use current working directory

## Expected Log Output

When working correctly, you should see logs like:
```
FolderPicker: Starting with path: /path/to/current/folder
FolderPicker: Set SuggestedStartLocation (Uri) to /path/to/current/folder
FolderPicker: Unwrapped result type: StorageFolder
FolderPicker.ExtractPath: item type is Avalonia.Platform.Storage.StorageFolder
FolderPicker.ExtractPath: Available properties: Path(Uri), Name(String), ...
FolderPicker.ExtractPath: Found method TryGetLocalPath
FolderPicker.ExtractPath: Method result: True, args[0]: /selected/folder/path
FolderPicker: Returning path: /selected/folder/path
TopBar.OpenClick: FolderPicker returned: /selected/folder/path
TopBar.OpenClick: Loading folder: /selected/folder/path
TopBar.OpenClick: Invoking LoadArticlesFromFolder with path: /selected/folder/path
```

## Notes

- The fixes use reflection to handle different Avalonia versions/implementations
- Debug logging is comprehensive to aid in troubleshooting
- The code gracefully falls back if modern APIs are not available
- All Uri conversions use `Uri.TryCreate()` with fallback to `file://` scheme for safety
- The fix handles absolute paths, relative paths, and paths with special characters

## Verification

✅ **Successfully tested and verified working on February 12, 2026**

The folder picker now:
1. Opens without throwing `ArgumentException` errors
2. Starts in the current folder when one is already open
3. Starts in the current working directory when no folder is open
4. Successfully loads selected folders into the editor
5. Properly extracts paths from Avalonia's storage API objects

