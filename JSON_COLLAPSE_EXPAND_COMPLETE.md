# JSON Collapse/Expand Feature - Implementation Complete

**Date**: March 4, 2026

## Summary

The IndexEditor's JSON overlay (Ctrl+I) now supports **collapsible/expandable tree view** for navigating large JSON files. Users can click on ▶/▼ indicators to fold/unfold objects and arrays.

## What Was Implemented

### 1. JsonTreeNode Data Structure
- **File**: `src/index-editor/Services/JsonTreeNode.cs`
- **Features**:
  - Parses JSON into a tree structure with parent-child relationships
  - Tracks expansion state for each node
  - Supports Objects, Arrays, Strings, Numbers, Booleans, and Null values
  - Auto-expands first 2 levels by default for quick overview
  - Handles nested structures recursively

### 2. JsonTreeViewer Control
- **File**: `src/index-editor/Services/JsonTreeViewer.cs`
- **Features**:
  - Custom Avalonia control that renders JSON as a collapsible tree
  - **Collapse indicators**: ▶ (collapsed) / ▼ (expanded)
  - **Syntax highlighting**:
    - Property keys: Blue (`#0451A5`)
    - String values: Red/brown (`#D14`)
    - Numbers: Green (`#09885A`)
    - Booleans/null: Blue (`#0000FF`)
    - Punctuation: Black
  - **Interactive**: Click on ▶/▼ to toggle expansion
  - **Preview mode**: Shows item count when collapsed (e.g., `{ ... 5 properties }`)
  - **Methods**:
    - `LoadJson(string)`: Parse and display JSON
    - `ExpandAll()`: Expand all nodes
    - `CollapseAll()`: Collapse all nodes

### 3. UI Updates
- **File**: `src/index-editor/MainWindow.axaml`
- **Changes**:
  - Added **Expand All** button
  - Added **Collapse All** button
  - Updated help text: "Press 'i' to close | Click ▶/▼ to expand/collapse sections"
  - ScrollViewer now hosts either JsonTreeViewer (for JSON) or TextBlock (for TXT)

### 4. OverlayManager Integration
- **File**: `src/index-editor/Services/OverlayManager.cs`
- **Changes**:
  - Detects JSON vs TXT format
  - For JSON files: Uses JsonTreeViewer with collapsible tree
  - For TXT files: Falls back to plain TextBlock display
  - Added `ExpandAllJsonNodes()` method
  - Added `CollapseAllJsonNodes()` method
  - Maintains reference to JsonTreeViewer for button actions

### 5. MainWindow Event Handlers
- **File**: `src/index-editor/MainWindow.axaml.cs`
- **Changes**:
  - Wired up "Expand All" button → `OverlayManager.ExpandAllJsonNodes()`
  - Wired up "Collapse All" button → `OverlayManager.CollapseAllJsonNodes()`

## How It Works

### Opening the Overlay (Ctrl+I)
1. User presses **Ctrl+I**
2. OverlayManager checks for `_index.json` or `_index.txt`
3. **If JSON**:
   - Parse JSON into `JsonTreeNode` tree structure
   - Render tree with `JsonTreeViewer`
   - Display with collapse/expand indicators
4. **If TXT**:
   - Display as plain monospace text

### Expanding/Collapsing
- **Click ▶**: Expands a collapsed node, showing its children
- **Click ▼**: Collapses an expanded node, hiding its children
- **Expand All button**: Recursively expands all objects and arrays
- **Collapse All button**: Recursively collapses all objects and arrays

### Visual Feedback
- Collapsed nodes show preview: `{ ... 5 properties }` or `[ ... 10 items ]`
- Expanded nodes show full content with proper indentation
- Color-coded syntax highlighting for easy reading

## Benefits

✅ **Navigate large JSON files easily**: Collapse irrelevant sections  
✅ **Visual hierarchy**: Indentation and collapse indicators show structure  
✅ **Quick overview**: Auto-expand first 2 levels, collapse the rest  
✅ **Syntax highlighting**: Property names, values, and punctuation are colored  
✅ **Interactive**: Click to expand/collapse, no keyboard needed  
✅ **Bulk actions**: Expand/Collapse All buttons for entire tree  
✅ **Backward compatible**: Still supports plain text `_index.txt` files  

## User Interface

### Top Bar Buttons
- **Expand All**: Opens all collapsed nodes
- **Collapse All**: Closes all expanded nodes
- **Close**: Closes the overlay

### Tree Navigation
- **▶**: Click to expand a collapsed node
- **▼**: Click to collapse an expanded node
- **Indentation**: 2 spaces per nesting level
- **Preview**: Shows item count when collapsed

### Keyboard
- **i** or **Esc**: Close overlay
- **Ctrl+I**: Open overlay

## Example Display

```
▼ "metadata": {
    "magazine": "Club International"
    "volume": 16
    "issue": 10
    "year": 1987
  }
▶ "articles": [ ... 18 items ]
```

After clicking "Expand All":
```
▼ "metadata": {
    "magazine": "Club International"
    "volume": 16
    "issue": 10
    "year": 1987
  }
▼ "articles": [
  ▼ [0]: {
      "title": "Contents"
      "category": "Index"
    ▼ "pages": [
        1
        2
      ]
    }
  ▼ [1]: {
      "title": "Tracy"
      "category": "Cover"
      "contributor": "David Paul"
    }
  ]
```

## Performance Considerations

- **Rendering**: Rebuilds entire tree on expand/collapse (acceptable for JSON files < 1000 nodes)
- **Memory**: Tree structure stored in memory for navigation state
- **Large files**: May be slow for JSON with > 10K nodes (virtualization not implemented)

## Future Enhancements (Optional)

If you want more advanced features:

1. **Search/filter**: Find specific keys or values in the tree
2. **Copy value**: Right-click to copy a value to clipboard
3. **Virtualization**: For large JSON files (> 1000 nodes)
4. **Keyboard navigation**: Arrow keys to navigate tree
5. **JSON validation**: Show errors with red indicators
6. **Edit support**: Make values editable inline

## Testing

To test the feature:
```bash
IndexEditor "/path/to/magazine/folder"
```

Then:
1. Press **Ctrl+I** to open the index overlay
2. Verify JSON is displayed as a tree with ▶/▼ indicators
3. Click **▶** to expand a collapsed section
4. Click **▼** to collapse an expanded section
5. Click **Expand All** to open entire tree
6. Click **Collapse All** to close entire tree
7. Press **i** or **Esc** to close

## Files Added

1. `src/index-editor/Services/JsonTreeNode.cs` - Tree data structure
2. `src/index-editor/Services/JsonTreeViewer.cs` - Collapsible tree control

## Files Modified

1. `src/index-editor/Services/OverlayManager.cs` - Tree viewer integration
2. `src/index-editor/MainWindow.axaml` - UI buttons and layout
3. `src/index-editor/MainWindow.axaml.cs` - Button event handlers

## Build Status

✅ **Build successful**: No compilation errors  
✅ **Feature complete**: Collapse/expand fully implemented  
✅ **Ready to test**: Run `IndexEditor` to try it out  

---

**Implementation Complete** ✓

## Quick Start

1. Run the application: `IndexEditor`
2. Open a folder with a `_index.json` file
3. Press **Ctrl+I**
4. Click ▶/▼ to navigate the JSON structure
5. Use "Expand All" / "Collapse All" for bulk operations

The JSON overlay is now a powerful navigation tool for large index files!

