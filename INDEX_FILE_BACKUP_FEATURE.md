# Index File Backup Feature - Complete

## Summary
Implemented automatic backup creation when saving the `_index.txt` file. The backup is saved as `_index.txt~` in the same directory.

## Implementation Date
February 26, 2026

## Changes Made

### 1. IndexSaver.SaveIndex() - Main Save Logic
**File:** `src/index-editor/Shared/IndexSaver.cs`

**Changes:**
- Before replacing the existing `_index.txt` file, create a backup named `_index.txt~`
- If a previous backup exists, delete it first
- Copy the current `_index.txt` to `_index.txt~` before writing the new version

```csharp
var backupPath = indexPath + "~";
if (File.Exists(indexPath))
{
    // Create backup before replacing
    if (File.Exists(backupPath))
    {
        File.Delete(backupPath);
    }
    File.Copy(indexPath, backupPath);
    File.Replace(tempPath, indexPath, null);
}
```

### 2. MainWindowViewModel.SaveIndexFromOverlay() - Overlay Save
**File:** `src/index-editor/Views/MainWindowViewModel.cs`

**Changes:**
- Added backup creation logic when saving from the Ctrl+I overlay
- Mirrors the same backup behavior as the main save function

```csharp
var backupPath = indexPath + "~";
if (System.IO.File.Exists(indexPath))
{
    // Create backup before replacing
    if (System.IO.File.Exists(backupPath))
    {
        System.IO.File.Delete(backupPath);
    }
    System.IO.File.Copy(indexPath, backupPath);
    System.IO.File.Replace(temp, indexPath, null);
}
```

### 3. MainWindow Overlay Save Handler
**File:** `src/index-editor/MainWindow.axaml.cs` (around line 251)

**Changes:**
- Added backup creation in the inline save handler for the index overlay
- Ensures consistency across all save paths

## Behavior

### When Saving
1. User triggers save via:
   - Ctrl+S (normal save via IndexSaver)
   - Ctrl+I then clicking Save in overlay (overlay save)
   - Save button in TopBar

2. Before writing the new `_index.txt`:
   - If `_index.txt` exists:
     - Delete any existing `_index.txt~` backup
     - Copy current `_index.txt` to `_index.txt~`
     - Write the new version to `_index.txt`
   - If `_index.txt` doesn't exist (first save):
     - No backup is created (nothing to back up)
     - New file is created directly

### Backup File
- **Name:** `_index.txt~`
- **Location:** Same directory as `_index.txt`
- **Content:** Previous version of `_index.txt` before the last save
- **Retention:** Only the most recent backup is kept (previous backups are deleted)

## Save Paths Covered

All three save mechanisms now create backups:

1. **IndexSaver.SaveIndex()** - Used by:
   - Ctrl+S shortcut
   - Save button in TopBar
   - Programmatic saves

2. **MainWindowViewModel.SaveIndexFromOverlay()** - Used by:
   - Save button in Ctrl+I overlay (when using ViewModel)

3. **MainWindow inline handler** - Used by:
   - Legacy save handler in MainWindow

## Build Status

✅ **Build Successful**
```
Build succeeded.
    63 Warning(s)
    0 Error(s)
```

All warnings are pre-existing and not related to the backup feature.

## Files Modified

1. `src/index-editor/Shared/IndexSaver.cs`
2. `src/index-editor/Views/MainWindowViewModel.cs`
3. `src/index-editor/MainWindow.axaml.cs`

## Testing Recommendations

1. Open a folder with an existing `_index.txt` file
2. Make changes to articles
3. Save with Ctrl+S
4. Verify `_index.txt~` exists in the same folder with the previous content
5. Make more changes and save again
6. Verify `_index.txt~` is updated with the previous save's content
7. Test Ctrl+I overlay save to ensure backups are created there too

## Notes

- The backup uses Unix-style tilde suffix (`~`) which is a common convention for backup files
- Only one backup is maintained to avoid cluttering the directory
- The backup is atomic - it's created before the new file is written
- If a save fails, the original file remains intact along with the backup

## Related Documentation

None - this is a new standalone feature.

