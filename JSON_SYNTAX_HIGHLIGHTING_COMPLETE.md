# JSON Syntax Highlighting - Implementation Complete

**Date**: March 4, 2026

## Summary

The IndexEditor's JSON overlay (Ctrl+I) now displays JSON files with basic syntax highlighting using Avalonia's TextBlock with inlines for colored text.

## What Was Implemented

### 1. JSON Syntax Highlighting Service
- **File**: `src/index-editor/Services/JsonSyntaxHighlighter.cs`
- **Features**:
  - Tokenizes JSON content into strings, numbers, booleans, null, braces, brackets, colons, commas
  - Applies color coding:
    - **Strings**: `#D14` (red/brown)
    - **Numbers**: `#098` (teal)
    - **Booleans/null**: `#008` (blue)
    - **Property keys**: `#900` (dark red)
    - **Braces/brackets**: `#333` (dark gray)
  - Returns `InlineCollection` for Avalonia TextBlock rendering

### 2. OverlayManager Integration
- **File**: `src/index-editor/Services/OverlayManager.cs`
- **Changes**:
  - Modified `ShowIndexOverlay()` to detect JSON files
  - When JSON is detected:
    - Pretty prints using `System.Text.Json` with indentation
    - Applies syntax highlighting via `JsonSyntaxHighlighter`
    - Sets the TextBlock's Inlines collection
  - For non-JSON files (legacy _index.txt):
    - Displays as plain text without highlighting

### 3. UI Updates
- **File**: `src/index-editor/MainWindow.axaml`
- **Changes**:
  - Index overlay now uses a `TextBlock` (instead of editable TextBox)
  - Configured for monospace font: `'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New'`
  - Read-only display with horizontal/vertical scrolling
  - ScrollViewer wraps the TextBlock for large JSON files

## How It Works

1. **User presses Ctrl+I**: Triggers `ShowIndexOverlay()` in `OverlayManager`
2. **File detection**: Checks if `_index.json` or `_index.txt` exists
3. **For JSON files**:
   - Load file content
   - Pretty print with `JsonSerializer` (4-space indentation)
   - Tokenize and apply syntax highlighting
   - Display colored text in overlay
4. **For TXT files**:
   - Load and display as plain monospace text
5. **User presses 'i' or Esc**: Closes overlay

## Benefits

✅ **Visual distinction**: Property names, values, and structure are color-coded  
✅ **Readable formatting**: Automatic indentation for nested JSON  
✅ **Monospace font**: Ensures proper alignment  
✅ **No external dependencies**: Uses built-in Avalonia TextBlock with Inline runs  
✅ **Backward compatible**: Still supports plain text _index.txt files  

## Limitations

- **Read-only**: The overlay is now view-only (syntax highlighting in editable text is complex)
- **No folding**: Code collapse/expand is not implemented (would require custom control)
- **Basic highlighting**: Not as sophisticated as full JSON editors (no hover, validation indicators)
- **Performance**: Large JSON files (>10K lines) may be slow to tokenize and render

## Future Enhancements (Optional)

If you want more advanced features, consider:

1. **AvaloniaEdit integration**: Full editor with line numbers, code folding, search
2. **TreeView representation**: Display JSON as expandable tree structure
3. **Validation indicators**: Highlight syntax errors in red
4. **Edit support**: Make overlay editable with live validation
5. **Theme support**: Light/dark color schemes

## Testing

To test the feature:
```bash
IndexEditor "/path/to/magazine/folder"
```

Then:
1. Press **Ctrl+I** to open the index overlay
2. Verify JSON content is displayed with colors:
   - Property names in dark red
   - String values in brown/red
   - Numbers in teal
   - Booleans/null in blue
3. Press **i** or **Esc** to close

## Files Modified

1. `src/index-editor/Services/JsonSyntaxHighlighter.cs` (new)
2. `src/index-editor/Services/OverlayManager.cs` (updated)
3. `src/index-editor/MainWindow.axaml` (TextBox → TextBlock)

## Build Status

✅ **Build successful**: No compilation errors  
✅ **Runtime tested**: Application starts correctly  
✅ **Feature working**: JSON overlay displays with syntax highlighting  

---

**Implementation Complete** ✓

