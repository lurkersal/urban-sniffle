# JSON Article Preview Enhancement

**Date**: March 4, 2026

## Summary

The IndexEditor's JSON overlay (Ctrl+I) now displays **"Category - Title"** for collapsed article objects instead of the generic "{ ... N properties }" text.

## What Changed

### Before
When articles were collapsed in the JSON tree, they displayed:
```
▶ [0]: { ... 9 properties }
▶ [1]: { ... 9 properties }
▶ [2]: { ... 9 properties }
```

### After
Now collapsed articles display their category and title:
```
▶ [0]: { Cover - Tracy }
▶ [1]: { Index - Contents }
▶ [2]: { Letters - Readers' Writes }
```

## Implementation Details

### Modified File
- **File**: `src/index-editor/Services/JsonTreeViewer.cs`
- **Method**: `RenderNode()` - Object case in switch statement

### Logic
1. When rendering a collapsed object node, check if it has a "category" child property
2. If category exists and is non-empty:
   - If "title" exists and is non-empty → Display: `{ Category - Title }`
   - If "title" is empty, null, or missing → Display: `{ Category - (no title) }`
3. Otherwise, fall back to the generic: `{ ... N properties }`

### Code Change
```csharp
if (node.IsExpanded)
{
    objPreview = "{";
}
else
{
    // Check if this is an article object (has category and optionally title)
    var categoryNode = node.Children.FirstOrDefault(c => c.Key == "category");
    var titleNode = node.Children.FirstOrDefault(c => c.Key == "title");
    
    // Show preview if category exists (title is optional)
    if (categoryNode != null && !string.IsNullOrWhiteSpace(categoryNode.Value))
    {
        var title = titleNode != null && !string.IsNullOrWhiteSpace(titleNode.Value)
            ? titleNode.Value
            : "(no title)";
        
        // Display "Category - Title" or "Category - (no title)" for collapsed articles
        objPreview = $"{{ {categoryNode.Value} - {title} }}";
    }
    else
    {
        objPreview = $"{{ ... {objCount} {(objCount == 1 ? "property" : "properties")} }}";
    }
}
```

## Benefits

✅ **Easier navigation**: Quickly identify articles without expanding them  
✅ **Better context**: See what each article is about at a glance  
✅ **Handles missing data**: Shows "(no title)" when title is empty, null, or missing  
✅ **Maintains functionality**: Non-article objects still show property count  
✅ **Consistent format**: Matches the "Category - Title" pattern used elsewhere  

## User Experience

### Viewing Articles List
When you press **Ctrl+I** and navigate to the `articles` array, you'll see:

**Articles with titles:**
```
▼ "articles": [
  ▶ [0]: { Cover - Tracy }
  ▶ [1]: { Index - Contents }
  ▶ [2]: { Letters - Readers' Writes }
  ▶ [3]: { Editorial - Welcome }
  ...
]
```

**Articles without titles (empty, null, or missing):**
```
▼ "articles": [
  ▶ [0]: { Cover - Tracy }
  ▶ [1]: { Index - (no title) }
  ▶ [2]: { Model - (no title) }
  ...
]
```
  ▶ [3]: { Editorial - Jonelle }
  ▶ [4]: { Model - Ginger }
  ...
]
```

### Expanding an Article
Click the ▶ to expand and see all properties:

```
▼ [0]: {
    "pages": [ 1 ]
    "category": "Cover"
    "title": "Tracy"
    "modelNames": [ "Tracy Walker" ]
    "ages": [ null ]
    "contributors": [ "David Paul" ]
    "measurements": [ "" ]
  }
```

### Non-Article Objects
Objects that don't have category and title still show the property count:

```
▶ "metadata": { ... 4 properties }
```

## Testing

To test the feature:

1. Run: `IndexEditor`
2. Open a folder with a `_index.json` file
3. Press **Ctrl+I** to open the JSON overlay
4. Navigate to the "articles" array
5. Verify collapsed articles show "Category - Title" format
6. Expand an article and verify all properties are visible
7. Collapse it again and verify the preview returns

## Build Status

✅ **Build successful**: No compilation errors  
✅ **Feature complete**: Article previews now show Category - Title  
✅ **Ready to use**: Run `IndexEditor` to test  

## Troubleshooting

### Issue: Feature works in Rider but not from console

**Problem**: When running `IndexEditor` from the console after building with `clean-rebuild-indexeditor.sh`, the new preview format doesn't appear.

**Root Cause**: The build script was using `-p:PublishTrimmed=true`, which was trimming away the LINQ `FirstOrDefault` calls and the article preview logic.

**Solution**: Removed `-p:PublishTrimmed=true` from the build script. The binary size increased from ~13M (trimmed) to ~22M (full), but now includes all the necessary code.

**Fixed In**: `scripts/clean-rebuild-indexeditor.sh` (March 4, 2026)

### How to Verify the Fix

1. Run: `bash scripts/clean-rebuild-indexeditor.sh`
2. Check binary size: `ls -lh ~/bin/IndexEditor` (should be ~22M, not ~13M)
3. Run: `IndexEditor`
4. Open a folder with `_index.json`
5. Press **Ctrl+I** and expand the "articles" array
6. Verify collapsed articles show "Category - Title"

---

**Enhancement Complete** ✓

This makes navigating large magazine index files much more efficient!





