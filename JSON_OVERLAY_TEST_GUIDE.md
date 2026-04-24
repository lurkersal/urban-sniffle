# JSON Overlay Dual-Mode Test Guide

## Quick Test Steps

### 1. Start Application
```bash
IndexEditor
```

### 2. Test View Mode (Default)

**Action**: Press `Ctrl+I`

**Expected**:
- ✅ Overlay appears with dark background
- ✅ JSON is displayed as a collapsible tree
- ✅ Property names are colored (light blue)
- ✅ String values are colored (orange)
- ✅ Numbers are colored (green)
- ✅ Booleans/null are colored (blue)
- ✅ "Switch to Edit Mode" button visible
- ✅ "Expand All" button visible
- ✅ "Collapse All" button visible
- ✅ "Save" button is hidden
- ✅ "Close" button visible

### 3. Test Expand/Collapse

**Action**: Click "Expand All"
**Expected**: All nodes expand, showing full tree

**Action**: Click "Collapse All"
**Expected**: All nodes collapse

**Action**: Click ▶ button next to "articles"
**Expected**: Articles array expands

**Action**: Click ▶ button next to first article
**Expected**: Article object expands, showing properties

**Action**: Click ▼ button next to first article
**Expected**: Article object collapses, showing "Category - Title" format
  - Example: `▶ Model - Ginger` or `▶ Editorial - (untitled)` if no title

### 4. Test Edit Mode

**Action**: Click "Switch to Edit Mode"

**Expected**:
- ✅ Tree view disappears
- ✅ Editable TextBox appears with JSON content
- ✅ JSON is formatted (pretty-printed)
- ✅ Font is monospace
- ✅ Button text changes to "Switch to View Mode"
- ✅ "Expand All" button is hidden
- ✅ "Collapse All" button is hidden
- ✅ "Save" button is visible

### 5. Test Editing

**Action**: Make a small edit (e.g., change an article title)

**Expected**: TextBox accepts the edit

**Action**: Click "Save"

**Expected**:
- ✅ Toast notification: "_index.json saved"
- ✅ Application reloads the folder
- ✅ Article list reflects the change

### 6. Test Mode Toggle

**Action**: Click "Switch to View Mode"

**Expected**:
- ✅ TextBox disappears
- ✅ Tree view reappears with updated content
- ✅ Button text changes back to "Switch to Edit Mode"
- ✅ "Expand All" and "Collapse All" buttons reappear
- ✅ "Save" button is hidden

### 7. Test Close

**Action**: Press `Ctrl+I` (or click "Close")

**Expected**: Overlay closes

## Edge Cases to Test

### Empty Title
**Setup**: Find or create an article with no title  
**Expected**: Shows "Category - (untitled)" when collapsed

### Plain Text File
**Setup**: Load a folder with `_index.txt` instead of `_index.json`  
**Expected**: Falls back to plain text display (existing behavior)

### Invalid JSON in Edit Mode
**Setup**: Edit JSON to be invalid, then save  
**Expected**: File saves (no validation yet), might show error on reload

### Rapid Mode Switching
**Action**: Click toggle button multiple times quickly  
**Expected**: Modes switch smoothly without errors

## Console Output to Monitor

Look for these log lines:

```
OverlayManager: Switching to Edit Mode
OverlayManager: Switching to View Mode
OverlayManager: Saved index file to /path/to/_index.json
```

## Known Limitations

1. **No validation**: Edit mode doesn't validate JSON before save
2. **No undo/redo**: Changes are immediate after save
3. **No search**: Can't search within the JSON
4. **No diff view**: Can't see what changed between original and edited

These can be added as future enhancements if needed.

## Success Criteria

✅ All buttons work as expected  
✅ Mode switching is smooth  
✅ Colors appear in View Mode  
✅ Collapse/expand works  
✅ Article summaries show "Category - Title"  
✅ Editing and saving works  
✅ Auto-reload after save works  
✅ No crashes or errors in console  

---

**Test Date**: March 4, 2026  
**Version**: Built with JsonTreeViewer.cs  
**Binary Size**: 76M

