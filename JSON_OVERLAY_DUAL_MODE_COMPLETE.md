# JSON Overlay - Dual Mode Complete

## Summary
The JSON index overlay (Ctrl+I) now supports **two modes**:
1. **View Mode** - Collapsible, color-coded tree view (read-only)
2. **Edit Mode** - Editable TextBox for direct JSON editing

Users can toggle between modes with a single button click.

## Features

### View Mode (Default)
✅ **Collapsible tree structure** with expand/collapse controls  
✅ **Color coding** for different JSON elements:
  - Property names in one color
  - String values in another
  - Numbers, booleans, null in distinct colors  
✅ **Category - Title format** for collapsed article nodes  
✅ **Expand All** button to expand entire tree  
✅ **Collapse All** button to collapse entire tree  
✅ **Read-only** - prevents accidental edits  

### Edit Mode
✅ **Direct editing** of JSON in a TextBox  
✅ **Monospace font** for better readability  
✅ **Multi-line support** with scrolling  
✅ **Save button** to write changes to disk  
✅ **Atomic save** using temp file + replace  
✅ **Auto-reload** after save to refresh UI  

## User Workflow

### Viewing JSON
1. Press `Ctrl+I` to open overlay (opens in View Mode by default)
2. Use **Expand All** / **Collapse All** buttons to navigate
3. Click ▶/▼ icons on individual nodes to expand/collapse
4. View article summary as "Category - Title" when collapsed

### Editing JSON
1. Press `Ctrl+I` to open overlay
2. Click **Toggle Edit Mode** button
3. Edit JSON directly in the text box
4. Click **Save** to write changes
5. Application auto-reloads to reflect changes
6. Click **Toggle View Mode** to return to tree view

## Button Layout

**View Mode:**
```
[Toggle Edit Mode] [Expand All] [Collapse All] [Close]
```

**Edit Mode:**
```
[Toggle View Mode] [Save] [Close]
```

## Implementation Details

### Files Modified

1. **MainWindow.axaml**
   - Added `IndexOverlayToggleModeBtn` button
   - Visibility bindings for mode-specific buttons:
     - Expand/Collapse: visible only in View Mode
     - Save: visible only in Edit Mode
   - Updated help text to describe both modes

2. **MainWindow.axaml.cs**
   - Wired up Toggle Mode button to `OverlayManager.ToggleEditMode()`
   - Auto-reload after save

3. **OverlayManager.cs**
   - Added `_isEditMode` field to track current mode
   - Modified `ShowJsonTree()` to create appropriate UI based on mode:
     - View Mode: Creates `JsonTreeViewer` with collapsible tree
     - Edit Mode: Creates `TextBox` for editing
   - Added `ToggleEditMode()` method to switch between modes
   - Updated `SaveIndexFromOverlay()` to work only in Edit Mode
   - Preserved `ExpandAllJsonNodes()` and `CollapseAllJsonNodes()` for View Mode

4. **Views/JsonTreeViewer.cs**
   - Custom control for rendering collapsible JSON tree
   - Handles expand/collapse state
   - Color-codes different JSON element types
   - Shows "Category - Title" for collapsed article nodes

## Technical Details

### State Management
- `_isEditMode` boolean tracks current mode
- Mode is reset to View when overlay is closed
- Mode persists while overlay remains open

### Tree Formatting
- Articles show as: `▶ Category - Title` when collapsed
- Metadata, links, and other sections use default JSON property names
- Empty titles are handled gracefully

### Save Operation
- Only available in Edit Mode
- Validates folder exists
- Auto-detects JSON vs text format
- Uses atomic write (temp file + replace) for safety
- Shows toast notification on success/failure
- Triggers auto-reload to update UI

### Color Scheme
- Uses Avalonia's default brush system
- Property names: one color
- String values: another color  
- Numbers/booleans/null: distinct colors
- Works with both light and dark themes

## Benefits

1. **Best of both worlds**: View for navigation, Edit for modifications
2. **No accidental edits**: View mode is read-only
3. **Easy navigation**: Collapse/expand makes large files manageable
4. **Direct editing power**: Full control in Edit mode
5. **Safe saves**: Atomic write prevents corruption
6. **Auto-reload**: Changes immediately reflected in UI

## Future Enhancements (Optional)

- [ ] Syntax validation in Edit Mode before save
- [ ] Search/filter in View Mode
- [ ] Highlight changes between original and edited
- [ ] Undo/redo in Edit Mode
- [ ] Export tree as collapsed/expanded JSON

## Migration from Single-Mode

Users upgrading from the previous single-mode (edit-only) overlay will now:
- Default to **View Mode** on Ctrl+I
- Need to click **Toggle Edit Mode** to edit
- Benefit from collapsible tree navigation

The change is **non-breaking** as all previous functionality is preserved in Edit Mode.

---

**Status**: ✅ **COMPLETE AND READY TO TEST**  
**Date**: March 4, 2026  
**Build**: ✅ Successful (76M binary)  
**Files Added**: `Views/JsonTreeViewer.cs`  
**Files Modified**: `MainWindow.axaml`, `MainWindow.axaml.cs`, `OverlayManager.cs`

## How to Test

1. Run `IndexEditor`
2. Open a folder with a `_index.json` file
3. Press `Ctrl+I` to open the overlay (defaults to **View Mode**)
4. You should see:
   - A collapsible tree structure with colored JSON
   - Articles displayed as "Category - Title" when collapsed
   - Expand/Collapse All buttons
5. Click **Toggle Edit Mode** button
6. You should see:
   - A TextBox with editable JSON
   - Save button enabled
   - Expand/Collapse buttons hidden
7. Make an edit and click **Save**
8. The folder should auto-reload
9. Click **Toggle View Mode** to return to tree view
10. Press `Ctrl+I` to close the overlay


