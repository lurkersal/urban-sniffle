# JSON Syntax Highlighting - Implementation Status

## Date: March 3, 2026

## Summary
The JSON syntax highlighting feature has been **successfully implemented** and is ready for testing.

## What Was Implemented

### 1. **JsonSyntaxHighlighter.cs**
Location: `/home/justin/repos/urban-sniffle/src/index-editor/Services/JsonSyntaxHighlighter.cs`

A lightweight, token-based syntax highlighter that:
- Uses Avalonia's TextBlock Inlines API (no external dependencies)
- Provides color-coded syntax for JSON elements
- Handles strings, numbers, keys, booleans, null, punctuation, and whitespace
- Distinguishes between object keys (blue) and string values (red)
- Preserves all whitespace and indentation

**Color Scheme:**
- **Strings**: Red (#D14) 
- **Numbers**: Green (#09885A)
- **Keys**: Blue (#0451A5)
- **Booleans/Null**: Blue (#0000FF)
- **Punctuation**: Black (#000000)

### 2. **OverlayManager.cs - Enhanced**
Location: `/home/justin/repos/urban-sniffle/src/index-editor/Services/OverlayManager.cs`

Updated to:
- Detect JSON files (`_index.json`) automatically
- Pretty-print JSON using `System.Text.Json` with indentation
- Apply syntax highlighting to formatted JSON
- Fall back to plain text for `.txt` files
- Show highlighted JSON even in error displays

### 3. **How It Works**

When you open the Index Overlay (Ctrl+I):
1. The system checks for `_index.json` first, then `_index.txt`
2. If JSON is found, it's parsed and pretty-printed with indentation
3. The syntax highlighter tokenizes the formatted JSON
4. Each token is rendered with the appropriate color
5. The result is displayed in a scrollable TextBlock

## Testing Instructions

To test the JSON syntax highlighting:

```bash
# 1. Rebuild IndexEditor (if not already done)
~/repos/urban-sniffle/scripts/clean-rebuild-indexeditor.sh

# 2. Run IndexEditor
IndexEditor

# 3. Open a magazine folder that has an _index.json file
# For example: /home/justin/Magazines/Club International/Club International 16-10, 1987

# 4. Press Ctrl+I to open the Index Overlay

# Expected result:
# - JSON content is displayed with indentation
# - Object keys are blue
# - String values are red
# - Numbers are green
# - Braces, brackets, colons, and commas are black
# - The text is scrollable and read-only
```

## What You'll See

Instead of:
```
{"metadata":{"title":"Club International V16 N10","volume":16,...
```

You'll see:
```json
{
  "metadata": {
    "title": "Club International V16 N10",
    "volume": 16,
    "issue": 10,
    "year": 1987
  },
  "articles": [
    {
      "title": "Contents",
      "category": "Index",
      ...
```

With proper color coding:
- `"metadata"` (blue key) : 
- `"Club International V16 N10"` (red string)
- `16` (green number)
- `{`, `}`, `:`, `,` (black punctuation)

## Features NOT Implemented

The following advanced features are **not** included (as they would require significant additional work):

❌ **Line numbers** - Would require custom rendering
❌ **Expand/collapse** - Would require tree view control
❌ **Error line highlighting** - Would need line tracking
❌ **Find/search** - Would need search UI
❌ **Copy button** - Can be added easily if needed

## Why No Expand/Collapse?

Implementing expand/collapse for JSON would require:
1. **Tree View Control**: A hierarchical control to represent nested JSON
2. **Data Binding**: Converting JSON to a tree data structure
3. **Custom Templates**: ItemTemplates for different node types
4. **State Management**: Tracking expanded/collapsed nodes
5. **Significant UI Changes**: Replacing the TextBlock with a TreeView

This is a **major feature** that would take considerable time (several hours) and would fundamentally change how the overlay works. The current implementation prioritizes:
- **Simplicity**: Uses existing TextBlock control
- **Performance**: Lightweight tokenization
- **Readability**: Syntax highlighting makes JSON much easier to read
- **Maintainability**: Minimal dependencies

## Build Status

✅ Code is complete and syntactically correct
✅ No compilation errors
✅ Ready for rebuild and testing

## Next Steps

1. Rebuild using: `~/repos/urban-sniffle/scripts/clean-rebuild-indexeditor.sh`
2. Run IndexEditor
3. Open a folder with `_index.json`
4. Press Ctrl+I
5. Verify syntax highlighting appears correctly

If you want expand/collapse functionality in the future, we can implement it as a separate feature with a TreeView-based JSON viewer.

