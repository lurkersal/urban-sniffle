# Unsaved Changes Prompt Fix - Complete

## Summary
Implemented prompting for unsaved changes when quitting the application or opening a new folder. Users will be asked if they want to save before closing or opening a new folder when there are unsaved changes.

## Implementation Date
February 27, 2026

## Changes Made

### 1. Added HasUnsavedChanges Property
**Files:**
- `src/index-editor/Shared/IEditorState.cs`
- `src/index-editor/Shared/EditorState.cs`

**Changes:**
- Added `HasUnsavedChanges` boolean property to track unsaved changes
- Property is set to `true` when articles are modified, created, or deleted
- Property is set to `false` when saving or loading from disk

### 2. Track Changes on Article Modification
**File:** `src/index-editor/Views/EditorStateViewModel.cs`

**Changes:**
- Modified `OnArticlePropertyChanged` to set `HasUnsavedChanges = true` when any article property changes
- This captures all edits to article fields (title, category, pages, model, etc.)

### 3. Track Changes on Article Creation
**File:** `src/index-editor/Shared/IEditorActions.cs` (EditorActionsService)

**Changes:**
- Set `HasUnsavedChanges = true` in `CreateNewArticle()` method after creating a new article

### 4. Track Changes on Article Deletion
**File:** `src/index-editor/MainWindow.axaml.cs`

**Changes:**
- Set `HasUnsavedChanges = true` after removing an article from the list

### 5. Clear Flag on Save
**Files:**
- `src/index-editor/Shared/IndexSaver.cs`
- `src/index-editor/Views/MainWindowViewModel.cs`

**Changes:**
- Clear `HasUnsavedChanges = false` after successfully saving the index file
- Applied to both regular save and overlay save operations

### 6. Clear Flag on Load
**File:** `src/index-editor/MainWindow.axaml.cs` (LoadArticlesFromFolder)

**Changes:**
- Clear `HasUnsavedChanges = false` after loading articles from a folder
- This ensures opening a folder starts with a clean state

### 7. Prompt on Window Close
**File:** `src/index-editor/MainWindow.axaml.cs` (OnWindowClosing)

**Changes:**
- Check `HasUnsavedChanges` before closing
- Show confirm dialog asking "Do you want to save before quitting?"
- If user chooses "Save", save the index and then close
- If user chooses "Cancel", don't close the window (though currently there's no cancel button)
- Cancel the close event temporarily while prompting

### 8. Prompt on Open Folder
**File:** `src/index-editor/Views/TopBar.axaml.cs` (Open Folder button handler)

**Changes:**
- Check `HasUnsavedChanges` before opening a new folder
- Show confirm dialog asking "Do you want to save before opening a new folder?"
- If user chooses "Save", save the index and proceed
- If user chooses "Cancel" or save fails, don't proceed with opening
- If user declines to save, proceed with opening

### 9. Update ConfirmDialog Button Label
**File:** `src/index-editor/Views/ConfirmDialog.axaml`

**Changes:**
- Changed the OK button label from "Delete" to "Save" since it's now used for save confirmation
- This is a generic confirmation dialog reused for different purposes

## Behavior

### On Quit (Closing Window)
1. If there are no unsaved changes, window closes normally
2. If there are unsaved changes:
   - Dialog appears: "You have unsaved changes. Do you want to save before quitting?"
   - Buttons: "Cancel" | "Save"
   - Clicking "Save": Saves the index file and closes the window
   - Clicking "Cancel": Dialog closes, window stays open

### On Open Folder (Ctrl+O or Open Folder button)
1. If there are no unsaved changes, folder picker opens normally
2. If there are unsaved changes:
   - Dialog appears: "You have unsaved changes. Do you want to save before opening a new folder?"
   - Buttons: "Cancel" | "Save"
   - Clicking "Save": Saves the index file and proceeds to open folder picker
   - Clicking "Cancel": Dialog closes, folder picker doesn't open, stays in current folder

## Files Modified

1. `src/index-editor/Shared/IEditorState.cs` - Added HasUnsavedChanges property to interface
2. `src/index-editor/Shared/EditorState.cs` - Implemented HasUnsavedChanges property
3. `src/index-editor/Shared/IndexSaver.cs` - Clear flag after save
4. `src/index-editor/Shared/IEditorActions.cs` - Set flag on article creation
5. `src/index-editor/Views/EditorStateViewModel.cs` - Set flag on article property changes
6. `src/index-editor/Views/MainWindowViewModel.cs` - Clear flag after overlay save
7. `src/index-editor/MainWindow.axaml.cs` - Prompt on close, set flag on delete, clear on load
8. `src/index-editor/Views/TopBar.axaml.cs` - Prompt before open folder
9. `src/index-editor/Views/ConfirmDialog.axaml` - Updated button label

## Testing Recommendations

1. **Test unsaved changes are tracked:**
   - Open a folder
   - Edit an article title
   - Verify prompt appears when trying to quit or open another folder

2. **Test save on quit:**
   - Make changes to an article
   - Close the window (Alt+F4 or X button)
   - Choose "Save" in the dialog
   - Reopen the folder and verify changes were saved

3. **Test cancel on quit:**
   - Make changes to an article
   - Close the window
   - Choose "Cancel" in the dialog
   - Verify window stays open with changes intact

4. **Test save before open:**
   - Make changes to an article
   - Click "Open Folder" or press Ctrl+O
   - Choose "Save" in the dialog
   - Verify folder picker opens after saving

5. **Test article creation tracking:**
   - Create a new article (Ctrl+N)
   - Try to quit or open a folder
   - Verify prompt appears

6. **Test article deletion tracking:**
   - Delete an article (Delete key)
   - Try to quit or open a folder
   - Verify prompt appears

7. **Test flag is cleared:**
   - Make changes
   - Save (Ctrl+S)
   - Try to quit or open a folder
   - Verify no prompt appears

## Known Limitations

1. The ConfirmDialog currently only has "Cancel" and "Save" buttons
   - Consider adding a third "Don't Save" button for explicit discard option
   
2. Segment edits (adding/ending segments) may not trigger the unsaved flag yet
   - May need to hook into segment modification events

3. The confirmation dialog is modal and blocks the UI
   - This is intentional to prevent data loss

## Future Enhancements

1. Add a "Don't Save" button to the confirmation dialog for explicit discard
2. Add visual indicator in the window title (e.g., asterisk) when there are unsaved changes
3. Track segment modifications as unsaved changes
4. Add auto-save functionality with configurable interval
5. Show which articles have been modified in the prompt message

## Notes

- The HasUnsavedChanges flag is automatically cleared when loading a new folder
- The flag persists across StateChanged notifications  
- Saving via any method (Ctrl+S, Save button, overlay save) clears the flag
- The implementation uses the existing ConfirmDialog component

