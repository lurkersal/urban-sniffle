# Ctrl+S Save Fix - "Saving via fallback" Issue Resolved ✅
## Problem
When pressing Ctrl+S to save the index file, users were seeing the message:
```
"Saving via fallback"
```
And the file was **not actually being saved**.
## Root Cause
The `FileKeyboardHandler.HandleCtrlS()` method was trying to cast the MainWindow's DataContext to `MainWindowViewModel`:
```csharp
var mainViewModel = _window.DataContext as MainWindowViewModel;
if (mainViewModel != null)
{
    mainViewModel.SaveIndex();
}
else
{
    // This fallback was reached because DataContext is NOT MainWindowViewModel
    ToastService.Show("Saving via fallback");
    // But then it did nothing - file wasn't saved!
}
```
However, in `App.axaml.cs` (line 99), the DataContext is actually set to **`EditorStateViewModel`**, not `MainWindowViewModel`:
```csharp
var editorVm = serviceProvider.GetRequiredService<Views.EditorStateViewModel>();
mainWindow.DataContext = editorVm; // EditorStateViewModel, not MainWindowViewModel!
```
## Solution
Updated `FileKeyboardHandler.HandleCtrlS()` to call `IndexSaver.SaveIndex()` directly instead of relying on a ViewModel that doesn't exist in the DataContext:
```csharp
// Save the index file directly
var saveFolder = EditorState.CurrentFolder;
if (string.IsNullOrWhiteSpace(saveFolder))
{
    ToastService.Show("No folder open; cannot save _index.txt");
    e.Handled = true;
    return;
}
try
{
    IndexSaver.SaveIndex(saveFolder);
    ToastService.Show("_index.txt saved");
}
catch (Exception saveEx)
{
    ToastService.Show("Failed to save _index.txt");
    DebugLogger.LogException("FileKeyboardHandler: Save index", saveEx);
}
```
## Changes Made
**File:** `/src/index-editor/Services/KeyboardHandlers/FileKeyboardHandler.cs`
1. **Removed** incorrect MainWindowViewModel cast
2. **Added** direct call to `IndexSaver.SaveIndex()`
3. **Added** proper error handling with toast notifications
4. **Fixed** variable naming conflict (`folder` → `saveFolder`)
## Testing
### Before Fix:
- Press Ctrl+S → Shows "Saving via fallback"
- File is **NOT saved**
- No error message
### After Fix:
- Press Ctrl+S → Shows "_index.txt saved"  
- File **IS saved** ✅
- Proper error handling if save fails
## Impact
- ✅ Ctrl+S now **actually saves** the index file
- ✅ Users get proper feedback ("_index.txt saved")
- ✅ Error messages shown if save fails
- ✅ No more confusing "Saving via fallback" message
## Build Status
✅ **Build succeeded: 0 Errors, 0 Warnings**
## Related Code
- **IndexSaver.SaveIndex()**: `/src/index-editor/Shared/IndexSaver.cs`
- **EditorState.CurrentFolder**: `/src/index-editor/Shared/EditorState.cs`
- **App.axaml.cs**: Sets DataContext to EditorStateViewModel (line 99)
---
**Fix Date:** February 19, 2026  
**Issue:** Ctrl+S not saving  
**Status:** ✅ RESOLVED  
**Build:** ✅ SUCCESS
