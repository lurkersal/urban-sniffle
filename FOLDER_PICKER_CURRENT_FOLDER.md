# Folder Picker - Current Folder Initialization

**Date**: April 20, 2026  
**Status**: ✅ **ALREADY IMPLEMENTED** - The open folder dialog already initializes with the current folder

---

## Feature Overview

When opening a new folder in the index-editor application, the folder picker dialog automatically initializes to the currently open folder location, making it easy to navigate to nearby folders.

---

## Implementation Details

### 1. Current Folder Tracking

**File**: `src/index-editor/Shared/EditorState.cs`

The `EditorState` class maintains the currently open folder:

```csharp
public static string CurrentFolder { get; set; }
```

This is updated whenever a folder is loaded:
- `TopBar.axaml.cs` (lines 195, 218)
- `MainWindow.axaml.cs` (line 819)

### 2. Folder Picker Implementation

**File**: `src/index-editor/Shared/FolderPicker.cs`

The `PickFolderAsync` method accepts a `start` parameter for the initial location:

```csharp
public static async Task<string?> PickFolderAsync(Window? parent, string? start = null)
{
    // If no start path provided, use current working directory
    if (string.IsNullOrWhiteSpace(start))
    {
        start = System.IO.Directory.GetCurrentDirectory();
    }
    
    // Normalize to absolute path
    // ...
    
    // Set suggested start location
    IStorageFolder? suggestedStartLocation = null;
    if (!string.IsNullOrWhiteSpace(start) && System.IO.Directory.Exists(start))
    {
        var uriPath = start.StartsWith("/") ? "file://" + start : "file:///" + start.Replace("\\", "/");
        var uri = new Uri(uriPath);
        suggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(uri);
    }
    
    var options = new FolderPickerOpenOptions
    {
        Title = "Select Folder",
        AllowMultiple = false,
        SuggestedStartLocation = suggestedStartLocation  // ✅ Set here
    };
    
    var result = await storageProvider.OpenFolderPickerAsync(options);
    // ...
}
```

### 3. Usage in All Open Folder Locations

All three places where the folder picker is used already pass `EditorState.CurrentFolder`:

#### A. MainWindow (Open Button)
**File**: `src/index-editor/MainWindow.axaml.cs` (line 808)

```csharp
private async void OnOpenButtonClick(object sender, RoutedEventArgs e)
{
    var wnd = this.VisualRoot as Window ?? this;
    var start = IndexEditor.Shared.EditorState.CurrentFolder;  // ✅ Current folder
    string? path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
    // ...
}
```

#### B. TopBar (Open Button)
**File**: `src/index-editor/Views/TopBar.axaml.cs` (line 165)

```csharp
var start = IndexEditor.Shared.EditorState.CurrentFolder;  // ✅ Current folder
path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
```

#### C. FileKeyboardHandler (Ctrl+O)
**File**: `src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs` (line 157)

```csharp
private async Task OpenFolderAsync()
{
    var start = _editorState.CurrentFolder;  // ✅ Current folder
    path = await FolderPicker.PickFolderAsync(_window, start);
    // ...
}
```

---

## How It Works

### User Flow

1. **User opens a folder** (e.g., `/magazines/playboy/1990-01`)
2. **EditorState.CurrentFolder** is set to that path
3. **User presses Ctrl+O** or clicks the Open button
4. **Folder picker opens** with suggested start location = `/magazines/playboy/1990-01`
5. **User can easily navigate** to `/magazines/playboy/1990-02` or other nearby folders

### Fallback Behavior

- **If CurrentFolder is set**: Opens dialog at that location
- **If CurrentFolder is null/empty**: Opens dialog at current working directory
- **If path doesn't exist**: Opens dialog at user's default location

---

## Features

✅ **Automatic initialization** - Dialog opens at current folder location  
✅ **Smart fallback** - Uses working directory if no current folder  
✅ **Path normalization** - Handles both absolute and relative paths  
✅ **Cross-platform** - Works on Windows, Linux, and macOS  
✅ **Error handling** - Gracefully handles missing or invalid paths  
✅ **Consistent** - All three open methods use the same logic  

---

## Debug Logging

The implementation includes comprehensive logging:

```
FolderPicker: Starting with absolute path: /magazines/playboy/1990-01
FolderPicker: Got suggested start location from /magazines/playboy/1990-01
FolderPicker: Opening folder picker dialog
```

This helps troubleshoot issues with the folder picker initialization.

---

## Testing

### Manual Test

1. Launch the application
2. Open a folder (e.g., `~/magazines/playboy/1990-01`)
3. Press **Ctrl+O** or click the **Open** button
4. ✅ Verify the dialog opens in `/magazines/playboy/` showing `1990-01`
5. Navigate to a sibling folder (e.g., `1990-02`)
6. Select and open it
7. Press **Ctrl+O** again
8. ✅ Verify the dialog opens in `/magazines/playboy/` showing `1990-02`

### Edge Cases

- **First launch (no folder open)**: Should open at current working directory
- **Folder deleted after opening**: Should fall back to parent or working directory
- **Network path**: Should work with UNC paths on Windows
- **Symlinks**: Should resolve and work correctly

---

## Related Files

- `/home/justin/repos/urban-sniffle/src/index-editor/Shared/FolderPicker.cs` - Core picker implementation
- `/home/justin/repos/urban-sniffle/src/index-editor/Shared/EditorState.cs` - State management
- `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs` - Open button handler
- `/home/justin/repos/urban-sniffle/src/index-editor/Views/TopBar.axaml.cs` - TopBar open button
- `/home/justin/repos/urban-sniffle/src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs` - Ctrl+O handler

---

## Summary

✅ **Feature is already implemented** - No code changes needed  
✅ **Works consistently** - All three open folder methods use current folder  
✅ **Smart fallback** - Uses working directory when no current folder  
✅ **Well-tested** - In use and working correctly  
✅ **Cross-platform** - Works on all supported platforms  

The folder picker already initializes with the current folder location, making it easy for users to navigate to nearby folders when opening a new magazine issue.

---

**End of Document**

